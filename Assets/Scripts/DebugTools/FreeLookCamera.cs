using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FreeLookCamera : MonoBehaviour
{

    [SerializeField] InputAction _cameraInput;
    [SerializeField] InputAction _lookInput;
    [SerializeField] InputAction _useInput;

    [SerializeField] InputAction _speedInput;

    Transform _transform;
    Rigidbody _rigidbody;
    Camera _camera;

    bool _isSafeInit;
    bool _isUse;

    float _speedUpValue;
    [SerializeField] int _priority;
    [SerializeField] float _moveSpeed;
    [SerializeField] float _lookSpeed;

    private void OnEnable()
    {
        _cameraInput.Enable();
        _lookInput.Enable();
        _useInput.Enable();
        _speedInput.Enable();
    }

    private void OnDisable()
    {
        _cameraInput.Disable();
        _lookInput.Disable();
        _useInput.Disable();
        _speedInput.Disable();
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if(_rigidbody == null)
        {
            Debug.LogError("[FreeLookCamera] Rigidbodyが存在しません");
            _isSafeInit = false;
            return;
        }
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("[FreeLookCamera] Cameraが存在しません");
            _isSafeInit = false;
            return;
        }

        _transform = GetComponent<Transform>();
        if (_transform == null)
        {
            Debug.LogError("[FreeLookCamera] Transformが存在しません");
            _isSafeInit = false;
            return;
        }

        _isSafeInit = true;
        _isUse = true;

        _speedUpValue = _moveSpeed * 0.1f;
    }

    private void Update()
    {
        if (!_isSafeInit) return;

        // カメラの優先度切り替え
        if (CameraToggleCheck()) CameraToggle();

        if (!_isUse) return;
        // カメラ操作
        CameraSpeedChange();
        CameraLook();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!_isSafeInit || !_isUse) return;
        CameraMove();
    }

    private bool CameraToggleCheck()
    {
        return _useInput.triggered;
    }

    private void CameraToggle()
    {
        _isUse = !_isUse;

        if(_isUse)
        {
            _camera.depth = _priority;
        }
        else
        {
            _camera.depth = -1;
        }

    }

    private void CameraSpeedChange()
    {
        // 移動速度変更
        float input = _speedInput.ReadValue<float>();
        _moveSpeed += input * _speedUpValue;
    }

    private void CameraMove()
    {
        // カメラ移動
        Vector3 moveInput = _cameraInput.ReadValue<Vector3>().normalized;
        Vector3 inputForce = moveInput * _moveSpeed;
        _rigidbody.linearVelocity = inputForce * Time.deltaTime;
    }

    private void CameraLook()
    {
        // カメラ向き更新
        Vector2 lookInput = _lookInput.ReadValue<Vector2>().normalized;
        Vector2 look = lookInput * _lookSpeed;
        Vector3 eulerRotation = _transform.rotation.eulerAngles;
        // pitchとyawに入力を加算
        eulerRotation.x += look.y;
        eulerRotation.y += look.x;
        if (eulerRotation.x > 180.0f) eulerRotation.x = 180.0f;
        else if (eulerRotation.x < -180.0f) eulerRotation.x = -180.0f;
        // 角度を更新して
        transform.rotation = Quaternion.Euler(eulerRotation);
    }
}
