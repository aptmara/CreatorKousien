using System;
using UnityEngine;

public enum TraySide { Left, Right, Level }

public interface IBossTrayItem
{
    void OnTrayRaised();
    void OnTrayLowered();
}

[DisallowMultipleComponent]
public sealed class BossBalanceBeamController : MonoBehaviour
{
    [SerializeField] private HingeJoint _beamJoint;
    [SerializeField] private Transform _leftTraySocket;
    [SerializeField] private Transform _rightTraySocket;
    [SerializeField, Range(0.5f, 15f)] private float _tiltThresholdAngle = 3.0f;

    [Header("==== 振り切り判定 ====")]
    [SerializeField, Range(10.0f, 90.0f)] private float _fullRaisedAngle = 35.0f;
    [SerializeField, Range(10.0f, 90.0f)] private float _lowerHysteresis = 5.0f;

    private bool _isFullyRaisedLeft;
    private bool _isFullyRaisedRight;    

    private readonly System.Collections.Generic.Dictionary<TraySide, IBossTrayItem> _items = new();
    private TraySide _currentRaisedSide = TraySide.Level;
    private bool _isLocked;

    public event Action<TraySide> OnRaisedSideChanged;
    public event Action<TraySide> OnTrayFullyRaised;
    public event Action<TraySide> OnTrayFullyLowered;

    public TraySide? FindSideOf<T>() where T : class
    {
        foreach(var kv in _items)
        {
            if (kv.Value is T) return kv.Key;
        }
        return null;
    }

    public Transform GetTraySocket(TraySide side) => side == TraySide.Left ? _leftTraySocket : _rightTraySocket;

    public void RegisterItem(TraySide side, IBossTrayItem item) => _items[side] = item;
    public void UnregisterItem(TraySide side) => _items.Remove(side);

    public void LockBeam(bool locked)
    {
        _isLocked = locked;
        if (_beamJoint == null) return;
        var motor = _beamJoint.motor;
        motor.freeSpin = !locked;
        _beamJoint.useMotor = locked; // Down中はモーターで角度を固定
    }

    public float GetTiltRatio(TraySide side)
    {
        if (_beamJoint == null) return 0.0f;
        float signedAngle = side == TraySide.Left ? _beamJoint.angle : -_beamJoint.angle;
        return Mathf.Clamp01(signedAngle / _fullRaisedAngle);
    }

    private void Update()
    {
        if (_isLocked || _beamJoint == null) return;

        float angle = _beamJoint.angle;

        UpdateFullyRaisedState(TraySide.Left, angle, ref _isFullyRaisedLeft);
        UpdateFullyRaisedState(TraySide.Right, -angle, ref _isFullyRaisedRight);

        TraySide newSide = angle > _tiltThresholdAngle ? TraySide.Left
                          : angle < -_tiltThresholdAngle ? TraySide.Right
                          : TraySide.Level;

        if (newSide == _currentRaisedSide) return;

        // 前回上がっていた側を下げる
        if (_currentRaisedSide != TraySide.Level && _items.TryGetValue(_currentRaisedSide, out var prev))
            prev.OnTrayLowered();

        _currentRaisedSide = newSide;

        if (newSide != TraySide.Level && _items.TryGetValue(newSide, out var next))
            next.OnTrayRaised();

        OnRaisedSideChanged?.Invoke(newSide);
    }

    private void UpdateFullyRaisedState(TraySide side,float signedAngle,ref bool state)
    {
        if(!state && signedAngle >= _fullRaisedAngle)
        {
            state = true;
            OnTrayFullyRaised?.Invoke(side);
        }
        else if(state && signedAngle <= _fullRaisedAngle - _lowerHysteresis)
        {
            state = false;
            OnTrayFullyLowered?.Invoke(side);
        }
    }
}
