// ================================================================================
// File         : ActionTelegraphSystem.cs
// Author       : Iwai Shogo
//
// Description  : 攻撃予告の進行とキャンセルを管理するシステム。
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 盤面上に表示されている全ての予告を管理する。
    /// </summary>
    public class ActionTelegraphSystem
    {
        // 発令中の全予告リスト
        private List<TelegraphRuntimeData> _activeTelegraphs = new List<TelegraphRuntimeData>();

        /// <summary>
        /// 新しい予告を登録する。
        /// </summary>
        /// <param name="newTelegraph"></param>
        public void RegisterTelegraph(TelegraphRuntimeData newTelegraph)
        {
            _activeTelegraphs.Add(newTelegraph);
        }

        /// <summary>
        /// 特定のアクターに関する予告をすべて削除する(死亡時やスタン時)。
        /// </summary>
        /// <param name="actorId"></param>
        public void CancelByActorId(int actorId)
        {
            _activeTelegraphs.RemoveAll(t => t.SourceActorId == actorId);
        }

        /// <summary>
        /// 全ての予告データを取得する。
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<TelegraphRuntimeData> GetAllTelegraphs() => _activeTelegraphs;

        /// <summary>
        /// 1ターン経過させ、即発動が必要な(RemainingTurnが0以下)予告を抽出して消去する。
        /// </summary>
        /// <returns></returns>
        public List<TelegraphRuntimeData> ExtractExpiredTelegraph()
        {
            var expired = _activeTelegraphs.FindAll(t => t.RemainingTurn <= 0);
            _activeTelegraphs.RemoveAll(t => t.RemainingTurn <= 0);
            return expired;
        }

        /// <summary>
        /// RemainingTurn の減少の処理を行う。
        /// </summary>
        public void TickAll()
        {
            foreach (var t in _activeTelegraphs)
            {
                t.RemainingTurn--;
            }
        }
    }
}
