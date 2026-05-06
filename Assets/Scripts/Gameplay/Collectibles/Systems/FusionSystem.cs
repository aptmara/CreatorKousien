// ================================================================================
// File         : FusionSystem.cs
// Author       : Iwai Shogo
//
// Description  : HeldItemの組み合わせ判定と合体処理を行うシステム。
// Created      : 2026-05-06
// ================================================================================

using System;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// PlayerHolderからHeldItem追加時に呼ばれ、合体を評価します。
    /// </summary>
    public class FusionSystem
    {
        public event Action FusionSucceeded;
        public event Action FusionFailed;

        public void EvaluateFusions(List<HeldItem> currentItem)
        {
            // TODO: レシピに基づく合体ロジックの実装
        }
    }
}
