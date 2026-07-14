
using UnityEngine;
using UnityEngine.InputSystem;

public class S_RoguelikeSelectInput : MonoBehaviour
{

    //____________________________________
    // serialized variables
    [Header("入力アクションアセット")]
    [SerializeField] private InputActionAsset _actions;

    [Header("ActionMap名")]
    [SerializeField] private string _actionMapName = "Roguelike";


    //____________________________________
    // private variables
    private InputActionMap _map;
    private InputAction _navigateAction = null;
    private InputAction _submitAction   = null;
    private InputAction _cancelAction   = null;
    private InputAction _pointAction    = null;
    private InputAction _clickAction    = null;
    private InputAction _exitAction     = null;

    private bool _submitPressed;
    private bool _cancelPressed;
    private bool _clickPressed;

    private bool _exitHeld;             // 押しっぱなし
    private float _exitPressStartTime;  // 押し始めた時間

    //____________________________________
    // public variables
    public Vector2 Navigate => _navigateAction != null ? _navigateAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 MousePosition => _pointAction != null ? _pointAction.ReadValue<Vector2>() : Vector2.zero;

    public float ExitHeldSeconds => _exitHeld ? Time.unscaledDeltaTime - _exitPressStartTime : 0.0f;


    //____________________________________
    // private functions
    private void Awake()
    {
        // アクションアセットが定義されているかどうか
        if(_actions == null)
        {
            Debug.LogError("[S_RoguelikeSelectInput] InputActionAssetが未設定です");
            return;
        }

        // アクションアセット内のアクションマップを指定
        _map = _actions.FindActionMap(_actionMapName, throwIfNotFound: false);
        if(_map == null)
        {
            Debug.LogError($"[S_RoguelikeSelectInput] ActionMap '{_actionMapName}'が見つかりません");
            return;
        }

        // アクションを指定
        _navigateAction = _map.FindAction("Navigate");
        _submitAction   = _map.FindAction("Submit");
        _cancelAction   = _map.FindAction("Cancel");
        _pointAction    = _map.FindAction("Point");
        _clickAction    = _map.FindAction("Click");
        _exitAction     = _map.FindAction("Exit");

    }

    private void OnEnable()
    {
        if (_map == null) return;

        _map.Enable();

        _submitAction.performed += OnSubmitPerformed;
        _cancelAction.performed += OnCancelPerformed;
        _clickAction.performed += OnClickPerformed;

        if(_exitAction != null)
        {
            _exitAction.started += OnExitStarted;
            _exitAction.canceled += OnExitCanceled;
        }
    }

    private void OnDisable()
    {
        if (_map == null) return;

        _submitAction.performed -= OnSubmitPerformed;
        _cancelAction.performed -= OnCancelPerformed;
        _clickAction.performed -= OnClickPerformed;

        _map.Disable();
    }


    //____________________________________
    // ActionMap actions

    private void OnSubmitPerformed(InputAction.CallbackContext context) => _submitPressed = true;
    private void OnCancelPerformed(InputAction.CallbackContext context) => _cancelPressed = true;
    private void OnClickPerformed(InputAction.CallbackContext context) => _clickPressed = true;


    private void OnExitStarted(InputAction.CallbackContext context)
    {
        _exitHeld = true;
        _exitPressStartTime = Time.unscaledDeltaTime;
    }

    private void OnExitCanceled(InputAction.CallbackContext context)
    {
        _exitHeld = false;
    }

    public bool ConsumeSubmit()
    {
        bool result = _submitPressed;
        _submitPressed = false;
        return result;
    }

    public bool ConsumeCancel()
    {
        bool result = _cancelPressed;
        _cancelPressed = false;
        return result;
    }

    public bool ConsumeClick()
    {
        bool result = _clickPressed;
        _clickPressed = false;
        return result;
    }
}
