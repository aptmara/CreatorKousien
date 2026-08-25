using Game.Gameplay.Collectibles;
using Game.Gameplay.Enemy.Boss;
using UnityEngine;

public class BalanceWeaknessObject : MonoBehaviour, IBossHittable, IBossTrayItem
{
    [SerializeField] private BossBattleFlowController _flowController;
    [SerializeField] private Collider _hitCollider;

    private bool _isExposed;

    public bool IsHittable => _isExposed && !_flowController.IsDown;

    private void Awake()
    {
        if (_hitCollider != null) _hitCollider.enabled = false;
    }

    public void Initialize(BossBattleFlowController flowController )
    {
        _flowController = flowController;
    }

    public void OnTrayRaised()
    {
        _isExposed = true;
        if (_hitCollider != null) _hitCollider.enabled = true;
    }

    public void OnTrayLowered()
    {
        _isExposed = false;
        if (_hitCollider != null) _hitCollider.enabled = false;
    }

    public void OnHit(float damage, Vector3 hitPosition, CollectibleObject collectible)
    {
        _flowController.TriggerDown();
    }
}
