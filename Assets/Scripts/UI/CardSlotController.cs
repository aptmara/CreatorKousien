using UnityEngine;


namespace CreatorKousien.View.UI
{
    public class CardSlotController : MonoBehaviour
    {
        [SerializeField] private InputProvider inputProvider;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CardSlotView cardSlotView;
        [SerializeField] private CardSelected cardSelected;

        private UICardData currentHoveredCard;

        private void Start()
        {
            SubscribeEvents();

            UpdateVisuals();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            if(!IsHandLocked() &&
                inputProvider  != null &&
                inputProvider.IsCancelPressed())
            {
                if(cardSelected != null)
                {
                    cardSelected.ClearSelection();
                }
            }
        }

        public UICardData GetHoveredUICardData()
        {
            return currentHoveredCard;
        }

        public UICardData GetSelectedUICardData()
        {
            return cardSelected != null ? cardSelected.GetSelectedCardData() : null;
        }

        public bool IsCardHovered(UICardData data)
        {
            if(currentHoveredCard == null || data ==null)
            {
                return false;
            }

            return currentHoveredCard.InstanceId == data.InstanceId;
        }

        public bool IsHandLocked()
        {
            return uiManager == null || !uiManager.IsHandInputAllowed();
        }

        private void SubscribeEvents()
        {
            if(cardSelected != null)
            {
                cardSelected.OnSelectionChanged += HandleSelectionChanged;
            }

            if(cardSlotView != null)
            {
                cardSlotView.OnCardHoverEntered += HandleCardHoverEntered;
                cardSlotView.OnCardHoverExited += HandleCardHoverExited;
                cardSlotView.OnCardClicked += HandleCardClicked;
            }
        }

        private void UnsubscribeEvents()
        {
            if (cardSelected != null)
            {
                cardSelected.OnSelectionChanged -= HandleSelectionChanged;
            }

            if (cardSlotView != null)
            {
                cardSlotView.OnCardHoverEntered -= HandleCardHoverEntered;
                cardSlotView.OnCardHoverExited -= HandleCardHoverExited;
                cardSlotView.OnCardClicked -= HandleCardClicked;
            }
        }

        private void HandleCardHoverEntered(UICardData data)
        {
            if(IsHandLocked())
            {
                return;
            }

            currentHoveredCard = data;

            UpdateVisuals();
        }

        private void HandleCardHoverExited(UICardData data)
        {
            if(data == null || currentHoveredCard == null)
            {
                return;
            }
            if(currentHoveredCard.InstanceId  == data.InstanceId)
            {
                currentHoveredCard = null;
                UpdateVisuals();
            }
        }

        private void HandleCardClicked(UICardData data)
        {
            if(IsHandLocked())
            {
                return;
            }

            if(cardSelected == null)
            {
                return;
            }

            cardSelected.SetSelectedCard(data);
        }

        private void HandleSelectionChanged(UICardData selected)
        {
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if(cardSlotView == null)
            {
                return;
            }

            UICardData selected = cardSelected != null ? cardSelected.GetSelectedCardData() : null;
            cardSlotView.RefreshSelectionVisual(currentHoveredCard, selected);
        }
    }
}
