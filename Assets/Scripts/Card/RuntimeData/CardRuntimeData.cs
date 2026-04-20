// ================================================================================
// File         : CardRuntimeData.cs
// Author       : Iwai Shogo
//
// Description  : 今のカードの情報を保持する。
// Created      : 2026-04-20
// ================================================================================

using CreatorKousien.Data;

namespace CreatorKousien.Battle
{
    public class CardRuntimeData
    {
        public int InstanceId { get; }

        // 参照する大元の設計書 (変更不可)
        public CardData BaseData { get; }

        // 現在の面
        public CardFace CurrentFace { get; private set; }

        public CardRuntimeData(int instanceId, CardData baseData, CardFace initialFace = CardFace.Front)
        {
            InstanceId = instanceId;
            BaseData = baseData;
            CurrentFace = initialFace;
        }

        /// <summary>
        /// カードを裏返す
        /// </summary>
        public void Flip()
        {
            CurrentFace = CurrentFace == CardFace.Front ? CardFace.Back : CardFace.Front;
        }

        public EffectData GetCurrentEffect()
        {
            return BaseData.GetEffectByFace(CurrentFace);
        }
    }
}
