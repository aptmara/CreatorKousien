// ================================================================================
// File         : HandState.cs
// Author       : Iwai Shogo
//
// Description  : 4つのスロットに、どのCardRuntimeDataがセットされているかを管理する箱。
// Created      : 2026-04-20
// ================================================================================

using System.Collections.Generic;
using CreatorKousien.Data;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// プレイヤーの4つのスロットの状態を保持するクラス
    /// </summary>
    public class HandState
    {
        private Dictionary<SlotPosition, CardRuntimeData> _slots = new Dictionary<SlotPosition, CardRuntimeData>();

        /// <summary>
        /// スロットにカードをセットする
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="card"></param>
        public void SetCard(SlotPosition slot, CardRuntimeData card)
        {
            _slots[slot] = card;
        }

        /// <summary>
        /// 指定したスロットのカードを取得する
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public CardRuntimeData GetCard(SlotPosition slot)
        {
            if (_slots.TryGetValue(slot, out var card))
            {
                return card;
            }
            return null;
        }

        /// <summary>
        /// 全スロットの情報を取得する
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<SlotPosition, CardRuntimeData> GetAllCards()
        {
            return _slots;
        }

        /// <summary>
        /// 手札をリセットする
        /// </summary>
        public void Clear()
        {
            _slots.Clear();
        }
    }
}
