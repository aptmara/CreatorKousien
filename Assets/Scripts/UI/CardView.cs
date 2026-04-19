using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject hoverHighLight;
    [SerializeField] private GameObject selectHighLight;

    private UICardData currentData;
    private RectTransform rectTransform;

    public event Action<UICardData> OnHoverEntered;
    public event Action<UICardData> OnHoverExited;
    public event Action<UICardData> OnClicked;

    public RectTransform RectTransform => rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetCardData(UICardData data)
    {
        currentData = data;
        if (data == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);
        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
        }

        if (nameText != null)
        {
            nameText.text = data.Name;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
        }

        SetHover(false);
        SetSelected(false);
    }

    public UICardData GetCardData()
    {
        return currentData;
    }

    public void SetHover(bool isHoverd)
    {
        if (hoverHighLight != null)
        {
            hoverHighLight.SetActive(isHoverd);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (selectHighLight != null)
        {
            selectHighLight.SetActive(isSelected);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentData == null)
        {
            return;
        }

        OnHoverEntered?.Invoke(currentData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentData == null)
        {
            return;
        }

        OnHoverExited?.Invoke(currentData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentData != null)
        {
            return;
        }
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClicked?.Invoke(currentData);
    }
}
