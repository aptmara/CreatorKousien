// ================================================================================
// File         : CommandPhaseState.cs
// Author       : Iwai Shogo
//
// Description  : コマンドフェーズの処理を行います。
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Command;
using CreatorKousien.Data;

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

        // --- タイマー機能用の変数 ---
        private readonly float _timeLimit = 5.0f;
        private float _currentTime;
        private bool _isTimeUp;

        public CommandPhaseState(TurnManager owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("--- コマンドフェーズ開始 ---");
            _playerSelectedActions.Clear();
            _enemyPlannedActions.Clear();

            // タイマーの初期化
            _currentTime = _timeLimit;
            _isTimeUp = false;

            _owner.EventBus.PublishCommandPhaseStarted();

            // フェーズ開始と同時に、全てのエネミーの3手分を計算させる
            var planCommand = new EnemyActionCommand((teamPlan) =>
            {
                _enemyPlannedActions = teamPlan;
                Debug.Log($"[Command Phase] 敵チーム全体の3手を受領しました！");
            });

            _owner.Dispatcher.Dispatch(planCommand);

            // TODO: EventBus経由で敵の予告表示をViewに出す
        }

        public void Update()
        {
            // 時間ゲージのカウントダウンロジック、カード選択等

            // 既にタイムアップしているか、3手入力済みなら何もしない
            if (_isTimeUp || _playerSelectedActions.Count >= 3) return;

            // タイマーを減算
            _currentTime -= Time.deltaTime;

            // Viewへ毎フレーム残り時間を通知
            _owner.EventBus.PublishCommandTimerUpdated(_currentTime, _timeLimit);

            // タイムアップ判定
            if (_currentTime <= 0f)
            {
                _currentTime = 0f;
                _isTimeUp = true;

                Debug.Log("<color=red>[Command Phase] タイムアップ！未入力の枠は待機になります。</color>");
                _owner.EventBus.PublishCommandTimeUp();

                // 足りない手数を強制的に待機で埋めて次へ
                FillRemainingActionsWithWait();
                FinishPhase();
            }
        }

        /// <summary>
        /// 時間切れ、またはプレイヤーの決定ボタンで呼ばれる
        /// </summary>
        public void Exit()
        {
            // クリーンアップ処理
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

        // 足りない手数を待機で埋めるメソッド
        private void FillRemainingActionsWithWait()
        {
            while (_playerSelectedActions.Count < 3)
            {
                var waitAction = new ActionRuntimeData(
                    1,
                    new AttackProperty { DamageMultiplier = 0 },
                    new List<Vector2Int>()
                );
                _playerSelectedActions.Add(waitAction);
            }
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
