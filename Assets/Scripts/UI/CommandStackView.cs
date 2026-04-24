//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   CommandStackView.cs
// @brief  コマンドスタックに積まれたカードを表示するView
///        データの保持や入力処理は行わず、表示更新のみ担当する
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System.Collections.Generic;
using UnityEngine;

public class CommandStackView : MonoBehaviour
{
    [SerializeField] private CardView commandCard0;
    [SerializeField] private CardView commandCard1;
    [SerializeField] private CardView commandCard2;
    [SerializeField] private CardView commandCard3;
    [SerializeField] private CardView commandCard4;

    /// <summary>
    /// コマンドカード一覧を表示に反映する
    /// </summary>
    /// <param name="cards">表示するカード一覧</param>
    public void Apply(IReadOnlyList<UICardData> cards)
    {
        ApplyCard(0, cards);
        ApplyCard(1, cards);
        ApplyCard(2, cards);
        ApplyCard(3, cards);
        ApplyCard(4, cards);
    }

    /// <summary>
    /// すべての表示を空にする
    /// </summary>
    public void Clear()
    {
        SetCard(0, null);
        SetCard(1, null);
        SetCard(2, null);
        SetCard(3, null);
        SetCard(4, null);
    }

    private void ApplyCard(int index, IReadOnlyList<UICardData> cards)
    {
        if (cards != null && index < cards.Count)
        {
            SetCard(index, cards[index]);
            return;
        }

        SetCard(index, null);
    }

    private void SetCard(int index, UICardData data)
    {
        CardView view = GetCardView(index);

        if (view == null)
        {
            return;
        }

        view.Apply(data);
    }

    private CardView GetCardView(int index)
    {
        switch (index)
        {
            case 0:
                return commandCard0;

            case 1:
                return commandCard1;

            case 2:
                return commandCard2;

            case 3:
                return commandCard3;

            case 4:
                return commandCard4;

            default:
                return null;
        }
    }
}
