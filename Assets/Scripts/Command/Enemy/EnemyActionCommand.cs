// ================================================================================
// File         : EnemyActionCommand.cs
// Author       : Iwai Shogo
//
// Description  : 敵に行動を促すコマンド。
// Created      : 2026-04-17
// ================================================================================

using CreatorKousien.Battle;
using System;
using System.Collections.Generic;

namespace CreatorKousien.Command
{
    /// <summary>
    /// 指定した敵キャラクターに行動を要求するコマンド
    /// </summary>
    public sealed class EnemyActionCommand : ICommand
    {
        // 全てのエネミーのプランをActorIDとキーにして受けとる
        public Action<List<ActionRuntimeData>> OnPlanGenerated { get; }

        public int RollEnemyActTimes { get; }

        public EnemyActionCommand(Action<List<ActionRuntimeData>> onPlanGenerated, int rollEnemyActTimes)
        {
            OnPlanGenerated = onPlanGenerated;
            RollEnemyActTimes = rollEnemyActTimes;
        }
    }
}
