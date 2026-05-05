// 制作者: 山内陽
using UnityEngine;

namespace Game.Core.Events
{
    public readonly struct CollectionChangedEvent
    {
        public readonly int CurrentCount;
        public readonly int Capacity;

        public CollectionChangedEvent(int currentCount, int capacity)
        {
            CurrentCount = currentCount;
            Capacity = capacity;
        }
    }

    public readonly struct PayloadReleasedEvent
    {
        public readonly int PayloadCount;
        public readonly float TotalPower;
        public readonly Vector3 ReleasePosition;
        public readonly Vector3 ReleaseDirection;

        public PayloadReleasedEvent(int count, float power, Vector3 pos, Vector3 dir)
        {
            PayloadCount = count;
            TotalPower = power;
            ReleasePosition = pos;
            ReleaseDirection = dir;
        }
    }

    public readonly struct EnemyHitBatchEvent
    {
        public readonly string EnemyId;
        public readonly int HitCount;
        public readonly float GaugeDamage;
        public readonly float BodyDamage;
        public readonly Vector3 HitPosition;

        public EnemyHitBatchEvent(string enemyId, int hitCount, float gaugeDamage, float bodyDamage, Vector3 pos)
        {
            EnemyId = enemyId;
            HitCount = hitCount;
            GaugeDamage = gaugeDamage;
            BodyDamage = bodyDamage;
            HitPosition = pos;
        }
    }

    public readonly struct EnemyGaugeBrokenEvent
    {
        public readonly string EnemyId;

        public EnemyGaugeBrokenEvent(string enemyId)
        {
            EnemyId = enemyId;
        }
    }

    public readonly struct EnemyDownStartedEvent
    {
        public readonly string EnemyId;
        public readonly float Duration;

        public EnemyDownStartedEvent(string enemyId, float duration)
        {
            EnemyId = enemyId;
            Duration = duration;
        }
    }

    public readonly struct StageTiltStartedEvent
    {
        public readonly Vector3 TiltDirection;

        public StageTiltStartedEvent(Vector3 tiltDirection)
        {
            TiltDirection = tiltDirection;
        }
    }
}
