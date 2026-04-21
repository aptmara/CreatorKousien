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
using CreatorKousien.Data;

namespace CreatorKousien.Battle
{
    public class TurnManager : MonoBehaviour
    {
        [Header("コマンドフェーズの設定")]
        [SerializeField] private int _maxInputCount = 3;        // プレイヤーがコマンドフェーズで入力できる最大アクション数
        [SerializeField] private float _commandTimeLimit = 3f;  // コマンドフェーズの時間制限（秒）

        /// <summary>
        /// 最大入力数
        /// </summary>
        public int MaxInputCount => _maxInputCount;

        /// <summary>
        /// コマンドフェーズの時間制限
        /// </summary>
        public float CommandTimeLimit => _commandTimeLimit;


        /// <summary>
        /// プレイヤーがカードを使ったことを通知するイベント
        /// </summary>
        public event System.Action OnPlayerActionSubmitted;


        private PhaseManager _phaseManager;
        private CommandDispatcher _dispatcher;
        private GameEventBus _eventBus;

        public CommandDispatcher Dispatcher => _dispatcher;
        public GameEventBus EventBus => _eventBus;

        private CommandPhaseState _commandPhase;
        private ActionPhaseState  _actionPhase;

        // 実行フェーズで1手ずつ取り出すためのアクション予約リスト
        private Queue<ActionRuntimeData> _actionQueue = new Queue<ActionRuntimeData>();
        private int _currentTimelineActionIndex = -1;

        /// <summary>
        /// バトル開始時の初期化処理
        /// </summary>
        /// <param name="dispatcher"></param>
        public void Initialize(CommandDispatcher dispatcher, GameEventBus eventBus, HandState handState)
        {
            _dispatcher = dispatcher;
            _eventBus = eventBus;
            _phaseManager = new PhaseManager();
            _currentTimelineActionIndex = -1;

            _commandPhase = new CommandPhaseState(this, handState);
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
            _currentTimelineActionIndex = -1;
            _eventBus?.PublishTimelineActionExecutionChanged(-1);
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
                _currentTimelineActionIndex = -1;
                _eventBus?.PublishTimelineActionExecutionChanged(-1);
                _phaseManager.TransitionTo(PhaseType.TurnEnd);
                return;
            }

            // キューから1手取り出す
            var nextAction = _actionQueue.Dequeue();
            _currentTimelineActionIndex++;
            _eventBus?.PublishTimelineActionExecutionChanged(_currentTimelineActionIndex);

            // チケットのカテゴリに応じて、適切なCommandとしてDispatcherに投げる
            switch (nextAction.Type)
            {
                // AttackUseCaseへ
                case ActionType.FastAttack:
                case ActionType.WideAttack:
                    _dispatcher.Dispatch(new Command.AttackCommand(
                        nextAction.ActorId,
                        nextAction.Property,
                        nextAction.TargetCells,
                        nextAction.IsDynamicOrigin,
                        nextAction.RelativeCells
                    ));
                    break;

                // MoveUseCaseへ
                case ActionType.Move:
                    _dispatcher.Dispatch(new Command.MoveCommand(
                        nextAction.ActorId,
                        nextAction.MoveDirection,
                        1));    // 1マス移動
                    break;

                case ActionType.Guard:
                    // TODO: 将来的にGuardCommandを発行
                    Debug.Log($"[TurnManager] ActorID:{nextAction.ActorId} は防御(Guard)の構えをとった。");
                    _eventBus.PublishActionLogicCompleted(nextAction.ActorId);
                    break;

                default:
                    // 待機の場合
                    Debug.Log($"[TurnManager] ActorID:{nextAction.ActorId} は待機、または行動をスキップした。");
                    _eventBus.PublishActionLogicCompleted(nextAction.ActorId);
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

                // プレイヤーのアクションが提出されたことを通知
                OnPlayerActionSubmitted?.Invoke();
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


        /// <summary>
        /// 現在のターンでスロットが既に使用済みか判定する
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public bool IsSlotUsedThisTurn(SlotPosition slot)
        {
            if (_phaseManager != null && _phaseManager.CurrentPhaseType == PhaseType.Command)
            {
                return _commandPhase.IsSlotUsed(slot);
            }
            return false;
        }
    }
}
