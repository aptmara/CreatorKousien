

// 制作者: 山内陽
using System.Collections.Generic;
using Game.Core.Events;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 自由移動アイテムの衝突を敵へのHitBatchEventへ変換する受け口。
    /// </summary>
    public class BaseHitRecovery : MonoBehaviour
    {

        [Tooltip("実行時に親のEnemyControllerから自動取得されるユニークなID。")]
        private string _enemyId;

        private void Start()
        {
            var controller = GetComponentInParent<EnemyController>();
            if (controller != null)
            {
                _enemyId = controller.InstanceEnemyId;
            }
        }

        [SerializeField]
        [Tooltip("この速度未満の衝突はダメージにしない。")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Tooltip("CollectibleData.DamageAmountと衝突速度に掛ける本体ダメージ倍率。")]
        private float _bodyDamageMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("同じアイテムから連続ヒットを受け付けない秒数。")]
        private float _sameItemCooldown = 0.25f;

        [SerializeField]
        [Tooltip("命中したアイテムをPoolへ戻すか。")]
        private bool _despawnItemOnHit = false;

        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();

        private void OnCollisionEnter(Collision collision)
        {
            CollectibleObject collectible = collision.collider.GetComponentInParent<CollectibleObject>();
            if (collectible == null)
            {
                return;
            }

            ApplyCollectibleHit(collectible, collision.relativeVelocity.magnitude, collision.GetContact(0).point);
        }

        private void OnTriggerEnter(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();
            if (collectible == null)
            {
                return;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            float speed = attachedRigidbody != null ? attachedRigidbody.linearVelocity.magnitude : 0f;
            ApplyCollectibleHit(collectible, speed, other.ClosestPoint(transform.position));
        }

        /// <summary>
        /// CollectibleObjectの命中を敵Hitイベントへ変換する。
        /// </summary>
        /// <param name="collectible">命中したアイテム</param>
        /// <param name="hitSpeed">衝突速度</param>
        /// <param name="hitPosition">命中位置</param>
        /// <returns>命中として処理した場合はtrue</returns>
        private bool ApplyCollectibleHit(CollectibleObject collectible, float hitSpeed, Vector3 hitPosition)
        {
            if (collectible == null || hitSpeed < _minimumHitSpeed)
            {
                return false;
            }

            int itemId = collectible.GetInstanceID();
            if (_nextHitTimes.TryGetValue(itemId, out float nextHitTime) && Time.time < nextHitTime)
            {
                return false;
            }

            _nextHitTimes[itemId] = Time.time + Mathf.Max(0f, _sameItemCooldown);

            float damage = ComputeDamage(collectible, hitSpeed);

            ApplyDamageRequest(1, damage, hitPosition);

            if (_despawnItemOnHit)
            {
                collectible.Despawn();
            }

            return true;
        }


        protected virtual float ComputeDamage(CollectibleObject collectible, float hitSpeed)
        {
            float baseDamage = Mathf.Max(1f, collectible.DamageAmount);
            float speedFactor = Mathf.Max(1f, hitSpeed);

            return baseDamage * speedFactor * _bodyDamageMultiplier;
        }


        protected virtual void ApplyDamageRequest(int hitCount, float damage, Vector3 hitPosition)
        {
            EventBus.Publish(new EnemyHitBatchEvent(_enemyId, 1, damage, hitPosition, transform));
        }

    }

}




