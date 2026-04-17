// ------------------------------------------------------------
// File		: GridDirection.cs
// Summary	: グリッドの方向を表す列挙型
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 設計書に基づいて、グリッドの方向を表す列挙型を定義しています。
// ------------------------------------------------------------
namespace CreatorKousien.Command
{
    /// <summary>
    /// グリッドの方向を表す列挙型
    /// </summary>
    public enum GridDirection
    {
        Up,         /// 上方向
        Down,       /// 下方向
        Left,       /// 左方向
        Right       /// 右方向
    }
}
