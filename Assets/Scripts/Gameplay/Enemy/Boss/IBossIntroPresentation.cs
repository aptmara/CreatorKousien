// ------------------------------------------------------------
// File		: IBossIntroPresentation.cs
// Summary	: ボスごとの登場演出を際込むためのインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - BossBattleFlowControllerはこれがついていればそちらを優先する
// - 未実装のボスは今までどおりBossIntroSequenceControllerかアニメ待ちを通る
// ------------------------------------------------------------
using System.Collections;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスの登場演出
    /// </summary>
    public interface IBossIntroPresentation
    {
        /// <summary>
        /// 登場演出を再生する
        /// </summary>
        IEnumerator PlayIntro();
    }
}

