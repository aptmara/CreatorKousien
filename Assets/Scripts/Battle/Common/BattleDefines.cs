// ================================================================================
// File         : BattleDefines.cs
// Author       : Iwai Shogo
//
// Description  : バトルで使用する共通のデータ定義。
// Created      : 2026-04-17
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Data
{
    /// <summary>
    /// アクションの威力や性質をまとめた純粋な構造体
    /// </summary>
    [System.Serializable]
    public struct ActionProperty
    {
        [Tooltip("威力倍率(攻撃者の基礎攻撃力 * この値 = 最終ダメージ) \n例: 通常攻撃 = 1.0, 大技 = 2.0")]
        [Min(0f)]
        public float DamageMultiplier;

        [Tooltip("ヒット数(多段ヒット攻撃用)")]
        [Min(1)]
        public int HitCount;

        // 将来的に追加
    }
}
