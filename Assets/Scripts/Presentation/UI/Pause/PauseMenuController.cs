using System.Collections;
using Game.Core.Management;
using Game.Presentation.UI.Loading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        private const int PauseCanvasSortingOrder = 31000;
        private const float DefaultFixedDeltaTime = 0.02f;

        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private string _titleSceneName = "Title";

        [Header("Input")]
        [SerializeField] private InputAction _pauseAction = new("Pause", InputActionType.Button, "<Gamepad>/start");
        [SerializeField] private InputAction _cancelAction = new("Cancel", InputActionType.Button, "<Gamepad>/buttonEast");

        private readonly Color _buttonColor = new(0.36f, 0.36f, 0.36f, 1f);
        private readonly Color _selectedButtonColor = new(0.58f, 0.58f, 0.58f, 1f);

        private GameObject _viewRoot;
        private GameObject _pausePanel;
        private GameObject _optionPanel;
        private Button _continueButton;
        private Button _optionButton;
        private Button _exitButton;
        private Button _backButton;
        private PlayerInput _pausedPlayerInput;
        private bool _playerInputWasActive;
        private bool _inputReactivationPending;
        private bool _isPaused;
        private bool _isShowingOption;
        private bool _isLoadingTitle;
        private float _previousTimeScale;
        private float _previousFixedDeltaTime;

        private void Awake()
        {
            CreateView();
        }

        private void OnEnable()
        {
            _pauseAction?.Enable();
            _cancelAction?.Enable();
        }

        private void OnDisable()
        {
            _pauseAction?.Disable();
            _cancelAction?.Disable();

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
            _pauseAction?.Dispose();
            _cancelAction?.Dispose();
        }

        private void Update()
        {
            if (_isLoadingTitle)
            {
                return;
            }

            if (!_isPaused)
            {
                if (_pauseAction != null && _pauseAction.WasPressedThisFrame() && CanOpenPause())
                {
                    OpenPause();
                }

                return;
            }

            if (_isShowingOption && _cancelAction != null && _cancelAction.WasPressedThisFrame())
            {
                ShowPauseMenu();
            }
        }

        private static bool CanOpenPause()
        {
            GameProgressionManager progression = GameProgressionManager.Instance;
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
            _isPaused = true;
            _previousTimeScale = Time.timeScale;
            _previousFixedDeltaTime = Time.fixedDeltaTime;

            _pausedPlayerInput = Object.FindFirstObjectByType<PlayerInput>();
            _playerInputWasActive = _pausedPlayerInput != null && _pausedPlayerInput.inputIsActive;
            if (_playerInputWasActive)
            {
                _pausedPlayerInput.DeactivateInput();
            }

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

            _isPaused = false;
            _viewRoot.SetActive(false);
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            ClearSelection();

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
            SelectButton(_backButton);
        }

        public void ShowPauseMenu()
        {
            if (!_isPaused || _isLoadingTitle)
            {
                return;
            }

            _isShowingOption = false;
            _optionPanel.SetActive(false);
            _pausePanel.SetActive(true);
            SelectButton(_continueButton);
        }

        public void ReturnToTitle()
        {
            if (!_isPaused || _isLoadingTitle)
            {
                return;
            }

            StartCoroutine(ReturnToTitleRoutine());
        }

        private IEnumerator ReactivatePlayerInputAfterSubmitReleased()
        {
            yield return null;

            while (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed)
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
                ShowPauseMenu();
                yield break;
            }

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
            _isPaused = false;
            Time.timeScale = _previousTimeScale;
            Time.fixedDeltaTime = _previousFixedDeltaTime;
            _viewRoot?.SetActive(false);
            ClearSelection();

            if (_playerInputWasActive && _pausedPlayerInput != null)
            {
                _pausedPlayerInput.ActivateInput();
            }

            _inputReactivationPending = false;
        }

        private void CreateView()
        {
            GameObject canvasObject = new("PauseCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = PauseCanvasSortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform viewRect = CreateRect("PauseView", canvasObject.transform);
            Stretch(viewRect);
            _viewRoot = viewRect.gameObject;

            Image scrim = CreateImage("GrayOut", viewRect, new Color(0f, 0f, 0f, 0.62f));
            Stretch(scrim.rectTransform);

            _pausePanel = CreateFullScreenPanel("PauseMenu", viewRect);
            _continueButton = CreateButton("ContinueButton", _pausePanel.transform, "つづける", new Vector2(0f, 190f), new Vector2(370f, 148f));
            _optionButton = CreateButton("OptionButton", _pausePanel.transform, "オプション", Vector2.zero, new Vector2(370f, 148f));
            _exitButton = CreateButton("ExitButton", _pausePanel.transform, "終わる", new Vector2(0f, -190f), new Vector2(370f, 148f));

            _continueButton.onClick.AddListener(ContinueGame);
            _optionButton.onClick.AddListener(ShowOption);
            _exitButton.onClick.AddListener(ReturnToTitle);
            ConfigurePauseNavigation();

            _optionPanel = CreateFullScreenPanel("OptionMenu", viewRect);
            CreateLabel(
                "OptionHeader",
                _optionPanel.transform,
                "オプション",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(32f, -20f),
                new Vector2(370f, 146f),
                58f);

            _backButton = CreateButton(
                "BackButton",
                _optionPanel.transform,
                "戻る",
                new Vector2(32f, 20f),
                new Vector2(196f, 149f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                52f);
            _backButton.onClick.AddListener(ShowPauseMenu);
            ConfigureBackNavigation();

            _optionPanel.SetActive(false);
            _viewRoot.SetActive(false);
        }

        private void ConfigurePauseNavigation()
        {
            _continueButton.navigation = CreateNavigation(_exitButton, _optionButton);
            _optionButton.navigation = CreateNavigation(_continueButton, _exitButton);
            _exitButton.navigation = CreateNavigation(_optionButton, _continueButton);
        }

        private void ConfigureBackNavigation()
        {
            _backButton.navigation = CreateNavigation(_backButton, _backButton);
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

        private Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 position,
            Vector2 size,
            Vector2? anchor = null,
            Vector2? pivot = null,
            float fontSize = 58f)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0.5f, 0.5f);
            rect.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = _buttonColor;
            colors.highlightedColor = _selectedButtonColor;
            colors.selectedColor = _selectedButtonColor;
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.disabledColor = new Color(0.24f, 0.24f, 0.24f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            CreateText("Text", rect, label, fontSize);
            return button;
        }

        private void CreateLabel(
            string name,
            Transform parent,
            string label,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size,
            float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = _buttonColor;
            image.raycastTarget = false;
            CreateText("Text", rect, label, fontSize);
        }

        private void CreateText(string name, RectTransform parent, string value, float fontSize)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);

            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (_fontAsset != null)
            {
                text.font = _fontAsset;
            }

            text.text = value;
            text.fontSize = fontSize;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
        }

        private static GameObject CreateFullScreenPanel(string name, Transform parent)
        {
            RectTransform rect = CreateRect(name, parent);
            Stretch(rect);
            return rect.gameObject;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.layer = 5;
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SelectButton(Button button)
        {
            if (EventSystem.current == null || button == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }
    }
}
