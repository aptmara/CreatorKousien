using UnityEngine;

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
            // CardDataのIDを文字列として保持
            MasterId = card.CardID.ToString(),

            // ユニークな識別子が必要な場合に設定
            InstanceId = instanceId,

            // EffectDataから表示情報を取得
            Name = effect.EffectName,
            Icon = effect.EffectIcon,
            Description = effect.EffectInfo
        };
    }
}
