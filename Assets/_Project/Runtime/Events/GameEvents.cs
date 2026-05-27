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
        public readonly float BodyDamage;
        public readonly Vector3 HitPosition;
        public readonly Transform EnemyTransform;

        public EnemyHitBatchEvent(string enemyId, int hitCount, float bodyDamage, Vector3 pos, Transform enemyTransform)
        {
            EnemyId = enemyId;
            HitCount = hitCount;
            BodyDamage = bodyDamage;
            HitPosition = pos;
            EnemyTransform = enemyTransform;
        }
    }

    public readonly struct BarrierHitBatchEvent
    {
        // バリアはあくまでもEnemyの一部でEnemyが管理するためEnemyIDを保持
        public readonly string EnemyId;
        public readonly int HitCount;
        public readonly float GaugeDamage;
        public readonly Vector3 HitPosition;
        public readonly Transform BarrierTransform;

        public BarrierHitBatchEvent(string enemyId, int hitCount, float gaugeDamage, Vector3 pos, Transform barrierTransform)
        {
            EnemyId = enemyId;
            HitCount = hitCount;
            GaugeDamage = gaugeDamage;
            HitPosition = pos;
            BarrierTransform = barrierTransform;
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

    public readonly struct RuleBarrierAttackEvent
    {
        public readonly float AttackPower;

        public RuleBarrierAttackEvent(float attackPower)
        {
            AttackPower = attackPower;
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

    /// <summary>
    /// 敵の攻撃ゲージ量変化通知。
    /// EnemyAttackGaugeが増減のたびに発行する。
    /// UIはRatioのみ使用することもでき、将来的な詳細表示にはCurrentGauge/MaxGaugeを利用する。
    /// </summary>
    public readonly struct EnemyGaugeChangedEvent
    {
        public readonly string EnemyId;
        public readonly float CurrentGauge;
        public readonly float MaxGauge;
        /// <summary>0.0〜1.0 の正規化ゲージ量（UI用）</summary>
        public readonly float Ratio;

        public EnemyGaugeChangedEvent(string enemyId, float currentGauge, float maxGauge)
        {
            EnemyId = enemyId;
            CurrentGauge = currentGauge;
            MaxGauge = maxGauge;
            Ratio = maxGauge > 0f ? currentGauge / maxGauge : 0f;
        }
    }

    /// <summary>
    /// 敵の本体HP変化通知。
    /// EnemyHealthが変化するたびに発行する。
    /// </summary>
    public readonly struct EnemyHealthChangedEvent
    {
        public readonly string EnemyId;
        public readonly float CurrentHp;
        public readonly float MaxHp;
        /// <summary>0.0〜1.0 の正規化HP（UI用）</summary>
        public readonly float Ratio;

        public EnemyHealthChangedEvent(string enemyId, float currentHp, float maxHp)
        {
            EnemyId = enemyId;
            CurrentHp = currentHp;
            MaxHp = maxHp;
            Ratio = maxHp > 0f ? currentHp / maxHp : 0f;
        }
    }

    /// <summary>
    /// 敵撃破通知。HP0到達時にEnemyHealthが発行する。
    /// </summary>
    public readonly struct EnemyDefeatedEvent
    {
        public readonly string EnemyId;

        public EnemyDefeatedEvent(string enemyId)
        {
            EnemyId = enemyId;
        }
    }

    /// <summary>
    /// 敵のゲージMAX到達による攻撃発動通知。
    /// Phase2以降でプレイヤーへのダメージ処理を受け取るために用意する。
    /// </summary>
    public readonly struct EnemyAttackFiredEvent
    {
        public readonly string EnemyId;

        public EnemyAttackFiredEvent(string enemyId)
        {
            EnemyId = enemyId;
         }
    }

    /// <summary>
    /// 防衛ラインオブジェクトをダメージリアクションを行わせる。
    /// </summary
    public readonly struct DefLineHitReactionEvent
    {
        // 現状防衛ラインが何をもって被弾演出を行うか不明なため
        // 空の構造体だが、演出に攻撃された座標や属性によるバリアの反応など
        // 何かしらが必要になった場合このイベントで渡す
    }

    /// <summary>
    /// 防衛ラインオブジェクトを破壊する
    /// </summary
    public readonly struct DefLineBreakReactionEvent
    {
        // 現状防衛ラインが何をもって被弾演出を行うか不明なため
        // 空の構造体だが、演出に攻撃された座標や属性によるバリアの反応など
        // 何かしらが必要になった場合このイベントで渡す
    }
}

