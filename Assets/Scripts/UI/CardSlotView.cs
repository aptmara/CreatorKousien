using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSlotView : MonoBehaviour
{
    [SerializeField] private List<CardView> cardViews = new List<CardView>();

    private CardSlotLayoutData layoutData;

    public event Action<CardData> OnCardHoverEntered;
    public event Action<CardData> OnCardHoverExited;
    public event Action<CardData> OnCardClicked;

    private void Awake()
    {
        SubscribeCardEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeCardEvents();
    }

    public void SetCardDataList(IReadOnlyList<CardData> dataList)
    {
        if(dataList ==null)
        {
            for(int i =0;i < cardViews.Count;i++)
            {
                if (cardViews[i] != null)
                {
                    cardViews[i].SetCardData(null);
                }
            }

            RefreshSelectionVisual(null, null);
            return;
        }

        for(int i = 0; i < cardViews.Count;i++)
        {
            CardView view = cardViews[i];
            if(view == null)
            {
                continue;
            }

            if(i < dataList.Count)
            {
                view.SetCardData(dataList[i]);
            }
            else
            {
                view.SetCardData(null);
            }
        }

        RefreshSelectionVisual(null, null);
    }

    public void RefreshSelectionVisual(CardData hovered,CardData selected)
    {
        foreach(CardView view in cardViews)
        {
            if (view == null || !view.gameObject.activeInHierarchy)
            {
                continue;
            }

            CardData data = view.GetCardData();

            bool isHovered =
                hovered != null &&
                data != null &&
                hovered.InstanceId == data.InstanceId;

            bool isSelected =
                selected != null &&
                data != null &&
                selected.InstanceId == data.InstanceId;

            view.SetHover(isHovered);
            view.SetSelected(isSelected);
        }
    }

    private void ApplyLayout()
    {
        if(layoutData == null)
        {
            return;
        }

        for (int i = 0; i < cardViews.Count; i++)
        {
            CardView view = cardViews[i];

            if(view == null)
            {
                continue;
            }

            RectTransform rect = view.RectTransform;
            if(rect == null)
            {
                continue;
            }

            Vector2 finalPosition = layoutData.CenterPosition;
            Vector2 finalSize = rect.sizeDelta;

            if(layoutData.CardLayouts != null &&
                i < layoutData.CardLayouts.Count &&
                layoutData.CardLayouts[i] != null)
            {
                CardLayoutData cardLayout = layoutData.CardLayouts[i];
                finalPosition += cardLayout.offset;
                finalSize = cardLayout.size;
            }

            rect.anchoredPosition = finalPosition;
            rect.sizeDelta = finalSize;
        }
    }

    private void SubscribeCardEvents()
    {
        foreach(CardView view in cardViews)
        {
            if(view == null)
            {
                continue;
            }

            view.OnHoverEntered += HandleHoverEntered;
            view.OnHoverExited += HandleHoverExited;
            view.OnClicked += HandleClicked;
        }
    }

    private void UnsubscribeCardEvents()
    {
        foreach (CardView view in cardViews)
        {
            if (view == null)
            {
                continue;
            }

            view.OnHoverEntered -= HandleHoverEntered;
            view.OnHoverExited -= HandleHoverExited;
            view.OnClicked -= HandleClicked;
        }
    }

    private void HandleHoverEntered(CardData data)
    {
        OnCardHoverEntered?.Invoke(data);
    }

    private void HandleHoverExited(CardData data)
    {
        OnCardHoverExited?.Invoke(data);
    }

    private void HandleClicked(CardData data)
    {
        OnCardClicked?.Invoke(data);
    }
}
