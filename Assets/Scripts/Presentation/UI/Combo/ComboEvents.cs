// ================================================================================
// File         : ComboEvents.cs
// Author       : Iwai Shogo
//
// Description  : コンボイベントの定義。
// Created      : 2026-06-08
// ================================================================================

namespace Game.Core.Events
{
    /// <summary>
    /// コンボが更新された
    /// </summary>
    public struct ComboUpdatedEvent
    {
        public int CurrentCombo;    // 現在のコンボ数
        public float TimeRatio;     
    }
}
