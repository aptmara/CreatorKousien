// ================================================================================
// File         : EnemyIntent.cs
// Author       : Iwai Shogo
//
// Description  : 敵の予定をまとめる箱
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Battle;

namespace CreatorKousien.Enemy
{
    /// <summary>
    /// 準備フェーズでEnemyAIが提出する「このターンに行う内容」
    /// </summary>
    public class EnemyIntent
    {
        public int SourceActorId;
        public ActionType Type;
        public ActionProperty Property;
        public List<Vector2Int> RawTargetCells;
        public Vector2Int MoveDirection;
        public int ChargeTurns;
        public bool IsInterruptible;

        // 静的ファクトリ：攻撃
        public static EnemyIntent CreateAttack(int id, ActionType type, ActionProperty property, List<Vector2Int> cells, int charge, bool interrupt)
        {
            return new EnemyIntent
            {
                SourceActorId = id,
                Type = type,
                Property = property,
                RawTargetCells = cells,
                ChargeTurns = charge,
                IsInterruptible = interrupt
            };
        }

        // 静的ファクトリ：移動
        public static EnemyIntent CreateMove(int id, Vector2Int dir)
        {
            return new EnemyIntent
            {
                SourceActorId = id,
                Type = ActionType.Move,
                MoveDirection = dir
            };
        }

        // 静的ファクトリ：待機
        public static EnemyIntent CreateWait(int id)
        {
            return new EnemyIntent
            {
                SourceActorId = id,
                Type = ActionType.Wait
            };
        }
    }
}
