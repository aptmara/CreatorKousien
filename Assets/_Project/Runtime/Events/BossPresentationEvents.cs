// ------------------------------------------------------------
// File     : BossPresentationEvents.cs
// Summary  : ボス戦の画面演出を要求するためのイベントを定義する
//
// Author   : [浅野勇生]
// Created  : 2026-07-17
//
// Notes:
// - イベントはUIを直接操作しない。
// - 実際の画面フチ警告演出はPresentation側が担当する。
// ------------------------------------------------------------
namespace Game.Core.Events
{
    /// <summary>
    /// イバラタックル接近中
    /// </summary>
    public readonly struct BossThornWarningStartedEvent
    {
    }


    /// <summary>
    /// 画面ふち警告演出を終了するイベント
    /// </summary>
    public readonly struct BossThornWarningEndedEvent
    {
    }
}
