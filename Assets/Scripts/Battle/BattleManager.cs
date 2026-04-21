// ================================================================================
// File         : BattleManager.cs
// Author       : Iwai Shogo
//
// Description  : ダメージ計算を担当する純粋なバトルロジック
// Created      : 2026-04-17
// ================================================================================

using UnityEngine;
using CreatorKousien.Data;
using UnityEditor;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 三竦みの判定結果
    /// </summary>
    public enum ClashResult
    {
        Normal,         // 通常ヒット
        CancelTarget,   // 相手の行動をキャンセルする
        GuardBreak,     // ガードを崩す
        Blocked,        // ガードに防がれる
    }

    public class BattleManager
    {
        /// <summary>
        /// 三竦みの相性を判定する
        /// </summary>
        /// <param name="attackerAction"></param>
        /// <param name="defenderAction"></param>
        /// <returns></returns>
        public ClashResult EvaluateClash(ActionType attackerAction, ActionType defenderAction)
        {
            // FastAttack は WideAttack をキャンセルする
            if (attackerAction == ActionType.FastAttack && defenderAction == ActionType.WideAttack)
            {
                return ClashResult.CancelTarget;
            }

            // WideAttack は Guard を崩す
            if (attackerAction == ActionType.WideAttack && defenderAction == ActionType.Guard)
            {
                return ClashResult.GuardBreak;
            }

            // Guard は FastAttack を防ぐ
            if (attackerAction == ActionType.FastAttack && defenderAction == ActionType.Guard)
            {
                return ClashResult.Blocked;
            }

            return ClashResult.Normal;
        }

        /// <summary>
        /// 攻撃力と技のプロパティから、最終的なダメージを計算する
        /// </summary>
        /// <param name="attackerBaseAttack"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public int CalculateDamage(int attackerBaseAttack, ActionProperty property, ClashResult clashResult)
        {
            // ガード成功時はダメージを完全に無効
            if (clashResult == ClashResult.Blocked)
            {
                return 0;
            }

            float multiplier = property.DamageMultiplier;

            // ガードブレイク時はダメージが1.5倍になるボーナス
            if (clashResult == ClashResult.GuardBreak)
            {
                multiplier *= 1.5f;
            }

            int finalDamage = UnityEngine.Mathf.RoundToInt(attackerBaseAttack * multiplier);
            return finalDamage;
        }
    }
}
