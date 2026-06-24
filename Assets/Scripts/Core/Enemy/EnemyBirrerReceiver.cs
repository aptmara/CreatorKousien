// 制作者: 越智晴彦
using System.Collections.Generic;
using Game.Core.Events;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 自由移動アイテムの衝突を敵へのHitBatchEventへ変換する受け口。
    /// </summary>
    public sealed class EnemyBirrerReceiver : MonoBehaviour
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

        public void Initialize(string enemyID)
        {
            _enemyId = enemyID;
            Debug.Log("バリア初期化完了 EnemyID =" + _enemyId);
        }

        [SerializeField]
        [Tooltip("この速度未満の衝突はダメージにしない。")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Tooltip("CollectibleData.DamageAmountと衝突速度に掛けるゲージダメージ倍率。")]
        private float _gaugeDamageMultiplier = 8.0f;

        [SerializeField]
        [Tooltip("同じアイテムから連続ヒットを受け付けない秒数。")]
        private float _sameItemCooldown = 0.25f;

        [SerializeField]
        [Tooltip("命中したアイテムをPoolへ戻すか。")]
        private bool _despawnItemOnHit = true;

        [SerializeField]
        [Tooltip("バリアがダメージを受けた際の加速補正")]
        private float _onHitAcceleration = 3.0f;



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
        public bool ApplyCollectibleHit(CollectibleObject collectible, float hitSpeed, Vector3 hitPosition)
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

            float baseDamage = Mathf.Max(1f, collectible.DamageAmount);
            float speedFactor = Mathf.Max(1f, hitSpeed);
            float gaugeDamage = baseDamage * speedFactor * _gaugeDamageMultiplier;

            EventBus.Publish(new BarrierHitBatchEvent(_enemyId, 1, gaugeDamage, hitPosition, transform));

            if (_despawnItemOnHit)
            {
               collectible.Despawn();
            }
            else
            {
                Rigidbody rb = collectible.GetComponent<Rigidbody>();
                Vector3 newVel = rb.linearVelocity * _onHitAcceleration;
                rb.linearVelocity = newVel;
            }

            return true;
        }
    }
}
