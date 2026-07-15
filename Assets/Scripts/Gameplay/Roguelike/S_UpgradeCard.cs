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

    [Header("旧Frame演出")]
    [SerializeField] private GameObject _highlightFrame;
    [Header("旧Frameを使用するか")]
    [SerializeField] private bool _useFrameHighlight = false;

    [Header("機能面")]
    [SerializeField] private Button _selectButton;


    private UpgradeData _cardData;


    //____________________________________
    // public function

    /// <summary>
    /// カードの表示内容を設定する
    /// </summary>
    /// <param name="cardData">表示するカードデータ</param>
    /// <param name="currentLevel">現在の取得済みレベル</param>
    /// <param name="onSelected">選択時に呼ばれるコールバック</param>
    public void Setup(UpgradeData cardData, int currentLevel,
        System.Action<UpgradeData> onSelected)
    {
        if (!_useFrameHighlight && _highlightFrame != null)
        {
            _highlightFrame.SetActive(false);
        }

        _cardData = cardData;

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => onSelected?.Invoke(_cardData));

        // levelが0スタートのため、補正値として + 1
        Refresh(currentLevel);
    }

    /// <summary>
    /// 現在レベルに基づいて表示を更新する(再選択時も呼び出し)
    /// </summary>
    /// <param name="currentLevel">現在の取得レベル(未取得なら0)</param>
    public void Refresh(int currentLevel)
    {
        int nextLevel = Mathf.Clamp(currentLevel + 1, 1, _cardData.MaxLevel);
        bool isMaxed = currentLevel >= _cardData.MaxLevel;

        _iconImage.sprite = _cardData.Icon;
        _nameText.text = _cardData.DisplayName;
        _descriptionText.text = _cardData.GetEffectText(nextLevel);
        _levelText.text = $"Lv.{currentLevel}/{_cardData.MaxLevel}";

        int cost = _cardData.GetCost(currentLevel);
        if(isMaxed)
        {
            _costText.text = $"Level Max";
        }
        else
        {
            _costText.text = $"Cost : {cost}";
        }

        if(_acquiredMark != null)
        {
            _acquiredMark.SetActive(currentLevel > 0);
        }

        // 次第レベル到達済みなら選べなくする
        _selectButton.interactable = !isMaxed;
    }


    /// <summary>
    /// キーボード/ゲームパッドでのフォーカス状態を切り替える関数
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void SetHighlighted(bool isHighlighted)
    {
        if(_scaleAnimator != null)
        {
            _scaleAnimator.SetHighlighted(isHighlighted);
        }

        // 旧Frame方式
        if(_useFrameHighlight && _highlightFrame != null)
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
        _scaleAnimator?.PlaySelectedAnimation(onComplete);
    }

}
