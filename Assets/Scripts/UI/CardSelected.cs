//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   CardSelected.cs
// @brief  選択中のカードを取得するとかどうとか...
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System;
using UnityEngine;

public class CardSelected : MonoBehaviour
{
    private UICardData selectedCard;

    public event Action<UICardData> OnSelectionChanged;

    public void SetSelectedCard(UICardData data)
    {
        if(data == null)
        {
            ClearSelection();
            return;
        }
        if(selectedCard != null && selectedCard.InstanceId == data.InstanceId)
        {
            return;
        }

        selectedCard = data;
        OnSelectionChanged?.Invoke(selectedCard);
    }
    public void ClearSelection()
    {
        if(selectedCard == null)
        {
            return;
        }

        selectedCard = null;
        OnSelectionChanged?.Invoke(null);
    }

    public UICardData GetSelectedCardData()
    {
        return selectedCard;
    }

    public bool HasSelection()
    {
        return selectedCard != null;
    }

    public bool IsCardSelected(UICardData data)
    {
        if(selectedCard == null || data ==null)
        {
            return false;
        }


        return selectedCard.InstanceId == data.InstanceId;
    }
}
