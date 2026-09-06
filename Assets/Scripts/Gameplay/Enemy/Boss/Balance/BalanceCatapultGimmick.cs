using Game.Core.Enemy;
using Game.Core.Events;
using System.Collections.Generic;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{

    [CreateAssetMenu(fileName = "BalanceCatapultGimmick", menuName = "Boss/Gimmick/Balance/Catapult")]
    public class BalanceCatapultGimmick : BossGimmickSO 
    {
        [Header("==== 攻撃皿 ====")]
        [SerializeField] private TraySide _attackSide = TraySide.Right;

        [Tooltip("攻撃皿が上がる速さ")]
        [SerializeField] private float _biasStrength = 6.0f;

        [Header("===== 妨害敵 =====")]
        [SerializeField] private List<EnemyDefinition> _defenderDefinitions = new List<EnemyDefinition>();
        [SerializeField] private float _defenderSpawnInterval = 4.0f;
        [SerializeField] private float _defenderHpRate = 1.0f;

        private EnemySpawner _enemySpawner;

        [Header("==== 鉄球 =====")]
        [Tooltip("鉄球が乗るまでの時間")]
        [SerializeField] private float _ironBallDelay = 8.0f;
        [Tooltip("空皿がここまで上がっていれば「逆転成功」")]
        [SerializeField, Range(0.0f, 1.0f)] private float _reverseThreshold = 0.9f;
        [SerializeField] private GameObject _ironBallPrefab;
        [SerializeField] private float _ballBossDamage = 50.0f;
        [SerializeField] private float _ballBarrierDamage = 25.0f;

        [Header("===== 開始演出(振り落とし) ======")]
        [SerializeField] private Animator _beamAnimator;
        [SerializeField] private string _shakeOffTriggerName = "ShakeOff";
        [Tooltip("振り落としアニメーションの再生時間")]
        [SerializeField] private float _shakeOffDuration = 0.8f; 

        private BossBalanceBeamController _beam;
        private RealisticBalanceScale _scale;
        private TraySide _emptySide;

        private float _defenderTimer;
        private float _ballTimer;
        private bool _hasResolved;
        private bool _isComplete;

        private bool _isShakingOff;
        private float _shakeOffTimer;

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
            _ballTimer = 0.0f;
            _hasResolved = false;
            _isComplete = false;

            _scale.ClearAllWeghts();
            _scale.ResetToLevel();

            if(_beamAnimator != null && !string.IsNullOrEmpty(_shakeOffTriggerName))
            {
                _beamAnimator.SetTrigger(_shakeOffTriggerName);
            }

                _isShakingOff = true;
            _shakeOffTimer = 0.0f;

            EventBus.Publish(new BossAttackWarningStartedEvent());
        }

        public override void Tick(float dt)
        {
            if (_isShakingOff)
            {
                _shakeOffTimer += dt;
                if (_shakeOffTimer >= _shakeOffDuration)
                {
                    _isShakingOff = false;

                    _scale.SetExternalBias(_attackSide == TraySide.Left ? -_biasStrength : _biasStrength);
                }
                return;
            }

            if (_hasResolved) return;

            _defenderTimer += dt;
            float scaledInterval = _defenderSpawnInterval * Context.PhaseMultipliers.SpawnIntervalMultiplier;
            if(_defenderTimer >= scaledInterval)
            {
                _defenderTimer = 0.0f;
                SpawnDefender();
            }

            _ballTimer += dt;
            if(_ballTimer >= _ironBallDelay)
            {
                ResolveIronBall();
            }

            // BlendShapeで攻撃側の皿をとげとげにする
            if (_emptySide == TraySide.Left)
            {
                var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
                LeftMesh?.SetBlendShapeWeight(0, 100.0f);
                var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
                RightMesh?.SetBlendShapeWeight(0, 0.0f);
            }
            else
            {
                var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
                LeftMesh?.SetBlendShapeWeight(0, 0.0f);
                var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
                RightMesh?.SetBlendShapeWeight(0, 100.0f);
            }
        }

        private void SpawnDefender()
        {
            if (_enemySpawner == null || _beam == null || _defenderDefinitions.Count == 0) return;

            var definition = _defenderDefinitions[UnityEngine.Random.Range(0, _defenderDefinitions.Count)];
            Transform socket = _beam.GetTraySocket(_attackSide);
            Vector3 SocketOffsetPos = socket.position;
            SocketOffsetPos.z += 3.6f;       
            _enemySpawner.TrySpawnEnemyAt(definition, SocketOffsetPos, _defenderHpRate, 1.0f, out _);
        }

        private void ResolveIronBall()
        {
            _hasResolved = true;

            bool reversed = _beam != null && _beam.GetTiltRatio(_emptySide) >= _reverseThreshold;

            if (reversed)
            {
                Context.Controller.TakeDamage(_ballBossDamage);
            }
            else
            {
                if(_ironBallPrefab != null && _beam != null)
                {
                    Transform socket = _beam.GetTraySocket(_emptySide);
                    Instantiate(_ironBallPrefab,socket.position, Quaternion.identity);
                }

                if(_beam != null)
                {
                    EventBus.Publish(new RuleBarrierAttackEvent(_ballBarrierDamage, _beam.GetTraySocket(_emptySide).position));
                }
            }

            Complete();
        }

        private void Complete()
        {
            _scale.SetExternalBias(0.0f);

            // 皿の見た目を平らに戻す
            var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
            LeftMesh?.SetBlendShapeWeight(0, 100.0f);
            var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
            RightMesh?.SetBlendShapeWeight(0, 100.0f);
            EventBus.Publish(new BossAttackWarningEndedEvent());
            _isComplete = true;
        }

        public override void Cancel()
        {
            _scale.SetExternalBias(0.0f);

            // 皿の見た目を平らに戻す
            var LeftMesh = Context.GetSocket(BossSocket.LeftHand).GetComponentInParent<SkinnedMeshRenderer>();
            LeftMesh?.SetBlendShapeWeight(0, 100.0f);
            var RightMesh = Context.GetSocket(BossSocket.RightHand).GetComponentInParent<SkinnedMeshRenderer>();
            RightMesh?.SetBlendShapeWeight(0, 100.0f);

            EventBus.Publish(new BossAttackWarningEndedEvent());
        }
    }

}
