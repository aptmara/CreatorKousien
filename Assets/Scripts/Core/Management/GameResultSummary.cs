// ================================================================================
// File         : GameResultSummary.cs
// Author       : Iwai Shogo
//
// Description  : バトルシーンからリザルト画面へ、ゲームプレイ実績データを引き渡すためのデータ構造体
// Created      : 2026-07-02
// ================================================================================

namespace Game.Core.Management
{
    /// <summary>
    /// バトルシーンからリザルト画面へ、ゲームプレイ実績データを引き渡すためのデータ構造体
    /// </summary>
    public sealed class GameResultSummary
    {
        public bool IsGameClear { get; }
        public int LastClearedWaveIndex { get; }
        public float RemainingDefenseLineHp { get; }

        /// <summary>
        /// 次のStageが存在するかどうか
        /// リザルト画面の「つぎへ」ボタンの表示切替に使用する
        /// </summary>
        public bool HasNextStage { get; }

        public GameResultSummary(bool isGameClear, int lastClearedWaveIndex, float remainingDefenseLineHp, bool hasNextStage = false)
        {
            IsGameClear = isGameClear;
            LastClearedWaveIndex = lastClearedWaveIndex;
            RemainingDefenseLineHp = remainingDefenseLineHp;
            HasNextStage = hasNextStage;
        }
    }
}
