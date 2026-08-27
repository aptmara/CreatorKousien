using Game.Presentation.UI.Pause;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.Presentation.UI.Common
{
    [DisallowMultipleComponent]
    public sealed class MenuSelectionFeedbackController : MonoBehaviour
    {
        private enum InputMode
        {
            Navigation,
            Mouse
        }

        [Header("Selection")]
        [SerializeField] private Selectable _initialSelection;
        [SerializeField] private Selectable[] _selectables;
        [SerializeField] private bool _selectOnEnable = true;
        [SerializeField] private bool _initializeOnFirstSelection;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float _unselectedScale = 1f;
        [SerializeField, Min(0f)] private float _selectedMinScale = 1f;
        [SerializeField, Min(0f)] private float _selectedMaxScale = 1.2f;
        [SerializeField, Min(0f)] private float _animationSpeed = 4f;
        [SerializeField] private Color _unselectedTint = new(0.62f, 0.62f, 0.62f, 1f);

        [Header("Outline")]
        [SerializeField] private Material _imageOutlineMaterial;
        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField, Min(0f)] private float _outlineSize = 4f;

        private PauseSelectionOutline[] _feedbacks;
        private InputSystemUIInputModule _uiInputModule;
        private GameObject _lastNavigationSelection;
        private InputMode _inputMode;
        private bool _resetSelectionInLateUpdate;
        private bool _inputEnabled = true;
        private bool _feedbackInitialized;

        private void Awake()
        {
            _uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
            _feedbacks = new PauseSelectionOutline[_selectables.Length];
        }

        private void InitializeFeedbacks()
        {
            if (_feedbackInitialized)
            {
                return;
            }

            for (int i = 0; i < _selectables.Length; i++)
            {
                Selectable selectable = _selectables[i];
                if (selectable == null || selectable.GetComponent<Graphic>() == null)
                {
                    continue;
                }

                PauseSelectionOutline feedback = selectable.GetComponent<PauseSelectionOutline>();
                if (feedback == null)
                {
                    feedback = selectable.gameObject.AddComponent<PauseSelectionOutline>();
                }

                feedback.Configure(
                    _outlineColor,
                    _outlineSize,
                    _unselectedTint,
                    _unselectedScale,
                    _selectedMinScale,
                    _selectedMaxScale,
                    _animationSpeed,
                    _imageOutlineMaterial,
                    true);
                feedback.SetInstancePointerMode(false);
                _feedbacks[i] = feedback;
            }

            _feedbackInitialized = true;
        }

        private void OnEnable()
        {
            if (!_initializeOnFirstSelection)
            {
                InitializeFeedbacks();
            }

            _inputMode = InputMode.Navigation;
            SetFeedbackPointerMode(false);
            if (_selectOnEnable)
            {
                Select(GetInitialSelection());
            }
        }

        private void Update()
        {
            if (!_inputEnabled || EventSystem.current == null)
            {
                return;
            }

            GameObject currentSelection = EventSystem.current.currentSelectedGameObject;
            if (!_feedbackInitialized && CanSelect(currentSelection != null ? currentSelection.GetComponent<Selectable>() : null))
            {
                InitializeFeedbacks();
            }

            if (currentSelection != null)
            {
                if (!OwnsSelection(currentSelection))
                {
                    return;
                }

                _lastNavigationSelection = currentSelection;
            }

            bool mouseMoved = Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0f;
            bool navigationOperated = WasNavigationDeviceOperated();

            if (navigationOperated)
            {
                SetInputMode(InputMode.Navigation);
            }
            else if (mouseMoved)
            {
                SetInputMode(InputMode.Mouse);
            }
        }

        private void LateUpdate()
        {
            if (!_resetSelectionInLateUpdate || !_inputEnabled)
            {
                return;
            }

            _resetSelectionInLateUpdate = false;
            if (!HasValidOwnedSelection())
            {
                Select(GetInitialSelection());
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            if (_inputEnabled == enabled)
            {
                return;
            }

            _inputEnabled = enabled;
            _resetSelectionInLateUpdate = false;
            for (int i = 0; i < _feedbacks.Length; i++)
            {
                if (_feedbacks[i] != null)
                {
                    _feedbacks[i].enabled = enabled;
                }
            }

            if (enabled)
            {
                _inputMode = InputMode.Navigation;
                SetFeedbackPointerMode(false);
            }
        }

        private bool WasNavigationDeviceOperated()
        {
            InputAction moveAction = _uiInputModule != null ? _uiInputModule.move?.action : null;
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

        private void SetInputMode(InputMode inputMode)
        {
            if (_inputMode == inputMode)
            {
                if (inputMode == InputMode.Navigation && !HasValidOwnedSelection())
                {
                    Select(GetInitialSelection());
                    _resetSelectionInLateUpdate = true;
                }

                return;
            }

            _inputMode = inputMode;
            bool mouseMode = inputMode == InputMode.Mouse;
            SetFeedbackPointerMode(mouseMode);

            if (mouseMode)
            {
                _resetSelectionInLateUpdate = false;
                if (EventSystem.current != null && OwnsSelection(EventSystem.current.currentSelectedGameObject))
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                return;
            }

            Select(GetNavigationModeInitialSelection());
            _resetSelectionInLateUpdate = true;
        }

        private void SetFeedbackPointerMode(bool pointerMode)
        {
            if (_feedbacks == null)
            {
                return;
            }

            for (int i = 0; i < _feedbacks.Length; i++)
            {
                _feedbacks[i]?.SetInstancePointerMode(pointerMode);
            }
        }

        private Selectable GetInitialSelection()
        {
            if (_lastNavigationSelection != null)
            {
                Selectable lastSelection = _lastNavigationSelection.GetComponent<Selectable>();
                if (CanSelect(lastSelection))
                {
                    return lastSelection;
                }
            }

            if (CanSelect(_initialSelection))
            {
                return _initialSelection;
            }

            for (int i = 0; i < _selectables.Length; i++)
            {
                if (CanSelect(_selectables[i]))
                {
                    return _selectables[i];
                }
            }

            return null;
        }

        private Selectable GetNavigationModeInitialSelection()
        {
            if (CanSelect(_initialSelection))
            {
                return _initialSelection;
            }

            for (int i = 0; i < _selectables.Length; i++)
            {
                if (CanSelect(_selectables[i]))
                {
                    return _selectables[i];
                }
            }

            return null;
        }

        private bool OwnsSelection(GameObject selectedObject)
        {
            if (selectedObject == null)
            {
                return false;
            }

            for (int i = 0; i < _selectables.Length; i++)
            {
                if (_selectables[i] != null && _selectables[i].gameObject == selectedObject)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasValidOwnedSelection()
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            GameObject selectedObject = EventSystem.current.currentSelectedGameObject;
            return OwnsSelection(selectedObject) &&
                   CanSelect(selectedObject != null ? selectedObject.GetComponent<Selectable>() : null);
        }

        private static bool CanSelect(Selectable selectable)
        {
            return selectable != null && selectable.IsActive() && selectable.IsInteractable();
        }

        private static void Select(Selectable selectable)
        {
            if (EventSystem.current == null || !CanSelect(selectable))
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(null);
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
