// ================================================================================
// File         : AttackTelegraphSystem.cs
// Author       : Iwai Shogo
//
// Description  : 攻撃予告の進行とキャンセルを管理するシステム。
// Created      : 2026-04-13
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Command;

namespace CreatorKousien.Enemy
{
    public class AttackTelegraphSystem
    {
        // 発令中の全予告リスト
        private List<TelegraphRuntimeData> _activeTelegraphs = new List<TelegraphRuntimeData>();

        /// <summary>
        /// EnemyAIから呼ばれる、新しい予告の登録
        /// </summary>
        /// <param name="newTelegraph"></param>
        public void RegisterTelegraph(TelegraphRuntimeData newTelegraph)
        {
            _activeTelegraphs.Add(newTelegraph);
            // TODO: GameManagerにイベントを飛ばす？
        }

        /// <summary>
        /// ターン経過処理。RemainingTurnを減らし、0になったら攻撃コマンドを発行する。
        /// TurnEndUseCaseから呼ばれる想定
        /// </summary>
        /// <returns></returns>
        public List<AttackCommand> ProcessTurn()
        {
            List<AttackCommand> commandsToExecute = new List<AttackCommand>();

            for (int i = _activeTelegraphs.Count - 1; i >= 0; i--)
            {
                var telegraph = _activeTelegraphs[i];
                telegraph.RemainingTurn--;

                if (telegraph.RemainingTurn <= 0)
                {
                    commandsToExecute.Add(new Command.AttackCommand
                    (
                        telegraph.SourceActorId,
                        telegraph.AttackInfo,
                        telegraph.TargetCells
                    ));

                    _activeTelegraphs.RemoveAt(i);
                    // TODO: GameManagerにイベントを飛ばす？
                }
            }
            return commandsToExecute;
        }

        /// <summary>
        /// プレイヤーの攻撃によるキャンセル処理
        /// </summary>
        /// <param name="sourceActorId"></param>
        public void TryInterrupt(int sourceActorId)
        {
            // IsInterruptible が true の予告だけを消す
            int removeCount = _activeTelegraphs.RemoveAll(t => t.SourceActorId == sourceActorId && t.IsInterruptible);

            if (removeCount > 0)
            {
                // TODO: マスの演出を消すイベントを飛ばす
            }
        }

        public void CancelTelegraphsByActorId(int actorId)
        {
            int removeCount = _activeTelegraphs.RemoveAll(t => t.SourceActorId == actorId);

            if (removeCount > 0)
            {
                Debug.Log($"[AttackTelegraphSystem] Actor:{actorId} の予告を {removeCount} 件キャンセルしました。");
                // TODO: Viewへ赤いマスを消すように通知
            }
        }
    }
}
