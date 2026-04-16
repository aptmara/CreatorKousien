using UnityEngine;

public class CardSlotController : MonoBehaviour
{
    [SerializeField] private InputProvider inputProvider;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CardSlotView cardSlotView;
    [SerializeField] private CardSelected cardSelected;

    private CardData currentHoveredCard;

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

    public CardData GetHoveredCardData()
    {
        return currentHoveredCard;
    }

    public CardData GetSelectedCardData()
    {
        return cardSelected != null ? cardSelected.GetSelectedCardData() : null;
    }

    public bool IsCardHovered(CardData data)
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

    private void HandleCardHoverEntered(CardData data)
    {
        if(IsHandLocked())
        {
            return;
        }

        currentHoveredCard = data;

        UpdateVisuals();
    }

    private void HandleCardHoverExited(CardData data)
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

    private void HandleCardClicked(CardData data)
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

    private void HandleSelectionChanged(CardData selected)
    {
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if(cardSlotView == null)
        {
            return;
        }

        CardData selected = cardSelected != null ? cardSelected.GetSelectedCardData() : null;
        cardSlotView.RefreshSelectionVisual(currentHoveredCard, selected);
    }
}
