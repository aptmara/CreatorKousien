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
        private GameEventBus _eventBus;

        /// <summary>
        /// GameManagerから必要なSystemとDispacherを渡して初期化する
        /// </summary>
        /// <param name="EnemySystem"></param>
        /// <param name="FieldService"></param>
        /// <param name="PlayerSystem"></param>
        /// <param name="Dispatcher"></param>
        public EnemyActionUseCase(EnemySystem EnemySystem, FieldService FieldService, PlayerSystem PlayerSystem, ActionTelegraphSystem telegraphSystem, CommandDispatcher Dispatcher, GameEventBus eventBus)
        {
            _enemySystem = EnemySystem;
            _fieldService = FieldService;
            _playerSystem = PlayerSystem;
            _telegraphSystem = telegraphSystem;
            _dispatcher = Dispatcher;
            _eventBus = eventBus;
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
                virtualPositions[id] = _fieldService.GetActorPosition(id);
            }

            // チーム全体で3手分をループ
            for (int step = 0; step < 3; step++)
            {
                // このステップで行動するエネミーを1体「ランダム」で指名する
                int actingEnemyId = aliveEnemyIds[Random.Range(0, aliveEnemyIds.Count)];

                var ai = _enemySystem.GetEnemyAI(actingEnemyId);
                var situation = CreateSituation();

                // 現在の仮想座標を取得
                Vector2Int currentVirtualPos = virtualPositions[actingEnemyId];

                // 指名されたエネミーに思考させる
                EnemyIntent intent = ai.Think(situation, virtualPositions[actingEnemyId]);
                var actionTicket = ConvertIntentToTicket(intent, step, currentVirtualPos);

                plan.Add(actionTicket);

                // 移動した場合は、そのエネミーの仮想座標だけを更新
                if (intent.Type == ActionType.Move)
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

        private ActionRuntimeData ConvertIntentToTicket(EnemyIntent intent, int step, Vector2Int currentVirtualPos)
        {
            switch (intent.Type)
            {
                case ActionType.FastAttack:
                    // UseCase側でクリッピング（場外・障害物判定）を行う
                    var validCells = intent.RawTargetCells.FindAll(p =>
                        !_fieldService.IsOutOfBounds(p.x, p.y) && !_fieldService.IsObstacle(p.x, p.y));

                    RegisterTelegraph(intent, validCells);

                    // Viewに攻撃予告の色変更を指示
                    _eventBus.PublishTelegraph(validCells, true, step);

                    return new ActionRuntimeData(intent.SourceActorId, intent.Type, intent.Property, validCells);

                case ActionType.Move:
                    GridDirection dir = ConvertToGridDirection(intent.MoveDirection);

                    Vector2Int targetPos = currentVirtualPos + intent.MoveDirection;
                    
                    // 盤面内かどうかに加え、敵陣内（x が BorderX 以上）かどうかもチェック
                    int borderX = _fieldService.GetBorderX();
                    bool isValidMove = !_fieldService.IsOutOfBounds(targetPos.x, targetPos.y)
                                    && !_fieldService.IsObstacle(targetPos.x, targetPos.y)
                                    && targetPos.x >= borderX;

                    // Viewに移動予告のSphere表示を指示
                    if (isValidMove)
                    {
                        _eventBus.PublishMoveTelegraph(targetPos, true, step);
                    }
                    else
                    {
                        // 無効ならその場に止まる
                    }

                    return new ActionRuntimeData(intent.SourceActorId, dir);

                default: // Wait
                    return new ActionRuntimeData(intent.SourceActorId, intent.Type, new ActionProperty { DamageMultiplier = 0 }, new List<Vector2Int>());
            }
        }

        private void RegisterTelegraph(EnemyIntent intent, List<Vector2Int> validCells)
        {
            if (validCells.Count == 0) return;

            var telegraphData = new TelegraphRuntimeData
            {
                TelegraphId = intent.SourceActorId * 1000 + Random.Range(1, 999),
                SourceActorId = intent.SourceActorId,
                Property = intent.Property,
                TargetCells = validCells,
                RemainingTurn = intent.ChargeTurns,
                IsInterruptible = intent.IsInterruptible
            };
            _telegraphSystem.RegisterTelegraph(telegraphData);
        }

        private GridDirection ConvertToGridDirection(Vector2Int dir)
        {
            if (dir == new Vector2Int(0, -1))   return GridDirection.Up;
            if (dir == new Vector2Int(0, 1))    return GridDirection.Down;
            if (dir == new Vector2Int(-1, 0))   return GridDirection.Left;
            if (dir == new Vector2Int(1, 0))    return GridDirection.Right;
            return GridDirection.Up;
        }
    }
}
