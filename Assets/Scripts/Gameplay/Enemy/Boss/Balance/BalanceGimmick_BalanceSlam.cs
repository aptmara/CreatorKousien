using System;
using UnityEngine;
using Game.Core.Events;
using Game.Gameplay.Collectibles;

namespace Game.Gameplay.Enemy.Boss
{
    [CreateAssetMenu(fileName = "Gimmick_BalanceSlam", menuName = "Boss/Gimmicks/Balance/Slam")]
    public class BalanceSlamGimmick : BossGimmickSO
    {
        [Header("--- 挑戦設定 ---")]
        [SerializeField] private float _challengeDuration = 10f;
        [Tooltip("この側が振り切るとバリアに大ダメージが入る")]
        [SerializeField] private TraySide _dangerSide = TraySide.Right;

        [Header("--- バリアダメージ ---")]
        [SerializeField] private float _slamBarrierDamage = 40f;

        [Header("--- クリア報酬 ---")]
        [Tooltip("完走に対する固定報酬個数")]
        [SerializeField] private int _baseRewardCount = 2;
        [Tooltip("振り切られなかった割合に応じて上乗せする最大個数")]
        [SerializeField] private int _bonusRewardCount = 4;
        [SerializeField] private Vector3 _rewardSpawnOffset = new Vector3(0f, 1f, 0f);

        [Header("==== 自動傾斜(抑制対象) =====")]
        [SerializeField] private RealisticBalanceScale _scale;
        [SerializeField] private float _biasStrength = 8.0f;

        [Header("==== 警告(演出フック) ====")]
        [Tooltip("この割合を超えたら警告イベントを一回だけ発行")]
        [SerializeField, Range(0.0f, 1.0f)] private float _warningThreshold = 0.7f;
        private bool _hasWarned;

        private BossBalanceBeamController _beam;
        private CollectibleSpawner _collectibleSpawner;

        private float _elapsed;
        private float _safeTime;
        private bool _hasDamagedThisRaise;
        private bool _isComplete;

        public override bool IsComplete => _isComplete;
        public override bool IsTick => true;

        public override void Initialize(BossContext context)
        {
            base.Initialize(context);
            _beam = context.Transform.GetComponentInChildren<BossBalanceBeamController>();
            _scale = context.Transform.GetComponentInChildren<RealisticBalanceScale>();
            _collectibleSpawner = UnityEngine.Object.FindFirstObjectByType<CollectibleSpawner>();
        }

        public override void Execute()
        {
            _elapsed = 0f;
            _safeTime = 0f;
            _hasDamagedThisRaise = false;
            _hasWarned = false;
            _isComplete = false;

            _scale.SetExternalBias(_dangerSide == TraySide.Left ? -_biasStrength : _biasStrength);

            EventBus.Publish(new BossAttackWarningStartedEvent());

            if (_beam != null)
            {
                _beam.OnTrayFullyRaised += HandleFullyRaised;
                _beam.OnTrayFullyLowered += HandleFullyLowered;
            }
        }

        public override void Tick(float dt)
        {
            _elapsed += dt;

            float tilt = _beam != null ? _beam.GetTiltRatio(_dangerSide) : 0.0f;

            if (tilt < 1.0f) _safeTime += dt;

            if (!_hasWarned && tilt >= _warningThreshold)
            {
                _hasWarned = true;
                EventBus.Publish(new BalanceSlamWarningEvent((int)_dangerSide));
            }
            else if (_hasWarned && tilt < _warningThreshold - 0.1f)
            {
                _hasWarned = false;
            }

            if (_elapsed >= _challengeDuration)
            {
                Complete();
            }
        }

        private void HandleFullyRaised(TraySide side)
        {
            if (side != _dangerSide || _hasDamagedThisRaise) return;

            _hasDamagedThisRaise = true;

            Vector3 hitPosition = _beam.GetTraySocket(side).position;
            EventBus.Publish(new RuleBarrierAttackEvent(_slamBarrierDamage, hitPosition));
        }

        private void HandleFullyLowered(TraySide side)
        {
            if (side != _dangerSide) return;

            // 振り切りが解除されたら、次に振り切った時また1回ダメージを与えられるようにする
            _hasDamagedThisRaise = false;
        }

        private void Complete()
        {
            _scale.SetExternalBias(0.0f);
            Unsubscribe();
            EventBus.Publish(new BossAttackWarningEndedEvent());

            float safeRatio = _challengeDuration > 0f ? Mathf.Clamp01(_safeTime / _challengeDuration) : 0f;
            GrantReward(safeRatio);

            _isComplete = true;
        }

        private void GrantReward(float safeRatio)
        {
            if (_collectibleSpawner == null) return;

            int bonus = Mathf.RoundToInt(_bonusRewardCount * safeRatio);
            int count = Mathf.Max(0, _baseRewardCount + bonus);
            if (count <= 0) return;

            Vector3 spawnPosition = Context.Transform.position + _rewardSpawnOffset;
            _collectibleSpawner.SpawnCollectiblesAt(spawnPosition, count);
        }

        public override void Cancel()
        {
            _scale.SetExternalBias(0.0f);
            Unsubscribe();
            EventBus.Publish(new BossAttackWarningEndedEvent());
        }

        private void Unsubscribe()
        {
            if (_beam == null) return;
            _beam.OnTrayFullyRaised -= HandleFullyRaised;
            _beam.OnTrayFullyLowered -= HandleFullyLowered;
        }
    }
}
