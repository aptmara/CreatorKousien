using System;
using System.Data;
using UnityEngine;

public class CardSelected : MonoBehaviour
{
    private CardData selectedCard;

    public event Action<CardData> OnSelectionChanged;

    public void SetSelectedCard(CardData data)
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

    public CardData GetSelectedCardData()
    {
        return selectedCard;
    }

    public bool HasSelection()
    {
        return selectedCard != null;
    }

    public bool IsCardSelected(CardData data)
    {
        if(selectedCard == null || data ==null)
        {
            return false;
        }


        return selectedCard.InstanceId == data.InstanceId;
    }
}
