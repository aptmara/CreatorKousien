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
// using CreatorKousien.Player;

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
        private CommandDispatcher _dispatcher;

        /// <summary>
        /// GameManagerから必要なSystemとDispacherを渡して初期化する
        /// </summary>
        /// <param name="EnemySystem"></param>
        /// <param name="FieldService"></param>
        /// <param name="PlayerSystem"></param>
        /// <param name="Dispatcher"></param>
        public EnemyActionUseCase(EnemySystem EnemySystem, FieldService FieldService, PlayerSystem PlayerSystem, CommandDispatcher Dispatcher)
        {
            _enemySystem = EnemySystem;
            _fieldService = FieldService;
            _playerSystem = PlayerSystem;
            _dispatcher = Dispatcher;
        }

        /// <summary>
        /// Dispatcherからコマンドを受け取って実行する
        /// </summary>
        /// <param name="command"></param>
        public void Execute(EnemyActionCommand command)
        {
            // 対象となる敵のAIデータを取得
            EnemyAI ai = _enemySystem.GetEnemyAI(command.EnemyActorId);
            EnemyRuntimeData enemyData = _enemySystem.GetEnemyData(command.EnemyActorId);

            if (ai == null || enemyData == null)
            {
                Debug.LogWarning($"[EnemyActionUseCase] ActorID: {command.EnemyActorId} の敵が見つかりません。");
                return;
            }

            // プレイヤーの現在情報を取得
            PlayerRuntimeData playerData = _playerSystem.RuntimeData;

            // AIに思考を依頼
            ICommand instantActionCommand = ai.Think(_fieldService, playerData);

            // もし実行のコマンドが返ってきたら、Dispatcherに横流し
            if (instantActionCommand != null)
            {
                _dispatcher.Dispatch(instantActionCommand);
            }

            // TODO: Mediator経由でViewに赤いマスを描画させる？敵の思考完了イベントを送る？
        }
    }
}
