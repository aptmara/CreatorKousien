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
using UnityEngine.Rendering;
using System.Linq;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 1手毎のプレイヤーと敵のアクションをまとめる箱
    /// </summary>
    public class BattleStep
    {
        public ActionRuntimeData PlayerAction {  get; set; }
        public ActionRuntimeData EnemyAction { get; set; }

        public BattleStep(ActionRuntimeData p, ActionRuntimeData e)
        {
            PlayerAction = p;
            EnemyAction = e;
        }
    }

    public class TurnManager : MonoBehaviour
    {
        [Header("コマンドフェーズの設定")]
        [SerializeField] private int _maxInputCount = 3;        // プレイヤーがコマンドフェーズで入力できる最大アクション数
        [SerializeField] private float _commandTimeLimit = 3f;  // コマンドフェーズの時間制限（秒）
        [SerializeField] private int _visibleEnemyAction = 5;   // コマンドフェーズで視認可能な敵の行動個数

        /// <summary>
        /// 最大入力数
        /// </summary>
        public int MaxInputCount => _maxInputCount;

        /// <summary>
        /// コマンドフェーズの時間制限
        /// </summary>
        public float CommandTimeLimit => _commandTimeLimit;

        /// <summary>
        /// 敵の最大行動視認可能数
        /// </summary>
        public int VisibleEnemyAction => _visibleEnemyAction;

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

        // ステップ単位のキューと、そのステップ内で順番に処理するマイクロキュー
        private Queue<BattleStep> _stepQueue = new Queue<BattleStep>();
        private Queue<ActionRuntimeData> _microQueue = new Queue<ActionRuntimeData>();
        // 敵の行動のみをスタックするキュー
        private List<ActionRuntimeData> _enemyActions = new List<ActionRuntimeData>();
        public IReadOnlyList<ActionRuntimeData> EnemyActions =>_enemyActions;

        public List<ActionRuntimeData> _enemyTelegraphs = new List<ActionRuntimeData>();

        // このステップでキャンセルされたアクターのIDのリスト
        private HashSet<int> _cancelledActorsThisStep = new HashSet<int>();

        private BattleStep _currentStep;
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
        /// <param name="steps"></param>
        public void SetActionQueue(List<BattleStep> steps)
        {
            _stepQueue.Clear();
            _microQueue.Clear();
            _currentTimelineActionIndex = -1;
            _eventBus?.PublishTimelineActionExecutionChanged(-1);

            foreach (var step in steps)
            {
                _stepQueue.Enqueue(step);
            }
            Debug.Log($"[TurnManager] {steps.Count}ステップ分の同時アクションキューを構築しました。");
        }

        /// <summary>
        /// 次のアクションを実行フェーズの指示で発火させる
        /// </summary>
        public void ExecuteNextAction()
        {
            // 1. 現在のステップ内にまだ未実行のアクションがあればそれを実行
            if (_microQueue.Count > 0)
            {
                // 全ての手順が終了したらターン終了フェーズへ
                var microAction = _microQueue.Dequeue();

                // 順番が回ってきた時に、すでにキャンセルされていたら待機にする
                if (_cancelledActorsThisStep.Contains(microAction.ActorId))
                {
                    Debug.Log($"[TurnManager] ActorID:{microAction.ActorId} の行動はキャンセル(スタン)されているため不発！");
                    microAction = new ActionRuntimeData(microAction.ActorId);
                }

                DispatchAction(microAction);
                return;
            }

            // 2. ステップが残っていれば、次のステップを取り出して【三すくみ判定】を行う
            if (_stepQueue.Count > 0)
            {
                _cancelledActorsThisStep.Clear();
                _currentStep = _stepQueue.Dequeue();

                // 取り出した後に、UIへ通知を行う
                _currentTimelineActionIndex++;
                _eventBus?.PublishTimelineActionExecutionChanged(_currentTimelineActionIndex);

                var pAction = _currentStep.PlayerAction;
                var eAction = _currentStep.EnemyAction;

                // 優先度で並び替えてマイクロキューに入れる
                int pPriority = GetPriority(pAction.Type);
                int ePriority = GetPriority(eAction.Type);

                if (pPriority >= ePriority)
                {
                    _microQueue.Enqueue(pAction);
                    _microQueue.Enqueue(eAction);
                }
                else
                {
                    _microQueue.Enqueue(eAction);
                    _microQueue.Enqueue(pAction);
                }

                // キューに詰めたので、1つ目のアクションを実行へ回す
                ExecuteNextAction();
                return;
            }

            // 3. 全て終了したらターンエンド
            _currentTimelineActionIndex = -1;
            _eventBus?.PublishTimelineActionExecutionChanged(-1);
            _phaseManager.TransitionTo(PhaseType.TurnEnd);
        }

        /// <summary>
        /// アクションのシステム絶対優先度
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private int GetPriority(ActionType type)
        {
            switch (type)
            {
                case ActionType.Guard:      return 40;  // ガードは最速
                case ActionType.Move:       return 30;  // 移動で避けるのが次
                case ActionType.FastAttack: return 20;  // 出の早い攻撃
                case ActionType.WideAttack: return 10;  // 大振り
                default:                    return 0;   // 待機
            }
        }

        /// <summary>
        /// コマンドの発行
        /// </summary>
        /// <param name="nextAction"></param>
        private void DispatchAction(ActionRuntimeData nextAction)
        {
            // 相手が今何をしているかを取得
            Dictionary<int, ActionType> stepActions = new Dictionary<int, ActionType>();
            if (_currentStep != null)
            {
                stepActions[_currentStep.PlayerAction.ActorId] = _currentStep.PlayerAction.Type;
                stepActions[_currentStep.EnemyAction.ActorId] = _currentStep.EnemyAction.Type;
            }

            switch (nextAction.Type)
            {
                case ActionType.FastAttack:
                case ActionType.WideAttack:
                    _dispatcher.Dispatch(new Command.AttackCommand(
                        nextAction.ActorId,
                        nextAction.Type,
                        nextAction.Property,
                        nextAction.TargetCells,
                        stepActions,
                        nextAction.IsDynamicOrigin,
                        nextAction.RelativeCells,
                        (targetId) => _cancelledActorsThisStep.Add(targetId)
                    ));
                    break;

                case ActionType.Move:
                    _dispatcher.Dispatch(new Command.MoveCommand(nextAction.ActorId, nextAction.MoveDirection, 1));
                    break;

                case ActionType.Guard:
                    Debug.Log($"[TurnManager] ActorID:{nextAction.ActorId} は防御の構えをとった。(Guard)");
                    _eventBus.PublishActionLogicCompleted(nextAction.ActorId);
                    break;

                default:
                    Debug.Log($"[TurnManager] ActorID:{nextAction.ActorId} は待機/スタンした。(Wait)");
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

        /// <summary>
        /// 敵の行動予告を格納する
        /// </summary>
        /// <param name="data"></param>
        public void AddEnemyActionTelegraph(List<ActionRuntimeData> data)
        {
            foreach (var action in data)
            {
                _enemyActions.Add(action);
            }
        }

        /// <summary>
        /// 敵の行動予告を取得
        /// </summary>
        /// <returns></returns>
        public ActionRuntimeData GetEnemyActionTelegraph()
        {
            ActionRuntimeData action;
            action = _enemyActions.FirstOrDefault();
            _enemyActions.Remove(action);
            return action;
        }
    }
}
