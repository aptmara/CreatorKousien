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
        // 将来的に ScoreManager とか作った時にここに Score, Kills を足す

        public GameResultSummary(bool isGameClear, int lastClearedWaveIndex, float remainingDefenseLineHp)
        {
            IsGameClear = isGameClear;
            LastClearedWaveIndex = lastClearedWaveIndex;
            RemainingDefenseLineHp = remainingDefenseLineHp;
        }
    }
}
