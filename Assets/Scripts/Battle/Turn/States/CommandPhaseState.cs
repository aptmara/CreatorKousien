// ================================================================================
// File         : CommandPhaseState.cs
// Author       : Iwai Shogo
//
// Description  : コマンドフェーズの処理を行います。
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// コマンドフェーズの処理を行います。
    /// </summary>
    public class CommandPhaseState : IBattlePhaseState
    {
        public PhaseType Type => PhaseType.Command;
        private readonly TurnManager _owner;

        // プレイヤーがこのフェーズ中に選択したアクション(最大3手)
        private List<ActionRuntimeData> _playerSelectedActions = new List<ActionRuntimeData>();

        // 敵がこのターンに行う予定のアクション
        private List<ActionRuntimeData> _enemyPlannedActions = new List<ActionRuntimeData>();

        public CommandPhaseState(TurnManager owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("--- コマンドフェーズ開始 ---");
            _playerSelectedActions.Clear();
            _enemyPlannedActions.Clear();

            _owner.NotifyCommandPhaseStarted();   // コマンドフェーズ開始のイベントを発火（デバック用なので改変しても大丈夫）

            // TODO: EventBus経由で敵の予告表示をViewに出す
        }

        public void Update()
        {
            // 時間ゲージのカウントダウンロジック、敵の思考、カード選択等
        }

        /// <summary>
        /// 時間切れ、またはプレイヤーの決定ボタンで呼ばれる
        /// </summary>
        public void Exit()
        {
            // ここでプレイヤーの3手と敵の3手を合体させて交互に並べる
        }

        /// <summary>
        /// プレイヤーがカードを選んだり、移動を入力した時に呼ばれる
        /// </summary>
        /// <param name="action"></param>
        public void AddPlayerAction(ActionRuntimeData action)
        {
            if (_playerSelectedActions.Count >= 3) return;

            _playerSelectedActions.Add(action);
            Debug.Log($"[Command] プレイヤーのアクションを予約: {action.Category} ({_playerSelectedActions.Count}/3)");

            // 3手選んだら自動で実行フェーズへ
            if (_playerSelectedActions.Count == 3)
            {
                FinishPhase();
            }
        }

        private void FinishPhase()
        {
            // プレイヤーと敵のアクションを交互に並べて、TurnManagerに渡す
            List<ActionRuntimeData> mergedQueue = InterleaveActions(_playerSelectedActions, _enemyPlannedActions);

            _owner.SetActionQueue(mergedQueue);
            _owner.TransitionTo(PhaseType.Action);
        }

        /// <summary>
        /// プレイヤーと敵のアクションを交互に並べるロジック
        /// </summary>
        /// <param name="pActions"></param>
        /// <param name="eActions"></param>
        /// <returns></returns>
        private List<ActionRuntimeData> InterleaveActions(List<ActionRuntimeData> pActions, List<ActionRuntimeData> eActions)
        {
            List<ActionRuntimeData> result = new List<ActionRuntimeData>();
            int maxSteps = 3;

            for (int i = 0; i < maxSteps; i++)
            {
                if (i < pActions.Count) result.Add(pActions[i]);
                if (i < eActions.Count) result.Add(eActions[i]);
            }

            return result;
        }


        /// <summary>
        /// 敵のアクションを予約するためのメソッド。（デバック用なので改変しても大丈夫）
        /// </summary>
        /// <param name="action"></param>
        public void AddEnemyAction(ActionRuntimeData action)
        {
            if (_enemyPlannedActions.Count >= 3) return;

            _enemyPlannedActions.Add(action);
            Debug.Log($"[Command] 敵のアクションを予約: {action.Category} ({_enemyPlannedActions.Count}/3)");
        }
    }
}
