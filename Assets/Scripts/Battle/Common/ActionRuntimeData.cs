// ================================================================================
// File         : ActionRuntimeData.cs
// Author       : Iwai Shogo
//
// Description  : 準備フェーズで予約され、実行フェーズで消費される1手分の行動データ。
// Created      : 2026-04-17
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Command;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 準備フェーズで予約され、実行フェーズで消費される1手分のデータ
    /// </summary>
    public class ActionRuntimeData
    {
        public int ActorId { get; }
        public ActionCategory Category { get; }

        // --- 攻撃用データ ---
        public AttackProperty AttackInfo { get; }
        public List<Vector2Int> TargetCells { get; }

        // --- 移動用データ ---
        public GridDirection MoveDirection { get; }

        /// <summary>
        /// 攻撃用コンストラクタ
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="attackInfo"></param>
        /// <param name="targetCells"></param>
        public ActionRuntimeData(int actorId, AttackProperty attackInfo, List<Vector2Int> targetCells)
        {
            ActorId = actorId;
            Category = ActionCategory.Attack;
            AttackInfo = attackInfo;
            TargetCells = targetCells;
        }

        public ActionRuntimeData(int actorId, GridDirection direction)
        {
            ActorId = actorId;
            Category = ActionCategory.Move;
            MoveDirection = direction;
        }
    }
}
