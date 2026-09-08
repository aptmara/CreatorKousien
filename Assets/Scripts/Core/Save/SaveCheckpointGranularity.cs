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
    /// GameProgressionManagerのInspectorから切り替えられます。
    /// </summary>
    public enum SaveCheckpointGranularity
    {
        /// <summary>
        /// Stageをクリアして次のStageへ進むタイミングだけセーブする。
        /// Stage内でWaveを何個クリアしても、Stageクリアまではセーブされません。
        /// </summary>
        PerStageClear = 0,

        /// <summary>
        /// Waveをクリアする度にセーブする(Stageクリア時のセーブも含む)。
        /// つづきからで再開したとき、Stage内の途中Waveから再開できます。
        /// </summary>
        PerWaveClear = 1,
    }
}
