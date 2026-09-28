//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_ShopExitFocusable.cs
// brief  : ショップ終了ボタンのフォーカス演出
//
// auther : Shohei Takitani
// date   : 2026/09/27 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class S_ShopExitFocusable : MonoBehaviour,
    IShopFocusable,
    IPointerEnterHandler,
    IPointerExitHandler,
    ISelectHandler,
    IDeselectHandler
{
    [Header("フォーカス演出")]
    [SerializeField] private S_UIScaleAnimator _scaleAnimator;

    private Button _button;
    private bool _pointerFocused;
    private bool _eventSelected;
    private bool _focusApplied;

    public Selectable Selectable => _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleClicked);
    }

    private void Update()
    {
        _eventSelected =
            EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == gameObject;

        RefreshFocus();
    }

    private void HandleClicked()
    {
        _scaleAnimator?.PlaySelectedAnimation();
    }

    /// <summary>
    /// EventSystemの選択状態とマウス状態からフォーカス演出を更新する
    /// </summary>
    private void RefreshFocus()
    {
        bool isFocused = S_RoguelikeSelectController.IsPointerMode
            ? _pointerFocused
            : _eventSelected;
        if (_focusApplied == isFocused)
            return;

        _focusApplied = isFocused;
        _scaleAnimator?.SetHighlighted(isFocused);
    }

    public void Focus(bool isFocused)
    {
        _eventSelected = isFocused;
        RefreshFocus();
    }

    public void TriggerClick()
    {
        _button.onClick.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _pointerFocused = true;
        RefreshFocus();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _pointerFocused = false;
        RefreshFocus();
    }

    public void OnSelect(BaseEventData eventData)
    {
        _eventSelected = true;
        RefreshFocus();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        _eventSelected = false;
        RefreshFocus();
    }
}
