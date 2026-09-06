using System.Collections.Generic;
using UnityEngine;

public class RealisticBalanceScale : MonoBehaviour
{
    [Header("==== コンポーネント参照 ====")]
    [SerializeField] private Transform _beamTransform;
    [SerializeField] private Transform _leftPanAnchor;
    [SerializeField] private Transform _rightPanAnchor;
    [SerializeField] private Transform _leftPanTransform;
    [SerializeField] private Transform _rightPanTransform;

    [Header("==== 天秤の物理パラメータ =====")]
    [SerializeField] private float _maxTiltAngle = 37.0f;
    [SerializeField,Tooltip("傾きの感度")] private float _tiltSensitivity = 5.0f;
    [SerializeField,Tooltip("剛性")] private float _stiffness = 12.0f;
    [SerializeField] private float _damping = 2.5f;

    [Header("==== カタパルト検知 ====")]
    [Tooltip("１回の登録でこの質量以上が一気に増えたら急速加重とみなす")]
    [SerializeField] private float _suddenWeightThreshold = 5.0f;
    // 角度が一定フレーム内に指定量変わったら
    [SerializeField,Tooltip("")]
    private float _suddenAngleThreshold = 20.0f;
    [SerializeField, Tooltip("")]
    private float _catapultValidityTime = 2.0f;

    [Header("===== 振り落とし =====")]
    [SerializeField] private float _shakeOffForce = 6.0f;

    // 外部バイアス　ギミックから傾きを強制
    private bool _isFrozen;
    public void SetFrozen(bool frozen) => _isFrozen = frozen;

    private float _externalBias;
    public void SetExternalBias(float bias) => _externalBias = bias;
    

    [Header("==== 皿の傾き(滑り落ち)設定 ====")]
    [SerializeField] private float _maxPanTiltAngle = 40.0f;
    [SerializeField] private float _raisedPanTiltFactor = 0.8f;
    [SerializeField] private float _maxRAisedPanTiltAngle = 25.0f;

    private float _currentAngle = 0.0f;
    private float _angularVelocity = 0.0f;

    private readonly HashSet<Rigidbody> _leftPanWeights = new HashSet<Rigidbody>();
    private readonly HashSet<Rigidbody> _rightPanWeights = new HashSet<Rigidbody>();

    public float CurrentAngle => _currentAngle;
    public Transform LeftPanTransform => _leftPanTransform;
    public Transform RightPanTransform => _rightPanTransform;

    public event System.Action<bool, float> OnWeightSuddenlyAdded;

    public IReadOnlyCollection<Rigidbody> GetWeightOnSide(bool isLeft) =>
        isLeft ? _leftPanWeights : _rightPanWeights;

    private void FixedUpdate()
    {
        if (_isFrozen) return;

        float leftMass = CalculateTotalMass(_leftPanWeights);
        float rightMass = CalculateTotalMass(_rightPanWeights);

        float massDifference = (rightMass - leftMass) + _externalBias;
        float targetAngle = Mathf.Clamp(massDifference * _tiltSensitivity, -_maxTiltAngle, _maxTiltAngle);

        float force = (targetAngle - _currentAngle) * _stiffness;
        float dampingForce = _angularVelocity * _damping;
        float acceleration = force - dampingForce;

        _angularVelocity += acceleration * Time.fixedDeltaTime;
        _currentAngle += _angularVelocity * Time.fixedDeltaTime;

        _beamTransform.localRotation = Quaternion.Euler(0, 0, _currentAngle);

        UpdatePanTransform(_leftPanTransform, _leftPanAnchor, isLeft: true);
        UpdatePanTransform(_rightPanTransform, _rightPanAnchor, isLeft: false);
    }

    private void UpdatePanTransform(Transform pan,Transform anchor ,bool isLeft)
    {
        if(pan == null || anchor == null) return;

        pan.position = anchor.position;

        float panTiltZ = 0.0f;

        if (isLeft)
        {
            if (_currentAngle < 0.0f)
            {
                panTiltZ = Mathf.Clamp(_currentAngle * 1.2f, -_maxPanTiltAngle, 0.0f);
            }
            else if (_currentAngle > 0.0f)
            {
                panTiltZ = Mathf.Clamp(-_currentAngle * _raisedPanTiltFactor,-_maxPanTiltAngle,0.0f);
            }
        }
        else
        {
            if (_currentAngle > 0.0f)
            {
                panTiltZ = Mathf.Clamp(_currentAngle * 1.2f, 0.0f ,_maxPanTiltAngle);
            }
            else if (_currentAngle < 0.0f)
            {
                panTiltZ = Mathf.Clamp(-_currentAngle * _raisedPanTiltFactor,0.0f,_maxPanTiltAngle);
            }
        }
        pan.rotation = Quaternion.Euler(0.0f, 0.0f, panTiltZ);
    }

    private float CalculateTotalMass(HashSet<Rigidbody> weights)
    {
        float total = 0.0f;
        weights.RemoveWhere(rb => rb == null);
        foreach(var rb in weights)
        {
            total += rb.mass;
        }
        return total;
    }

    public void RegisterWeight(bool isLeft,Rigidbody rb)
    {
        if (rb == null) return;

        var set = isLeft ? _leftPanWeights : _rightPanWeights;
        if (set.Contains(rb)) return;

        set.Add(rb);
        
        if(rb.mass >= _suddenWeightThreshold)
        {
            OnWeightSuddenlyAdded?.Invoke(isLeft, rb.mass);
        }
    }

    public void UnregisterWeight(bool isLeft, Rigidbody rb)
    {
        if (rb == null) return;
        if (isLeft) _leftPanWeights.Remove(rb);
        else _rightPanWeights.Remove(rb);
    }

    public void ClearAllWeghts()
    {
        ShakeOffSide(_leftPanWeights, isLeft: true);
        ShakeOffSide(_rightPanWeights, isLeft: false);
    }

    private void ShakeOffSide(HashSet<Rigidbody> weights, bool isLeft)
    {
        foreach(var rb in weights)
        {
            if(rb == null) continue;

            Vector3 knockDirection = (isLeft ? Vector3.left : Vector3.right) + Vector3.up;
            rb.AddForce(knockDirection.normalized * _shakeOffForce,ForceMode.Impulse);
        }

        weights.Clear();
    }

    public void ResetToLevel()
    {
        _currentAngle = 0.0f;
        _angularVelocity = 0.0f;

        if(_beamTransform != null)
        {
            _beamTransform.localRotation = Quaternion.identity;
        }
    }

}
