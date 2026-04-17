// ================================================================================
// File         : BattleSituation.cs
// Author       : Iwai Shogo
//
// Description  : UseCaseからEnemyAIへ渡される、そのターンの「戦況」をまとめたデータ。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Enemy
{
    public struct BattleSituation
    {
        public Vector2Int PlayerPos;
        public int MaxX;
        public int MaxY;
        public int BorderX;

        // UseCaseからマスの判定関数だけを渡してもらう
        public System.Func<int, int, bool> IsValidCell;
    }
}
