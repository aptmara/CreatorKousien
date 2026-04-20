// ================================================================================
// File         : CardData.cs
// Author       : Iwai Shogo
//
// Description  : 表裏一体を体現するデータ。
// Created      : 2026-04-20
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Data
{
    [CreateAssetMenu(fileName = "NewCardData", menuName = "CreatorKousien/Data/CardData")]
    public class CardData : ScriptableObject
    {
        [Header("カード基本情報")]
        public int CardId;
        public string CardName;

        [Header("表裏のエフェクト設定")]
        [Tooltip("表面 (基本的に移動)")]
        public EffectData FrontEffect;

        [Tooltip("裏面 (基本的に攻撃)")]
        public EffectData BackEffect;

        public EffectData GetEffectByFace(CardFace face)
        {
            return face == CardFace.Front ? FrontEffect : BackEffect;
        }
    }
}
