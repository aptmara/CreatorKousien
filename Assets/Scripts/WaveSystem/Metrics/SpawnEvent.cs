// ------------------------------------------------------------
// File		: SpawnEvent.cs
// Summary	: Wave内で敵が1体出現するタイミングを表します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveMetricsCalculatorが、WaveDataSOから静的に組み立てます!
// - 進行条件が「敵の全滅」の場合、それ以降の時刻は確定しません!
// - 確定していない時刻は「最短でもこの時刻」という下限を表します!
// ------------------------------------------------------------
using Game.Core.Enemy;

namespace Game.WaveSystem
{
    /// <summary>
    /// Wave内で敵が1体出現するタイミングを表します。
    /// </summary>
    public readonly struct SpawnEvent
    {
        public readonly int GroupIndex;                     ///< 出現元のGroupのインデックス
        public readonly int EntryIndex;                     ///< 出現元のEntryのインデックス
        public readonly EnemyDefinition Definition;         ///< 出現する敵の定義
        public readonly float Time;                         ///< 出現する時刻
        public readonly bool IsTimeConfirmed;               ///< 時刻が確定しているかどうか


        /// <summary>
        /// SpawnEventを初期化します。
        /// </summary>
        /// <param name="groupIndex">出現元のGroupのインデックス</param>
        /// <param name="entryIndex">出現元のEntryのインデックス</param>
        /// <param name="definition">出現する敵の定義</param>
        /// <param name="time">出現する時刻</param>
        /// <param name="isTimeConfirmed">時刻が確定しているかどうか</param>
        public SpawnEvent(int groupIndex, int entryIndex, EnemyDefinition definition, float time, bool isTimeConfirmed)
        {
            GroupIndex = groupIndex;
            EntryIndex = entryIndex;
            Definition = definition;
            Time = time;
            IsTimeConfirmed = isTimeConfirmed;
        }
    }
}
