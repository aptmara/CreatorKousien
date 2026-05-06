// ================================================================================
// File         : ShapeChangeSystem.cs
// Author       : Iwai Shogo
//
// Description  : 移動距離に応じたHeldItemの形状変化を管理するシステム。
// Created      : 2026-05-06
// ================================================================================

using System;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// PlayerMoverの移動距離を受け取り、HeldItemの形状を変化させます。
    /// </summary>
    public class ShapeChangeSystem
    {
        public event Action HeldItemShapeChanged;

        public void AddRollingDistance(List<HeldItem> currentItems, float distanceMoved)
        {
            // TODO: 距離加算と形状変化ロジックを実装。
        }
    }
}
