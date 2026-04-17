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
    /// 攻撃種別
    /// </summary>
    public enum AttackPatternType
    {
        [Tooltip("通常の攻撃")] Normal,
        [Tooltip("ガードを崩す溜め攻撃")] HeavyCharge,
        [Tooltip("プレイヤーの攻撃を防ぐ")] Defend,
        [Tooltip("特殊なデバフなどを付与")] Special
        // 以下、その他アクションを追加
    }

    /// <summary>
    /// 攻撃の性質をまとめた構造体
    /// </summary>
    [System.Serializable]
    public struct AttackProperty
    {
        [Tooltip("攻撃の種別")]
        public AttackPatternType Type;

        [Tooltip("威力倍率(攻撃者の基礎攻撃力 * この値 = 最終ダメージ) \n例: 通常攻撃 = 1.0, 大技 = 2.0")]
        [Min(0f)]
        public float DamageMultiplier;

        [Tooltip("ヒット数(多段ヒット攻撃用)")]
        [Min(1)]
        public int HitCount;

        // 将来的に追加
    }
}
