// ------------------------------------------------------------
// File		: WaveMetrics.cs
// Summary	: 1Wave分の見積り値をまとめて保持します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveMetricsCalculatorが、WaveDataSOから静的に計算します!
// - Wave同士の重さを比べるための相対的な指標です！実測値じゃないです！
// - HpRateやBarrierRateを適用済みの値を保持します!
// ------------------------------------------------------------
namespace Game.WaveSystem
{
    /// <summary>
    /// 1Wave分の見積り値をまとめて保持します。
    /// </summary>
    public readonly struct WaveMetrics
    {
        public readonly int GroupCount;                         ///< Groupの数
        public readonly int TotalEnemyCount;                    ///< Wave内の敵の総数
        public readonly int BossCount;                          ///< Wave内のボスの数
        public readonly float TotalHp;                          ///< Wave内の敵の総HP
        public readonly float TotalBarrier;                     ///< Wave内の敵の総Barrier

        public readonly float TotalExp;                         ///< Wave内の敵の総Exp
        public readonly float AttackPressure;                   ///< Wave内の全敵が生存した場合の防衛ラインへの秒間ダメージ
        public readonly float MinDuration;                      ///< Waveの最短時間
        public readonly bool IsDurationConfirmed;               ///< Waveの最短時間が確定しているかどうか

        /// <summary>
        /// Waveの見積り値を生成します。
        /// </summary>
        /// <param name="groupCount">Groupの数</param>
        /// <param name="totalEnemyCount">出現する敵の総数</param>
        /// <param name="bossCount">出現するボスの数</param>
        /// <param name="totalHp">敵のHP合計</param>
        /// <param name="totalBarrier">バリアゲージの合計</param>
        /// <param name="totalExp">獲得できる経験値の合計</param>
        /// <param name="attackPressure">防衛ラインへの秒間ダメージ</param>
        /// <param name="minDuration">最後の敵が出現するまでの最短秒数</param>
        /// <param name="isDurationConfirmed">minDurationが確定値かどうか</param>
        public WaveMetrics(int groupCount, int totalEnemyCount, int bossCount, float totalHp, float totalBarrier, float totalExp, float attackPressure, float minDuration, bool isDurationConfirmed)
        {
            GroupCount = groupCount;
            TotalEnemyCount = totalEnemyCount;
            BossCount = bossCount;
            TotalHp = totalHp;
            TotalBarrier = totalBarrier;
            TotalExp = totalExp;
            AttackPressure = attackPressure;
            MinDuration = minDuration;
            IsDurationConfirmed = isDurationConfirmed;
        }
    }
}

