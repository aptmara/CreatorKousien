//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeCard.cs
// brief  : 常設ショップメニューの1枚分のボタンUI
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
// update : 2026/09/07 - 常設MENU化に伴い購入ボタン形式へ改修 - 浅野
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Game.Data.Player;

public class S_UpgradeCard : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    //____________________________________
    // variables

    [Header("見た目")]
    [Header("アイコン")]
    [SerializeField] private Image _iconImage;
    [Header("強化名")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [Header("詳細説明")]
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [Header("レベル表示(〇/〇)")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [Header("使用していない(空でOK)")]
    [SerializeField] private GameObject _acquiredMark;
    [Header("コストを表示するテキスト")]
    [SerializeField] private TextMeshProUGUI _costText;

    [Header("フォーカス演出")]
    [SerializeField] private S_UIScaleAnimator _scaleAnimator;
    [SerializeField] private Shader _grayscaleShader;

    [Header("旧Frame演出")]
    [SerializeField] private GameObject _highlightFrame;
    [Header("旧Frameを使用するか")]
    [SerializeField] private bool _useFrameHighlight = false;

    [Header("機能面")]
    [SerializeField] private Button _selectButton;


    private UpgradeData _cardData;
    private Image _runtimeFrame;
    private Outline _runtimeOutline;
    private Material _defaultIconMaterial;
    private static Material _sharedGrayscaleMaterial;

    /// <summary>ホバー開始時に呼ばれる(自分自身を渡す)</summary>
    public event Action<S_UpgradeCard> HoverEnter;
    /// <summary>ホバー終了時に呼ばれる</summary>
    public event Action<S_UpgradeCard> HoverExit;
    /// <summary>クリック(購入試行)時に呼ばれる</summary>
    public event Action<S_UpgradeCard> Clicked;

    public UpgradeData CardData => _cardData;


    //____________________________________
    // public function

    private void Awake()
    {
        if (_selectButton != null)
            _selectButton.onClick.AddListener(() => Clicked?.Invoke(this));
    }

    /// <summary>
    /// ボタンの表示内容を設定する
    /// </summary>
    /// <param name="cardData">表示する強化データ</param>
    /// <param name="currentLevel">現在の取得済みレベル</param>
    public void Setup(UpgradeData cardData, int currentLevel)
    {
        if (!_useFrameHighlight && _highlightFrame != null)
        {
            _highlightFrame.SetActive(false);
        }

        _cardData = cardData;
        EnsureRuntimeFrame();
        PrepareIconMaterial();
        ApplyCardLayout();
        DisableScaleAnimation();
        _nameText.gameObject.SetActive(true);
        _descriptionText.gameObject.SetActive(false);
        _levelText.gameObject.SetActive(true);
        if (_costText != null)
            _costText.gameObject.SetActive(true);
        Refresh(currentLevel);
    }

    /// <summary>
    /// 現在レベルに基づいて表示を更新する(購入後の即時反映用)
    /// </summary>
    /// <param name="currentLevel">現在の取得レベル(未取得なら0)</param>
    public void Refresh(int currentLevel)
    {
        bool isMaxed = currentLevel >= _cardData.MaxLevel;

        _iconImage.sprite = _cardData.Icon;
        _nameText.text = _cardData.DisplayName;
        _levelText.text = isMaxed
            ? "MAX"
            : GetStageLabel(currentLevel, Mathf.Clamp(currentLevel + 1, 1, _cardData.MaxLevel));
        ApplyFrameColor(false);

        if (_costText != null)
        {
            _costText.text = isMaxed ? "取得済み" : $"{_cardData.GetCost(currentLevel)} コイン";
        }

        if (_acquiredMark != null)
        {
            _acquiredMark.SetActive(currentLevel > 0);
        }

        // 最大レベル到達済みなら購入不可にする
        if (_selectButton != null)
            _selectButton.interactable = !isMaxed;
    }

    private static string GetStageLabel(int currentLevel, int nextLevel)
    {
        return currentLevel <= 0 ? $"Lv.{nextLevel}" : $"Lv.{currentLevel}→{nextLevel}";
    }

    private void ApplyFrameColor(bool isHighlighted)
    {
        if (_runtimeFrame != null)
            _runtimeFrame.color = isHighlighted
                ? new Color(0.28f, 0.12f, 0.30f, 1f)
                : new Color(0.12f, 0.07f, 0.16f, 0.96f);
        if (_runtimeOutline != null)
            _runtimeOutline.effectColor = isHighlighted
                ? Color.white
                : new Color(0.48f, 0.32f, 0.18f, 1f);
    }

    private void EnsureRuntimeFrame()
    {
        if (_runtimeFrame != null)
            return;

        var frameObject = new GameObject("RuntimeCardFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        frameObject.transform.SetParent(transform, false);
        frameObject.transform.SetAsFirstSibling();

        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.anchoredPosition = Vector2.zero;
        frameRect.sizeDelta = new Vector2(214f, 214f);

        _runtimeFrame = frameObject.GetComponent<Image>();
        _runtimeFrame.color = new Color(0.12f, 0.07f, 0.16f, 0.96f);
        _runtimeFrame.raycastTarget = false;

        _runtimeOutline = frameObject.GetComponent<Outline>();
        _runtimeOutline.effectColor = new Color(0.48f, 0.32f, 0.18f, 1f);
        _runtimeOutline.effectDistance = new Vector2(3f, -3f);
    }

    private void PrepareIconMaterial()
    {
        _defaultIconMaterial = _iconImage.material;

        if (_sharedGrayscaleMaterial == null && _grayscaleShader != null)
        {
            _sharedGrayscaleMaterial = new Material(_grayscaleShader)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
        }
    }

    private void DisableScaleAnimation()
    {
        if (_scaleAnimator != null)
            _scaleAnimator.enabled = false;
    }

    private void ApplyCardLayout()
    {
        if (transform is RectTransform rootRect)
            rootRect.sizeDelta = new Vector2(216f, 216f);

        ConfigureRect(_iconImage.rectTransform, new Vector2(0f, 30f), new Vector2(140f, 140f));
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;

        ConfigureText(_nameText, new Vector2(0f, -58f), new Vector2(200f, 40f), 24f, FontStyles.Bold);
        ConfigureText(_levelText, new Vector2(0f, -88f), new Vector2(194f, 30f), 22f, FontStyles.Bold);
        _levelText.color = new Color(1f, 0.72f, 0.28f, 1f);
        if (_costText != null)
        {
            ConfigureText(_costText, new Vector2(0f, -112f), new Vector2(194f, 28f), 20f, FontStyles.Bold);
            _costText.color = new Color(1f, 0.92f, 0.55f, 1f);
        }
    }

    private static void ConfigureText(
        TextMeshProUGUI text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style)
    {
        ConfigureRect(text.rectTransform, position, size);
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;
        text.color = new Color(0.96f, 0.92f, 0.82f, 1f);
        text.raycastTarget = false;
    }

    private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    //____________________________________
    // pointer handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        ApplyFrameColor(true);
        _iconImage.material = _sharedGrayscaleMaterial == null ? _defaultIconMaterial : _defaultIconMaterial;
        HoverEnter?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ApplyFrameColor(false);
        HoverExit?.Invoke(this);
    }

    /// <summary>
    /// 選択確定時のアニメーションを再生する関数
    /// </summary>
    /// <param name="onComplete"></param>
    public void PlaySelectedAnimation(System.Action onComplete = null)
    {
        onComplete?.Invoke();
    }

}
