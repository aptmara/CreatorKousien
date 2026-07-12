// 制作者: 山内陽
using Game.Core.Events;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 自由移動アイテムの衝突を敵へのHitBatchEventへ変換する受け口。
    /// </summary>
    public sealed class EnemyHitReceiver : MonoBehaviour
    {
        [Tooltip("実行時に親のEnemyControllerから自動取得されるユニークなID。")]
        private string _enemyId;
        private EnemyController _controller;

        public Action OnHitAction;

        private void Start()
        {
            _controller = GetComponentInParent<EnemyController>();
            if (_controller != null)
            {
                _enemyId = _controller.InstanceEnemyId;
            }
        }

        [SerializeField]
        [Tooltip("この速度未満の衝突はダメージにしない。")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Tooltip("CollectibleData.DamageAmountと衝突速度に掛ける本体ダメージ倍率。")]
        private float _bodyDamageMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("命中したアイテムをPoolへ戻すか。")]
        private bool _despawnItemOnHit = false;

        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();

        public void Initialize(string enemyID)
        {
            _enemyId = enemyID;
        }

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

            float cooldown = collectible.SameItemCooldown;
            _nextHitTimes[itemId] = Time.time + Mathf.Max(0f, cooldown);

            float baseDamage = Mathf.Max(1f, collectible.DamageAmount);
            float speedFactor = Mathf.Max(1f, hitSpeed);
            float bodyDamage = baseDamage * speedFactor * _bodyDamageMultiplier;

            bool isHitProcessed = collectible.ExecuteHitImpact(_enemyId, bodyDamage, hitPosition, transform);

            if (!isHitProcessed) return false;

            if (_controller != null)
            {
                _controller.OnBodyHit(bodyDamage);
            }

            if (OnHitAction != null) OnHitAction.Invoke();
            // 状態異常があるある場合はデータを生成してコントローラーに追加
            EnemyDebuffConfig config;
            if(CreateDebuff(collectible.GetCollectableData(), out config))
            {
                _controller.OnAddDebuff(config);
            }

                var data = collectible.GetCollectableData();
            bool shouldDespawn = _despawnItemOnHit;
            if (data != null && data.Type == CollectibleType.Gummy)
            {
                shouldDespawn = false;
            }

            if (shouldDespawn)
            {
                collectible.Despawn();
            }

            return true;
        }


        /// <summary>
        /// 小物の情報からデバフのコンフィグを生成する、デバフが存在しない場合はfalseを返す
        /// </summary>
        /// <param name="data">小物のデータ</param>
        /// <param name="config">戻り値のデバフコンフィグ</param>
        /// <returns></returns>
        private bool CreateDebuff(CollectibleData data, out EnemyDebuffConfig config)
        {
            // コンフィグを初期化
            config = new EnemyDebuffConfig(data.Type.ToString(), 0.0f);
            bool hasDebuff = false;
            // 小物の種類ごとにデバフ効果を追加
            switch(data.Type)
            {
                case CollectibleType.Poison:
                    config.Duration = Mathf.Max(config.Duration, data.PoisonDuration);
                    config.InitDamageOverTime(data.PoisonDuration, data.PoisonMaxDamage, data.PoisonMinDamage, 1.0f);
                    hasDebuff = true;
                    break;

                case CollectibleType.Ice:
                    // 確率を判定する
                    float random = UnityEngine.Random.Range(0.0f, 100.0f);
                    if(random > data.FreezeProbability)
                    {
                        hasDebuff = false;
                        break;
                    }

                    config.Duration = Mathf.Max(config.Duration, data.FreezeDuration);
                    config.InitFreeze(data.FreezeDuration, data.FreezeHitDurability, data.FreezeBreakDamage);
                    hasDebuff = true;
                    break;

                default: hasDebuff = false;
                    break;
            }

            return hasDebuff;
        }

    }

}
