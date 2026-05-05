// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;
using System.Collections;

namespace Game.Presentation.CameraFeedback
{
    /// <summary>
    /// 大量解放時や崩落時にカメラを揺らすフィードバックコントローラ。
    /// </summary>
    public class CameraFeedbackController : MonoBehaviour
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeMagnitude = 0.2f;

        private Vector3 _originalPos;
        private Coroutine _shakeCoroutine;

        private void Start()
        {
            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_targetCamera != null) _originalPos = _targetCamera.transform.localPosition;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PayloadReleasedEvent>(OnPayloadReleased);
            EventBus.Subscribe<StageTiltStartedEvent>(OnStageTiltStarted);
            EventBus.Subscribe<EnemyDownStartedEvent>(OnEnemyDownStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PayloadReleasedEvent>(OnPayloadReleased);
            EventBus.Unsubscribe<StageTiltStartedEvent>(OnStageTiltStarted);
            EventBus.Unsubscribe<EnemyDownStartedEvent>(OnEnemyDownStarted);
        }

        private void OnPayloadReleased(PayloadReleasedEvent ev)
        {
            if (ev.PayloadCount >= 10) // 10個以上解放で揺らす（仮）
            {
                TriggerShake(_shakeDuration, _shakeMagnitude * (ev.PayloadCount / 10f));
            }
        }

        private void OnStageTiltStarted(StageTiltStartedEvent ev)
        {
            TriggerShake(_shakeDuration * 2f, _shakeMagnitude * 2f);
        }

        private void OnEnemyDownStarted(EnemyDownStartedEvent ev)
        {
            TriggerShake(_shakeDuration, _shakeMagnitude * 1.5f);
        }

        private void TriggerShake(float duration, float magnitude)
        {
            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                if (_targetCamera != null)
                {
                    float x = Random.Range(-1f, 1f) * magnitude;
                    float y = Random.Range(-1f, 1f) * magnitude;
                    _targetCamera.transform.localPosition = new Vector3(_originalPos.x + x, _originalPos.y + y, _originalPos.z);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_targetCamera != null)
            {
                _targetCamera.transform.localPosition = _originalPos;
            }
        }
    }
}
