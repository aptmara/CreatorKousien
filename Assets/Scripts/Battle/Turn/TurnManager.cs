// ================================================================================
// File         : TurnManager.cs
// Author       : Iwai Shogo
//
// Description  : バトル全体の進行管理。
//                PhaseManagerを制御し、6手分のアクションキューを消化します。
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Core;
using System.Linq.Expressions;

namespace CreatorKousien.Battle
{
    public class TurnManager : MonoBehaviour
    {
        private PhaseManager _phaseManager;
        private CommandDispatcher _dispatcher;

        private CommandPhaseState _commandPhase;
        private ActionPhaseState  _actionPhase;

        // 実行フェーズで1手ずつ取り出すためのアクション予約リスト
        private Queue<ActionRuntimeData> _actionQueue = new Queue<ActionRuntimeData>();

        /// <summary>
        /// コマンドフェーズが開始したタイミングで発火するイベント。
        /// TODO: これをEventBusに移すか、Mediator経由でViewに通知するかは要検討ですが、とりあえず置いておきます。
        /// </summary>
        public event System.Action OnCommandPhaseStarted;

        /// <summary>
        /// バトル開始時の初期化処理
        /// </summary>
        /// <param name="dispatcher"></param>
        public void Initialize(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _phaseManager = new PhaseManager();

            _commandPhase = new CommandPhaseState(this);
            _actionPhase = new ActionPhaseState(this);

            // ステートを登録
            _phaseManager.RegisterState(_commandPhase);
            _phaseManager.RegisterState(_actionPhase);
            _phaseManager.RegisterState(new TurnEndState(this));

            // 最初のフェーズへ遷移
            _phaseManager.TransitionTo(PhaseType.Command);
        }

        private void Update()
        {
            // 現在のフェーズの Update を実行
            _phaseManager?.Update();
        }

        /// <summary>
        /// 外部から決定したアクションリストを受け取りキューに積める
        /// </summary>
        /// <param name="actions"></param>
        public void SetActionQueue(List<ActionRuntimeData> actions)
        {
            _actionQueue.Clear();
            foreach (var action in actions)
            {
                _actionQueue.Enqueue(action);
            }
            Debug.Log($"[TurnManager] アクションキューを構築しました。件数: {_actionQueue.Count}");
        }

        /// <summary>
        /// 次のアクションを実行フェーズの指示で発火させる
        /// </summary>
        public void ExecuteNextAction()
        {
            if (_actionQueue.Count == 0)
            {
                // 全ての手順が終了したらターン終了フェーズへ
                _phaseManager.TransitionTo(PhaseType.TurnEnd);
                return;
            }

            // キューから1手取り出す
            var nextAction = _actionQueue.Dequeue();

            // チケットのカテゴリに応じて、適切なCommandとしてDispatcherに投げる
            switch (nextAction.Category)
            {
                // AttackUseCaseへ
                case ActionCategory.Attack:
                    _dispatcher.Dispatch(new Command.AttackCommand(
                        nextAction.ActorId,
                        nextAction.AttackInfo,
                        nextAction.TargetCells));
                    break;

                // MoveUseCaseへ
                case ActionCategory.Move:
                    _dispatcher.Dispatch(new Command.MoveCommand(
                        nextAction.ActorId,
                        nextAction.MoveDirection,
                        1));    // 1マス移動
                    break;
            }
        }

        /// <summary>
        /// フェーズ遷移のショートカット
        /// </summary>
        /// <param name="type"></param>
        public void TransitionTo(PhaseType type) => _phaseManager.TransitionTo(type);



        // 外部からアクションを受け取るためのインターフェース
        // ------------------------------------------------------------

        /// <summary>
        /// プレイヤーがアクションを選んだ後、Commandフェーズから呼び出される想定のメソッド。
        /// </summary>
        /// <param name="action">選んだアクション</param>
        public void SubmitPlayerAction(ActionRuntimeData action)
        {
            if (_phaseManager.CurrentPhaseType == PhaseType.Command)
            {
                _commandPhase.AddPlayerAction(action);
            }
        }


        /// <summary>
        /// View側でアクションのアニメーションが完了したタイミングで呼び出される想定のメソッド。
        /// </summary>
        public void CompleteCurrentActionAnimation()
        {
            if (_phaseManager.CurrentPhaseType == PhaseType.Action)
            {
                _actionPhase.OnActionAnimationComplete();
            }
        }


        /// <summary>
        /// コマンドフェーズが開始したタイミングで呼び出される想定のメソッド。
        /// </summary>
        public void NotifyCommandPhaseStarted()
        {
            OnCommandPhaseStarted?.Invoke();
        }


        /// <summary>
        /// 敵がアクションを決定した後、Commandフェーズから呼び出される想定のメソッド。（デバック用なので改変しても大丈夫）
        /// </summary>
        /// <param name="action"></param>
        public void SubmitEnemyAction(ActionRuntimeData action)
        {
            if (_phaseManager.CurrentPhaseType == PhaseType.Command)
            {
                _commandPhase.AddEnemyAction(action);
            }
        }

    }
}
