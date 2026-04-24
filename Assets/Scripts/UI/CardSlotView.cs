using System;
using UnityEngine;

/// <summary>
/// 4方向のカードViewを管理するView
/// データの所有や選択判定は行わず、表示更新とイベント中継だけを担当する
/// </summary>
public class CardSlotView : MonoBehaviour
{
    [SerializeField] private CardView upCardView;
    [SerializeField] private CardView downCardView;
    [SerializeField] private CardView leftCardView;
    [SerializeField] private CardView rightCardView;

    /// <summary>
    /// 指定方向のカードにカーソルが乗ったときに通知されるイベント
    /// </summary>
    public event Action<SlotDirection> OnCardHoverEntered;

    /// <summary>
    /// 指定方向のカードからカーソルが離れたときに通知されるイベント
    /// </summary>
    public event Action<SlotDirection> OnCardHoverExited;

    /// <summary>
    /// 指定方向のカードがクリックされたときに通知されるイベント
    /// </summary>
    public event Action<SlotDirection> OnCardClicked;

    private void Awake()
    {
        SubscribeCardEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeCardEvents();
    }

    /// <summary>
    /// 指定方向のCardViewを取得する
    /// </summary>
    /// <param name="direction">取得対象の方向</param>
    /// <returns>指定方向のCardView</returns>
    public CardView GetCardView(SlotDirection direction)
    {
        switch (direction)
        {
            case SlotDirection.Up:
                return upCardView;

            case SlotDirection.Down:
                return downCardView;

            case SlotDirection.Left:
                return leftCardView;

            case SlotDirection.Right:
                return rightCardView;

            default:
                return null;
        }
    }

    /// <summary>
    /// 指定方向のカード表示を更新する
    /// </summary>
    /// <param name="direction">更新対象の方向</param>
    /// <param name="data">表示するカードデータ</param>
    public void ApplyCard(SlotDirection direction, UICardData data)
    {
        CardView view = GetCardView(direction);

        if (view == null)
        {
            return;
        }

        view.Apply(data);
    }

    /// <summary>
    /// 指定方向のホバー表示を切り替える
    /// </summary>
    /// <param name="direction">対象方向</param>
    /// <param name="isHovered">ホバー表示を有効にするならtrue</param>
    public void SetHover(SlotDirection direction, bool isHovered)
    {
        CardView view = GetCardView(direction);

        if (view == null)
        {
            return;
        }

        view.SetHover(isHovered);
    }

    /// <summary>
    /// 指定方向の選択表示を切り替える
    /// </summary>
    /// <param name="direction">対象方向</param>
    /// <param name="isSelected">選択表示を有効にするならtrue</param>
    public void SetSelected(SlotDirection direction, bool isSelected)
    {
        CardView view = GetCardView(direction);

        if (view == null)
        {
            return;
        }

        view.SetSelected(isSelected);
    }

    /// <summary>
    /// すべてのカード表示を更新する
    /// </summary>
    /// <param name="up">上方向のカード</param>
    /// <param name="down">下方向のカード</param>
    /// <param name="left">左方向のカード</param>
    /// <param name="right">右方向のカード</param>
    public void ApplyAllCards(UICardData up, UICardData down, UICardData left, UICardData right)
    {
        ApplyCard(SlotDirection.Up, up);
        ApplyCard(SlotDirection.Down, down);
        ApplyCard(SlotDirection.Left, left);
        ApplyCard(SlotDirection.Right, right);
    }

    /// <summary>
    /// すべてのホバー表示と選択表示を解除する
    /// </summary>
    public void ClearAllVisualState()
    {
        SetHover(SlotDirection.Up, false);
        SetHover(SlotDirection.Down, false);
        SetHover(SlotDirection.Left, false);
        SetHover(SlotDirection.Right, false);

        SetSelected(SlotDirection.Up, false);
        SetSelected(SlotDirection.Down, false);
        SetSelected(SlotDirection.Left, false);
        SetSelected(SlotDirection.Right, false);
    }

    private void SubscribeCardEvents()
    {
        if (upCardView != null)
        {
            upCardView.OnHoverEntered += HandleUpHoverEntered;
            upCardView.OnHoverExited += HandleUpHoverExited;
            upCardView.OnClicked += HandleUpClicked;
        }

        if (downCardView != null)
        {
            downCardView.OnHoverEntered += HandleDownHoverEntered;
            downCardView.OnHoverExited += HandleDownHoverExited;
            downCardView.OnClicked += HandleDownClicked;
        }

        if (leftCardView != null)
        {
            leftCardView.OnHoverEntered += HandleLeftHoverEntered;
            leftCardView.OnHoverExited += HandleLeftHoverExited;
            leftCardView.OnClicked += HandleLeftClicked;
        }

        if (rightCardView != null)
        {
            rightCardView.OnHoverEntered += HandleRightHoverEntered;
            rightCardView.OnHoverExited += HandleRightHoverExited;
            rightCardView.OnClicked += HandleRightClicked;
        }
    }

    private void UnsubscribeCardEvents()
    {
        if (upCardView != null)
        {
            upCardView.OnHoverEntered -= HandleUpHoverEntered;
            upCardView.OnHoverExited -= HandleUpHoverExited;
            upCardView.OnClicked -= HandleUpClicked;
        }

        if (downCardView != null)
        {
            downCardView.OnHoverEntered -= HandleDownHoverEntered;
            downCardView.OnHoverExited -= HandleDownHoverExited;
            downCardView.OnClicked -= HandleDownClicked;
        }

        if (leftCardView != null)
        {
            leftCardView.OnHoverEntered -= HandleLeftHoverEntered;
            leftCardView.OnHoverExited -= HandleLeftHoverExited;
            leftCardView.OnClicked -= HandleLeftClicked;
        }

        if (rightCardView != null)
        {
            rightCardView.OnHoverEntered -= HandleRightHoverEntered;
            rightCardView.OnHoverExited -= HandleRightHoverExited;
            rightCardView.OnClicked -= HandleRightClicked;
        }
    }

    private void HandleUpHoverEntered()
    {
        OnCardHoverEntered?.Invoke(SlotDirection.Up);
    }

    private void HandleUpHoverExited()
    {
        OnCardHoverExited?.Invoke(SlotDirection.Up);
    }

    private void HandleUpClicked()
    {
        OnCardClicked?.Invoke(SlotDirection.Up);
    }

    private void HandleDownHoverEntered()
    {
        OnCardHoverEntered?.Invoke(SlotDirection.Down);
    }

    private void HandleDownHoverExited()
    {
        OnCardHoverExited?.Invoke(SlotDirection.Down);
    }

    private void HandleDownClicked()
    {
        OnCardClicked?.Invoke(SlotDirection.Down);
    }

    private void HandleLeftHoverEntered()
    {
        OnCardHoverEntered?.Invoke(SlotDirection.Left);
    }

    private void HandleLeftHoverExited()
    {
        OnCardHoverExited?.Invoke(SlotDirection.Left);
    }

    private void HandleLeftClicked()
    {
        OnCardClicked?.Invoke(SlotDirection.Left);
    }

    private void HandleRightHoverEntered()
    {
        OnCardHoverEntered?.Invoke(SlotDirection.Right);
    }

    private void HandleRightHoverExited()
    {
        OnCardHoverExited?.Invoke(SlotDirection.Right);
    }

    private void HandleRightClicked()
    {
        OnCardClicked?.Invoke(SlotDirection.Right);
    }
}
