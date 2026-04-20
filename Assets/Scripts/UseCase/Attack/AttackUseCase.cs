// ================================================================================
// File         : AttackUseCase.cs
// Author       : Iwai Shogo
//
// Description  : 攻撃処理の進行台本。BattleManagerへ解決を依頼する。
// Created      : 2026-04-17
//
// Note         : GameEventBusを持たせて攻撃の結果を通知するようにしました！(4/18 : Asano)
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
        private GameEventBus _eventBus;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="battleManager">バトルマネージャーの参照</param>
        /// <param name="fieldService ">フィールドサービスの参照</param>
        /// <param name="playerSystem ">プレイヤーシステムの参照</param>
        /// <param name="enemySystem  ">敵システムの参照</param>
        /// <param name="dispatcher   ">コマンドディスパッチャーの参照</param>
        /// <param name="eventBus     ">ゲームイベントバスの参照</param>
        public AttackUseCase(BattleManager battleManager, FieldService fieldService, PlayerSystem playerSystem, EnemySystem enemySystem, CommandDispatcher dispatcher, GameEventBus eventBus)
        {
            _battleManager = battleManager;
            _fieldService = fieldService;
            _playerSystem = playerSystem;
            _enemySystem = enemySystem;
            _dispatcher = dispatcher;
            _eventBus = eventBus;
        }

        /// <summary>
        /// Dispatcherからコマンドを受け取って実行する。
        /// </summary>
        /// <param name="command"></param>
        public void Execute(AttackCommand command)
        {
            // ログ用: 誰の行動かを色分け
            string actorLabel = GetActorLabel(command.SourceActorId);

            // ゴーストアクション防止
            Vector2Int currentPos = _fieldService.GetActorPosition(command.SourceActorId);
            if (currentPos.x == -1)
            {
                _eventBus.PublishActionLogicCompleted(command.SourceActorId);
                return;
            }

            // 攻撃が発動した瞬間に対象のマスをEventBusで通知 (4/19 追加)
            _eventBus.OnAttackAreaExecuted?.Invoke(command.TargetCells);

            // 1. FieldServiceにマスにいるActorIDのリストをもらう
            List<int> targetActorIds = _fieldService.GetActorsInCells(command.TargetCells);

            // 2. もし誰もいなければ、空振りエフェクトを出して終了
            if (targetActorIds.Count == 0)
            {
                // TODO: 空振りエフェクト、どんな感じで渡すかは検討中
                Debug.Log($"<b>[ATTACK]</b> {actorLabel} の攻撃 ⇒ <color=orange>空振り</color>");

                // アクション終了を通知
                _eventBus.PublishActionLogicCompleted(command.SourceActorId);
                return;
            }

            // 3. 攻撃者の基礎攻撃力を取得
            int attackerAttack = GetBaseAttack(command.SourceActorId);

            // 4. ダメージを計算
            int finalDamage = _battleManager.CalculateDamage(attackerAttack, command.Property);

            // 5. SystemにHPを減らすように指示する
            foreach (int targetId in targetActorIds)
            {
                string targetLabel = GetActorLabel(targetId);
                Debug.Log($"<b>[ATTACK]</b> {actorLabel} ⇒ {targetLabel} に <b><color=orange>{finalDamage}</color></b> ダメージ！");

                ApplyDamageToSystem(targetId, finalDamage);

                // ダメージを与えたことをイベントで通知 (4/18 追加)
                _eventBus.PublishDamageTaken(targetId, finalDamage);
            }

            // ロジックが全て終わったので完了通知
            _eventBus.PublishActionLogicCompleted(command.SourceActorId);
        }

        // ラベル生成メソッド
        private string GetActorLabel(int id)
        {
            if (id == 1) return "<color=#00e6ff>PLAYER</color>";
            return $"<color=#ff4d4d>ENEMY(ID:{id})</color>";
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
                // TODO: return _playerSystem.RuntimeData.CurrentAttack;
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
                    Debug.Log($"<b>[DEATH]</b> {GetActorLabel(targetId)} が倒れた！");
                    _eventBus.PublishActorDeath(targetId);
                }
            }
        }
    }
}
