// ================================================================================
// File         : ActionCategory.cs
// Author       : Iwai Shogo
//
// Description  : アクションの分類を定義する列挙。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// アクションの分類
    /// </summary>
    public enum ActionCategory
    {
        Attack,     // 攻撃
        Move,       // 移動
        Defend,     // 防御
        Special,    // 特殊効果など
    }
}
