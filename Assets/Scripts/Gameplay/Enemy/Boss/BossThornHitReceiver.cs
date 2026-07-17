// ------------------------------------------------------------
// File     : BossThornHitReceiver.cs
// Summary  : 落とし物とボスの棘の衝突を検出し、棘へダメージを渡すクラス
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 物理衝突の検出とダメージ量の計算を担当する。
// - 棘のHP変更や破壊判定はBossThornへ任せる。
// - フェーズ進行や棘の抽選はBossControllerへ任せる。
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 落とし物とボスの棘の衝突を検出し、
    /// 衝突速度と落とし物の攻撃力からダメージ量を計算する。
    ///
    /// このクラスが担当するもの:
    /// ・Collision／Triggerによる落とし物の検出
    /// ・落とし物の攻撃力と衝突速度からダメージ量を計算
    /// ・同じ落とし物による連続ヒットの制限
    /// ・BossThornへのダメージ通知
    /// ・必要に応じた落とし物のデスポーン！
    /// → ワンちゃんデスポーンはかわるかも？
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossThorn))]
    [RequireComponent(typeof(Collider))]
    public sealed class BossThornHitReceiver : MonoBehaviour
    {
        [Header("--- 参照 ---")]

        [SerializeField]
        [Tooltip("衝突によるダメージを渡すBossThorn。同じGameObjectのBossThornを設定する。")]
        private BossThorn _thorn;


        [Header("--- ダメージ設定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("この速度未満の衝突は、棘へのダメージとして扱わない。")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("落とし物のDamageAmountと衝突速度に掛けるダメージ倍率。")]
        private float _damageMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("命中した落とし物をオブジェクトプールへ戻すかどうか。")]
        private bool _despawnItemOnHit;


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// 実行時にBossControllerから設定される、
        /// ボス個体を識別するためのID。
        ///
        /// EnemyHitBatchEventを発行する際に使用する。
        /// </summary>
        private string _bossInstanceId;

        /// <summary>
        /// 同じ落とし物が短時間に何度も衝突することを防ぐため、
        /// 落とし物ごとの次回ヒット可能時刻を保持する。
        ///
        /// Key   : CollectibleObjectのInstanceID
        /// Value : 次にヒット可能になるTime.time
        /// </summary>
        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネントを追加した際に、同じGameObjectにアタッチされているBossThornを自動的に取得して設定する。
        /// </summary>
        private void Reset()
        {
            _thorn = GetComponent<BossThorn>();
        }


        /// <summary>
        /// InspectorでBossThornが設定されていない場合に、
        /// 自動的に取得して設定する。
        /// </summary>
        private void OnValidate()
        {
            if (_thorn == null)
            {
                _thorn = GetComponent<BossThorn>();
            }
        }


        /// <summary>
        /// 実行開始時にBossThornが設定されているか確認する。
        /// </summary>
        private void Awake()
        {
            if (_thorn == null)
            {
                _thorn = GetComponent<BossThorn>();
            }

            if (_thorn == null)
            {
                Debug.LogError("[BossThornHitReceiver] BossThornが設定されていません。", this);

                enabled = false;
            }
        }


        /// <summary>
        /// 無効化時にヒット履歴を破棄する。
        /// </summary>
        private void OnDisable()
        {
            _nextHitTimes.Clear();
        }


        // 初期化
        // ------------------------------------------------------------

        /// <summary>
        /// ボス個体を識別するためのIDを設定する。
        /// </summary>
        /// <param name="bossInstanceId">Wave内で一意にボスを識別するためのID</param>
        public void Initialize(string bossInstanceId)
        {
            _bossInstanceId = bossInstanceId;

            if (string.IsNullOrWhiteSpace(_bossInstanceId))
            {
                Debug.LogWarning("[BossThornHitReceiver] BossInstanceIdが空です。棘へのダメージは処理されますが、EnemyHitBatchEventは発行されません。", this);
            }
        }


        // 衝突判定
        // ------------------------------------------------------------

        /// <summary>
        /// 棘のコライダーに落とし物が衝突した際に呼ばれる。
        /// </summary>
        /// <param name="collision"></param>
        private void OnCollisionEnter(Collision collision)
        {
            Debug.Log(
                $"[ThornDebug] 落とし物との衝突を受信しました。" +
                $" ThornId={_thorn?.ThornId}," +
                $" Other={collision.collider.name}," +
                $" Damageable={_thorn != null && _thorn.IsDamageable}," +
                $" Speed={collision.relativeVelocity.magnitude:F2}",
                this);

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
        /// 棘のコライダーに落とし物が衝突した際に呼ばれる。
        /// </summary>
        /// <param name="other"></param>
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

        /// <summary>
        /// 落とし物が棘に衝突した際に、棘へダメージを渡す。
        /// </summary>
        /// <param name="collectible">衝突した落とし物</param>
        /// <param name="hitSpeed">衝突速度</param>
        /// <param name="hitPosition">衝突位置</param>
        /// <returns>棘へのダメージ適用結果</returns>
        private bool TryApplyCollectibleHit(CollectibleObject collectible, float hitSpeed, Vector3 hitPosition)
        {
            if (collectible == null || _thorn == null)
            {
                return false;
            }

            // 非アクティブまたは破壊済みの棘はヒット判定しない
            if (!_thorn.IsDamageable)
            {
                return false;
            }

            //一定速度未満の接触はダメージとして扱わない
            if (hitSpeed < _minimumHitSpeed)
            {
                return false;
            }

            int itemInstanceId = collectible.GetInstanceID();

            // 同じ落とし物が短時間に何度も衝突することを防ぐ
            if (_nextHitTimes.TryGetValue(itemInstanceId, out float nextHitTime) && Time.time < nextHitTime)
            {
                return false;
            }

            // 落とし物側で設定されている連続ヒット間隔を使用する
            float hitCooldown = Mathf.Max(0f, collectible.SameItemCooldown);

            _nextHitTimes[itemInstanceId] = Time.time + hitCooldown;

            // 通常敵のEnemyHitReceiverと同じ計算方式を使用する
            float baseDamage = Mathf.Max(1f,collectible.DamageAmount);

            float speedMultiplier = Mathf.Max(1f, hitSpeed);

            float thornDamage = baseDamage * speedMultiplier * _damageMultiplier;

            // BossControllerからIDが設定されている場合は通常敵と同じヒットイベントや落とし物固有効果を実行する
            if (!string.IsNullOrEmpty(_bossInstanceId))
            {
                bool isHitImpactProcessed = collectible.ExecuteHitImpact(_bossInstanceId, thornDamage, hitPosition, transform);

                // グミの最大反射回数など、落とし物側でヒット時の処理が完了している場合は棘へのダメージを渡さない
                if (!isHitImpactProcessed)
                {
                    return false;
                }
            }

            bool isDamageApplied = _thorn.ApplyDamage(thornDamage);

            if (!isDamageApplied)
            {
                return false;
            }

            HandleCollectibleAfterHit(collectible);

            return true;
        }


        /// <summary>
        /// 棘へのダメージ処理後に落とし物をデスポーンするかどうかを判定する。
        /// </summary>
        /// <param name="collectible">棘に当たった落とし物</param>
        private void HandleCollectibleAfterHit(CollectibleObject collectible)
        {
            if (!_despawnItemOnHit)
            {
                return;
            }

            CollectibleData collectibleData = collectible.GetCollectableData();

            // グミは通常敵と同じく、ヒット時に反射・連続ヒットさせる
            if (collectibleData != null && collectibleData.Type == CollectibleType.Gummy)
            {
                return;
            }

            collectible.Despawn();
        }
    }
}
