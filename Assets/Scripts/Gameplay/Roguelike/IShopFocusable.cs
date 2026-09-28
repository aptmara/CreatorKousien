//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeCard.cs
// brief  : ショップ画面でコント―らのフォーカス移動対象となるものの共通インターフェース
//          カード(S_UpgradeCard)とExitボタン(S_ShopExitFocusable)の両方を実装
//
// auther : Shohei Takitani
// date   : 2026/09/28 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine.UI;

public interface IShopFocusable
{
    /// <summary>
    /// FindSelectableOnUp/Down/Left/Right()を呼ぶために公開
    /// </summary>
    Selectable Selectable { get; }

    /// <summary>
    /// フォーカス状態の切り替え
    /// </summary>
    /// <param name="isFocused"></param>
    void Focus(bool isFocused);

    /// <summary>
    /// 決定
    /// 実際のボタンクリックと同じ効果
    /// </summary>
    void TriggerClick();

}
