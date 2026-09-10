// ------------------------------------------------------------
// File		: IBossDefeatPresentation.cs
// Summary	: ボスごとの撃破演出を定義するインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - これがついていると、演出が終わるまでStopBattleを持たせる
// - Waveへの撃破通知が送れるので、リザルトが演出の途中で始まらない！！
// ------------------------------------------------------------
using System.Collections;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスの撃破演出
    /// </summary>
    public interface IBossDefeatPresentation
    {
        /// <summary>
        /// 撃破演出を再生する
        /// </summary>
        IEnumerator PlayDefeat();
    }
}

