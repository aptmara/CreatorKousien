//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeSelectionUI.cs
// brief  : 画面全体の制御(候補表示・選択の受付)
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//          2026/07/02 - EventBus連携を追加
//                       複数選択 + 終了ボタン
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class S_UpgradeSelectionUI : MonoBehaviour
{
    //____________________________________
    // variables

    [Header("データ参照")]
    [SerializeField] private SO_UpgradePool _upgradePool;
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;

    [Header("結果反映先")]
    [SerializeField] private S_RoguelikeResultController _resultController;

    [Header("UI")]
    [SerializeField] private S_UpgradeCard _cardPrefab;
    [SerializeField] private Transform _cardParent;
    [Tooltip("選択画面のルートオブジェクト(非表示切り替え対象)")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _exitButton;
    [SerializeField] private Image _backGround;


    [Header("設定")]
    [Tooltip("1回に提示する候補数")]
    [SerializeField] private int _cardidateCount = 3;

    private readonly List<S_UpgradeCard> _spawnedCards = new();
    private readonly Dictionary<S_UpgradeCard, SO_UpgradeCardData> _cardToData = new();

    public MoneyData MoneyData { get; set; }

#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            OpenSelection();
        }
    }
#endif

    //____________________________________
    // public function

    /// <summary>
    /// 選択画面を開き、候補を表示
    /// 一旦ランダム設定
    /// </summary>
    public void OpenSelection()
    {
        if(_panelRoot != null)
        {
            _panelRoot.SetActive(true);
        }

        ClearCards();

        List<SO_UpgradeCardData> available = new List<SO_UpgradeCardData>(
            _upgradePool.GetAvailableUpgrades(_upgradeRuntimeState));

        for(int i = 0; i< _cardidateCount; ++i)
        {
            if (available.Count == 0) break;

            int index = Random.Range(0, available.Count);
            SO_UpgradeCardData picked = available[index];
            available.RemoveAt(index);

            SpawnCard(picked);
        }

        // 背景
        Instantiate(_backGround, _panelRoot.transform).transform.SetAsFirstSibling();
    }

    public void OnFinishButtonPressed()
    {

        ClearCards();

        if(_panelRoot != null)
        {
            _panelRoot.SetActive(false);
        }

        _resultController.FinishRoguelikeScene();
    }

    private void Start()
    {
        _panelRoot.SetActive(false);
        OpenSelection();
    }

    //____________________________________
    // private function

    private void OnEnable()
    {
        EventBus.Subscribe<UpgradeSelectionRequestedEvent>(
            OnUpgradeSelectionRequested);
        _exitButton.onClick.AddListener(OnFinishButtonPressed);
        Debug.Log("RoguelikeEnable");
        //CreateCanvasChild();
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UpgradeSelectionRequestedEvent>(
            OnUpgradeSelectionRequested);
        _exitButton.onClick.RemoveListener(OnFinishButtonPressed);
    }

    /// <summary>
    /// レベルアップ通知を受けて選択画面を開くコールバック
    /// </summary>
    /// <param name="e"></param>
    private void OnUpgradeSelectionRequested(UpgradeSelectionRequestedEvent e)
    {
        Debug.Log($"[S_UpgradeSelectionUI] 強化選択リクエスト受信(Lv.{e.Level})");
        OpenSelection();
    }


    private void SpawnCard(SO_UpgradeCardData cardData)
    {
        int currentLevel = _upgradeRuntimeState.GetLevel(cardData);

        S_UpgradeCard card = Instantiate(_cardPrefab, _cardParent);
        card.Setup(cardData, currentLevel, OnCardSelected);

        _spawnedCards.Add(card);
        _cardToData[card] = cardData;
    }

    private void OnCardSelected(SO_UpgradeCardData selectedCard)
    {
        // 所持金を減らす
//        MoneyData.SubtractMoney(10);

        _resultController.SelectUpgrade(selectedCard);

        // 選択したカードのみ表示を更新
        int newLevel = _upgradeRuntimeState.GetLevel(selectedCard);

        foreach(var card in _spawnedCards)
        {
            if(_cardToData.TryGetValue(card, out var data) && data == selectedCard)
            {
                card.Refresh(newLevel);
                break;
            }
        }
    }

    private void ClearCards()
    {
        foreach(var card in _spawnedCards)
        {
            if(card != null)
            {
                Destroy(card.gameObject);
            }
        }
        _spawnedCards.Clear();
        _cardToData.Clear();
    }

    private void CreateCanvasChild()
    {
        // 背景
        Instantiate(_backGround, _panelRoot.transform).transform.SetAsFirstSibling() ;

        // カード生成
        S_UpgradeCard card = Instantiate(_cardPrefab, _cardParent);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchoredPosition = new Vector2(-600.0f, 0);

        Button exit = Instantiate(_exitButton, _cardParent);
        RectTransform buttonRect = exit.GetComponent<RectTransform>();
        buttonRect.anchoredPosition = new Vector2(700.0f, -450.0f);     // 画面端基準にしたい

        exit.onClick.AddListener(OnFinishButtonPressed);
    }

}
