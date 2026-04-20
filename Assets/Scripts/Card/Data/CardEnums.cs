// ================================================================================
// File         : CardEnums.cs
// Author       : Iwai Shogo
//
// Description  : カードやエフェクトに関する状態の定義。
// Created      : 2026-04-20
// ================================================================================

namespace CreatorKousien.Data
{
    /// <summary>
    /// カードの表裏(面)
    /// </summary>
    public enum CardFace
    {
        Front,  // 表 (移動など)
        Back,   // 裏 (アクション・攻撃など)
    }

    /// <summary>
    /// コントローラーのボタンと同期するスロット位置
    /// </summary>
    public enum SlotPosition
    {
        Up,     // 上
        Down,   // 下
        Left,   // 左
        Right,  // 右
    }

    /// <summary>
    /// 攻撃のターゲット範囲タイプ
    /// </summary>
    public enum TargetAreaType
    {
        Front1,         // 目の前の1マス
        FrontPierce2,   // 前方2マス貫通
        Surround,       // 周囲8マス
        Self,           // 自分自身 (ガードやバフなど)
    }
}
