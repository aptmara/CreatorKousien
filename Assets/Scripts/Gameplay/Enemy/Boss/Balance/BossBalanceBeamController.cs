using System;
using System.Collections.Generic;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{
public enum TraySide { Left, Right, Level }

public interface IBossTrayItem
{
    void OnTrayRaised();
    void OnTrayLowered();
}

[DisallowMultipleComponent]
public sealed class BossBalanceBeamController : MonoBehaviour
{
    [SerializeField] private RealisticBalanceScale _scale;

    [Header("==== 振り切り判定 ====")]
    [SerializeField, Range(0.5f, 15.0f)] private float _tiltThresholdAngle = 3.0f;
    [SerializeField, Range(10.0f, 90.0f)] private float _fullyRaisedAngle = 35.0f;
    [SerializeField, Range(0.0f, 20.0f)] private float _lowerHysteresis = 5.0f;
    [SerializeField, Tooltip("実際の傾きと左右が逆に判定される場合はTrueにする")]
    private bool _inverSign = false;

    private readonly Dictionary<TraySide, IBossTrayItem> _items = new();
    private TraySide _currentRaisedSide = TraySide.Level;
    private bool _isFullyRaisedLeft;
    private bool _isFullyRaisedRight;    

    public event Action<TraySide> OnRaisedSideChanged;
    public event Action<TraySide> OnTrayFullyRaised;
    public event Action<TraySide> OnTrayFullyLowered;

    public Transform GetTraySocket(TraySide side) =>
        side == TraySide.Left ? _scale.LeftPanTransform : _scale.RightPanTransform;

    public void RegisterItem(TraySide side, IBossTrayItem item) => _items[side] = item;
    public void UnregisterItem(TraySide side) => _items.Remove(side);

    public float GetTiltRatio(TraySide side)
    {
        float signedAngle = SignedAngle;
        float value = side == TraySide.Left ?signedAngle : -signedAngle;
        return Mathf.Clamp01(value / _fullyRaisedAngle);
    }

    public TraySide? FindSideOf<T>() where T : class
    {
        foreach(var kv in _items)
        {
            if (kv.Value is T) return kv.Key;
        }
        return null;
    }

    private float SignedAngle => (_inverSign? -1.0f : 1.0f) * _scale.CurrentAngle;

    private void Update()
    {
        if (_scale == null) return;

        float signedAngle = SignedAngle;

        UpdateFullyRaisedState(TraySide.Left, signedAngle, ref _isFullyRaisedLeft);
        UpdateFullyRaisedState(TraySide.Right, -signedAngle, ref _isFullyRaisedRight);

        TraySide newSide = signedAngle > _tiltThresholdAngle ? TraySide.Left
                          : signedAngle < -_tiltThresholdAngle ? TraySide.Right
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
        if(!state && signedAngle >= _fullyRaisedAngle)
        {
            state = true;
            OnTrayFullyRaised?.Invoke(side);
        }
        else if(state && signedAngle <= _fullyRaisedAngle - _lowerHysteresis)
        {
            state = false;
            OnTrayFullyLowered?.Invoke(side);
        }
    }
}
}
