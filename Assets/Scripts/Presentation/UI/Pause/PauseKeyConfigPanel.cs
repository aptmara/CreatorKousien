using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseKeyConfigPanel : MonoBehaviour
    {
        public enum KeyBindingId
        {
            Move = 0,
            Punch = 2,
            AttachmentScale = 3,
            Submit = 4,
            Cancel = 5,
            Pause = 6,
            RoguelikeExit = 7
        }

        private enum RebindDevice
        {
            Keyboard,
            Mouse,
            Gamepad
        }

        [Serializable]
        private sealed class KeyBindingRow
        {
            public KeyBindingId Id = KeyBindingId.Move;
            public Button KeyboardButton = null;
            public Button GamepadButton = null;
            public TextMeshProUGUI KeyboardText = null;
            public TextMeshProUGUI GamepadText = null;
        }

        private const string PlayerBindingsKey = "Options.KeyConfig.PlayerBindings";
        private const string RoguelikeBindingsKey = "Options.KeyConfig.RoguelikeBindings";
        private const string PauseKeyboardBindingKey = "Options.KeyConfig.Pause.Keyboard";
        private const string PauseGamepadBindingKey = "Options.KeyConfig.Pause.Gamepad";
        private const string SubmitKeyboardBindingKey = "Options.KeyConfig.Submit.Keyboard";
        private const string SubmitGamepadBindingKey = "Options.KeyConfig.Submit.Gamepad";
        private const string CancelKeyboardBindingKey = "Options.KeyConfig.Cancel.Keyboard";
        private const string CancelGamepadBindingKey = "Options.KeyConfig.Cancel.Gamepad";

        [SerializeField] private KeyBindingRow[] _rows = new KeyBindingRow[7];
        [SerializeField] private GameObject _rebindOverlay;
        [SerializeField] private TextMeshProUGUI _rebindMessage;

        private readonly Dictionary<KeyBindingId, KeyBindingRow> _rowById = new();
        private readonly List<int> _pendingRebindIndices = new();
        private readonly List<string> _pendingRebindNames = new();

        private InputActionAsset _playerActions;
        private InputActionAsset _roguelikeActions;
        private InputAction _pauseAction;
        private InputAction _submitAction;
        private InputAction _cancelAction;
        private InputActionRebindingExtensions.RebindingOperation _rebindOperation;
        private InputAction _pendingRebindAction;
        private RebindDevice _pendingRebindDevice;
        private Button _pendingRebindOriginButton;
        private bool _pendingRebindActionWasEnabled;
        private bool _initialized;
        private int _appliedPlayerInputInstanceId;

        public bool IsRebinding { get; private set; }
        public Selectable FirstSelectable => _rows.Length > 0 ? _rows[0].KeyboardButton : null;
        public Selectable LastSelectable => _rows.Length > 0 ? _rows[^1].KeyboardButton : null;

        public void Initialize(
            InputActionAsset playerActions,
            InputActionAsset roguelikeActions,
            InputAction pauseAction,
            InputAction submitAction,
            InputAction cancelAction)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _playerActions = playerActions;
            _roguelikeActions = roguelikeActions;
            _pauseAction = pauseAction;
            _submitAction = submitAction;
            _cancelAction = cancelAction;

            _rowById.Clear();
            for (int i = 0; i < _rows.Length; i++)
            {
                KeyBindingRow row = _rows[i];
                _rowById[row.Id] = row;
                KeyBindingRow capturedRow = row;
                row.KeyboardButton.onClick.AddListener(() => BeginKeyRebind(capturedRow.Id, false, capturedRow.KeyboardButton));
                row.GamepadButton.onClick.AddListener(() => BeginKeyRebind(capturedRow.Id, true, capturedRow.GamepadButton));
            }

            LoadSavedBindingOverrides();
            ConfigureNavigation();
            RefreshKeyBindingLabels();
            _rebindOverlay.SetActive(false);
        }

        public void SetRebindOverlayVisible(bool visible)
        {
            _rebindOverlay.SetActive(visible);
        }

        public void SelectDefault()
        {
            if (_rowById.TryGetValue(KeyBindingId.Move, out KeyBindingRow row))
            {
                Select(row.KeyboardButton);
            }
        }

        public void SetNavigationBoundaries(Selectable tab, Selectable back)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                KeyBindingRow previous = i == 0 ? null : _rows[i - 1];
                KeyBindingRow next = i == _rows.Length - 1 ? null : _rows[i + 1];
                Selectable keyboardUp = previous == null ? tab : previous.KeyboardButton;
                Selectable keyboardDown = next == null ? back : next.KeyboardButton;
                Selectable gamepadUp = previous == null ? tab : previous.GamepadButton;
                Selectable gamepadDown = next == null ? back : next.GamepadButton;
                _rows[i].KeyboardButton.navigation = CreateNavigation(keyboardUp, keyboardDown, null, _rows[i].GamepadButton);
                _rows[i].GamepadButton.navigation = CreateNavigation(gamepadUp, gamepadDown, _rows[i].KeyboardButton, null);
            }
        }

        public void ApplyToNewPlayerInput()
        {
            PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
            if (playerInput != null && playerInput.GetInstanceID() != _appliedPlayerInputInstanceId)
            {
                ApplyPlayerBindingsToRuntime(playerInput);
            }
        }

        public void ApplyPlayerBindingsToRuntime(PlayerInput playerInput)
        {
            if (playerInput == null || playerInput.actions == null)
            {
                return;
            }

            bool wasActive = playerInput.inputIsActive;
            if (wasActive)
            {
                playerInput.DeactivateInput();
            }

            if (PlayerPrefs.HasKey(PlayerBindingsKey))
            {
                playerInput.actions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PlayerBindingsKey));
            }

            if (wasActive)
            {
                playerInput.ActivateInput();
            }

            _appliedPlayerInputInstanceId = playerInput.GetInstanceID();
        }

        public void CancelRebind()
        {
            if (!IsRebinding)
            {
                return;
            }

            if (_rebindOperation != null)
            {
                _rebindOperation.Cancel();
                return;
            }

            FinishRebindUi();
        }

        private void BeginKeyRebind(KeyBindingId id, bool gamepad, Button originButton)
        {
            if (IsRebinding)
            {
                return;
            }

            InputAction action;
            RebindDevice device = gamepad ? RebindDevice.Gamepad : RebindDevice.Keyboard;
            List<int> bindingIndices = new();
            List<string> stepNames = new();

            switch (id)
            {
                case KeyBindingId.Move:
                    action = _playerActions?.FindAction("Gameplay/Move");
                    if (gamepad)
                    {
                        bindingIndices.Add(FindBindingIndex(action, "<Gamepad>"));
                        stepNames.Add("移動");
                    }
                    else
                    {
                        AddCompositePart(action, "up", "移動・上", bindingIndices, stepNames);
                        AddCompositePart(action, "down", "移動・下", bindingIndices, stepNames);
                        AddCompositePart(action, "left", "移動・左", bindingIndices, stepNames);
                        AddCompositePart(action, "right", "移動・右", bindingIndices, stepNames);
                    }
                    break;
                case KeyBindingId.Punch:
                    action = _playerActions?.FindAction("Gameplay/Interact");
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Keyboard>"));
                    stepNames.Add("殴る");
                    break;
                case KeyBindingId.AttachmentScale:
                    action = _playerActions?.FindAction("Gameplay/AttachmentScale");
                    device = gamepad ? RebindDevice.Gamepad : RebindDevice.Mouse;
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Mouse>"));
                    stepNames.Add("手を広げる");
                    break;
                case KeyBindingId.Submit:
                    action = _submitAction;
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Keyboard>"));
                    stepNames.Add("決定");
                    break;
                case KeyBindingId.Cancel:
                    action = _cancelAction;
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Keyboard>"));
                    stepNames.Add("キャンセル");
                    break;
                case KeyBindingId.Pause:
                    action = _pauseAction;
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Keyboard>"));
                    stepNames.Add("ポーズ");
                    break;
                case KeyBindingId.RoguelikeExit:
                    action = _roguelikeActions?.FindAction("Roguelike/Exit");
                    bindingIndices.Add(FindBindingIndex(action, gamepad ? "<Gamepad>" : "<Keyboard>"));
                    stepNames.Add("ローグライク終了");
                    break;
                default:
                    return;
            }

            bindingIndices.RemoveAll(index => index < 0);
            if (action == null || bindingIndices.Count == 0)
            {
                return;
            }

            _pendingRebindAction = action;
            _pendingRebindActionWasEnabled = action.enabled;
            _pendingRebindDevice = device;
            _pendingRebindOriginButton = originButton;
            _pendingRebindIndices.Clear();
            _pendingRebindIndices.AddRange(bindingIndices);
            _pendingRebindNames.Clear();
            _pendingRebindNames.AddRange(stepNames);
            IsRebinding = true;
            _rebindOverlay.SetActive(true);
            ClearSelection();
            StartCoroutine(StartRebindAfterCurrentInputReleased());
        }

        private IEnumerator StartRebindAfterCurrentInputReleased()
        {
            yield return null;
            while ((Gamepad.current != null && Gamepad.current.buttonSouth.isPressed) ||
                   (Mouse.current != null && Mouse.current.leftButton.isPressed))
            {
                yield return null;
            }

            StartNextRebind();
        }

        private void StartNextRebind()
        {
            if (!IsRebinding || _pendingRebindAction == null || _pendingRebindIndices.Count == 0)
            {
                CompleteRebindSequence();
                return;
            }

            int bindingIndex = _pendingRebindIndices[0];
            string stepName = _pendingRebindNames.Count > 0 ? _pendingRebindNames[0] : _pendingRebindAction.name;
            _pendingRebindIndices.RemoveAt(0);
            if (_pendingRebindNames.Count > 0)
            {
                _pendingRebindNames.RemoveAt(0);
            }

            string deviceLabel = _pendingRebindDevice == RebindDevice.Gamepad ? "ゲームパッド" : "キーボード / マウス";
            _rebindMessage.text = $"{stepName}\n{deviceLabel}の入力を押してください\nBackspace：キャンセル";

            _pendingRebindAction.Disable();
            InputActionRebindingExtensions.RebindingOperation operation = _pendingRebindAction.PerformInteractiveRebinding(bindingIndex)
                .WithCancelingThrough("<Keyboard>/backspace")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/scroll")
                .OnCancel(OnRebindCancelled)
                .OnComplete(OnRebindCompleted);

            switch (_pendingRebindDevice)
            {
                case RebindDevice.Keyboard:
                    operation.WithControlsHavingToMatchPath("<Keyboard>");
                    break;
                case RebindDevice.Mouse:
                    operation.WithControlsHavingToMatchPath("<Mouse>");
                    break;
                case RebindDevice.Gamepad:
                    operation.WithControlsHavingToMatchPath("<Gamepad>");
                    break;
            }

            _rebindOperation = operation;
            _rebindOperation.Start();
        }

        private void OnRebindCompleted(InputActionRebindingExtensions.RebindingOperation operation)
        {
            operation.Dispose();
            _rebindOperation = null;
            if (_pendingRebindIndices.Count > 0)
            {
                StartCoroutine(StartNextRebindOnNextFrame());
                return;
            }

            CompleteRebindSequence();
        }

        private IEnumerator StartNextRebindOnNextFrame()
        {
            yield return null;
            StartNextRebind();
        }

        private void OnRebindCancelled(InputActionRebindingExtensions.RebindingOperation operation)
        {
            operation.Dispose();
            _rebindOperation = null;
            FinishRebindUi();
        }

        private void CompleteRebindSequence()
        {
            SaveBindingOverrides();
            SynchronizeRoguelikeBindings();
            ApplyPlayerBindingsToRuntime(FindFirstObjectByType<PlayerInput>());
            RefreshKeyBindingLabels();
            FinishRebindUi();
        }

        private void FinishRebindUi()
        {
            if (_pendingRebindActionWasEnabled)
            {
                _pendingRebindAction?.Enable();
            }

            _pendingRebindAction = null;
            _pendingRebindIndices.Clear();
            _pendingRebindNames.Clear();
            IsRebinding = false;
            _rebindOverlay.SetActive(false);
            Select(_pendingRebindOriginButton);
            _pendingRebindOriginButton = null;
            _pendingRebindActionWasEnabled = false;
        }

        private void ConfigureNavigation()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].KeyboardButton.navigation = CreateNavigation(null, null, null, _rows[i].GamepadButton);
                _rows[i].GamepadButton.navigation = CreateNavigation(null, null, _rows[i].KeyboardButton, null);
            }
        }

        private static Navigation CreateNavigation(Selectable up, Selectable down, Selectable left, Selectable right)
        {
            return new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = up,
                selectOnDown = down,
                selectOnLeft = left,
                selectOnRight = right
            };
        }

        private void SaveBindingOverrides()
        {
            if (_playerActions != null)
            {
                PlayerPrefs.SetString(PlayerBindingsKey, _playerActions.SaveBindingOverridesAsJson());
            }

            if (_roguelikeActions != null)
            {
                PlayerPrefs.SetString(RoguelikeBindingsKey, _roguelikeActions.SaveBindingOverridesAsJson());
            }

            SaveStandaloneBindings(_pauseAction, PauseKeyboardBindingKey, PauseGamepadBindingKey);
            SaveStandaloneBindings(_submitAction, SubmitKeyboardBindingKey, SubmitGamepadBindingKey);
            SaveStandaloneBindings(_cancelAction, CancelKeyboardBindingKey, CancelGamepadBindingKey);
            PlayerPrefs.Save();
        }

        private void LoadSavedBindingOverrides()
        {
            LoadAssetOverrides(_playerActions, PlayerBindingsKey);
            LoadAssetOverrides(_roguelikeActions, RoguelikeBindingsKey);
            LoadStandaloneBindings(_pauseAction, PauseKeyboardBindingKey, PauseGamepadBindingKey);
            LoadStandaloneBindings(_submitAction, SubmitKeyboardBindingKey, SubmitGamepadBindingKey);
            LoadStandaloneBindings(_cancelAction, CancelKeyboardBindingKey, CancelGamepadBindingKey);
            SynchronizeRoguelikeBindings();
        }

        private void SynchronizeRoguelikeBindings()
        {
            if (_playerActions == null || _roguelikeActions == null)
            {
                return;
            }

            bool wasEnabled = _roguelikeActions.enabled;
            if (wasEnabled)
            {
                _roguelikeActions.Disable();
            }

            InputAction move = _playerActions.FindAction("Gameplay/Move");
            InputAction navigate = _roguelikeActions.FindAction("Roguelike/Navigate");
            if (move != null && navigate != null)
            {
                CopyCompositePart(move, navigate, "up");
                CopyCompositePart(move, navigate, "down");
                CopyCompositePart(move, navigate, "left");
                CopyCompositePart(move, navigate, "right");
                CopyDeviceBinding(move, navigate, "<Gamepad>");
            }

            CopyStandaloneToAsset(_submitAction, _roguelikeActions.FindAction("Roguelike/Submit"));
            CopyStandaloneToAsset(_cancelAction, _roguelikeActions.FindAction("Roguelike/Cancel"));
            PlayerPrefs.SetString(RoguelikeBindingsKey, _roguelikeActions.SaveBindingOverridesAsJson());
            PlayerPrefs.Save();

            if (wasEnabled)
            {
                _roguelikeActions.Enable();
            }
        }

        private void RefreshKeyBindingLabels()
        {
            SetLabels(KeyBindingId.Move, GetMoveKeyboardDisplay(_playerActions?.FindAction("Gameplay/Move")), GetDeviceDisplay(_playerActions?.FindAction("Gameplay/Move"), "<Gamepad>"));
            SetLabels(KeyBindingId.Punch, GetDeviceDisplay(_playerActions?.FindAction("Gameplay/Interact"), "<Keyboard>"), GetDeviceDisplay(_playerActions?.FindAction("Gameplay/Interact"), "<Gamepad>"));
            SetLabels(KeyBindingId.AttachmentScale, GetDeviceDisplay(_playerActions?.FindAction("Gameplay/AttachmentScale"), "<Mouse>"), GetDeviceDisplay(_playerActions?.FindAction("Gameplay/AttachmentScale"), "<Gamepad>"));
            SetLabels(KeyBindingId.Submit, GetDeviceDisplay(_submitAction, "<Keyboard>"), GetDeviceDisplay(_submitAction, "<Gamepad>"));
            SetLabels(KeyBindingId.Cancel, GetDeviceDisplay(_cancelAction, "<Keyboard>"), GetDeviceDisplay(_cancelAction, "<Gamepad>"));
            SetLabels(KeyBindingId.Pause, GetDeviceDisplay(_pauseAction, "<Keyboard>"), GetDeviceDisplay(_pauseAction, "<Gamepad>"));
            SetLabels(KeyBindingId.RoguelikeExit, GetDeviceDisplay(_roguelikeActions?.FindAction("Roguelike/Exit"), "<Keyboard>"), GetDeviceDisplay(_roguelikeActions?.FindAction("Roguelike/Exit"), "<Gamepad>"));
        }

        private void SetLabels(KeyBindingId id, string keyboard, string gamepad)
        {
            if (!_rowById.TryGetValue(id, out KeyBindingRow row))
            {
                return;
            }

            row.KeyboardText.text = keyboard;
            row.GamepadText.text = gamepad;
            row.KeyboardButton.interactable = keyboard != "未設定";
            row.GamepadButton.interactable = gamepad != "未設定";
        }

        private static string GetMoveKeyboardDisplay(InputAction action)
        {
            if (action == null)
            {
                return "未設定";
            }

            string up = GetBindingDisplay(action, FindCompositePartIndex(action, "up"));
            string left = GetBindingDisplay(action, FindCompositePartIndex(action, "left"));
            string down = GetBindingDisplay(action, FindCompositePartIndex(action, "down"));
            string right = GetBindingDisplay(action, FindCompositePartIndex(action, "right"));
            return $"{up} / {left} / {down} / {right}";
        }

        private static string GetDeviceDisplay(InputAction action, string devicePath)
        {
            return GetBindingDisplay(action, FindBindingIndex(action, devicePath));
        }

        private static string GetBindingDisplay(InputAction action, int bindingIndex)
        {
            if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            {
                return "未設定";
            }

            string display = action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontIncludeInteractions);
            return string.IsNullOrEmpty(display) ? "未設定" : display;
        }

        private static int FindBindingIndex(InputAction action, string devicePath)
        {
            if (action == null)
            {
                return -1;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                string path = string.IsNullOrEmpty(binding.overridePath) ? binding.path : binding.overridePath;
                if (!binding.isComposite && path.StartsWith(devicePath, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindCompositePartIndex(InputAction action, string partName)
        {
            if (action == null)
            {
                return -1;
            }

            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                if (binding.isPartOfComposite && string.Equals(binding.name, partName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void AddCompositePart(InputAction action, string partName, string stepName, List<int> indices, List<string> names)
        {
            int index = FindCompositePartIndex(action, partName);
            if (index >= 0)
            {
                indices.Add(index);
                names.Add(stepName);
            }
        }

        private static void SaveStandaloneBindings(InputAction action, string keyboardKey, string gamepadKey)
        {
            int keyboardIndex = FindBindingIndex(action, "<Keyboard>");
            int gamepadIndex = FindBindingIndex(action, "<Gamepad>");
            if (keyboardIndex >= 0)
            {
                PlayerPrefs.SetString(keyboardKey, action.bindings[keyboardIndex].effectivePath);
            }

            if (gamepadIndex >= 0)
            {
                PlayerPrefs.SetString(gamepadKey, action.bindings[gamepadIndex].effectivePath);
            }
        }

        private static void LoadStandaloneBindings(InputAction action, string keyboardKey, string gamepadKey)
        {
            ApplyStandaloneBinding(action, "<Keyboard>", keyboardKey);
            ApplyStandaloneBinding(action, "<Gamepad>", gamepadKey);
        }

        private static void ApplyStandaloneBinding(InputAction action, string devicePath, string key)
        {
            if (action == null || !PlayerPrefs.HasKey(key))
            {
                return;
            }

            int index = FindBindingIndex(action, devicePath);
            if (index >= 0)
            {
                action.ApplyBindingOverride(index, PlayerPrefs.GetString(key));
            }
        }

        private static void LoadAssetOverrides(InputActionAsset asset, string key)
        {
            if (asset == null || !PlayerPrefs.HasKey(key))
            {
                return;
            }

            bool wasEnabled = asset.enabled;
            if (wasEnabled)
            {
                asset.Disable();
            }

            asset.LoadBindingOverridesFromJson(PlayerPrefs.GetString(key));
            if (wasEnabled)
            {
                asset.Enable();
            }
        }

        private static void CopyCompositePart(InputAction source, InputAction destination, string partName)
        {
            int sourceIndex = FindCompositePartIndex(source, partName);
            int destinationIndex = FindCompositePartIndex(destination, partName);
            if (sourceIndex >= 0 && destinationIndex >= 0)
            {
                destination.ApplyBindingOverride(destinationIndex, source.bindings[sourceIndex].effectivePath);
            }
        }

        private static void CopyDeviceBinding(InputAction source, InputAction destination, string devicePath)
        {
            int sourceIndex = FindBindingIndex(source, devicePath);
            int destinationIndex = FindBindingIndex(destination, devicePath);
            if (sourceIndex >= 0 && destinationIndex >= 0)
            {
                destination.ApplyBindingOverride(destinationIndex, source.bindings[sourceIndex].effectivePath);
            }
        }

        private static void CopyStandaloneToAsset(InputAction source, InputAction destination)
        {
            if (source == null || destination == null)
            {
                return;
            }

            CopyDeviceBinding(source, destination, "<Keyboard>");
            CopyDeviceBinding(source, destination, "<Gamepad>");
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
    }
}
