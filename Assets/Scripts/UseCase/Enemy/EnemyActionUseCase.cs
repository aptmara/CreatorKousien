// ================================================================================
// File         : EnemyActionUseCase.cs
// Author       : Iwai Shogo
//
// Description  : 敵の行動を管理する進行台本。
// Created      : 2026-04-17
// ================================================================================

using CreatorKousien.Battle;
using CreatorKousien.Command;
using CreatorKousien.Core;
using CreatorKousien.Data;
using CreatorKousien.Enemy;
using CreatorKousien.Field;
using CreatorKousien.Player;
using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.UseCase
{
    /// <summary>
    /// 敵の行動フェーズを進行するUseCase
    /// </summary>
    public class EnemyActionUseCase
    {
        private EnemySystem _enemySystem;
        private FieldService _fieldService;
        private PlayerSystem _playerSystem;
        private ActionTelegraphSystem _telegraphSystem;
        private CommandDispatcher _dispatcher;

        /// <summary>
        /// GameManagerから必要なSystemとDispacherを渡して初期化する
        /// </summary>
        /// <param name="EnemySystem"></param>
        /// <param name="FieldService"></param>
        /// <param name="PlayerSystem"></param>
        /// <param name="Dispatcher"></param>
        public EnemyActionUseCase(EnemySystem EnemySystem, FieldService FieldService, PlayerSystem PlayerSystem, ActionTelegraphSystem telegraphSystem, CommandDispatcher Dispatcher)
        {
            _enemySystem = EnemySystem;
            _fieldService = FieldService;
            _playerSystem = PlayerSystem;
            _telegraphSystem = telegraphSystem;
            _dispatcher = Dispatcher;
        }

        /// <summary>
        /// Dispatcherからコマンドを受け取って実行する
        /// </summary>
        /// <param name="command"></param>
        public void Execute(EnemyActionCommand command)
        {
            // 1. 3手分のプランをシミュレーションして構築する
            List<ActionRuntimeData> plan = PlanThreeActions(command.EnemyActorId);

            // 2. コマンドにセットされたコールバックに結果を書き込んで、呼び出し元に返す
            command.OnPlanGenerated?.Invoke(plan);

            // TODO: Mediator経由でViewに赤いマスを描画させる？敵の思考完了イベントを送る？
        }

        /// <summary>
        /// 敵の3手分の行動をシミュレーションして、プランを構築する内部ロジック
        /// </summary>
        /// <param name="actorId"></param>
        /// <returns></returns>
        private List<ActionRuntimeData> PlanThreeActions(int actorId)
        {
            var plan = new List<ActionRuntimeData>();
            var ai = _enemySystem.GetEnemyAI(actorId);
            var data = _enemySystem.GetEnemyData(actorId);

            if (ai == null || data == null) return plan;

            Vector2Int virtualPos = data.Position; // 現在地からスタート

            for (int i = 0; i < 3; i++)
            {
                var situation = CreateSituation();
                EnemyIntent intent = ai.Think(situation, virtualPos);
                var actionTicket = ConvertIntentToTicket(intent);

                plan.Add(actionTicket);

                // 移動したなら、次の一手のために仮想座標を更新
                if (intent.Category == ActionCategory.Move)
                {
                    virtualPos += intent.MoveDirection;
                }
            }
            return plan;
        }

        private BattleSituation CreateSituation()
        {
            Vector2Int fSize = _fieldService.GetFieldSize();
            return new BattleSituation
            {
                PlayerPos = _playerSystem.RuntimeData.Position,
                MaxX = fSize.x - 1,
                MaxY = fSize.y - 1,
                BorderX = _fieldService.GetBorderX()
            };
        }

        private ActionRuntimeData ConvertIntentToTicket(EnemyIntent intent)
        {
            switch (intent.Category)
            {
                case ActionCategory.Attack:
                    // UseCase側でクリッピング（場外・障害物判定）を行う
                    var validCells = intent.RawTargetCells.FindAll(p =>
                        !_fieldService.IsOutOfBounds(p.x, p.y) && !_fieldService.IsObstacle(p.x, p.y));

                    RegisterTelegraph(intent, validCells);
                    return new ActionRuntimeData(intent.SourceActorId, intent.AttackInfo, validCells);

                case ActionCategory.Move:
                    GridDirection dir = ConvertToGridDirection(intent.MoveDirection);
                    return new ActionRuntimeData(intent.SourceActorId, dir);

                default: // Wait
                    return new ActionRuntimeData(intent.SourceActorId, new AttackProperty { DamageMultiplier = 0 }, new List<Vector2Int>());
            }
        }

        private void RegisterTelegraph(EnemyIntent intent, List<Vector2Int> validCells)
        {
            if (validCells.Count == 0) return;

            var telegraphData = new TelegraphRuntimeData
            {
                TelegraphId = intent.SourceActorId * 1000 + Random.Range(1, 999),
                SourceActorId = intent.SourceActorId,
                AttackInfo = intent.AttackInfo,
                TargetCells = validCells,
                RemainingTurn = intent.ChargeTurns,
                IsInterruptible = intent.IsInterruptible
            };
            _telegraphSystem.RegisterTelegraph(telegraphData);
        }

        private GridDirection ConvertToGridDirection(Vector2Int dir)
        {
            if (dir == Vector2Int.up) return GridDirection.Up;
            if (dir == Vector2Int.down) return GridDirection.Down;
            if (dir == Vector2Int.left) return GridDirection.Left;
            if (dir == Vector2Int.right) return GridDirection.Right;
            return GridDirection.Up;
        }
    }
}
