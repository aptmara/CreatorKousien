using System;
using UnityEngine;

public class EnemyHoldCounter
{
    int _enemyHitCounter = 0;
    float _enemyHitTimer = 0.0f;
    float _addHoldDuration = 0.0f;
    float _maxHoldDuration = 0.0f;


    Action OnHoldEnd;

    public void Initialize(float maxHoldDuration, float addHoldDuration, Action OnHoldEnd)
    {
        _maxHoldDuration = maxHoldDuration;
        _addHoldDuration = addHoldDuration;
        this.OnHoldEnd = OnHoldEnd;
    }

    public void StartCount(float defaultDuration)
    {
        _enemyHitTimer = defaultDuration;
        _enemyHitCounter = 0;
    }

    public void UpdateHold()
    {

        if (_enemyHitTimer <= 0.0f)
        {
            return;
        }

        _enemyHitTimer -= Time.deltaTime;

        if(_enemyHitTimer <= 0.0f)
        {
            OnHoldEnd?.Invoke();
            ResetHit();
        }

    }

    public void ResetHit()
    {
        _enemyHitCounter = 0;
        _enemyHitTimer = 0.0f;
    }

    public void AddHit()
    {
        _enemyHitCounter++;
        _enemyHitTimer += _addHoldDuration;
        _enemyHitTimer = Mathf.Clamp(_enemyHitTimer, 0.0f, _maxHoldDuration);
    }

}
