// ------------------------------------------------------------
// File		: GameEventBus.cs
// Summary	: ゲーム内のイベントを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - システムからViewへ結果を通知するためのイベントバス
// ------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;
using CreatorKousien.Battle;

namespace CreatorKousien.Core
{
    /// <summary>
    /// UseCaseでの処理結果を、Viewに伝えられるためのイベントバス
    /// </summary>
    public class GameEventBus
    {
        // イベントの定義
        // ------------------------------------------------------------

        /// <summary>
        /// 攻撃のヒットを通知するイベント
        /// </summary>
        public event Action<int> OnAttackHit;

        /// <summary>
        /// 敵の攻撃予告の表示/非表示を通知するイベント
        /// </summary>
        public event Action<List<Vector2Int>, bool, int> OnTelegraphRequested;

        /// <summary>
        /// 敵の移動予告を通知するイベント
        /// </summary>
        public event Action<Vector2Int, bool, int> OnMoveTelegraphRequested;

        /// <summary>
        /// 画面上の全ての予告表示をクリアするイベント
        /// </summary>
        public event Action OnClearAllTelegraphsRequested;

        /// <summary>
        /// 攻撃が発動したマスを一斉に光らせるイベント。引数は攻撃が発動したマスの座標リスト。
        /// </summary>
        public System.Action<List<Vector2Int>> OnAttackAreaExecuted;

        /// <summary>
        /// 移動に失敗したことを通知するイベント(ActorID, 失敗した先のX, 失敗した先のY)
        /// </summary>
        public System.Action<int, int, int> OnMoveFailed;

        /// <summary>
        /// ダメージを受けたことを通知するイベント
        /// 誰が、どれだけのダメージを受けたかを引数で渡す
        /// </summary>
        public event Action<int, int> OnDamageTaken;

        /// <summary>
        /// タイムラインが更新されたことを通知するイベント
        /// </summary>
        public event System.Action<List<ActionType>, List<ActionType>> OnTimelineUpdated;


        /// <summary>
        /// アクターの死亡を通知するイベント
        /// 誰が死亡したかを引数で渡す
        /// </summary>
        public event Action<int> OnActorDeath;

        /// <summary>
        /// アクションのロジック計算が完全に終了したことを通知するイベント
        /// 引数は行動したActorID
        /// </summary>
        public event Action<int> OnActionLogicCompleted;

        /// <summary>
        /// コマンドフェーズが開始したことを全体に通知する
        /// </summary>
        public event Action OnCommandPhaseStarted;

        /// <summary>
        /// 特定のアクターが座標移動を完了し、見た目を動かすべきタイミングで発火
        /// </summary>
        public event Action<int, Vector2Int> OnActorMoveRequested;

        /// <summary>
        /// コマンドフェーズのタイマーが更新されたことを通知するイベント
        /// 引数1: 残り時間, 引数2: 最大時間
        /// </summary>
        public event Action<float, float> OnCommandTimerUpdated;

        /// <summary>
        /// コマンドフェーズの制限時間が切れたことを通知するイベント
        /// </summary>
        public event Action OnCommandTimeUp;






        // イベントの発火メソッド
        // ------------------------------------------------------------

        /// <summary>
        /// 攻撃ヒットイベントを発火するメソッド
        /// </summary>
        /// <param name="targetActorId">対象のアクターID</param>
        public void PublishAttackHit(int targetActorId)
        {
            OnAttackHit?.Invoke(targetActorId);
        }


        /// <summary>
        /// 攻撃予告の表示イベントを発火するメソッド
        /// </summary>
        /// <param name="targetCells">表示するセルのリスト</param>
        /// <param name="isWarning">  警告表示かどうか</param>
        public void PublishTelegraph(List<Vector2Int> targetCells, bool isWarning, int stepOrder = 0)
        {
            OnTelegraphRequested?.Invoke(targetCells, isWarning, stepOrder);
        }


        /// <summary>
        /// 移動予告の表示イベントを発火するメソッド
        /// </summary>
        /// <param name="targetCell"></param>
        /// <param name="isWarning"></param>
        /// <param name="stepOrder"></param>
        public void PublishMoveTelegraph(Vector2Int targetCell, bool isWarning, int stepOrder = 0)
        {
            OnMoveTelegraphRequested?.Invoke(targetCell, isWarning, stepOrder);
        }


        /// <summary>
        /// 攻撃が発動したエリアの視覚的ハイライトを要求するメソッド
        /// </summary>
        /// <param name="targetCells"></param>
        public void PublishAttackAreaExecuted(List<Vector2Int> targetCells)
        {
            OnAttackAreaExecuted?.Invoke(targetCells);
        }


        /// <summary>
        /// 予告表示クリアイベントを発火するイベント
        /// </summary>
        public void PublishClearAllTelegraphs()
        {
            OnClearAllTelegraphsRequested?.Invoke();
        }


        /// <summary>
        /// ダメージイベントを発火するメソッド
        /// </summary>
        /// <param name="targetActorId">ダメージを受けたアクターのID</param>
        /// <param name="damageAmount">ダメージ量</param>
        public void PublishDamageTaken(int targetActorId, int damageAmount)
        {
            OnDamageTaken?.Invoke(targetActorId, damageAmount);
        }


        /// <summary>
        /// アクターの死亡イベントを発火するメソッド
        /// </summary>
        /// <param name="actorId">死亡したアクターのID</param>
        public void PublishActorDeath(int actorId)
        {
            OnActorDeath?.Invoke(actorId);
        }


        /// <summary>
        /// アクション終了イベントを発火するメソッド
        /// </summary>
        /// <param name="actorId">行動したActorID</param>
        public void PublishActionLogicCompleted(int actorId)
        {
            OnActionLogicCompleted?.Invoke(actorId);
        }


        /// <summary>
        /// コマンドフェーズ開始イベントを発火するメソッド
        /// </summary>
        public void PublishCommandPhaseStarted()
        {
            OnCommandPhaseStarted?.Invoke();
        }


        /// <summary>
        /// 特定のアクターの座標移動イベントを発火するメソッド
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="targetGridPos"></param>
        public void PublishActorMoveRequested(int actorId, Vector2Int targetGridPos)
        {
            OnActorMoveRequested?.Invoke(actorId, targetGridPos);
        }


        /// <summary>
        /// タイマー更新イベントを発火するメソッド
        /// </summary>
        /// <param name="currentTime"></param>
        /// <param name="maxTime"></param>
        public void PublishCommandTimerUpdated(float currentTime, float maxTime)
        {
            OnCommandTimerUpdated?.Invoke(currentTime, maxTime);
        }


        /// <summary>
        /// タイムアップイベントを発火するメソッド
        /// </summary>
        public void PublishCommandTimeUp()
        {
            OnCommandTimeUp?.Invoke();
        }


        /// <summary>
        /// タイムライン更新イベントを発火するメソッド
        /// </summary>
        /// <param name="enemyActions">敵の行動リスト</param>
        /// <param name="playerActions">プレイヤーの行動リスト</param>
        public void PublishTimelineUpdated(List<ActionType> enemyActions, List<ActionType> playerActions)
        {
            OnTimelineUpdated?.Invoke(enemyActions, playerActions);
        }
    }
}
