// ================================================================================
// File         : EnemyActionUseCase.cs
// Author       : Iwai Shogo
//
// Description  : 敵の行動を管理する進行台本。
// Created      : 2026-04-17
// ================================================================================

using UnityEngine;
using CreatorKousien.Command;
using CreatorKousien.Core;
using CreatorKousien.Enemy;
using CreatorKousien.Field;
using CreatorKousien.Player;
using CreatorKousien.Battle;
using System.Collections.Generic;

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
        private ActionTelegraphSystem _TelegraphSystem;
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
            _TelegraphSystem = telegraphSystem;
            _dispatcher = Dispatcher;
        }

        /// <summary>
        /// Dispatcherからコマンドを受け取って実行する
        /// </summary>
        /// <param name="command"></param>
        public void Execute(EnemyActionCommand command)
        {
            // 1. 対象となる敵のAIデータを取得
            EnemyAI ai = _enemySystem.GetEnemyAI(command.EnemyActorId);

            if (ai == null) return;

            // 2. 司令塔が各システムから必要な情報を取得
            Vector2Int pPos = _playerSystem.RuntimeData.Position;
            Vector2Int fSize = _fieldService.GetFieldSize();
            int border = _fieldService.GetBorderX();

            // 3. 戦況パッケージを作成
            var situation = new BattleSituation
            {
                PlayerPos = pPos,
                MaxX = fSize.x - 1,
                MaxY = fSize.y - 1,
                BorderX = border,
                IsValidCell = (x, y) => !_fieldService.IsOutOfBounds(x, y) && !_fieldService.IsObstacle(x, y)
            };

            // 4. AIに思考を依頼
            EnemyIntent intent = ai.Think(situation);
            if (intent == null) return;

            // 5. FieldServiceを使ってクリッピング
            var validCells = new List<Vector2Int>();
            foreach (var pos in intent.RawTargetCells)
            {
                if (!_fieldService.IsOutOfBounds(pos.x, pos.y) && !_fieldService.IsObstacle(pos.x, pos.y))
                {
                    validCells.Add(pos);
                }
            }

            if (validCells.Count == 0) return;  // 全部場外なら終了

            // 6. 予約カレンダーに登録
            var telegraphData = new TelegraphRuntimeData
            {
                TelegraphId = intent.SourceActorId * 1000 + Random.Range(1, 999), // 簡易ID
                SourceActorId = intent.SourceActorId,
                AttackInfo = intent.AttackInfo,
                TargetCells = validCells,
                RemainingTurn = intent.ChargeTurns,
                IsInterruptible = intent.IsInterruptible
            };

            _TelegraphSystem.RegisterTelegraph(telegraphData);

            // TODO: Mediator経由でViewに赤いマスを描画させる？敵の思考完了イベントを送る？
        }
    }
}
