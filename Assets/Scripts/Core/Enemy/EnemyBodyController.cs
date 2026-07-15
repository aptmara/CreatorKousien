using Game.Core.Enemy;
using Game.Core.Events;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.InputSystem.PlayerInput;

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

        int _test = 0;
        int _test2 = 0;

        private int _attackStateLayer = 0; // 攻撃ステートがあるレイヤー（通常0）
        private static readonly int AttackStateHash = Animator.StringToHash("Attack");
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

            EventBus.Subscribe<EnemyAttackMotionStartedEvent>(OnAttackMotion);
            EventBus.Subscribe<EnemyAttackMotionEndedEvent>(OnAttackMotionEnd);
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

        void OnAttackMotion(EnemyAttackMotionStartedEvent motionEvent)
        {
            if(motionEvent.EnemyId != _enemyID) return;
            if (_animator == null) return;

            // 現在のステート情報を取得
            var state = _animator.GetCurrentAnimatorStateInfo(0);

            // 現在ステートが攻撃ステートか（shortNameHashで比較）
            bool isInAttackState = state.shortNameHash == AttackStateHash;

            // トランジション中かどうか（必要に応じて確認）
            bool isInTransition = _animator.IsInTransition(0);

            if (isInAttackState && !isInTransition)
            {
                _test2++;
                // 攻撃ステート名が指定されていればそのステートを先頭から強制再生（リスタート）
                Debug.Log(_test2 + "回目の攻撃モーションを再起動");
                _animator.CrossFadeInFixedTime(AttackStateHash, 0.08f, _attackStateLayer, 0f);
                // 即時反映
                _animator.Update(0f);
            }
            else
            {
                _test++;
                Debug.Log(_test + "回目の攻撃モーションイベントを発行されました");
                _animator.SetTrigger("AttackAnimeEvent");
            }
        }

        void OnAttackMotionEnd(EnemyAttackMotionEndedEvent endEvent)
        {
            if (endEvent.EnemyId != _enemyID) return;

            _animator.SetTrigger("AttackAnimeEndEvent");

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
