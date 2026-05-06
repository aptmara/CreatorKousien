// ================================================================================
// File         : CollectableElement.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の属性を定義する列挙型。
// Created      : 2026-05-06
// ================================================================================

namespace Game.Data.Collectables
{
    /// <summary>
    /// 収集物の属性。敵の属性バリアを突破する際などに判定されます。
    /// </summary>
    public enum CollectableElement
    {
        None,   // 無属性
        Red,    // 赤属性
        Blue,   // 青属性
        Yellow, // 黄属性
    }
}
