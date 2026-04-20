using CreatorKousien.Data;
using System.Runtime.ExceptionServices;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 手札を管理・操作するシステムクラス
    /// </summary>
    public class CardSystem
    {
        private HandState _handState;

        public CardSystem(HandState handState)
        {
            _handState = handState;
        }

        /// <summary>
        /// 初期化時や入れ替え時に、特定のスロットにカードをセットする
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="card"></param>
        public void SetCardToSlot(SlotPosition slot, CardRuntimeData card)
        {
            _handState.SetCard(slot, card);
        }

        /// <summary>
        /// プレイヤーがカードを使用する
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public EffectData UseCard(SlotPosition slot)
        {
            // スロットのカードを取得
            CardRuntimeData card = _handState.GetCard(slot);
            if (card == null) return null;

            // 現在の面のエフェクトを取得
            EffectData effect = card.GetCurrentEffect();

            // 使ったら裏返す
            card.Flip();

            return effect;
        }

        /// <summary>
        /// カードを使わずに、現在の効果だけを覗き見る。
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public EffectData PeekCurrentEffect(SlotPosition slot)
        {
            CardRuntimeData card = _handState.GetCard(slot);
            return card?.GetCurrentEffect();
        }
    }
}
