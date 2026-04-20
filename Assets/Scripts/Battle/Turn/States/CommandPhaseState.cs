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
using UnityEngine.InputSystem;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// コマンドフェーズの処理を行います。
    /// </summary>
    public class CommandPhaseState : IBattlePhaseState
    {
        public PhaseType Type => PhaseType.Command;
        private readonly TurnManager _owner;
        private readonly HandState _handState;

        // プレイヤーがこのフェーズ中に選択したアクション(最大3手)
        private List<ActionRuntimeData> _playerSelectedActions = new List<ActionRuntimeData>();

        // 敵がこのターンに行う予定のアクション
        private List<ActionRuntimeData> _enemyPlannedActions = new List<ActionRuntimeData>();

        // --- タイマー機能用の変数 ---
        private readonly float _timeLimit = 5.0f;
        private float _currentTime;
        private bool _isTimeUp;

        public CommandPhaseState(TurnManager owner, HandState handState)
        {
            _owner = owner;
            _handState = handState;
        }

        public void Enter()
        {
            Debug.Log("--- コマンドフェーズ開始 ---");

            // デバッグログ: 現在のカード状況を報告
            ReportCurrentHand();

            _playerSelectedActions.Clear();
            _enemyPlannedActions.Clear();

            // タイマーの初期化
            _currentTime = _owner.CommandTimeLimit;
            _isTimeUp = false;

            _owner.EventBus.PublishCommandPhaseStarted();

            // 新しい思考を始める前に、画面上の前回の予告を全て消す
            _owner.EventBus.PublishClearAllTelegraphs();

            // フェーズ開始と同時に、全てのエネミーの3手分を計算させる
            var planCommand = new EnemyActionCommand((teamPlan) =>
            {
                _enemyPlannedActions = teamPlan;
                UpdateTimelineUI();
                Debug.Log($"[Command Phase] 敵チーム全体の3手を受領しました！");
            });

            _owner.Dispatcher.Dispatch(planCommand);

            // TODO: EventBus経由で敵の予告表示をViewに出す
        }

        private void ReportCurrentHand()
        {
            string report = "<b>【現在の手札状況】</b>\n";
            foreach (SlotPosition slot in System.Enum.GetValues(typeof(SlotPosition)))
            {
                var card = _handState.GetCard(slot);
                if (card != null)
                {
                    var effect = card.GetCurrentEffect();
                    string faceStr = card.CurrentFace == CardFace.Front ? "<color=lime>【表】</color>" : "<color=yellow>【裏】</color>";
                    report += $"{slot}: {card.BaseData.CardName} {faceStr} -> 実行内容: {effect.EffectName} ({effect.Type})\n";
                }
            }
            Debug.Log(report);
        }

        public void Update()
        {
            // 時間ゲージのカウントダウンロジック、カード選択等
            // タイムアップしているか、3手入力済みなら何もしない
            if (_isTimeUp || _playerSelectedActions.Count >= _owner.MaxInputCount) return;

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

            HandleInput();
        }


        /// <summary>
        /// プレイヤーの入力を処理するメソッド。キーボードとゲームパッドの両方に対応しています。
        /// </summary>
        private void HandleInput()
        {
            // キーボードとゲームパッドの両方に対応した入力処理
            var kb = Keyboard.current;
            var pad = Gamepad.current;

            bool pressUp = (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame)) ||
                           (pad != null && (pad.dpad.up.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame));
            bool pressDown = (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame)) ||
                             (pad != null && (pad.dpad.down.wasPressedThisFrame || pad.buttonSouth.wasPressedThisFrame));
            bool pressLeft = (kb != null && (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)) ||
                             (pad != null && (pad.dpad.left.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame));
            bool pressRight = (kb != null && (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)) ||
                              (pad != null && (pad.dpad.right.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame));

            if (pressUp) _owner.Dispatcher.Dispatch(new PlayerActionCommand(SlotPosition.Up));
            if (pressDown) _owner.Dispatcher.Dispatch(new PlayerActionCommand(SlotPosition.Down));
            if (pressLeft) _owner.Dispatcher.Dispatch(new PlayerActionCommand(SlotPosition.Left));
            if (pressRight) _owner.Dispatcher.Dispatch(new PlayerActionCommand(SlotPosition.Right));

            if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame))
            {
                // 決定ボタンでフェーズを強制終了
                Debug.Log("<color=yellow>[Command Phase] プレイヤーが決定ボタンを押しました。フェーズを終了します。</color>");
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
            UpdateTimelineUI();
            Debug.Log($"[Command] プレイヤーのアクションを予約: {action.Type} ({_playerSelectedActions.Count}/3)");

            // 3手選んだら自動で実行フェーズへ
            if (_playerSelectedActions.Count == _owner.MaxInputCount)
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
            while (_playerSelectedActions.Count < _owner.MaxInputCount)
            {
                var waitAction = new ActionRuntimeData(
                    1,
                    ActionType.Wait,
                    new ActionProperty { DamageMultiplier = 0 },
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
            int maxSteps = _owner.MaxInputCount;

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
            if (_enemyPlannedActions.Count >= _owner.MaxInputCount) return;

            _enemyPlannedActions.Add(action);
            Debug.Log($"[Command] 敵のアクションを予約: {action.Type} ({_enemyPlannedActions.Count}/{_owner.MaxInputCount})");
        }


        /// <summary>
        /// タイムラインUIを更新するためのメソッド。プレイヤーと敵のアクションをViewに通知します。
        /// </summary>
        private void UpdateTimelineUI()
        {
            var eTypes = new List<ActionType>();
            foreach (var a in _enemyPlannedActions)
            {
                eTypes.Add(a.Type);
            }

            var pTypes = new List<ActionType>();
            foreach (var a in _playerSelectedActions)
            {
                pTypes.Add(a.Type);
            }

            _owner.EventBus.PublishTimelineUpdated(eTypes, pTypes);
        }
    }
}
