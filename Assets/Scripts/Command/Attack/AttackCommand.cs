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

namespace CreatorKousien.Command
{
    public sealed class AttackCommand : ICommand
    {
        /// <summary>攻撃を発動したキャラクターのID</summary>
        public int SourceActorId { get; }

        /// <summary>攻撃の性質</summary>
        public AttackProperty Property { get; }

        /// <summary>攻撃が着弾するマスのリスト</summary>
        public List<Vector2Int> TargetCells { get; }

        public AttackCommand(int sourceActorId, AttackProperty property, List<Vector2Int> targetCells)
        {
            SourceActorId = sourceActorId;
            Property = property;
            TargetCells = targetCells;
        }
    }
}
