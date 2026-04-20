// ------------------------------------------------------------
// File		: CardView.cs
// Summary	: カードのUI表示を担当するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - カードのアイコン、名前、コスト、説明を表示するUIコンポーネント
// ------------------------------------------------------------
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CreatorKousien.View.UI
{
    public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI costText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private GameObject hoverHighLight;
        [SerializeField] private GameObject selectHighLight;

        [Header("方向アイコン設定")]
        [Tooltip("矢印オブジェクトの本体")]
        [SerializeField] private GameObject directionIconRoot;
        [Tooltip("矢印の画像")]
        [SerializeField] private RectTransform directionArrow;

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

            // 方向アイコンの表示設定
            if (directionIconRoot != null && directionArrow != null)
            {
                // 方向がNone以外なら矢印を表示
                if (data.Direction != UIDirection.None)
                {
                    directionIconRoot.SetActive(true);
                    float angle = 0f;

                    switch (data.Direction)
                    {
                        case UIDirection.Up:
                            angle = 0f;
                            break;
                        case UIDirection.Down:
                            angle = 180f;
                            break;
                        case UIDirection.Left:
                            angle = 90f;
                            break;
                        case UIDirection.Right:
                            angle = -90f;
                            break;
                    }
                    directionArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
                else
                {
                    directionIconRoot.SetActive(false);
                }
            }
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
}

