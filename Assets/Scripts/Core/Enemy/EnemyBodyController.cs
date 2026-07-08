using Game.Core.Enemy;
using UnityEngine;
using Game.Core.Events;
using Unity.Mathematics;
using System.Collections;

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

        private void OnEnable()
        {
            var controller = GetComponentInParent<EnemyController>();
            if (controller != null)
            {
                controller.OnDropStarted += OnDropStarted;
            }
        }

        private void OnDisable()
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
            if (_animator != null) _animator.SetTrigger("HitAnimeEvent");
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
