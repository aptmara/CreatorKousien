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

        // 実行フェーズで1手ずつ取り出すためのアクション予約リスト
        private Queue<ActionRuntimeData> _actionQueue = new Queue<ActionRuntimeData>();

        /// <summary>
        /// バトル開始時の初期化処理
        /// </summary>
        /// <param name="dispatcher"></param>
        public void Initialize(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _phaseManager = new PhaseManager();

            // ステートを登録
            _phaseManager.RegisterState(new CommandPhaseState(this));
            _phaseManager.RegisterState(new ActionPhaseState(this));
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
    }
}
