// ================================================================================
// File         : AttackUseCase.cs
// Author       : Iwai Shogo
//
// Description  : 攻撃処理の進行台本。BattleManagerへ解決を依頼する。
// Created      : 2026-04-17
// ================================================================================

using CreatorKousien.Command;
using CreatorKousien.Core;
using CreatorKousien.Field;
using CreatorKousien.Battle;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.UseCase
{
    public class AttackUseCase
    {
        private BattleManager _battleManager;
        private FieldService _fieldService;
        private CommandDispatcher _dispatcher;

        public AttackUseCase(BattleManager battleManager, FieldService fieldService, CommandDispatcher dispatcher)
        {
            _battleManager = battleManager;
            _fieldService = fieldService;
            _dispatcher = dispatcher;
        }

        /// <summary>
        /// Dispatcherからコマンドを受け取って実行する。
        /// </summary>
        /// <param name="command"></param>
        public void Execute(AttackCommand command)
        {
            // 1. FieldServiceにマスにいるActorIDのリストをもらう
            List<int> targetActorIds = new List<int>();
            foreach(var cell in command.TargetCells)
            {
                int occupierId = _fieldService.GetOccupierId(cell.x, cell.y);
                if (occupierId != -1)
                {
                    // 重複ヒットを防ぐためのチェック
                    // TODO: 後に実装
                    if (!targetActorIds.Contains(occupierId))
                    {
                        targetActorIds.Add(occupierId);
                    }
                }
            }

            // 2. もし誰もいなければ、空振りエフェクトを出して終了
            if (targetActorIds.Count == 0)
            {
                // TODO: 空振りエフェクト、どんな感じで渡すかは検討中
                Debug.Log($"[AttackUseCase] Actor:{command.SourceActorId} の攻撃は空振りに終わった！");
                return;
            }

            // 3. 誰かいた場合、BattleManagerにダメージ計算を依頼
            _battleManager.ResolveAttack(command.SourceActorId, targetActorIds, command.Property);

            // 4. 計算が終わったら、Viewへヒット演出エフェクトを指示
            // TODO: ヒットエフェクト
        }
    }
}
