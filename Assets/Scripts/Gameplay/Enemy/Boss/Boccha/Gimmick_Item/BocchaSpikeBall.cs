// ------------------------------------------------------------
// File		: BocchaSpikeBall.cs
// Summary	: トゲ玉。フィールドの幅を取り、危険圏に落ちるとバリアへダメージを与える
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 「誤って落としてしまうと、バリアにダメージが入ってしまう」を着地位置で判定する。
// - 位置で判定するため、ボスが投げても、プレイヤーが動かしても同じルールで成立するようになる！！
// - 着地後も動かされた場合に備え、止まったタイミングで再判定する！！
// - ボスの爆発時はBocchaHazardRegistry経由で消えるのでやるけど、なんか退場演出あったほうがいいよねぇ
// ------------------------------------------------------------
using Game.Core.DefenceLine;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 大きなトゲ玉。フィールド上のお邪魔であり、置き場所を誤るとバリアを削る。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaSpikeBall : BocchaHazardBase
    {
        [Header("==== バリアダメージ ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("危険圏に落ちた場合に防衛バリアへ与えるダメージ")]
        private float _barrierDamage = 20.0f;

        [SerializeField]
        [Tooltip("ダメージを与えたあと消滅するかどうか")]
        private bool _despawnAfterDamage = true;

        [SerializeField]
        [Tooltip("バリアへダメージが入った時のVFX")]
        private GameObject _barrierHitVfxPrefab;


        [Header("==== 危険圏の代替判定 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("BocchaBarrierDangerZoneが1つも置かれていない場合に使う、DefenceLineからの距離")]
        private float _fallbackDangerRadius = 8.0f;


        [Header("==== 着地後の監視 ====")]

        [SerializeField]
        [Tooltip("着地後に動かされた場合、止まったところで再判定するかどうか")]
        private bool _watchAfterLanding = true;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("再判定を行う間隔")]
        private float _restCheckInterval = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("この速度以下なら「止まっている」とみなす")]
        private float _restSpeedThreshold = 0.2f;


        private Rigidbody _rigidbody;
        private DefenseLineGauge _defenseLine;
        private float _restCheckTimer;
        private bool _hasDamaged;

        // アイテムの種類を返す
        public override BocchaHazardType HazardType => BocchaHazardType.SpikeBall;

        // 初期化処理
        private void Awake() => _rigidbody = GetComponent<Rigidbody>();


        /// <summary>
        /// 着地した位置が危険圏なら、その時点でバリアへダメージを入れる。
        /// </summary>
        protected override void OnLanded() => TryDamageBarrier();


        protected override void Update()
        {
            base.Update();

            // 着地後の監視
            if (!_watchAfterLanding || _hasDamaged || !IsLanded) return;

            _restCheckTimer -= Time.deltaTime;

            if (_restCheckTimer > 0.0f) return;

            _restCheckTimer = _restCheckInterval;

            // 転がっている最中に反応させないよう、止まっている時だけ判定する
            if (_rigidbody != null &&
                _rigidbody.linearVelocity.sqrMagnitude > _restSpeedThreshold * _restSpeedThreshold)
            {
                return;
            }

            TryDamageBarrier();
        }


        /// <summary>
        /// 危険圏に落下していた場合、バリアへダメージを入れる
        /// </summary>
        private void TryDamageBarrier()
        {
            if (_hasDamaged) return;

            // 危険圏に入っていなければ何もしない
            if (!TryGetDangerMultiplier(transform.position, out float multiplier)) return;

            _hasDamaged = true;

            float damage = _barrierDamage * multiplier;

            PlayVfx(_barrierHitVfxPrefab);

            EventBus.Publish(new RuleBarrierAttackEvent(damage, transform.position));
            EventBus.Publish(new BocchaSpikeBallDroppedEvent(transform.position, damage));

            Debug.Log($"[SpikeBall] <color=red>危険圏に落下！ バリアへ {damage:0.#} ダメージ</color>", this);

            if (_despawnAfterDamage) ForceDespawn();
        }


        /// <summary>
        /// 危険圏内かどうかを判定する。
        /// 危険圏が配置されていない場合はDefenceLineからの距離で代用
        /// </summary>
        private bool TryGetDangerMultiplier(Vector3 position, out float multiplier)
        {
            if (BocchaBarrierDangerZone.HasAnyZone)
            {
                return BocchaBarrierDangerZone.TryGetZone(position, out multiplier);
            }

            multiplier = 1.0f;

            if (_defenseLine == null)
            {
                _defenseLine = FindFirstObjectByType<DefenseLineGauge>();
            }

            if (_defenseLine == null) return false;

            return Vector3.Distance(position, _defenseLine.transform.position) <= _fallbackDangerRadius;
        }
    }
}
