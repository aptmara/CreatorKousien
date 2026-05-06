// ================================================================================
// File         : CollectableCategory.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の基本的なカテゴリを定義する列挙型。
// Created      : 2026-05-06
// ================================================================================

namespace Game.Data.Collectables
{
    /// <summary>
    /// 収集物のカテゴリ。合体条件やバッファ管理に使用します。
    /// </summary>
    public enum CollectableCategory
    {
        Common,     // 通常の収集物
        Heavy,      // 重い/大きい収集物
        Special,    // 特殊効果を持つ収集物
        Fusion,     // 合体によって生成された収集物
    }
}
