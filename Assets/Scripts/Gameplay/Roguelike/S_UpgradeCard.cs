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
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private GameObject _acquiredMark;
    [SerializeField] private GameObject _highlightFrame;

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
        _cardData = cardData;

        _selectButton.onClick.RemoveAllListeners();
        _selectButton.onClick.AddListener(() => onSelected?.Invoke(_cardData));

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

        if(_acquiredMark != null)
        {
            _acquiredMark.SetActive(currentLevel > 0);
        }

        // 次第レベル到達済みなら選べなくする
        _selectButton.interactable = !isMaxed;
    }

    public void SetHighlightFrame(bool isHighlighted)
    {
        if(_highlightFrame != null)
        {
            _highlightFrame.SetActive(isHighlighted);
        }
    }

}
