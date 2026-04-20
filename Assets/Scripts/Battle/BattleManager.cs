// ================================================================================
// File         : BattleManager.cs
// Author       : Iwai Shogo
//
// Description  : ダメージ計算を担当する純粋なバトルロジック
// Created      : 2026-04-17
// ================================================================================

using UnityEngine;
using CreatorKousien.Data;

namespace CreatorKousien.Battle
{
    public class BattleManager
    {
        /// <summary>
        /// 攻撃力と技のプロパティから、最終的なダメージを計算する
        /// </summary>
        /// <param name="attackerBaseAttack"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public int CalculateDamage(int attackerBaseAttack, ActionProperty property)
        {
            int finalDamage = UnityEngine.Mathf.RoundToInt(attackerBaseAttack * property.DamageMultiplier);
            return finalDamage;
        }
    }
}
