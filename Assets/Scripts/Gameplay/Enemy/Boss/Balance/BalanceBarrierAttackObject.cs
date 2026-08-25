using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Enemy.Boss;

public class BalanceBarrierAttackObject : MonoBehaviour, IBossTrayItem
{
    [SerializeField] private float _attackInterval = 2.0f;
    [SerializeField] private float _barrierDamage = 10.0f;

    private bool _isActive;
    private float _timer;

    public event System.Action<bool> OnActiveChanged;

    public void OnTrayRaised()
    {
        _isActive = true;
        OnActiveChanged?.Invoke(true);
    }
    public void OnTrayLowered()
    {
        _isActive = false; _timer = 0f;
        OnActiveChanged?.Invoke(false);
    }

    private void Update()
    {
        if (!_isActive) return;
        _timer += Time.deltaTime;
        if (_timer < _attackInterval) return;
        _timer = 0f;
        Game.Core.Events.EventBus.Publish(new RuleBarrierAttackEvent(_barrierDamage, transform.position));
    }
}
