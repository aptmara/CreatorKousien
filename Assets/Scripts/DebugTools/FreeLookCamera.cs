using Game.Gameplay.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FreeLookCamera : MonoBehaviour
{

    [SerializeField] InputAction _cameraInput;
    [SerializeField] InputAction _lookInput;
    [SerializeField] InputAction _useInput;

    [SerializeField] InputAction _speedInput;

    Transform _transform;
    Rigidbody _rigidbody;
    Camera _camera;

    Vector2 _currentLookMove;

    bool _isSafeInit = false;
    bool _isUse = false;

    float _speedUpValue;
    [SerializeField] int _priority;
    [SerializeField] float _moveSpeed;
    [SerializeField] Vector2 _lookSpeed;

    // かなり雑ではあるけど動画撮影用なので許して
    PlayerController _playerController;
    PlayerInput _playerInput;
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

    private IEnumerator Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        if(_rigidbody == null)
        {
            Debug.LogError("[FreeLookCamera] Rigidbodyが存在しません");
            _isSafeInit = false;
            yield break;
        }
        _camera = GetComponent<Camera>();
        if (_camera == null)
        {
            Debug.LogError("[FreeLookCamera] Cameraが存在しません");
            _isSafeInit = false;
            yield break;
        }

        _transform = GetComponent<Transform>();
        if (_transform == null)
        {
            Debug.LogError("[FreeLookCamera] Transformが存在しません");
            _isSafeInit = false;
            yield break;
        }

        while(_playerController == null)
        {
            _playerController = GameObject.FindAnyObjectByType<PlayerController>();
            yield return null;
        }

        while(_playerInput == null)
        {

            _playerInput = GameObject.FindAnyObjectByType<PlayerInput>();
            yield return null;
        }
        _isSafeInit = true;
        _isUse = false;

        _camera.depth = _priority;
        _speedUpValue = _moveSpeed * 0.1f;
    }

    private void Update()
    {
        if (!_isSafeInit) return;

        // カメラの優先度切り替え
        if (CameraToggleCheck()) CameraToggle();

        if (!_isUse) return;
        // 数値決定
        CameraSpeedChange();
        LookMoveResolve();
    }

    private void LateUpdate()
    {
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
            _playerController.SetCanMove(false);
            _playerInput.enabled = false;
        }
        else
        {
            _playerController.SetCanMove(true);
            _playerInput.enabled = true;
        }

    }

    private void CameraSpeedChange()
    {
        // 移動速度変更
        float input = _speedInput.ReadValue<float>();
        _moveSpeed += input * _speedUpValue;
        _moveSpeed = Mathf.Clamp(_moveSpeed, 1.0f, 100.0f);
    }

    private void CameraMove()
    {
        // カメラ移動
        Vector3 moveInput = _cameraInput.ReadValue<Vector3>().normalized;
        Vector3 inputForce = moveInput * _moveSpeed;
       
        Matrix4x4 mat = Matrix4x4.Rotate(_transform.rotation);

        _rigidbody.linearVelocity = mat.MultiplyVector(inputForce);
    }

    private void CameraLook()
    {
        // カメラ向き更新
        Vector3 eulerRotation = _transform.rotation.eulerAngles;
        eulerRotation.x -= _currentLookMove.y;
        eulerRotation.y += _currentLookMove.x;

        // 角度を正規化
        eulerRotation.x = Mathf.DeltaAngle(0.0f, eulerRotation.x);
        eulerRotation.x = Mathf.Clamp(eulerRotation.x, -90.0f, 90.0f);
        // 角度を更新
        transform.rotation = Quaternion.Euler(eulerRotation);

    }

    private void LookMoveResolve()
    {
        Vector2 lookInput = _lookInput.ReadValue<Vector2>();
        _currentLookMove = lookInput * _lookSpeed * Time.deltaTime;
    }

}
