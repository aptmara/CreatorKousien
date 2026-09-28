/**
 * File: BalanceTrayContent_Weakness.cs
 * テンビンの弱点オブジェクトを生成する
 */
using UnityEngine;
using Game.Core.Events;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;

namespace Game.Gameplay.Enemy.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossHitReceiver))]
    public sealed class BalanceTrayContent_Weakness : BalanceTrayContentBase,IBossHittable
    {
        [Header("====== 命中 ======")]
        [SerializeField]
        private Collider _hitCollider;
        [SerializeField, Tooltip("Onなら皿が上がっている時だけ攻撃できる。OFFなら開封後は常に攻撃可能")]
        private bool _requireRaisedToExpose = true;
        [SerializeField]
        private bool _restrictRequiredType = false;
        [SerializeField]
        private CollectibleType _requiredType = CollectibleType.BossWeak;
        [SerializeField, Min(0.0f)]
        private float _hitCooldown = 0.2f;
        [SerializeField]
        private GameObject _hitVFX;

        [Header("===== あげすぎペナルティ ======")]
        [SerializeField, Tooltip("弱点の皿が上がり切った時にバリアへ入るダメージ")]
        private float _barrierDamage = 20.0f;
        [SerializeField, Tooltip("この傾きを超えたら警告イベントを一回発行")]
        [Range(0.0f, 1.0f)] private float _warningTilt = 0.7f;

        private bool _exposed;
        private bool _warned;
        private bool _penaltyFired;
        private float _nextHitTime;

        public override bool IsWeakness => true;
        public bool IsHittable => IsRevealed && _exposed && Time.time >= _nextHitTime;

        protected override void OnBound()
        {
            var reciever = GetComponent<BossHitReceiver>();
            if (reciever != null && Ctx.Boss != null)
            {
                reciever.Initialize(Ctx.Boss.Controller);
            }
        }

        protected override void OnRevealed()
        {
            _warned = false;
            _penaltyFired = false;

            if (Ctx.Beam != null)
            {
                Ctx.Beam.OnTrayFullyRaised += HandleFullyRaised;
                Ctx.Beam.OnTrayFullyLowered += HandleFullyLowered;
            }

            ApplyExposed(!_requireRaisedToExpose);
        }

        protected override void OnRoundEnd()
        {
            if (Ctx != null && Ctx.Beam != null)
            {
                Ctx.Beam.OnTrayFullyRaised -= HandleFullyRaised;
                Ctx.Beam.OnTrayFullyLowered -= HandleFullyLowered;
            }
            ApplyExposed(false);
        }

        private void OnDestroy() => OnRoundEnd();

        protected override void HandleTrayRaised() => ApplyExposed(true);

        protected override void HandleTrayLowered()
        {
            if (_requireRaisedToExpose) ApplyExposed(false);
        }

        private void ApplyExposed(bool exposed)
        {
            _exposed = exposed;
            if (_hitCollider != null) _hitCollider.enabled = exposed;
        }

        private void Update()
        {
            if (!IsRevealed || Ctx.Beam == null) return;

            float tilt = Ctx.Beam.GetTiltRatio(Side);
            if (!_warned && tilt >= _warningTilt)
            {
                _warned = true;
                EventBus.Publish(new BalanceSlamWarningEvent((int)Side));
            }
            else if (_warned && tilt < _warningTilt - 0.1f)
            {
                _warned = false;
            }
        }

        private void HandleFullyRaised(TraySide side)
        {
            if (side != Side || _penaltyFired) return;

            _penaltyFired = true;
            if (_hitVFX != null)
            {
                Destroy(Instantiate(_hitVFX,transform.position,Quaternion.identity),2.0f);
            }
            EventBus.Publish(new RuleBarrierAttackEvent(_barrierDamage, transform.position));
        }

        private void HandleFullyLowered(TraySide side)
        {
            if (side != Side) return;
            _penaltyFired = false;
        }

        public void OnHit(float damage,Vector3 hitPosition,CollectibleObject collectible)
        {
            if (_restrictRequiredType && collectible != null && collectible.Type != _requiredType) return;

            _nextHitTime = Time.time + _hitCooldown;

            BossBattleFlowController flow = Ctx?.Boss?.Controller;
            if(flow == null) return;

            if (!flow.IsDown) flow.TriggerDown();
            flow.TakeDamage(damage);

        }
    }
}
