// ================================================================================
// File         : ActionType.cs
// Author       : Iwai Shogo
//
// Description  : ゲーム内の全てのアクションの種類。
// Created      : 2026-04-20
// ================================================================================

namespace CreatorKousien.Battle
{
    /// <summary>
    /// ゲーム内の全てのアクションの種類
    /// </summary>
    public enum ActionType
    {
        Move,       // 移動
        FastAttack, // 素早い攻撃
        WideAttack, // 範囲攻撃
        Guard,      // 防御
        Wait,       // 待機・サボり
    }
}
