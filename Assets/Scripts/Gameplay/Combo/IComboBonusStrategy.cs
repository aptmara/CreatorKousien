// ================================================================================
// File         : IComboBonusStrategy.cs
// Author       : Iwai Shogo
//
// Description  : コンボ数に応じたボーナスやバフ倍率を計算するインターフェース。
// Created      : 2026-06-08
// ================================================================================

namespace Game.Gameplay.Combo
{
    public interface IComboBonusStrategy
    {
        /// <summary>
        /// 現在のコンボ数から攻撃力などの補正倍率を計算する
        /// </summary>
        float GetDamageMultiplier(int currentCombo);
    }

    /// <summary>
    /// デフォルトの実装 (常に1倍)
    /// </summary>
    public class DefaultComboBonus : IComboBonusStrategy
    {
        public float GetDamageMultiplier(int currentCombo) => 1.0f;
    }
}
