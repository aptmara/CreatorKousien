using System;
using System.Data;
using UnityEngine;

namespace CreatorKousien.View.UI
{
    public class CardSelected : MonoBehaviour
    {
        private UICardData selectedCard;

        public event Action<UICardData> OnSelectionChanged;

        public void SetSelectedCard(UICardData data)
        {
            if (data == null)
            {
                ClearSelection();
                return;
            }
            if (selectedCard != null && selectedCard.InstanceId == data.InstanceId)
            {
                return;
            }

            selectedCard = data;
            OnSelectionChanged?.Invoke(selectedCard);
        }
        public void ClearSelection()
        {
            if (selectedCard == null)
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
            if (selectedCard == null || data == null)
            {
                return false;
            }


            return selectedCard.InstanceId == data.InstanceId;
        }
    }
}
