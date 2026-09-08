// ================================================================================
// File         : SaveCheckpointGranularity.cs
// Author       : Iwai Shogo
//
// Description  : セーブを行うタイミングの粒度設定
// Created      : 2026-09-08
// ================================================================================

namespace Game.Core.Save
{
    /// <summary>
    /// セーブを行うタイミングの粒度。
    /// </summary>
    public enum SaveCheckpointGranularity
    {
        /// <summary>
        /// Stageをクリアして次のStageへ進むタイミングだけセーブする。
        /// </summary>
        PerStageClear = 0,

        /// <summary>
        /// Waveをクリアする度にセーブする(Stageクリア時のセーブも含む)。
        /// </summary>
        PerWaveClear = 1,
    }
}
