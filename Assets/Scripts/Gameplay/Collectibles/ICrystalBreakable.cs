// ------------------------------------------------------------
// File		: ICrystalBreakable.cs
// Summary	: クリスタルを壊すためのインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-06-29
//
// Notes	:
// - 壊れた時の処理は実装側が持つ
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// プレイヤーの殴りで壊せるクリスタルの共通インターフェース
    /// 壊れた時の処理は実装側が持つ
    /// </summary>
    public interface ICrystalBreakable
    {
        /// <summary>
        /// クリスタルを壊す
        /// </summary>
        /// <param name="hitPoint">当たった位置</param>
        /// <param name="hitDirection">当たった方向</param>
        void Break(Vector3 hitPoint, Vector3 hitDirection);
    }
}
