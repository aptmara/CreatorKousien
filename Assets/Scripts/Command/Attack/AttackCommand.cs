// ================================================================================
// File         : AttackCommand.cs
// Author       : Iwai Shogo
//
// Description  : 攻撃の実行を要求するコマンド(敵・味方共通)。
// Created      : 2026-04-17
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Battle;

namespace CreatorKousien.Command
{
    public sealed class AttackCommand : ICommand
    {
        /// <summary>攻撃を発動したキャラクターのID</summary>
        public int SourceActorId { get; }

        /// <summary>自分の攻撃タイプ</summary>
        public ActionType AttackerType { get; }

        /// <summary>攻撃の性質</summary>
        public ActionProperty Property { get; }

        /// <summary>攻撃が着弾するマスのリスト</summary>
        public List<Vector2Int> TargetCells { get; }

        /// <summary>相手の行動を辞書で受け取る</summary>
        public Dictionary<int, ActionType> StepActions { get; }

        public bool IsDynamicOrigin { get; }
        public List<Vector2Int> RelativeCells { get; }

        public System.Action<int> OnCancelTarget { get; }

        public AttackCommand(int sourceActorId, ActionType attackerType, ActionProperty property, List<Vector2Int> targetCells, Dictionary<int, ActionType> stepActions, bool isDynamic = false, List<Vector2Int> relativeCells = null, System.Action<int> onCancelTarget = null)
        {
            SourceActorId = sourceActorId;
            AttackerType = attackerType;
            Property = property;
            TargetCells = targetCells;
            StepActions = stepActions;
            IsDynamicOrigin = isDynamic;
            RelativeCells = relativeCells ?? new List<Vector2Int>();
            OnCancelTarget = onCancelTarget;
        }
    }
}
