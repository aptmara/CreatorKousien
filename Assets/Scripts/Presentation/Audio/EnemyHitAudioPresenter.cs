using Game.Core.Events;
using UnityEngine;

namespace Game.Presentation.Audio
{
    [DisallowMultipleComponent]
    public sealed class EnemyHitAudioPresenter : MonoBehaviour
    {
        private const string HitEventName = "Play_CollectableHit";
        private const string HitStreakRtpcName = "HitStreak";

        [Header("連続ヒット")]
        [SerializeField, Min(0f)] private float _streakWindowSeconds = 0.6f;
        [SerializeField, Min(1)] private int _maxStreak = 8;

        private int _currentStreak;
        private float _lastHitTime = float.NegativeInfinity;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            _currentStreak = 0;
            _lastHitTime = float.NegativeInfinity;
        }

        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            if (ev.HitCount <= 0)
            {
                return;
            }

            float currentTime = Time.time;
            if (currentTime - _lastHitTime <= _streakWindowSeconds)
            {
                _currentStreak += ev.HitCount;
            }
            else
            {
                _currentStreak = ev.HitCount;
            }

            _currentStreak = Mathf.Clamp(_currentStreak, 1, _maxStreak);
            _lastHitTime = currentTime;

            AkUnitySoundEngine.SetRTPCValue(HitStreakRtpcName, _currentStreak, gameObject);
            AkUnitySoundEngine.PostEvent(HitEventName, gameObject);
        }
    }
}
