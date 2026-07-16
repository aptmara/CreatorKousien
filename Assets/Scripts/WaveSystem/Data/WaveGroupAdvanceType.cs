// ------------------------------------------------------------
// File		: WaveGroupAdvanceType.cs
// Summary	: ウェーブグループの進行タイプを定義します。
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - WaveGroupから次のWaveGroupに進む条件を定義するための列挙型定義
// ------------------------------------------------------------
namespace Game.WaveSystem
{
    /// <summary>
    /// ウェーブグループの進行タイプを定義
    /// Wave全体の終了条件ではなく、
    /// 「次の敵グループをいつ出し始めるか」を表す
    /// </summary>
    public enum WaveGroupAdvanceType
    {
        /// <summary>
        /// このgroupから出現した敵をすべて倒すと次のgroupに進む
        /// </summary>
        AllEnemiesDefeated = 0,

        /// <summary>
        /// このgroupの開始から一定時間経過すると次のgroupに進む
        /// </summary>
        TimeAfterGroupStart = 1,
    }
}

