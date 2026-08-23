// 制作者: 山内陽
using System.Collections.Generic;
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

    public readonly struct GameChangeEvent
    {
        public readonly int MaxEnemy;
        public readonly int SpawnedEnemy;

        public GameChangeEvent(int spawnedEnemy, int maxEnemy)
        {
            MaxEnemy = maxEnemy;
            SpawnedEnemy = spawnedEnemy;
        }
    }

    public readonly struct GroupChangeEvent
    {
        public readonly int CurrentCount;
        public readonly int MaxCount;

        public GroupChangeEvent(int currentCount, int maxCount)
        {
            CurrentCount = currentCount;
            MaxCount = maxCount;
        }


    }


    public readonly struct StageCursorSelectEvent
    {
        public readonly string StageName;
        public readonly string StageInfo;
        public readonly Sprite StageIcon;

        public StageCursorSelectEvent(string stageName, string stageInfo, Sprite stageIcon)
        {
            StageName = stageName;
            StageInfo = stageInfo;
            StageIcon = stageIcon;
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

    /// <summary>
    /// 敵の状態異常が変化した通知。ラン中の状態異常ビルドが敵全探索をせず集計するために使用する。
    /// </summary>
    public readonly struct EnemyStatusChangedEvent
    {
        public readonly string EnemyId;
        public readonly string StatusType;
        public readonly int StackCount;
        public readonly bool IsActive;

        public EnemyStatusChangedEvent(string enemyId, string statusType, int stackCount, bool isActive)
        {
            EnemyId = enemyId;
            StatusType = statusType;
            StackCount = stackCount;
            IsActive = isActive;
        }
    }

    /// <summary>
    /// HP0到達時、状態異常が解除される直前のスナップショット。
    /// 状態異常撃破を条件にするラン中ビルドが参照する。
    /// </summary>
    public readonly struct EnemyDefeatStatusSnapshotEvent
    {
        public readonly string EnemyId;
        public readonly Vector3 Position;
        public readonly IReadOnlyList<string> ActiveStatusTypes;

        public EnemyDefeatStatusSnapshotEvent(
            string enemyId,
            Vector3 position,
            IReadOnlyList<string> activeStatusTypes)
        {
            EnemyId = enemyId;
            Position = position;
            ActiveStatusTypes = activeStatusTypes;
        }
    }

    /// <summary>
    /// 凍結が耐久ヒット数によって破壊された通知。
    /// 時間切れによる解除とは区別する。
    /// </summary>
    public readonly struct EnemyFreezeBrokenEvent
    {
        public readonly string EnemyId;
        public readonly Vector3 Position;

        public EnemyFreezeBrokenEvent(string enemyId, Vector3 position)
        {
            EnemyId = enemyId;
            Position = position;
        }
    }

    /// <summary>
    /// コンボ数を増やさず、現在のコンボ猶予だけを回復する要求。
    /// </summary>
    public readonly struct ComboDurationRecoveryRequestedEvent
    {
        public readonly float Seconds;

        public ComboDurationRecoveryRequestedEvent(float seconds)
        {
            Seconds = seconds;
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
        public readonly Vector3 AttackPosition;

        public RuleBarrierAttackEvent(float attackPower, Vector3 attackPosition)
        {
            AttackPower = attackPower;
            AttackPosition = attackPosition;
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
    /// 敵の攻撃モーション開始通知。
    /// モーション開始を受けて演出やサウンドを再生する用途に使う（現状は EnemyId のみ）。
    /// </summary>
    public readonly struct EnemyAttackMotionStartedEvent
    {
        public readonly string EnemyId;

        public EnemyAttackMotionStartedEvent(string enemyId)
        {
            EnemyId = enemyId;
        }
    }

    /// <summary>
    /// 敵の攻撃モーション終了通知。
    /// モーション開始を受けて演出やサウンドを再生する用途に使う（現状は EnemyId のみ）。
    /// </summary>
    public readonly struct EnemyAttackMotionEndedEvent
    {
        public readonly string EnemyId;

        public EnemyAttackMotionEndedEvent(string enemyId)
        {
            EnemyId = enemyId;
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
        public readonly Vector3 Position;

        public EnemyDefeatedEvent(string enemyId)
            : this(enemyId, Vector3.zero)
        {
        }

        public EnemyDefeatedEvent(string enemyId, Vector3 position)
        {
            EnemyId = enemyId;
            Position = position;
        }
    }

    /// <summary>
    /// 撃破された敵が落下を開始した通知。EnemyControllerが発行する。
    /// Wave側が「最後の敵の落下開始」を検知してクリア演出を早出しするために使用する。
    /// </summary>
    public readonly struct EnemyDefeatDropStartedEvent
    {
        public readonly string EnemyId;
        public readonly Transform EnemyTransform;

        public EnemyDefeatDropStartedEvent(string enemyId, Transform enemyTransform)
        {
            EnemyId = enemyId;
            EnemyTransform = enemyTransform;
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
        public readonly float Damage;
        public readonly float RemainingHpRatio;
        public readonly Vector3 AttackPosition;

        public DefLineHitReactionEvent(float damage, float remainingHpRatio, Vector3 attackPosition)
        {
            Damage = damage;
            RemainingHpRatio = remainingHpRatio;
            AttackPosition = attackPosition;
        }
    }

    /// <summary>
    /// 防衛ラインオブジェクトを破壊する
    /// </summary
    public readonly struct DefLineBreakReactionEvent
    {
        public readonly float Damage;
        public readonly Vector3 AttackPosition;

        public DefLineBreakReactionEvent(float damage, Vector3 attackPosition)
        {
            Damage = damage;
            AttackPosition = attackPosition;
        }
    }

    /// <summary>
    /// 防衛ラインの現在HPと最大HPが変化したことを通知する。
    /// </summary>
    public readonly struct DefenseLineHealthChangedEvent
    {
        public readonly float CurrentHp;
        public readonly float MaxHp;
        public readonly float Ratio;

        public DefenseLineHealthChangedEvent(float currentHp, float maxHp)
        {
            CurrentHp = currentHp;
            MaxHp = maxHp;
            Ratio = maxHp > 0f ? currentHp / maxHp : 0f;
        }
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

    /// <summary>
    /// 猶予時間切れ等で有効なコンボが終了した通知
    /// </summary>
    public readonly struct ComboEndedEvent
    {
        public readonly int FinalCombo;
        public readonly Vector3 Position;

        public ComboEndedEvent(int finalCombo, Vector3 position)
        {
            FinalCombo = finalCombo;
            Position = position;
        }
    }
}
