using Game.Gameplay.Collectibles;
using Game.Gameplay.Enemy.Boss;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BossHitReceiver : MonoBehaviour
{
    [SerializeField] private BossBattleFlowController _flowController;
    [SerializeField] private float _minimumHitSpeed = 0.75f;
    [SerializeField] private float _damageMultiplier = 2.5f;
    [SerializeField] private bool _despawnItemOnHit = true;

    private IBossHittable _customTarget;
    private readonly Dictionary<int, float> _nextHitTimes = new();

    private void Awake() => _customTarget = GetComponent<IBossHittable>();

    private void OnTriggerEnter(Collider other) => TryHandle(other.GetComponentInParent<CollectibleObject>(),
        other.attachedRigidbody ? other.attachedRigidbody.linearVelocity.magnitude : 0f,
        other.ClosestPoint(transform.position));

    private void OnCollisionEnter(Collision c) => TryHandle(c.collider.GetComponentInParent<CollectibleObject>(),
        c.relativeVelocity.magnitude, c.GetContact(0).point);

    private void TryHandle(CollectibleObject collectible, float speed, Vector3 pos)
    {
        if (collectible == null || speed < _minimumHitSpeed) return;
        if (_customTarget != null && !_customTarget.IsHittable) return;

        int id = collectible.GetInstanceID();
        if (_nextHitTimes.TryGetValue(id, out float next) && Time.time < next) return;
        _nextHitTimes[id] = Time.time + Mathf.Max(0f, collectible.SameItemCooldown);

        float damage = Mathf.Max(1f, collectible.DamageAmount) * Mathf.Max(1f, speed) * _damageMultiplier
            * Game.Core.Roguelike.RoguelikeUpgradeRuntime.CollectibleDamageMultiplier;

        if (!collectible.ExecuteHitImpact(_flowController.BossInstanceId, damage, pos, transform)) return;

        if (_customTarget != null) _customTarget.OnHit(damage, pos, collectible);
        else _flowController.TakeDamage(damage);

        if (_despawnItemOnHit) collectible.Despawn();
    }
}
