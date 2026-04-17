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
using CreatorKousien.Player;
using CreatorKousien.Enemy;
using CreatorKousien.Battle;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.UseCase
{
    public class AttackUseCase
    {
        private BattleManager _battleManager;
        private FieldService _fieldService;
        private PlayerSystem _playerSystem;
        private EnemySystem _enemySystem;
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
            List<int> targetActorIds = _fieldService.GetActorsInCells(command.TargetCells);

            // 2. もし誰もいなければ、空振りエフェクトを出して終了
            if (targetActorIds.Count == 0)
            {
                // TODO: 空振りエフェクト、どんな感じで渡すかは検討中
                Debug.Log($"[AttackUseCase] Actor:{command.SourceActorId} の攻撃は空振りに終わった！");
                return;
            }

            // 3. 攻撃者の基礎攻撃力を取得
            int attackerAttack = GetBaseAttack(command.SourceActorId);

            // 4. ダメージを計算
            int finalDamage = _battleManager.CalculateDamage(attackerAttack, command.Property);

            // 5. SystemにHPを減らすように指示する
            foreach (int targetId in targetActorIds)
            {
                ApplyDamageToSystem(targetId, finalDamage);
            }

            // 6. 計算が終わったら、Viewへヒット演出エフェクトを指示
            // TODO: ヒットエフェクト
        }

        /// <summary>
        /// 基礎攻撃力を取得する
        /// </summary>
        /// <param name="actorId"></param>
        /// <returns></returns>
        private int GetBaseAttack(int actorId)
        {
            if (actorId == 1)
            {
                // return _playerSystem.RuntimeData.CurrentAttack;
                return 10;
            }
            else
            {
                var enemyData = _enemySystem.GetEnemyData(actorId);
                return enemyData != null ? enemyData.CurrentAttack : 0;
            }
        }

        private void ApplyDamageToSystem(int targetId, int damage)
        {
            if (targetId == 1)
            {
                _playerSystem.ChangeHp(-damage);
            }
            else
            {
                bool isDead = _enemySystem.TakeDamage(targetId, damage);

                if (isDead)
                {
                    // 敵が死んだらそのマスを空きマスにする
                    Vector2Int enemyPos = _fieldService.GetActorPosition(targetId);

                    if (enemyPos.x != -1)
                    {
                        _fieldService.UpdateOccupancy(targetId, enemyPos.x, enemyPos.y, -1, -1);
                    }

                    // TODO: 消滅エフェクトを通知
                }
            }
        }
    }
}
