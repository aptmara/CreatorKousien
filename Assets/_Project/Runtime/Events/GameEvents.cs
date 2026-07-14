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

        public readonly ScriptableObject ItemDataRaw;

        public EnemyHitBatchEvent(string enemyId, int hitCount, float bodyDamage, Vector3 pos, Transform enemyTransform, ScriptableObject itemDataRaw = null)
        {
            EnemyId = enemyId;
            HitCount = hitCount;
            BodyDamage = bodyDamage;
            HitPosition = pos;
            EnemyTransform = enemyTransform;
            ItemDataRaw = itemDataRaw;
        }
    }

    public readonly struct BarrierHitBatchEvent
    {
        public readonly string EnemyId;
        public readonly int HitCount;
        public readonly float GaugeDamage;
        public readonly Vector3 HitPosition;
        public readonly Transform BarrierTransform;

        public readonly ScriptableObject ItemDataRaw;

        public BarrierHitBatchEvent(string enemyId, int hitCount, float gaugeDamage, Vector3 pos, Transform barrierTransform, ScriptableObject itemDataRaw = null)
        {
            EnemyId = enemyId;
            HitCount = hitCount;
            GaugeDamage = gaugeDamage;
            HitPosition = pos;
            BarrierTransform = barrierTransform;
            ItemDataRaw = itemDataRaw;
        }
    }

    public readonly struct EnemyDropEvent
    {
        public readonly string EnemyId;

        public EnemyDropEvent(string enemyId)
        {
            EnemyId = enemyId;
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





    // プレイヤー育成系イベント!
    // 制作者: 浅野
    // 制作日: 2026/06/19
    // ------------------------------------------------------------

    /// <summary>
    /// 経験値獲得通知。敵/ダメージ系マネージャーが発行する。
    /// Amountは「敵が持っている経験値量」であり、換算はPlayerExpSystem側で行う。
    /// 敵担当？はるひこかな？は、多分これ呼ぶ感じになるかも！それかダメージマネージャーかも！
    /// </summary>
    public readonly struct ExpGainedEvent
    {
        public readonly float Amount;           ///< 獲得経験値量
        public readonly string SourceId;        ///< 経験値獲得元のID（敵IDなど）を保持する。nullの場合は不明。

        /// <summary>
        /// 経験値獲得通知イベントを作成する。
        /// </summary>
        /// <param name="amount">値</param>
        /// <param name="sourceId">経験値獲得元のID</param>
        public ExpGainedEvent(float amount, string sourceId = null)
        {
            Amount = amount;
            SourceId = sourceId;
        }
    }

    /// <summary>
    /// 経験値量の変化通知。PlayerExpSystemが発行する。
    /// 経験値バーUIはRatioのみ使用することもできる。
    /// </summary>
    public readonly struct PlayerExpChangedEvent
    {
        public readonly float CurrentExp;                           ///< 現在の経験値量
        public readonly float ExpToNextLevel;                       ///< 次のレベルまでの経験値量
        public readonly float Ratio;                                ///< 0.0〜1.0 の正規化経験値量（UI用）

        /// <summary>
        /// 経験値量の変化通知イベントを作成する。
        /// </summary>
        public PlayerExpChangedEvent(float currentExp, float expToNextLevel)
        {
            CurrentExp = currentExp;
            ExpToNextLevel = expToNextLevel;
            Ratio = expToNextLevel > 0f ? currentExp / expToNextLevel : 0f;
        }
    }

    /// <summary>
    /// レベルアップ通知。PlayerExpSystemがレベル上昇確定時に発行する。
    /// </summary>
    public readonly struct PlayerLeveledUpEvent
    {
        public readonly int NewLevel;               ///< 新しいレベル値

        /// <summary>
        /// プレイヤーレベルアップ通知イベントを作成する。
        /// </summary>
        /// <param name="newLevel">新しいレベル値</param>
        public PlayerLeveledUpEvent(int newLevel)
        {
            NewLevel = newLevel;
        }
    }

    /// <summary>
    /// ローグライク強化の開始要求。PlayerExpSystemがレベルアップ時に発行する。
    /// 候補提示・一時停止・選択はローグライク担当が受け取って行う。
    /// </summary>
    public readonly struct UpgradeSelectionRequestedEvent
    {
        public readonly int Level;                  ///< レベルアップ後のレベル値

        /// <summary>
        /// ローグライク強化の開始要求イベントを作成する。
        /// </summary>
        /// <param name="level">レベルアップ後のレベル値</param>
        public UpgradeSelectionRequestedEvent(int level)
        {
            Level = level;
        }
    }

    /// <summary>
    /// 強化適用完了通知。PlayerStatsServiceが適用後に発行する。
    /// </summary>
    public readonly struct UpgradeAppliedEvent
    {
        public readonly string UpgradeId;               ///< 適用された強化のID

        /// <summary>
        /// 強化適用完了通知イベントを作成する。
        /// </summary>
        /// <param name="upgradeId">適用された強化のID</param>
        public UpgradeAppliedEvent(string upgradeId)
        {
            UpgradeId = upgradeId;
        }
    }

    /// <summary>
    /// プレイヤーステータス変化通知。PlayerStatsServiceが更新後に発行する。
    /// 各View/機能はこれを購読してPlayerRuntimeDataを再Readする。
    /// </summary>
    public readonly struct PlayerStatsChangedEvent
    {
        // 現状は「何か変わった」だけを伝える空イベント
    }


    /// <summary>
    /// コンボ数の変化通知。ComboViewControlが発行する。
    /// VFXなどが購読してコンボ数に応じた演出を行う。
    /// </summary>
    public readonly struct ComboChangedEvent
    {
        public readonly int CurrentCombo;
        public readonly float DurationRatio;

        public ComboChangedEvent(int currentCombo, float durationRatio)
        {
            CurrentCombo = currentCombo;
            DurationRatio = durationRatio;
        }
    }

    public readonly struct PlayerTiltEvent
    {
        public readonly float CurrentTilt;

        public PlayerTiltEvent(float currentTilt)
        {
            CurrentTilt = currentTilt;
        }
    }

}

