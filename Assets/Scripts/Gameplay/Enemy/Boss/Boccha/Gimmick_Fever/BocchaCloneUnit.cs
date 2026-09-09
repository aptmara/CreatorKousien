// ------------------------------------------------------------
// File		: BocchaCloneUnit.cs
// Summary	: 分身フェーズのダミー
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - BossHitReceiverはSerializeFieldでFlowControllerを持つためPrefabからシーン参照を張れない!
// - 見た目はMaterialPropertyBlockで着色するので、マテリアルは増えない
// - 棘玉はボスにダメージを与えない危険物なので、ダミーにも通さない！
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Roguelike;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 分身フェーズのダミー個体
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaCloneUnit : MonoBehaviour
    {
        [Header("==== 被弾 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("この速度未満の衝突はヒットとして扱わない")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("落し物のダメージにかける倍率")]
        private float _damageMultiplier = 2.5f;

        [SerializeField]
        [Tooltip("命中した落し物を消すかどうか")]
        private bool _despawnItemOnHit = true;


        [Header("==== 演出 ====")]

        [SerializeField]
        [Tooltip("破壊された時のVFX")]
        private GameObject _breakVfxPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXを破棄するまでの時間")]
        private float _vfxLifeTime = 3.0f;


        // ランタイム状態
        // ------------------------------------------------------------

        // 直近のヒット時刻を保持する。連続ヒットを防ぐために使う
        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();

        // MaterialPropertyBlockを使って着色する。マテリアルは増えない
        private MaterialPropertyBlock _propertyBlock;

        // Rendererをキャッシュしておく
        private Renderer[] _renderers;

        // ボスのインスタンスIDを保持する
        private string _bossInstanceId;

        // ボスの現在HPを保持する
        private float _currentHp;

        // ボスが壊れたかどうか
        private bool _isBroken;


        /// <summary>まだ壊れていないかどうか。</summary>
        public bool IsAlive => !_isBroken;


        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="bossInstanceId">ボスのインスタンスID</param>
        /// <param name="maxHp">最大HP</param>
        /// <param name="appearance">見た目</param>
        /// <param name="distinctiveness">見た目の差別化</param>
        /// <param name="baseScale">基準スケール</param>
        public void Initialize(string bossInstanceId, float maxHp, BocchaAppearance appearance, float distinctiveness, float baseScale)
        {
            _bossInstanceId = bossInstanceId;
            _currentHp = Mathf.Max(1.0f, maxHp);
            _isBroken = false;

            // フィーバー中の全体倍率に、ダミーごとの差分を掛ける
            float scale = Mathf.Max(0.1f, baseScale);

            if (appearance != null)
            {
                scale *= appearance.GetScale(distinctiveness);

                ApplyTint(appearance.GetTint(distinctiveness));
            }

            transform.localScale *= scale;
        }


        // 衝突イベント: Trigger
        private void OnTriggerEnter(Collider other) => TryHandle(other.GetComponentInParent<CollectibleObject>(), other.attachedRigidbody != null ? other.attachedRigidbody.linearVelocity.magnitude : 0.0f, other.ClosestPoint(transform.position));


        // 衝突イベント: Collision
        private void OnCollisionEnter(Collision collision) => TryHandle(collision.collider.GetComponentInParent<CollectibleObject>(), collision.relativeVelocity.magnitude, collision.GetContact(0).point);


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 落し物のヒットを処理する
        /// </summary>
        /// <param name="collectible">落し物</param>
        /// <param name="speed">衝突速度</param>
        /// <param name="hitPosition">衝突位置</param>
        private void TryHandle(CollectibleObject collectible, float speed, Vector3 hitPosition)
        {
            if (_isBroken || collectible == null || speed < _minimumHitSpeed)
            {
                return;
            }

            // トゲ玉はバリアを削るための危険物なので、ダミーにもダメージを与えない
            if (collectible.GetComponent<BocchaSpikeBall>() != null)
            {
                return;
            }

            int id = collectible.GetInstanceID();

            if (_nextHitTimes.TryGetValue(id, out float next) && Time.time < next)
            {
                return;
            }

            _nextHitTimes[id] = Time.time + Mathf.Max(0.0f, collectible.SameItemCooldown);

            float damage = Mathf.Max(1.0f, collectible.DamageAmount) * Mathf.Max(1.0f, speed) * _damageMultiplier * RoguelikeUpgradeRuntime.CollectibleDamageMultiplier;

            // コンボ加算・ヒットVFX・ポップアップは既存のイベント購読側が処理する
            if (!collectible.ExecuteHitImpact(_bossInstanceId, damage, hitPosition, transform))
            {
                return;
            }

            _currentHp -= damage;

            if (_despawnItemOnHit)
            {
                collectible.Despawn();
            }

            if (_currentHp > 0.0f)
            {
                return;
            }

            Break();
        }


        /// <summary>
        /// 壊れて消える
        /// </summary>
        private void Break()
        {
            _isBroken = true;

            // 破壊時の演出
            if (_breakVfxPrefab != null)
            {
                GameObject vfx = Instantiate(_breakVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, _vfxLifeTime);
            }

            Destroy(gameObject);
        }


        /// <summary>
        /// 見た目に色味を乗せる
        /// </summary>
        /// <param name="tint">乗せる色</param>
        private void ApplyTint(Color tint)
        {
            if (_renderers == null)
            {
                _renderers = GetComponentsInChildren<Renderer>(true);
            }

            _propertyBlock ??= new MaterialPropertyBlock();

            foreach (Renderer renderer in _renderers)
            {
                if (renderer == null) continue;

                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetColor("_BaseColor", tint);
                renderer.SetPropertyBlock(_propertyBlock);
            }
        }
    }
}
