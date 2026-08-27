using Game.Core.Events;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Presentation.Audio
{
    [DisallowMultipleComponent]
    public sealed class EnemyHitAudioPresenter : MonoBehaviour
    {
        [Header("落ちもの別ヒットSE")]
        [SerializeField] private AK.Wwise.Event _candyHitEvent;
        [SerializeField] private AK.Wwise.Event _togeHitEvent;
        [SerializeField] private AK.Wwise.Event _poisonHitEvent;
        [SerializeField] private AK.Wwise.Event _iceHitEvent;
        [SerializeField] private AK.Wwise.Event _gummyHitEvent;

        [Header("連続ヒット")]
        [SerializeField, Min(0f)] private float _streakWindowSeconds = 0.6f;
        [SerializeField, Min(1)] private int _maxStreak = 8;
        [SerializeField] private AK.Wwise.RTPC _hitStreakRtpc;

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
            if (ev.HitCount <= 0 || ev.ItemDataRaw is not CollectibleData collectibleData)
            {
                return;
            }

            AK.Wwise.Event hitEvent = GetHitEvent(collectibleData.Type);
            if (hitEvent == null || !hitEvent.IsValid())
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

            _hitStreakRtpc?.SetValue(gameObject, _currentStreak);
            hitEvent.Post(gameObject);
        }

        private AK.Wwise.Event GetHitEvent(CollectibleType type)
        {
            return type switch
            {
                CollectibleType.Candy => _candyHitEvent,
                CollectibleType.Cross => _candyHitEvent,
                CollectibleType.Toge => _togeHitEvent,
                CollectibleType.Poison => _poisonHitEvent,
                CollectibleType.Ice => _iceHitEvent,
                CollectibleType.Gummy => _gummyHitEvent,
                _ => null,
            };
        }
    }
}
