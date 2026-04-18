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
        public event Action<List<Vector2Int>, bool> OnTelegraphRequested;


        /// <summary>
        /// ダメージを受けたことを通知するイベント
        /// 誰が、どれだけのダメージを受けたかを引数で渡す
        /// </summary>
        public event Action<int, int> OnDamageTaken;


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
        public void PublishTelegraph(List<Vector2Int> targetCells, bool isWarning)
        {
            OnTelegraphRequested?.Invoke(targetCells, isWarning);
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
    }
}
