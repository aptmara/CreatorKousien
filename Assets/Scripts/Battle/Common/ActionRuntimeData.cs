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
        public ActionType Type { get; }

        // --- 攻撃用データ ---
        public ActionProperty Property { get; }
        public List<Vector2Int> TargetCells { get; }

        // --- 移動用データ ---
        public GridDirection MoveDirection { get; }

        // 動的計算するかどうか
        public bool IsDynamicOrigin { get; }
        // 相対座標
        public List<Vector2Int> RelativeCells { get; }

        /// <summary>
        /// 攻撃用コンストラクタ
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="attackInfo"></param>
        /// <param name="targetCells"></param>
        public ActionRuntimeData(int actorId, ActionType type, ActionProperty property, List<Vector2Int> targetCells, bool isDynamic = false, List<Vector2Int> relativeCells = null)
        {
            ActorId = actorId;
            Type = type;
            Property = property;
            TargetCells = targetCells;
            IsDynamicOrigin = isDynamic;
            RelativeCells = relativeCells ?? new List<Vector2Int>();
        }

        /// <summary>
        /// 移動用コンストラクタ
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="direction"></param>
        public ActionRuntimeData(int actorId, GridDirection direction)
        {
            ActorId = actorId;
            Type = ActionType.Move;
            MoveDirection = direction;
        }

        public ActionRuntimeData(int actorId)
        {
            ActorId = actorId;
            Type = ActionType.Wait;
        }
    }
}
