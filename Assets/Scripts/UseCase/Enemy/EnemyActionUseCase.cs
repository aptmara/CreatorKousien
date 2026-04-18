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
            // 敵チーム全体の「合計3手」のプランを作成
            List<ActionRuntimeData> teamPlan = PlanTeamActions();

            // コマンドの報告書に結果を書き込む
            command.OnPlanGenerated?.Invoke(teamPlan);

            // TODO: Mediator経由でViewに赤いマスを描画させる？敵の思考完了イベントを送る？
        }

        /// <summary>
        /// 存在している全敵の中からステップごとに1体を指名し、チーム全体で3手のプランを作る
        /// </summary>
        /// <returns></returns>
        private List<ActionRuntimeData> PlanTeamActions()
        {
            var plan = new List<ActionRuntimeData>();
            var aliveEnemyIds = _enemySystem.GetAllAliveEnemyIds();

            // 敵が1体もいなければ空のプランを返す
            if (aliveEnemyIds.Count == 0) return plan;

            // 全エネミーの「シミュレーション用仮想座標」を初期化
            var virtualPositions = new Dictionary<int, Vector2Int>();
            foreach (var id in aliveEnemyIds)
            {
                virtualPositions[id] = _enemySystem.GetEnemyData(id).Position;
            }

            // チーム全体で3手分をループ
            for (int step = 0; step < 3; step++)
            {
                // このステップで行動するエネミーを1体「ランダム」で指名する
                int actingEnemyId = aliveEnemyIds[Random.Range(0, aliveEnemyIds.Count)];

                var ai = _enemySystem.GetEnemyAI(actingEnemyId);
                var situation = CreateSituation();

                // 指名されたエネミーに思考させる
                EnemyIntent intent = ai.Think(situation, virtualPositions[actingEnemyId]);
                var actionTicket = ConvertIntentToTicket(intent);

                plan.Add(actionTicket);

                // 移動した場合は、そのエネミーの仮想座標だけを更新
                if (intent.Category == ActionCategory.Move)
                {
                    virtualPositions[actingEnemyId] += intent.MoveDirection;
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
