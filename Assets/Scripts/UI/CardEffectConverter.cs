// ------------------------------------------------------------
// File		: CardEffectConverter.cs
// Summary	: CardDataを表示用に変換するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - CardDataをCardEffectDataに変換するクラス
// ------------------------------------------------------------
using UnityEngine;
using CreatorKousien.Data;

namespace CreatorKousien.View.UI
{
    public static class UICardConverter
    {
        /// <summary>
        /// CardDataとEffectDataを統合し、UI表示用のUICardDataに変換します。
        /// </summary>
        /// <param name="card">変換元のカードデータ</param>
        /// <param name="effect">カードに対応する効果データ</param>
        /// <param name="instanceId">個別のインスタンスID（任意）</param>
        /// <returns>UIで使用可能なUICardData</returns>
        public static UICardData ConvertToUICardData(CardData card, EffectData effect, string instanceId = "")
        {
            if (card == null)
            {
                Debug.LogWarning("CardData が null です。");
                return null;
            }

            return new UICardData
            {
                MasterId = card.CardId.ToString(),

                // ユニークな識別子が必要な場合に設定
                InstanceId = instanceId,

                // effect が null だった時にエラーで落ちないように
                Name = effect != null ? effect.EffectName : card.CardName,
                Icon = effect != null ? effect.EffectIcon : null,

                Description = effect != null ? effect.Description : ""
            };
        }
    }
}
