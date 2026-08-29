using System.Collections;
using Game.Core.Management;
using Game.Presentation.UI.Loading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private enum OptionTab
        {
            Audio,
            View,
            KeyConfig
        }

        private enum PauseInputMode
        {
            Navigation,
            Mouse
        }

        private const float DefaultFixedDeltaTime = 0.02f;

        [SerializeField] private string _titleSceneName = "Title";

        [Header("Input Assets")]
        [SerializeField] private InputActionAsset _playerActions;
        [SerializeField] private InputActionAsset _roguelikeActions;

        [Header("Input")]
        [SerializeField] private InputAction _pauseAction = new("Pause", InputActionType.Button, "<Gamepad>/start");
        [SerializeField] private InputAction _cancelAction = new("Cancel", InputActionType.Button, "<Gamepad>/buttonEast");
        [SerializeField] private InputAction _submitAction = new("Submit", InputActionType.Button, "<Gamepad>/buttonSouth");
        [SerializeField] private InputAction _previousTabAction = new("PreviousTab", InputActionType.Button, "<Gamepad>/leftShoulder");
        [SerializeField] private InputAction _nextTabAction = new("NextTab", InputActionType.Button, "<Gamepad>/rightShoulder");

        [Header("View")]
        [SerializeField] private GameObject _viewRoot;
        [SerializeField] private GameObject _pausePanel;
        [SerializeField] private GameObject _optionPanel;
        [SerializeField] private GameObject[] _optionContentPanels = new GameObject[3];
        [SerializeField] private Button[] _tabButtons = new Button[3];
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _optionButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _backButton;

        [Header("Setting Panels")]
        [SerializeField] private PauseAudioSettingsPanel _audioPanel;
        [SerializeField] private PauseViewSettingsPanel _viewPanel;
        [SerializeField] private PauseKeyConfigPanel _keyConfigPanel;

        private PlayerInput _pausedPlayerInput;
        private InputSystemUIInputModule _uiInputModule;
        private InputAction _originalUiSubmitAction;
        private InputAction _originalUiCancelAction;

        private bool _playerInputWasActive;
        private bool _inputReactivationPending;
        private bool _isPaused;
        private bool _isShowingOption;
        private bool _isShowingTitleOptions;
        private bool _isLoadingTitle;
        private bool _uiInputActionsReplaced;
        private bool _originalUiSubmitWasEnabled;
        private bool _originalUiCancelWasEnabled;
        private OptionTab _currentOptionTab;
        private float _previousTimeScale;
        private float _previousFixedDeltaTime;
        private CursorLockMode _previousCursorLockState;
        private bool _previousCursorVisible;
        private bool _cursorStateCaptured;
        private bool _resetNavigationSelectionInLateUpdate;
        private PauseInputMode _inputMode;
        private Selectable _titleReturnSelection;

        public bool IsShowingTitleOptions => _isShowingTitleOptions;

        private void Awake()
        {
            EnsureDefaultBindings();
            DisableOptionScaling();
            _audioPanel.Initialize();
            _viewPanel.Initialize();
            _keyConfigPanel.Initialize(_playerActions, _roguelikeActions, _pauseAction, _submitAction, _cancelAction);
            ConfigureCallbacks();

            _optionPanel.SetActive(false);
            _keyConfigPanel.SetRebindOverlayVisible(false);
            _viewRoot.SetActive(false);
        }

        private void DisableOptionScaling()
        {
            PauseSelectionOutline[] outlines = _optionPanel.GetComponentsInChildren<PauseSelectionOutline>(true);
            for (int i = 0; i < outlines.Length; i++)
            {
                outlines[i].SetScalingEnabled(false);
            }
        }

        private void OnEnable()
        {
            _pauseAction?.Enable();
            _cancelAction?.Enable();
            _submitAction?.Enable();
            _previousTabAction?.Enable();
            _nextTabAction?.Enable();
        }

        private void OnDisable()
        {
            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            RestoreUiInputActions();
            ResetInputMode();

            _pauseAction?.Disable();
            _cancelAction?.Disable();
            _submitAction?.Disable();
            _previousTabAction?.Disable();
            _nextTabAction?.Disable();

            if (_isPaused && !_isLoadingTitle)
            {
                RestoreInterruptedPause();
            }
            else if (_inputReactivationPending && _pausedPlayerInput != null)
            {
                _pausedPlayerInput.ActivateInput();
                _inputReactivationPending = false;
            }
        }

        private void OnDestroy()
        {
            _audioPanel?.CancelEditing();
            _keyConfigPanel?.CancelRebind();
            RestoreUiInputActions();

            _pauseAction?.Dispose();
            _cancelAction?.Dispose();
            _submitAction?.Dispose();
            _previousTabAction?.Dispose();
            _nextTabAction?.Dispose();
        }

        private void Update()
        {
            _audioPanel.ApplyToNewSoundManager();
            _keyConfigPanel.ApplyToNewPlayerInput();

            if (_isPaused)
            {
                UnlockPauseCursor();
            }

            if (_isLoadingTitle)
            {
                return;
            }

            if (!_isPaused && !_isShowingTitleOptions)
            {
                if (_pauseAction != null && _pauseAction.WasPressedThisFrame() && CanOpenPause())
                {
                    OpenPause();
                }

                return;
            }

            UpdateInputMode();

            if (_keyConfigPanel.IsRebinding)
            {
                return;
            }

            bool submitPressed = _submitAction != null && _submitAction.WasPressedThisFrame();
            bool cancelPressed = _cancelAction != null && _cancelAction.WasPressedThisFrame();
            if (_audioPanel.IsEditing)
            {
                _audioPanel.HandleEditingInput(submitPressed, cancelPressed, GetUiMoveAction());
                return;
            }

            if (submitPressed)
            {
                SubmitSelectedObject();
            }

            if (!_isShowingOption)
            {
                return;
            }

            if (_previousTabAction != null && _previousTabAction.WasPressedThisFrame())
            {
                SwitchOptionTab((int)_currentOptionTab - 1);
            }
            else if (_nextTabAction != null && _nextTabAction.WasPressedThisFrame())
            {
                SwitchOptionTab((int)_currentOptionTab + 1);
            }

            if (cancelPressed)
            {
                if (_isShowingTitleOptions)
                {
                    CloseTitleOptions();
                }
                else
                {
                    ShowPauseMenu();
                }
            }
        }

        private void LateUpdate()
        {
            if (!_resetNavigationSelectionInLateUpdate)
            {
                return;
            }

            _resetNavigationSelectionInLateUpdate = false;
            if ((_isPaused || _isShowingTitleOptions) &&
                !_isLoadingTitle &&
                !_keyConfigPanel.IsRebinding &&
                !_audioPanel.IsEditing &&
                (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null))
            {
                Select(GetInputModeInitialSelectable());
            }
        }

        private void ConfigureCallbacks()
        {
            _continueButton.onClick.AddListener(ContinueGame);
            _optionButton.onClick.AddListener(ShowOption);
            _exitButton.onClick.AddListener(ReturnToTitle);
            _backButton.onClick.AddListener(ShowPauseMenu);

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int tabIndex = i;
                _tabButtons[i].onClick.AddListener(() => SwitchOptionTab(tabIndex));
            }

            _continueButton.navigation = CreateNavigation(_exitButton, _optionButton);
            _optionButton.navigation = CreateNavigation(_continueButton, _exitButton);
            _exitButton.navigation = CreateNavigation(_optionButton, _continueButton);
        }

        private static bool CanOpenPause()
        {
            GameProgressionManagerBase progression = GameProgressionManagerBase.Instance;
            if (progression == null || Time.timeScale <= 0f)
            {
                return false;
            }

            GameProgressionState state = progression.CurrentState;
            return state == GameProgressionState.Setup ||
                   state == GameProgressionState.Battle ||
                   state == GameProgressionState.Roguelike;
        }

        private void OpenPause()
        {
            CaptureCursorState();
            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            _previousFixedDeltaTime = Time.fixedDeltaTime;

            _pausedPlayerInput = FindFirstObjectByType<PlayerInput>();
            _keyConfigPanel.ApplyPlayerBindingsToRuntime(_pausedPlayerInput);
            _playerInputWasActive = _pausedPlayerInput != null && _pausedPlayerInput.inputIsActive;
            if (_playerInputWasActive)
            {
                _pausedPlayerInput.DeactivateInput();
            }

            ReplaceUiInputActions();
            ResetInputMode();
            Time.timeScale = 0f;
            _viewRoot.SetActive(true);
            ShowPauseMenu();
        }

        public void ContinueGame()
        {
            if (!_isPaused || _isLoadingTitle)
            {
                return;
            }

            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            RestoreUiInputActions();
            _isPaused = false;
            _viewRoot.SetActive(false);
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            ClearSelection();
            ResetInputMode();
            RestoreCursorState();

            if (_playerInputWasActive && _pausedPlayerInput != null)
            {
                _inputReactivationPending = true;
                StartCoroutine(ReactivatePlayerInputAfterSubmitReleased());
            }
        }

        public void ShowOption()
        {
            if (!_isPaused || _isLoadingTitle)
            {
                return;
            }

            _isShowingOption = true;
            _pausePanel.SetActive(false);
            _optionPanel.SetActive(true);
            SwitchOptionTab((int)_currentOptionTab);
        }

        public void ShowPauseMenu()
        {
            if (_isShowingTitleOptions)
            {
                CloseTitleOptions();
                return;
            }

            if (!_isPaused || _isLoadingTitle)
            {
                return;
            }

            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            _isShowingOption = false;
            _optionPanel.SetActive(false);
            _pausePanel.SetActive(true);
            Select(_continueButton);
        }

        public void OpenTitleOptions(Selectable returnSelection)
        {
            if (_isPaused || _isShowingTitleOptions || _isLoadingTitle)
            {
                return;
            }

            _titleReturnSelection = returnSelection;
            _isShowingTitleOptions = true;
            _isShowingOption = true;
            ReplaceUiInputActions();
            ResetInputMode();
            _pausePanel.SetActive(false);
            _optionPanel.SetActive(true);
            _viewRoot.SetActive(true);
            SwitchOptionTab((int)_currentOptionTab);
        }

        private void CloseTitleOptions()
        {
            if (!_isShowingTitleOptions)
            {
                return;
            }

            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            RestoreUiInputActions();
            _isShowingTitleOptions = false;
            _isShowingOption = false;
            _optionPanel.SetActive(false);
            _pausePanel.SetActive(true);
            _viewRoot.SetActive(false);
            ClearSelection();
            ResetInputMode();
            Select(_titleReturnSelection);
            _titleReturnSelection = null;
        }

        public void ReturnToTitle()
        {
            if (_isPaused && !_isLoadingTitle)
            {
                StartCoroutine(ReturnToTitleRoutine());
            }
        }

        private IEnumerator ReactivatePlayerInputAfterSubmitReleased()
        {
            yield return null;
            while (_submitAction != null && _submitAction.IsPressed())
            {
                yield return null;
            }

            if (_pausedPlayerInput != null)
            {
                _pausedPlayerInput.ActivateInput();
            }

            _inputReactivationPending = false;
        }

        private IEnumerator ReturnToTitleRoutine()
        {
            _isLoadingTitle = true;
            _inputReactivationPending = false;
            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            RestoreUiInputActions();
            ClearSelection();

            GameObject loadingObject = new("TitleLoadingView");
            loadingObject.transform.SetParent(transform, false);
            LoadingView loadingView = loadingObject.AddComponent<LoadingView>();
            loadingView.Initialize();
            _viewRoot.SetActive(false);

            yield return null;
            AsyncOperation operation = SceneManager.LoadSceneAsync(_titleSceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                Destroy(loadingObject);
                _isLoadingTitle = false;
                Time.timeScale = 0f;
                Time.fixedDeltaTime = _previousFixedDeltaTime;
                _viewRoot.SetActive(true);
                ReplaceUiInputActions();
                ShowPauseMenu();
                yield break;
            }

            SoundManager.instance?.StopBGM();
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
            operation.allowSceneActivation = true;
            while (!operation.isDone)
            {
                yield return null;
            }
        }

        private void RestoreInterruptedPause()
        {
            _audioPanel.CancelEditing();
            _keyConfigPanel.CancelRebind();
            RestoreUiInputActions();
            _isPaused = false;
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            _viewRoot?.SetActive(false);
            ClearSelection();
            ResetInputMode();
            RestoreCursorState();

            if (_playerInputWasActive && _pausedPlayerInput != null)
            {
                _pausedPlayerInput.ActivateInput();
            }

            _inputReactivationPending = false;
        }

        private void SwitchOptionTab(int index)
        {
            _audioPanel.CancelEditing();
            int wrappedIndex = WrapIndex(index, _optionContentPanels.Length);
            _currentOptionTab = (OptionTab)wrappedIndex;

            for (int i = 0; i < _optionContentPanels.Length; i++)
            {
                bool active = i == wrappedIndex;
                _optionContentPanels[i].SetActive(active);
            }

            ConfigureOptionNavigation();
            Select(_tabButtons[wrappedIndex]);
        }

        private void ConfigureOptionNavigation()
        {
            int activeIndex = (int)_currentOptionTab;
            Selectable first = GetCurrentFirstSelectable();
            Selectable last = GetCurrentLastSelectable();

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                _tabButtons[i].navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = _backButton,
                    selectOnDown = i == activeIndex ? first : null,
                    selectOnLeft = _tabButtons[WrapIndex(i - 1, _tabButtons.Length)],
                    selectOnRight = _tabButtons[WrapIndex(i + 1, _tabButtons.Length)]
                };
            }

            Selectable activeTab = _tabButtons[activeIndex];
            switch (_currentOptionTab)
            {
                case OptionTab.Audio:
                    _audioPanel.SetNavigationBoundaries(activeTab, _backButton);
                    break;
                case OptionTab.View:
                    _viewPanel.SetNavigationBoundaries(activeTab, _backButton);
                    break;
                case OptionTab.KeyConfig:
                    _keyConfigPanel.SetNavigationBoundaries(activeTab, _backButton);
                    break;
            }

            _backButton.navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = last,
                selectOnDown = activeTab
            };
        }

        private Selectable GetCurrentFirstSelectable()
        {
            return _currentOptionTab switch
            {
                OptionTab.Audio => _audioPanel.FirstSelectable,
                OptionTab.View => _viewPanel.FirstSelectable,
                OptionTab.KeyConfig => _keyConfigPanel.FirstSelectable,
                _ => null
            };
        }

        private Selectable GetCurrentLastSelectable()
        {
            return _currentOptionTab switch
            {
                OptionTab.Audio => _audioPanel.LastSelectable,
                OptionTab.View => _viewPanel.LastSelectable,
                OptionTab.KeyConfig => _keyConfigPanel.LastSelectable,
                _ => null
            };
        }

        private void ReplaceUiInputActions()
        {
            if (_uiInputActionsReplaced)
            {
                return;
            }

            _uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            if (_uiInputModule == null)
            {
                return;
            }

            _originalUiSubmitAction = _uiInputModule.submit?.action;
            _originalUiCancelAction = _uiInputModule.cancel?.action;
            _originalUiSubmitWasEnabled = _originalUiSubmitAction != null && _originalUiSubmitAction.enabled;
            _originalUiCancelWasEnabled = _originalUiCancelAction != null && _originalUiCancelAction.enabled;
            _originalUiSubmitAction?.Disable();
            _originalUiCancelAction?.Disable();
            _uiInputActionsReplaced = true;
        }

        private void RestoreUiInputActions()
        {
            if (!_uiInputActionsReplaced)
            {
                return;
            }

            if (_originalUiSubmitWasEnabled)
            {
                _originalUiSubmitAction?.Enable();
            }

            if (_originalUiCancelWasEnabled)
            {
                _originalUiCancelAction?.Enable();
            }

            _uiInputActionsReplaced = false;
            _originalUiSubmitAction = null;
            _originalUiCancelAction = null;
        }

        private InputAction GetUiMoveAction()
        {
            return _uiInputModule != null ? _uiInputModule.move?.action : null;
        }

        private void UpdateInputMode()
        {
            if (_keyConfigPanel.IsRebinding || _audioPanel.IsEditing)
            {
                return;
            }

            bool mouseMoved = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f;
            bool navigationOperated = WasNavigationDeviceOperated();

            if (navigationOperated)
            {
                SetInputMode(PauseInputMode.Navigation);
            }
            else if (mouseMoved)
            {
                SetInputMode(PauseInputMode.Mouse);
            }
        }

        private bool WasNavigationDeviceOperated()
        {
            InputAction moveAction = GetUiMoveAction();
            if ((moveAction != null && moveAction.WasPerformedThisFrame()) ||
                (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame))
            {
                return true;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad == null)
            {
                return false;
            }

            if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
                gamepad.rightStick.ReadValue().sqrMagnitude > 0.25f)
            {
                return true;
            }

            for (int i = 0; i < gamepad.allControls.Count; i++)
            {
                if (gamepad.allControls[i] is ButtonControl button && button.wasPressedThisFrame)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetInputMode(PauseInputMode inputMode)
        {
            if (_inputMode == inputMode)
            {
                if (inputMode == PauseInputMode.Navigation &&
                    (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null))
                {
                    Select(GetInputModeInitialSelectable());
                    _resetNavigationSelectionInLateUpdate = true;
                }

                return;
            }

            _inputMode = inputMode;
            bool mouseMode = inputMode == PauseInputMode.Mouse;
            PauseSelectionOutline.SetPointerMode(mouseMode);

            if (mouseMode)
            {
                _resetNavigationSelectionInLateUpdate = false;
                ClearSelection();
                return;
            }

            Select(GetInputModeInitialSelectable());
            _resetNavigationSelectionInLateUpdate = true;
        }

        private void ResetInputMode()
        {
            _inputMode = PauseInputMode.Navigation;
            _resetNavigationSelectionInLateUpdate = false;
            PauseSelectionOutline.SetPointerMode(false);
        }

        private Selectable GetInputModeInitialSelectable()
        {
            if (!_isShowingOption)
            {
                return _continueButton;
            }

            int tabIndex = Mathf.Clamp((int)_currentOptionTab, 0, _tabButtons.Length - 1);
            return _tabButtons.Length > 0 ? _tabButtons[tabIndex] : _backButton;
        }

        private void CaptureCursorState()
        {
            _previousCursorLockState = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;
            _cursorStateCaptured = true;
            UnlockPauseCursor();
        }

        private static void UnlockPauseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void RestoreCursorState()
        {
            if (!_cursorStateCaptured)
            {
                return;
            }

            _cursorStateCaptured = false;
            Cursor.lockState = _previousCursorLockState;
            Cursor.visible = _previousCursorVisible;
        }

        private void EnsureDefaultBindings()
        {
            EnsureDeviceBinding(_pauseAction, "<Keyboard>/escape");
            EnsureDeviceBinding(_cancelAction, "<Keyboard>/escape");
            EnsureDeviceBinding(_submitAction, "<Keyboard>/enter");
        }

        private static void EnsureDeviceBinding(InputAction action, string path)
        {
            if (action == null)
            {
                return;
            }

            int separator = path.IndexOf('>');
            string devicePath = separator >= 0 ? path.Substring(0, separator + 1) : path;
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].effectivePath.StartsWith(devicePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            action.AddBinding(path);
        }

        private static Navigation CreateNavigation(Selectable up, Selectable down)
        {
            return new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down
            };
        }

        private static int WrapIndex(int index, int count)
        {
            return count <= 0 ? 0 : (index % count + count) % count;
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current == null || selectable == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }

        private static void ClearSelection()
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private static void SubmitSelectedObject()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                return;
            }

            BaseEventData eventData = new(EventSystem.current);
            ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject, eventData, ExecuteEvents.submitHandler);
        }
    }
}
