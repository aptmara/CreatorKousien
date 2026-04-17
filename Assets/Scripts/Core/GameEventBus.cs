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
        /// <summary>
        /// 攻撃のヒットを通知するイベント
        /// </summary>
        public event Action<int> OnAttackHit;

        /// <summary>
        /// 敵の攻撃予告の表示/非表示を通知するイベント
        /// </summary>
        public event Action<List<Vector2Int>, bool> OnTelegraphRequested;


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
        public void PublishTelegraph(List<Vector2> targetCells, bool isWarning)
        {
            OnTelegraphRequested?.Invoke(targetCells, isWarning);
        }
    }
}
