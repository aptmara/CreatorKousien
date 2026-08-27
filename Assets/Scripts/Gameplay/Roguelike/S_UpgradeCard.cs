//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeCard.cs
// brief  : 強化選択画面の1枚分のUI
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Data.Player;
using Game.Data.Collectibles;
using Game.Core.Roguelike;

public class S_UpgradeCard : MonoBehaviour
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
    private bool _isRuleChange;


    //____________________________________
    // public function

    /// <summary>
    /// カードの表示内容を設定する
    /// </summary>
    /// <param name="cardData">表示するカードデータ</param>
    /// <param name="currentLevel">現在の取得済みレベル</param>
    /// <param name="onSelected">選択時に呼ばれるコールバック</param>
    public void Setup(
        UpgradeData cardData,
        int currentLevel,
        int levelGain = 1,
        bool isDeepening = false)
    {
        if (!_useFrameHighlight && _highlightFrame != null)
        {
            _highlightFrame.SetActive(false);
        }

        // 使用しない
        //_selectButton.onClick.RemoveAllListeners();
        //_selectButton.onClick.AddListener(() => onSelected?.Invoke(_cardData));

        _cardData = cardData;
        EnsureRuntimeFrame();
        PrepareIconMaterial();
        ApplyCardLayout();
        DisableScaleAnimation();
        _nameText.gameObject.SetActive(false);
        _descriptionText.gameObject.SetActive(false);
        _levelText.gameObject.SetActive(true);
        if (_costText != null)
            _costText.gameObject.SetActive(false);
        Refresh(currentLevel, levelGain, isDeepening);
    }

    public void SetupFocusTarget(CollectibleData target, Sprite fallbackIcon)
    {
        if (target == null) return;

        if (!_useFrameHighlight && _highlightFrame != null)
        {
            _highlightFrame.SetActive(false);
        }

        _cardData = null;
        EnsureRuntimeFrame();
        PrepareIconMaterial();
        ApplyFocusTargetLayout();
        DisableScaleAnimation();
        _iconImage.sprite = fallbackIcon;
        _nameText.gameObject.SetActive(true);
        _nameText.text = CollectibleTable.GetDisplayName(target.Type);
        _descriptionText.gameObject.SetActive(false);
        _levelText.gameObject.SetActive(false);
        if (_costText != null)
            _costText.gameObject.SetActive(false);
        _selectButton.interactable = true;
    }

    /// <summary>
    /// 現在レベルに基づいて表示を更新する(再選択時も呼び出し)
    /// </summary>
    /// <param name="currentLevel">現在の取得レベル(未取得なら0)</param>
    public void Refresh(int currentLevel, int levelGain = 1, bool isDeepening = false)
    {
        int nextLevel = Mathf.Clamp(currentLevel + Mathf.Max(1, levelGain), 1, _cardData.MaxLevel);
        bool isMaxed = currentLevel >= _cardData.MaxLevel;

        _iconImage.sprite = _cardData.Icon;
        _nameText.text = _cardData.DisplayName;
        bool isOneTimeRule = _cardData.OfferType == UpgradeOfferType.Relic ||
                             _cardData.OfferType == UpgradeOfferType.Contract ||
                             _cardData.OfferType == UpgradeOfferType.Evolution;
        _descriptionText.text = isOneTimeRule
            ? _cardData.GetCardText(nextLevel)
            : _cardData.GetTransitionText(currentLevel, levelGain);
        _levelText.text = isMaxed
            ? "MAX"
            : GetStageLabel(currentLevel, nextLevel);
        ApplyOfferColors(_cardData.OfferType, isDeepening);

        if (_costText != null && isMaxed)
        {
            _costText.text = "取得済み";
        }
        else if (_costText != null)
        {
            _costText.text = GetStageLabel(currentLevel, nextLevel);
        }

        if(_acquiredMark != null)
        {
            _acquiredMark.SetActive(currentLevel > 0);
        }

        // 次第レベル到達済みなら選べなくする
        _selectButton.interactable = !isMaxed;
    }

    private static string GetStageLabel(int currentLevel, int nextLevel)
    {
        return currentLevel <= 0 ? $"Lv.{nextLevel}" : $"Lv.{currentLevel}→{nextLevel}";
    }

    private void ApplyOfferColors(UpgradeOfferType offerType, bool isDeepening)
    {
        bool isRuleChange = isDeepening ||
                            offerType == UpgradeOfferType.Relic ||
                            offerType == UpgradeOfferType.Contract ||
                            offerType == UpgradeOfferType.Evolution;
        _isRuleChange = isRuleChange;
        if (_runtimeFrame != null)
            _runtimeFrame.color = isRuleChange
                ? new Color(0.20f, 0.06f, 0.27f, 0.98f)
                : new Color(0.12f, 0.07f, 0.16f, 0.96f);
        if (_runtimeOutline != null)
            _runtimeOutline.effectColor = isRuleChange
                ? new Color(0.88f, 0.40f, 1f, 1f)
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

        ConfigureRect(_iconImage.rectTransform, new Vector2(0f, 16f), new Vector2(156f, 156f));
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;

        ConfigureText(_levelText, new Vector2(0f, -84f), new Vector2(194f, 32f), 28f, FontStyles.Bold);
        _levelText.color = new Color(1f, 0.72f, 0.28f, 1f);
    }

    private void ApplyFocusTargetLayout()
    {
        if (transform is RectTransform rootRect)
            rootRect.sizeDelta = new Vector2(216f, 216f);

        ConfigureRect(_iconImage.rectTransform, new Vector2(0f, 30f), new Vector2(138f, 138f));
        _iconImage.preserveAspect = true;
        _iconImage.raycastTarget = false;

        ConfigureText(_nameText, new Vector2(0f, -78f), new Vector2(194f, 42f), 30f, FontStyles.Bold);
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


    /// <summary>
    /// キーボード/ゲームパッドでのフォーカス状態を切り替える関数
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void SetHighlighted(bool isHighlighted)
    {
        if (_runtimeFrame != null)
            _runtimeFrame.color = isHighlighted
                ? new Color(0.28f, 0.12f, 0.30f, 1f)
                : _isRuleChange
                    ? new Color(0.20f, 0.06f, 0.27f, 0.98f)
                    : new Color(0.12f, 0.07f, 0.16f, 0.96f);
        if (_runtimeOutline != null)
            _runtimeOutline.effectColor = isHighlighted
                ? Color.white
                : _isRuleChange
                    ? new Color(0.88f, 0.40f, 1f, 1f)
                    : new Color(0.48f, 0.32f, 0.18f, 1f);

        _iconImage.material = isHighlighted || _sharedGrayscaleMaterial == null
            ? _defaultIconMaterial
            : _sharedGrayscaleMaterial;

        // 旧Frame方式
        if (_useFrameHighlight && _highlightFrame != null)
        {
            _highlightFrame.SetActive(isHighlighted);
        }
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
