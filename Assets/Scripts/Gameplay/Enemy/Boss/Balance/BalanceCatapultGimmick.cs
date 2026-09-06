using Game.Core.Enemy;
using Game.Core.Events;
using System.Collections.Generic;
using TMPro.EditorUtilities;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{

    [CreateAssetMenu(fileName = "BalanceCatapultGimmick", menuName = "Boss/Gimmick/Balance/Catapult")]
    public class BalanceCatapultGimmick : BossGimmickSO 
    {
        private enum Phase { ShakeOff,Prep,BallBurst,Resolve,Done}

        [Header("==== 攻撃皿 ====")]
        [SerializeField] private TraySide _attackSide = TraySide.Right;

        [Header("===== 妨害敵 =====")]
        [SerializeField] private List<EnemyDefinition> _defenderDefinitions = new List<EnemyDefinition>();
        [SerializeField] private float _defenderSpawnInterval = 4.0f;
        [SerializeField] private float _defenderHpRate = 1.0f;
        [SerializeField, Tooltip("既存の敵との最低距離")] private float _defenderMinDistance = 2.0f;
        [SerializeField] GameObject _spawnerVfxPrefab;

        [Header("==== 鉄球 =====")]
        [Tooltip("鉄球が乗るまでの時間")]
        [SerializeField] private float _ironBallDelay = 8.0f;
        [Tooltip("挑戦中に追加される鉄球の総数")]
        [SerializeField] private int _ballCount = 4;
        [SerializeField] private GameObject _ironBallPrefab;
        [SerializeField] private float _settleDuration = 1.0f;
        [Tooltip("空皿がここまで上がっていれば「逆転成功」")]
        [SerializeField, Range(0.0f, 1.0f)] private float _reverseThreshold = 0.9f;
        [SerializeField] private float _ballBossDamage = 50.0f;
        [SerializeField] private float _ballBarrierDamage = 25.0f;

        [Header("===== 開始演出(振り落とし) ======")]
        [SerializeField] private Animator _beamAnimator;
        [SerializeField] private string _shakeOffTriggerName = "ShakeOff";
        [Tooltip("振り落としアニメーションの再生時間")]
        [SerializeField] private float _shakeOffDuration = 0.8f;

        [Header("===== 振り切り攻撃 ======")]
        [SerializeField] private Animator _bossAnimator;
        [SerializeField] private string _barrierAttackTriggerName = "BarrierSlam";
        [SerializeField] private float _fullyRaisedBarrierDamage = 15.0f;

        [Header("==== 演出 =====")]
        [SerializeField] private GameObject _barrierAttackVfxPrefab;
        [SerializeField] private float _vfxLifetime = 2.0f;

        private BossBalanceBeamController _beam;
        private RealisticBalanceScale _scale;
        private EnemySpawner _enemySpawner;
        private TraySide _emptySide;

        private GameObject _currentSpawnerVfx;

        private Phase _phase;
        private float _phaseTimer;
        private float _defenderTimer;
        private bool _hasDamagedThisRaise;
        private bool _isComplete;

        public override bool IsComplete => _isComplete;
        public override bool IsTick => true;

        public override void Initialize(BossContext context)
        {
            base.Initialize(context);
            _beam = context.Transform.GetComponentInChildren<BossBalanceBeamController>();
            _scale = context.Transform.GetComponentInChildren<RealisticBalanceScale>();
            _enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
        }

        public override void Execute()
        {
            _emptySide = _attackSide == TraySide.Left ? TraySide.Right : TraySide.Left;
            _defenderTimer = 0.0f;
            _hasDamagedThisRaise = false;
            _isComplete = false;

            _scale.ClearAllWeghts();
            _scale.ResetToLevel();

            if(_beamAnimator != null && !string.IsNullOrEmpty(_shakeOffTriggerName))
            {
                _beamAnimator.SetTrigger(_shakeOffTriggerName);
            }

            if(_beam != null)
            {
                _beam.OnTrayFullyRaised += HandleFullyRaised;
                _beam.OnTrayFullyLowered += HandleFullyLowered;
            }
            // BlendShapeで攻撃側の皿をとげとげにする
            if (_emptySide == TraySide.Left)
            {
                var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
                LeftMesh?.SetBlendShapeWeight(0, 100.0f);
                var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
                RightMesh?.SetBlendShapeWeight(0,  0.0f);
            }
            if (_emptySide == TraySide.Right)
            {
                var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
                LeftMesh?.SetBlendShapeWeight(0, 0.0f);
                var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
                RightMesh?.SetBlendShapeWeight(0, 100.0f);
            }

            EventBus.Publish(new BossAttackWarningStartedEvent());

            ChangePhase(Phase.ShakeOff);
        }

        public override void Tick(float dt)
        {
           _phaseTimer += dt;

            switch (_phase)
            {
                case Phase.ShakeOff:
                    TickShakeOf();
                    break;
                case Phase.Prep:
                    TickPrep(dt);
                    break;
                case Phase.BallBurst:
                    if (_phaseTimer >= _settleDuration)
                    {
                        ChangePhase(Phase.Resolve);
                    }
                    break;
                case Phase.Resolve:
                    ResolveOutcome();
                    break;
            }

        }

        private void ChangePhase(Phase phase)
        {
            _phase = phase;
            _phaseTimer = 0.0f;

            if(phase == Phase.BallBurst)
            {
                DropAllIronBalls();
            }
        }

        private void TickShakeOf()
        {
            if(_phaseTimer >= _shakeOffDuration)
            {
                _currentSpawnerVfx = Instantiate(_spawnerVfxPrefab, Context.GetSocket(
                    _attackSide == TraySide.Left ? BossSocket.LeftHand : BossSocket.RightHand).transform);
                ChangePhase(Phase.Prep);
            }
        }

        private void TickPrep(float dt)
        {
            _defenderTimer += dt;
            float scaledInterval = _defenderSpawnInterval * Context.PhaseMultipliers.SpawnIntervalMultiplier;
            if(_defenderTimer >= scaledInterval)
            {
                _defenderTimer = 0.0f;
                SpawnDefender();
            }

            if(_phaseTimer >= _ironBallDelay)
            {
                ChangePhase(Phase.BallBurst);
            }
        }

        private void SpawnDefender()
        {
            if (_enemySpawner == null || _beam == null || _defenderDefinitions.Count == 0) return;

            var definition = _defenderDefinitions[UnityEngine.Random.Range(0, _defenderDefinitions.Count)];
            Transform socket = _beam.GetTraySocket(_attackSide);

            _enemySpawner.TrySpawnEnemy(definition, _defenderHpRate, 1.0f,_defenderMinDistance, out _);
        }

        private void DropAllIronBalls()
        {
            if (_ironBallPrefab == null || _beam == null) return;

            Transform socket = _beam.GetTraySocket(_emptySide);

            for(int i = 0;i < _ballCount;++i)
            {
                Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.4f;
                offset.y = Mathf.Abs(offset.y) + i * 0.15f;
                var obj = Instantiate(_ironBallPrefab,socket.position + offset,Quaternion.identity);
                if(obj.TryGetComponent<BalanceCatapultBall>(out BalanceCatapultBall ball))
                {
                    ball.Initialize(Context);
                }
            }

            EventBus.Publish(new CameraShakeRequestedEvent());

            Complete();
        }

        private void ResolveOutcome()
        {
            bool reversed = _beam != null && _beam.GetTiltRatio(_emptySide) >= _reverseThreshold;

            if(reversed)
            {
                Context.Controller.TakeDamage(_ballBarrierDamage);
            }
            else
            {
                Vector3 hitPosition = _beam.GetTraySocket(_emptySide).position;
                PlayBarrierAttackVfx(hitPosition);
                EventBus.Publish(new RuleBarrierAttackEvent(_ballBarrierDamage, hitPosition));
            }

            Complete();
        }

        private void HandleFullyRaised(TraySide side)
        {
            if (side != _attackSide || _hasDamagedThisRaise) return;
            _hasDamagedThisRaise = true;

            Vector3 hitPosition = _beam.GetTraySocket(side).position;

            if (_bossAnimator != null && !string.IsNullOrEmpty(_barrierAttackTriggerName))
                _bossAnimator.SetTrigger(_barrierAttackTriggerName);

            // ここでだけカタパルト処理            

            PlayBarrierAttackVfx(hitPosition);
            EventBus.Publish(new RuleBarrierAttackEvent(_fullyRaisedBarrierDamage,hitPosition));

            Complete();
        }

        private void HandleFullyLowered(TraySide side)
        {
            if(side != _attackSide) return;
            _hasDamagedThisRaise = false;
        }

        private void PlayBarrierAttackVfx(Vector3 position)
        {
            if (_barrierAttackVfxPrefab == null) return;
            var vfx = Instantiate(_barrierAttackVfxPrefab, position, Quaternion.identity);
            Object.Destroy(vfx, _vfxLifetime);
        }

        private void Complete()
        {
            Unsubscribe();
            EventBus.Publish(new BossAttackWarningEndedEvent());
            _isComplete = true;
        }

        public override void Cancel()
        {
            Unsubscribe();
            EventBus.Publish(new BossAttackWarningEndedEvent());
        }

        private void Unsubscribe()
        {
            if (_beam == null) return;
            if (_currentSpawnerVfx) Destroy(_currentSpawnerVfx);
            // 皿の見た目を平らに戻す
            var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
            LeftMesh?.SetBlendShapeWeight(0, 100.0f);
            var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
            RightMesh?.SetBlendShapeWeight(0, 100.0f);

            _beam.OnTrayFullyRaised -= HandleFullyRaised;
            _beam.OnTrayFullyLowered -= HandleFullyLowered;
        }
    }

}
