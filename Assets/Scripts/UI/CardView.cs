//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// @file   UIData.cs
// @brief  カード一枚分の表示と、入力通知を行う
// @author 山本郁也
// @date   2026/04/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// カード1枚分の表示と入力通知を担当するView
/// カードデータの保持や選択状態の管理は行わない
/// </summary>
public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private GameObject hoverHighlight;
    [SerializeField] private GameObject selectHighlight;

    /// <summary>
    /// カードにカーソルが乗ったときに通知されるイベント
    /// </summary>
    public event Action OnHoverEntered;

    /// <summary>
    /// カードからカーソルが離れたときに通知されるイベント
    /// </summary>
    public event Action OnHoverExited;

    /// <summary>
    /// カードがクリックされたときに通知されるイベント
    /// </summary>
    public event Action OnClicked;

    /// <summary>
    /// カードデータを表示に反映する
    /// nullが渡された場合はこのViewを非表示にする
    /// </summary>
    /// <param name="data">表示するカードデータ</param>
    public void Apply(UICardData data)
    {
        if (data == null)
        {
            gameObject.SetActive(false);
            SetHover(false);
            SetSelected(false);
            return;
        }

        gameObject.SetActive(true);

        if (iconImage != null)
        {
            iconImage.sprite = data.Icon;
        }

        if (nameText != null)
        {
            nameText.text = data.EffectName;
        }

        if (descriptionText != null)
        {
            descriptionText.text = data.Description;
        }

        SetHover(false);
        SetSelected(false);
    }

    /// <summary>
    /// ホバー表示を切り替える
    /// </summary>
    /// <param name="isHovered">ホバー表示を有効にするならtrue</param>
    public void SetHover(bool isHovered)
    {
        if (hoverHighlight != null)
        {
            hoverHighlight.SetActive(isHovered);
        }
    }

    /// <summary>
    /// 選択表示を切り替える
    /// </summary>
    /// <param name="isSelected">選択表示を有効にするならtrue</param>
    public void SetSelected(bool isSelected)
    {
        if (selectHighlight != null)
        {
            selectHighlight.SetActive(isSelected);
        }
    }

    /// <summary>
    /// カードのドロー演出を再生する
    /// </summary>
    public void PlayDrawAnimation()
    {
    }

    /// <summary>
    /// ポインターがカード上に入ったときに呼ばれる
    /// </summary>
    /// <param name="eventData">ポインターイベント情報</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        OnHoverEntered?.Invoke();
    }

    /// <summary>
    /// ポインターがカード上から外れたときに呼ばれる
    /// </summary>
    /// <param name="eventData">ポインターイベント情報</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExited?.Invoke();
    }

    /// <summary>
    /// カードがクリックされたときに呼ばれる
    /// 左クリック時のみ通知する
    /// </summary>
    /// <param name="eventData">ポインターイベント情報</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        OnClicked?.Invoke();
    }
}
