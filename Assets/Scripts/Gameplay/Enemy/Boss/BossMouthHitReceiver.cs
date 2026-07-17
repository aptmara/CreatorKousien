// ------------------------------------------------------------
// File     : BossMouthHitReceiver.cs
// Summary  : 口へ入った落とし物を検出し、口のHPへダメージを渡す
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 落とし物との物理衝突とダメージ計算を担当する。
// - 口のHP変更と成功判定はBossMouthHealthへ任せる。
// - 落とし物の個数は数えず、通常敵と同じダメージ方式を使用する。
// - 命中した実体は基本的にPoolへ戻す。
// - 口内に残す見た目は、今後別の演出クラスで生成する。
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using Game.Gameplay.Collectibles;
using UnityEngine;
using Game.Core.Roguelike;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 口へ入った落とし物を検出し、
    /// BossMouthHealthへダメージを渡すコンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・Collision／Triggerによる落とし物の検出
    /// ・落とし物の攻撃力と衝突速度からダメージを計算
    /// ・1回の接触による物理イベントの多重発火防止
    /// ・BossMouthHealthへのダメージ通知
    /// ・命中した落とし物のPool返却
    /// ・口内表示演出へ命中情報を通知
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossMouthHealth))]
    [RequireComponent(typeof(Collider))]
    public sealed class BossMouthHitReceiver : MonoBehaviour
    {
        [Header("--- 参照 ---")]

        [SerializeField]
        [Tooltip("ダメージを渡すBossMouthHealth")]
        private BossMouthHealth _mouthHealth;


        [Header("--- ダメージ設定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("この速度未満の接触は、口へのダメージとして扱わない")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("落とし物のDamageAmountと衝突速度に掛けるダメージ倍率")]
        private float _damageMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("Poolに戻すかどうか")]
        private bool _despawnItemOnAccepted = true;


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// ボス個体の識別ID
        /// </summary>
        private string _bossInstanceId;

        /// <summary>
        /// 落とし物の衝突時に、次にダメージを与えられる時間を記録する辞書
        /// </summary>
        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// 落とし物による口へのダメージが発生したときに発火するイベント
        ///
        /// 引数:
        /// 1. 口へ入った落とし物のCollectibleObject
        /// 2. 口へのダメージ量
        /// 3. 落とし物の衝突位置
        /// </summary>
        public event Action<CollectibleObject, float, Vector3> CollectibleAccepted;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時にBossMouthHealthを自動取得する
        /// </summary>
        private void Reset()
        {
            _mouthHealth = GetComponent<BossMouthHealth>();
        }

        /// <summary>
        /// コンポーネントの値が変更されたときにBossMouthHealthを自動取得する
        /// </summary>
        private void OnValidate()
        {
            if (_mouthHealth == null)
            {
                _mouthHealth = GetComponent<BossMouthHealth>();
            }
        }

        /// <summary>
        /// 実行開始時にBossMouthHealthがアタッチされているか確認する
        /// </summary>
        private void Awake()
        {
            if (_mouthHealth == null)
            {
                _mouthHealth = GetComponent<BossMouthHealth>();
            }

            if (_mouthHealth == null)
            {
                Debug.LogError($"[{nameof(BossMouthHitReceiver)}] BossMouthHealthがアタッチされていません。", this);

                enabled = false;
            }
        }

        /// <summary>
        /// コンポーネントが無効化されたときに、落とし物の衝突記録をクリアする
        /// </summary>
        private void OnDisable()
        {
            _nextHitTimes.Clear();
        }


        // 初期化
        // ------------------------------------------------------------

        /// <summary>
        /// ボス個体IDを設定する
        /// </summary>
        /// <param name="bossInstanceId">Wave内で一意に識別されるボス個体のID</param>
        public void Initialize(string bossInstanceId)
        {
            _bossInstanceId = bossInstanceId;

            if (string.IsNullOrEmpty(_bossInstanceId))
            {
                Debug.LogWarning($"[{nameof(BossMouthHitReceiver)}] Initialize()に空のボス個体IDが渡されました。", this);
            }
        }


        // 衝突判定
        // ------------------------------------------------------------

        private void OnCollisionEnter(Collision collision)
        {
            CollectibleObject collectible = collision.collider.GetComponentInParent<CollectibleObject>();

            if (collectible == null)
            {
                return;
            }

            float hitSpeed = collision.relativeVelocity.magnitude;

            Vector3 hitPosition = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);

            TryApplyCollectibleHit(collectible, hitSpeed, hitPosition);
        }


        /// <summary>
        /// 落とし物が口のTriggerに入ったときに呼ばれる
        /// </summary>
        /// <param name="other">コライダー</param>
        private void OnTriggerEnter(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();

            if (collectible == null)
            {
                return;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;

            float hitSpeed = attachedRigidbody != null ? attachedRigidbody.linearVelocity.magnitude : 0f;

            Vector3 hitPosition = other.ClosestPoint(transform.position);

            TryApplyCollectibleHit(collectible, hitSpeed, hitPosition);
        }


        // ダメージ変換
        // ------------------------------------------------------------

        private bool TryApplyCollectibleHit(CollectibleObject collectible, float hitSpeed, Vector3 hitPosition)
        {
            if (collectible == null || _mouthHealth == null)
            {
                return false;
            }


            // アングリバイト中以外は口へのダメージを受け付けない
            if (!_mouthHealth.IsDamageable)
            {
                return false;
            }

            // 衝突速度が閾値未満の場合は口へのダメージを受け付けない
            if (hitSpeed < _minimumHitSpeed)
            {
                return false;
            }

            int itemInstanceId = collectible.GetInstanceID();

            // 同じ接触による物理イベントの多重発火を防ぐ
            if (_nextHitTimes.TryGetValue(itemInstanceId, out float nextHitTime) && Time.time < nextHitTime)
            {
                return false;
            }

            // クールタイムを設定！
            float hitCooldown = Mathf.Max(0f, collectible.SameItemCooldown);
            _nextHitTimes[itemInstanceId] = Time.time + hitCooldown;


            // 通常の敵と同じダメージ計算を行う
            float baseDamage = Mathf.Max(1f, collectible.DamageAmount);

            float speedMultiplier = Mathf.Max(1f, hitSpeed);

            float mouthDamage = baseDamage * speedMultiplier * _damageMultiplier
                * RoguelikeUpgradeRuntime.CollectibleDamageMultiplier;

            // ボス個体IDがある場合はヒットイベントから、落ち物固有効果を実行する
            if (!string.IsNullOrEmpty(_bossInstanceId))
            {
                bool isHitImpactProcessed = collectible.ExecuteHitImpact(_bossInstanceId, mouthDamage, hitPosition, transform);

                if (!isHitImpactProcessed)
                {
                    return false;
                }
            }

            bool isDamageApplied = _mouthHealth.ApplyDamage(mouthDamage);

            if (!isDamageApplied)
            {
                return false;
            }

            CollectibleAccepted?.Invoke(collectible, mouthDamage, hitPosition);

            // Poolに戻すかどうか
            if (_despawnItemOnAccepted)
            {
                collectible.Despawn();
            }

            return true;
        }
    }
}
