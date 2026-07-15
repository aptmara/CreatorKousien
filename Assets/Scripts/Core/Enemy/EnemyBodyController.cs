using System;
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
        [SerializeField, Min(0f)] float _postFlipDelay = 0.25f;
        Coroutine _dropPoseCoroutine;

        [Header("Cartoon Feedback")]
        [SerializeField] bool _useCartoonFeedback;
        [SerializeField] Transform _squashTarget;
        [SerializeField, Min(0f)] float _hitSquashDuration = 0.06f;
        [SerializeField, Min(0f)] float _hitStretchDuration = 0.07f;
        [SerializeField, Min(0f)] float _hitRecoverDuration = 0.09f;
        [SerializeField] Vector3 _hitSquashScale = new Vector3(1.12f, 0.82f, 1.12f);
        [SerializeField] Vector3 _hitStretchScale = new Vector3(0.94f, 1.12f, 0.94f);
        [SerializeField] Vector3 _hitRecoilRotation = new Vector3(-10f, 5f, 12f);
        [SerializeField] Vector3 _hitReboundRotation = new Vector3(4f, -2f, -5f);
        [SerializeField, Min(0f)] float _deathAnticipationDuration = 0.08f;
        [SerializeField] Vector3 _deathSquashScale = new Vector3(1.18f, 0.72f, 1.18f);
        [SerializeField] Vector3 _deathStretchScale = new Vector3(0.88f, 1.18f, 0.88f);
        [SerializeField] Vector3 _flipOvershoot;
        [SerializeField, Min(0f)] float _deathWobbleAngle = 6f;
        [SerializeField, Min(0)] int _deathWobbleCycles = 2;

        bool _enableCartoonFeedback;
        bool _isPlayingDropPose;
        Quaternion _baseBodyRotation;
        Quaternion _hitRotationOffset = Quaternion.identity;
        int _hitDirection = 1;
        Vector3 _baseVisualScale;
        Vector3 _visualScaleMultiplier = Vector3.one;
        Coroutine _hitFeedbackCoroutine;

        public void Initialize(string enemyID, bool allowCartoonFeedback)
        {
            _enemyID = enemyID;
            _enableCartoonFeedback = _useCartoonFeedback && allowCartoonFeedback;
            _baseBodyRotation = transform.localRotation;

            if (_enableCartoonFeedback && _squashTarget == null)
            {
                _squashTarget = FindChildByName(transform, "root");
            }

            if (_squashTarget != null)
            {
                _baseVisualScale = _squashTarget.localScale;
            }

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

        private void LateUpdate()
        {
            if (!_enableCartoonFeedback) return;

            if (_squashTarget != null)
            {
                _squashTarget.localScale = Vector3.Scale(_baseVisualScale, _visualScaleMultiplier);
            }

            if (!_isPlayingDropPose)
            {
                transform.localRotation = _baseBodyRotation * _hitRotationOffset;
            }
        }

        void HandleHitDamage()
        {
            _hitAnimationTime += _addHitAnimationTime;
            _hitAnimationTime = Mathf.Min(_hitAnimationTime, _maxHitAnimationTime);
            if (_animator != null) _animator.SetTrigger("HitAnimeEvent");

            if (_enableCartoonFeedback && !_isPlayingDropPose)
            {
                if (_hitFeedbackCoroutine != null) StopCoroutine(_hitFeedbackCoroutine);
                _visualScaleMultiplier = Vector3.one;
                _hitRotationOffset = Quaternion.identity;
                _hitDirection *= -1;
                _hitFeedbackCoroutine = StartCoroutine(HitFeedbackRoutine());
            }
        }

        public void PlayDropPose(Action onCompleted)
        {
            if (_dropPoseCoroutine != null) StopCoroutine(_dropPoseCoroutine);
            if (_hitFeedbackCoroutine != null)
            {
                StopCoroutine(_hitFeedbackCoroutine);
                _hitFeedbackCoroutine = null;
            }

            _isPlayingDropPose = true;
            _visualScaleMultiplier = Vector3.one;
            _hitRotationOffset = Quaternion.identity;
            transform.localRotation = _baseBodyRotation;
            _dropPoseCoroutine = StartCoroutine(DropRoutine(onCompleted));
        }

        private IEnumerator HitFeedbackRoutine()
        {
            Quaternion recoilRotation = CreateDirectionalRotation(_hitRecoilRotation, _hitDirection);
            Quaternion reboundRotation = CreateDirectionalRotation(_hitReboundRotation, _hitDirection);

            yield return AnimateHitPose(Vector3.one, _hitSquashScale, Quaternion.identity, recoilRotation, _hitSquashDuration);
            yield return AnimateHitPose(_hitSquashScale, _hitStretchScale, recoilRotation, reboundRotation, _hitStretchDuration);
            yield return AnimateHitPose(_hitStretchScale, Vector3.one, reboundRotation, Quaternion.identity, _hitRecoverDuration);

            _visualScaleMultiplier = Vector3.one;
            _hitRotationOffset = Quaternion.identity;
            _hitFeedbackCoroutine = null;
        }

        private IEnumerator DropRoutine(Action onCompleted)
        {
            Quaternion startRotation = transform.localRotation;
            Quaternion targetRotation = Quaternion.Euler(_dropRot);

            if (!_enableCartoonFeedback)
            {
                float rotationElapsedTime = 0f;
                while (rotationElapsedTime < _dropDuration)
                {
                    rotationElapsedTime += Time.deltaTime;
                    float progress = _dropDuration > 0f ? Mathf.Clamp01(rotationElapsedTime / _dropDuration) : 1f;
                    transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.SmoothStep(0f, 1f, progress));
                    yield return null;
                }

                transform.localRotation = targetRotation;
                if (_postFlipDelay > 0f) yield return new WaitForSeconds(_postFlipDelay);

                _dropPoseCoroutine = null;
                onCompleted?.Invoke();
                yield break;
            }

            if (_enableCartoonFeedback)
            {
                yield return AnimateVisualScale(Vector3.one, _deathSquashScale, _deathAnticipationDuration);
            }

            float elapsedTime = 0f;
            Quaternion overshootRotation = targetRotation * Quaternion.Euler(_flipOvershoot);
            float overshootDuration = _dropDuration * 0.72f;
            float settleDuration = Mathf.Max(0f, _dropDuration - overshootDuration);

            while (elapsedTime < overshootDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = overshootDuration > 0f ? Mathf.Clamp01(elapsedTime / overshootDuration) : 1f;
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                if (_enableCartoonFeedback && easedProgress > 0.8f)
                {
                    transform.localRotation = Quaternion.Slerp(targetRotation, overshootRotation, (easedProgress - 0.8f) / 0.2f);
                }
                else
                {
                    transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, Mathf.Min(easedProgress / 0.8f, 1f));
                }

                if (_enableCartoonFeedback)
                {
                    _visualScaleMultiplier = Vector3.LerpUnclamped(_deathSquashScale, _deathStretchScale, easedProgress);
                }
                yield return null;
            }

            elapsedTime = 0f;
            while (_enableCartoonFeedback && elapsedTime < settleDuration)
            {
                elapsedTime += Time.deltaTime;
                float progress = settleDuration > 0f ? Mathf.Clamp01(elapsedTime / settleDuration) : 1f;
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                transform.localRotation = Quaternion.Slerp(overshootRotation, targetRotation, easedProgress);
                _visualScaleMultiplier = Vector3.LerpUnclamped(_deathStretchScale, Vector3.one, easedProgress);
                yield return null;
            }

            transform.localRotation = targetRotation;
            _visualScaleMultiplier = Vector3.one;

            if (_postFlipDelay > 0f)
            {
                if (_enableCartoonFeedback && _deathWobbleAngle > 0f && _deathWobbleCycles > 0)
                {
                    elapsedTime = 0f;
                    while (elapsedTime < _postFlipDelay)
                    {
                        elapsedTime += Time.deltaTime;
                        float progress = Mathf.Clamp01(elapsedTime / _postFlipDelay);
                        float damping = 1f - progress;
                        float wobble = Mathf.Sin(progress * Mathf.PI * 2f * _deathWobbleCycles) * _deathWobbleAngle * damping;
                        transform.localRotation = targetRotation * Quaternion.Euler(0f, 0f, wobble);
                        yield return null;
                    }
                    transform.localRotation = targetRotation;
                }
                else
                {
                    yield return new WaitForSeconds(_postFlipDelay);
                }
            }

            _dropPoseCoroutine = null;
            onCompleted?.Invoke();
        }

        private IEnumerator AnimateVisualScale(Vector3 from, Vector3 to, float duration)
        {
            if (_squashTarget == null || duration <= 0f)
            {
                _visualScaleMultiplier = to;
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                _visualScaleMultiplier = Vector3.LerpUnclamped(from, to, Mathf.SmoothStep(0f, 1f, progress));
                yield return null;
            }

            _visualScaleMultiplier = to;
        }

        private IEnumerator AnimateHitPose(
            Vector3 fromScale,
            Vector3 toScale,
            Quaternion fromRotation,
            Quaternion toRotation,
            float duration)
        {
            if (duration <= 0f)
            {
                _visualScaleMultiplier = toScale;
                _hitRotationOffset = toRotation;
                yield break;
            }

            float elapsedTime = 0f;
            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsedTime / duration));
                _visualScaleMultiplier = Vector3.LerpUnclamped(fromScale, toScale, progress);
                _hitRotationOffset = Quaternion.SlerpUnclamped(fromRotation, toRotation, progress);
                yield return null;
            }

            _visualScaleMultiplier = toScale;
            _hitRotationOffset = toRotation;
        }

        private static Quaternion CreateDirectionalRotation(Vector3 eulerAngles, int direction)
        {
            return Quaternion.Euler(eulerAngles.x, eulerAngles.y * direction, eulerAngles.z * direction);
        }

        private static Transform FindChildByName(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }

                Transform found = FindChildByName(child, childName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
