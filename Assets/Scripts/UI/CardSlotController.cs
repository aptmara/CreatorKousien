
using UnityEngine;

/// <summary>
/// 4方向カードスロットの制御を担当するクラス
/// カードデータの保持、ホバー状態、選択状態を管理する
/// </summary>
public class CardSlotController : MonoBehaviour
{
    [SerializeField] private InputProvider inputProvider;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private CardSlotView cardSlotView;

    private UICardData upCard;
    private UICardData downCard;
    private UICardData leftCard;
    private UICardData rightCard;

    private SlotDirection? hoveredDirection;
    private SlotDirection? selectedDirection;

    private void Start()
    {
        SubscribeEvents();
        RefreshAllViews();
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void Update()
    {
        if (IsHandLocked())
        {
            return;
        }

        if (inputProvider != null && inputProvider.IsCancelPressed())
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// 指定方向にカードデータを設定する
    /// </summary>
    /// <param name="direction">設定先の方向</param>
    /// <param name="data">設定するカードデータ</param>
    public void SetCard(SlotDirection direction, UICardData data)
    {
        switch (direction)
        {
            case SlotDirection.Up:
                upCard = data;
                break;

            case SlotDirection.Down:
                downCard = data;
                break;

            case SlotDirection.Left:
                leftCard = data;
                break;

            case SlotDirection.Right:
                rightCard = data;
                break;
        }

        if (cardSlotView != null)
        {
            cardSlotView.ApplyCard(direction, data);
        }

        if (selectedDirection.HasValue && selectedDirection.Value == direction && data == null)
        {
            selectedDirection = null;
        }

        if (hoveredDirection.HasValue && hoveredDirection.Value == direction && data == null)
        {
            hoveredDirection = null;
        }

        RefreshVisuals();
    }

    /// <summary>
    /// 4方向すべてのカードデータを設定する
    /// </summary>
    /// <param name="up">上方向のカード</param>
    /// <param name="down">下方向のカード</param>
    /// <param name="left">左方向のカード</param>
    /// <param name="right">右方向のカード</param>
    public void SetAllCards(UICardData up, UICardData down, UICardData left, UICardData right)
    {
        upCard = up;
        downCard = down;
        leftCard = left;
        rightCard = right;

        if (hoveredDirection.HasValue && GetCard(hoveredDirection.Value) == null)
        {
            hoveredDirection = null;
        }

        if (selectedDirection.HasValue && GetCard(selectedDirection.Value) == null)
        {
            selectedDirection = null;
        }

        RefreshAllViews();
    }

    /// <summary>
    /// 指定方向のカードデータを取得する
    /// </summary>
    /// <param name="direction">取得対象の方向</param>
    /// <returns>指定方向のカードデータ</returns>
    public UICardData GetCard(SlotDirection direction)
    {
        switch (direction)
        {
            case SlotDirection.Up:
                return upCard;

            case SlotDirection.Down:
                return downCard;

            case SlotDirection.Left:
                return leftCard;

            case SlotDirection.Right:
                return rightCard;

            default:
                return null;
        }
    }

    /// <summary>
    /// 上方向のカードデータを取得する
    /// </summary>
    /// <returns>上方向のカードデータ</returns>
    public UICardData GetUpCard()
    {
        return upCard;
    }

    /// <summary>
    /// 下方向のカードデータを取得する
    /// </summary>
    /// <returns>下方向のカードデータ</returns>
    public UICardData GetDownCard()
    {
        return downCard;
    }

    /// <summary>
    /// 左方向のカードデータを取得する
    /// </summary>
    /// <returns>左方向のカードデータ</returns>
    public UICardData GetLeftCard()
    {
        return leftCard;
    }

    /// <summary>
    /// 右方向のカードデータを取得する
    /// </summary>
    /// <returns>右方向のカードデータ</returns>
    public UICardData GetRightCard()
    {
        return rightCard;
    }

    /// <summary>
    /// 現在ホバー中の方向を取得する
    /// </summary>
    /// <returns>ホバー中の方向。存在しない場合はnull</returns>
    public SlotDirection? GetHoveredDirection()
    {
        return hoveredDirection;
    }

    /// <summary>
    /// 現在選択中の方向を取得する
    /// </summary>
    /// <returns>選択中の方向。存在しない場合はnull</returns>
    public SlotDirection? GetSelectedDirection()
    {
        return selectedDirection;
    }

    /// <summary>
    /// 現在ホバー中のカードデータを取得する
    /// </summary>
    /// <returns>ホバー中のカードデータ。存在しない場合はnull</returns>
    public UICardData GetHoveredCardData()
    {
        if (!hoveredDirection.HasValue)
        {
            return null;
        }

        return GetCard(hoveredDirection.Value);
    }

    /// <summary>
    /// 現在選択中のカードデータを取得する
    /// </summary>
    /// <returns>選択中のカードデータ。存在しない場合はnull</returns>
    public UICardData GetSelectedCardData()
    {
        if (!selectedDirection.HasValue)
        {
            return null;
        }

        return GetCard(selectedDirection.Value);
    }

    /// <summary>
    /// 指定方向が現在ホバー中か判定する
    /// </summary>
    /// <param name="direction">判定対象の方向</param>
    /// <returns>ホバー中ならtrue</returns>
    public bool IsCardHovered(SlotDirection direction)
    {
        return hoveredDirection.HasValue && hoveredDirection.Value == direction;
    }

    /// <summary>
    /// 指定方向が現在選択中か判定する
    /// </summary>
    /// <param name="direction">判定対象の方向</param>
    /// <returns>選択中ならtrue</returns>
    public bool IsCardSelected(SlotDirection direction)
    {
        return selectedDirection.HasValue && selectedDirection.Value == direction;
    }

    /// <summary>
    /// 選択状態を解除する
    /// </summary>
    public void ClearSelection()
    {
        selectedDirection = null;
        RefreshVisuals();
    }

    /// <summary>
    /// ホバー状態を解除する
    /// </summary>
    public void ClearHover()
    {
        hoveredDirection = null;
        RefreshVisuals();
    }

    /// <summary>
    /// 現在手札入力がロックされているか判定する
    /// </summary>
    /// <returns>入力不可ならtrue</returns>
    public bool IsHandLocked()
    {
        return uiManager == null || !uiManager.IsHandInputAllowed();
    }

    private void SubscribeEvents()
    {
        if (cardSlotView != null)
        {
            cardSlotView.OnCardHoverEntered += HandleCardHoverEntered;
            cardSlotView.OnCardHoverExited += HandleCardHoverExited;
            cardSlotView.OnCardClicked += HandleCardClicked;
        }
    }

    private void UnsubscribeEvents()
    {
        if (cardSlotView != null)
        {
            cardSlotView.OnCardHoverEntered -= HandleCardHoverEntered;
            cardSlotView.OnCardHoverExited -= HandleCardHoverExited;
            cardSlotView.OnCardClicked -= HandleCardClicked;
        }
    }

    private void HandleCardHoverEntered(SlotDirection direction)
    {
        if (IsHandLocked())
        {
            return;
        }

        if (GetCard(direction) == null)
        {
            return;
        }

        hoveredDirection = direction;
        RefreshVisuals();
    }

    private void HandleCardHoverExited(SlotDirection direction)
    {
        if (!hoveredDirection.HasValue)
        {
            return;
        }

        if (hoveredDirection.Value == direction)
        {
            hoveredDirection = null;
            RefreshVisuals();
        }
    }

    private void HandleCardClicked(SlotDirection direction)
    {
        if (IsHandLocked())
        {
            return;
        }

        if (GetCard(direction) == null)
        {
            return;
        }

        selectedDirection = direction;
        RefreshVisuals();
    }

    private void RefreshAllViews()
    {
        if (cardSlotView == null)
        {
            return;
        }

        cardSlotView.ApplyAllCards(upCard, downCard, leftCard, rightCard);
        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        if (cardSlotView == null)
        {
            return;
        }

        cardSlotView.ClearAllVisualState();

        if (hoveredDirection.HasValue && GetCard(hoveredDirection.Value) != null)
        {
            cardSlotView.SetHover(hoveredDirection.Value, true);
        }

        if (selectedDirection.HasValue && GetCard(selectedDirection.Value) != null)
        {
            cardSlotView.SetSelected(selectedDirection.Value, true);
        }
    }
}
