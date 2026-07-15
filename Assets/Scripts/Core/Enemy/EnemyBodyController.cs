using Game.Core.Events;
using System.Collections;
using UnityEngine;

namespace Game.Core.Enemy
{
    public class EnemyBodyController : MonoBehaviour
    {
        EnemyHitReceiver _receiver;
        string _enemyID;

        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [Tooltip("ヒット時に増加するアニメーション継続時間の最大値")]
        [SerializeField] private float _maxHitAnimationTime;
        [Tooltip("1ヒットでのアニメーション継続時間の増加量")]
        [SerializeField] private float _addHitAnimationTime;
        float _hitAnimationTime;

        [Header("Pose")]
        [SerializeField] float _dropDuration = 1.0f;
        [SerializeField] Vector3 _dropRot;
        Coroutine _dropPoseCoroutine;
        float _elapsedTime;
        Quaternion _startRot;

        private const int AttackStateLayer = 0;
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");
        private static readonly int AttackStartParameterHash = Animator.StringToHash("AttackAnimeEvent");
        private static readonly int AttackEndParameterHash = Animator.StringToHash("AttackAnimeEndEvent");

        private void Start()
        {
            var controller = GetComponentInParent<EnemyController>();
            if (controller != null)
            {
                controller.OnDropStarted += OnDropStarted;
            }
        }

        private void OnDestroy()
        {
            var controller = GetComponentInParent<EnemyController>();
            if (controller != null)
            {
                controller.OnDropStarted -= OnDropStarted;
            }

            EventBus.Unsubscribe<EnemyAttackMotionStartedEvent>(OnAttackMotionStarted);
            EventBus.Unsubscribe<EnemyAttackMotionEndedEvent>(OnAttackMotionEnded);
        }

        public void Initialize(string enemyID)
        {
            _enemyID = enemyID;
            _receiver = GetComponent<EnemyHitReceiver>();
            if (_receiver != null)
            {
                _receiver.Initialize(enemyID);
                _receiver.OnHitAction = HandleHitDamage;
            }

            EventBus.Subscribe<EnemyAttackMotionStartedEvent>(OnAttackMotionStarted);
            EventBus.Subscribe<EnemyAttackMotionEndedEvent>(OnAttackMotionEnded);
        }

        private void Update()
        {
            if (_animator == null) return;
            if (_hitAnimationTime > 0.0f)
            {
                _hitAnimationTime -= Time.deltaTime;
                _hitAnimationTime = Mathf.Max(_hitAnimationTime, 0.0f);
            }
            _animator.SetFloat("HitTime", _hitAnimationTime);
        }

        void HandleHitDamage()
        {
            _hitAnimationTime += _addHitAnimationTime;
            _hitAnimationTime = Mathf.Min(_hitAnimationTime, _maxHitAnimationTime);
            if (_animator != null) _animator.SetFloat("HitTime", _hitAnimationTime);
        }

        void OnAttackMotionStarted(EnemyAttackMotionStartedEvent motionEvent)
        {
            if (motionEvent.EnemyId != _enemyID) return;
            if (_animator == null) return;

            bool isInTransition = _animator.IsInTransition(AttackStateLayer);
            bool isCurrentStateAttack = _animator.GetCurrentAnimatorStateInfo(AttackStateLayer).shortNameHash == AttackStateHash;
            bool isNextStateAttack = isInTransition
                && _animator.GetNextAnimatorStateInfo(AttackStateLayer).shortNameHash == AttackStateHash;

            _animator.ResetTrigger(AttackEndParameterHash);

            if (isCurrentStateAttack && !isInTransition)
            {
                _animator.CrossFadeInFixedTime(AttackStateHash, 0.08f, AttackStateLayer, 0f);
            }
            else if (!isNextStateAttack)
            {
                _animator.SetTrigger(AttackStartParameterHash);
            }
        }

        void OnAttackMotionEnded(EnemyAttackMotionEndedEvent endEvent)
        {
            if (endEvent.EnemyId != _enemyID) return;
            if (_animator == null) return;

            bool isInTransition = _animator.IsInTransition(AttackStateLayer);
            bool isCurrentStateAttack = _animator.GetCurrentAnimatorStateInfo(AttackStateLayer).shortNameHash == AttackStateHash;
            bool isNextStateAttack = isInTransition
                && _animator.GetNextAnimatorStateInfo(AttackStateLayer).shortNameHash == AttackStateHash;

            _animator.ResetTrigger(AttackStartParameterHash);
            if ((!isInTransition && isCurrentStateAttack) || isNextStateAttack)
            {
                _animator.SetTrigger(AttackEndParameterHash);
            }
            else
            {
                _animator.ResetTrigger(AttackEndParameterHash);
            }
        }
        void OnDropStarted()
        {
            _elapsedTime = 0.0f;
            _startRot = transform.rotation;
            if (_dropPoseCoroutine != null) StopCoroutine(_dropPoseCoroutine);
            _dropPoseCoroutine = StartCoroutine(DropRoutine());
        }

        private IEnumerator DropRoutine()
        {
            while (_elapsedTime <= 1.0f)
            {
                _elapsedTime += Time.deltaTime / _dropDuration;
                transform.rotation = Quaternion.Euler(Vector3.Lerp(_startRot.eulerAngles, _dropRot, _elapsedTime));
                yield return null;
            }
        }
    }
}
