//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeSelectionUI.cs
// brief  : 画面全体の制御(候補表示・選択の受付)
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//          2026/07/02 - EventBus連携を追加
//                       複数選択 + 終了ボタン
//
// todo : line 191 
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Player;
using UnityEngine.UI;

public class S_UpgradeSelectionUI : MonoBehaviour
{
    //____________________________________
    // variables

    [Header("データ参照")]
    [SerializeField] private SO_UpgradePool _upgradePool;
    [Header("強化データを保存")]
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;

    [Header("結果反映先")]
    [SerializeField] private S_RoguelikeResultController _resultController;

    [Header("UI")]
    [Space(10)]
    [Header("強化カードのプレハブ")]
    [SerializeField] private S_UpgradeCard _cardPrefab;
    [Header("カードの生成位置")]
    [SerializeField] private Transform _cardParent;
    [Tooltip("選択画面のルートオブジェクト(非表示切り替え対象)")]
    [SerializeField] private GameObject _panelRoot;
    [Header("終了ボタンUI")]
    [SerializeField] private Button _exitButton;
    [Header("背景(Shader入れたら使わないかも)")]
    [SerializeField] private Image _backGround;

    [Header("カードの背景に配置するボードのプレハブ")]
    [SerializeField] private Image _upgradeBoard;

    [Header("SOから取得するお金のデータ")]
    [SerializeField] private MoneyData _moneyData;
    [Header("お金表示UI")]
    [SerializeField] private S_RoguelikeMoneyUI _moneyUI;

    [Header("1回に提示する候補数")]
    [SerializeField] private int _cardidateCount = 3;


    [Header("グリッド設定")]
    [Tooltip("1行当たりのカード数(GridLayoutGroupと一致させる)")]
    [SerializeField] private int _columnCount = 3;


    private readonly List<S_UpgradeCard> _spawnedCards = new();
    private readonly Dictionary<S_UpgradeCard, UpgradeData> _cardToData = new();

    public int CardCount => _spawnedCards.Count;
    public int ColumnCount => _columnCount;


    //__________________________________________
    // basic functions

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

    private void Start()
    {
        _panelRoot.SetActive(false);
        OpenSelection();
    }


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

        _moneyUI.SpawnMoneyUI(_moneyData.moneyOnHand);

        //! 怪しい
        List<UpgradeData> available = new List<UpgradeData>(
            _upgradePool.GetAvailableUpgrades(_upgradeRuntimeState));

        for(int i = 0; i< _cardidateCount; ++i)
        {
            if (available.Count == 0) break;

            int index = Random.Range(0, available.Count);
            UpgradeData picked = available[index];
            available.RemoveAt(index);

            SpawnCard(picked);
        }


        // ボード
        Instantiate(_upgradeBoard, _panelRoot.transform).transform.SetAsFirstSibling();

        // 背景
        Canvas backCanvas = GameObject.Find("Canvas_Back")?.GetComponent<Canvas>();
        if(backCanvas != null)
        {
            Instantiate(_backGround, backCanvas.transform).transform.SetAsFirstSibling();
        }

        // 開始時、1枚目にフォーカス
        SetFocusIndex(0);

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




    //____________________________________
    // private function

    /// <summary>
    /// レベルアップ通知を受けて選択画面を開くコールバック
    /// </summary>
    /// <param name="e"></param>
    private void OnUpgradeSelectionRequested(UpgradeSelectionRequestedEvent e)
    {
        Debug.Log($"[S_UpgradeSelectionUI] 強化選択リクエスト受信(Lv.{e.Level})");
        OpenSelection();
    }


    private void SpawnCard(UpgradeData cardData)
    {
        int currentLevel = _upgradeRuntimeState.GetLevel(cardData);

        S_UpgradeCard card = Instantiate(_cardPrefab, _cardParent);
        card.Setup(cardData, currentLevel, OnCardSelected);

        _spawnedCards.Add(card);
        _cardToData[card] = cardData;
    }

    private void OnCardSelected(UpgradeData selectedCard)
    {
        // 現在のレベルを取得
        int nowLevel = _upgradeRuntimeState.GetLevel(selectedCard);
        // 現在のレベルが最大レベルなら処理しない
        if (nowLevel >= selectedCard.MaxLevel) return;

        // 減らす量を計算
        int subtractMoney = selectedCard.GetCost(nowLevel);


        // お金が足りなければ処理しない
        int nowMoney = _moneyData.moneyOnHand;
        if (nowMoney < subtractMoney)    return;


        // 所持金を減らす
        Debug.Log($"[S_UpgradeSelectionUI] '{selectedCard.DisplayName}'購入！");
        _moneyData.SubtractMoney((int)subtractMoney);
        _moneyUI.ChangeMoneyUI(_moneyData.moneyOnHand);

        _resultController.SelectUpgrade(selectedCard);

        // 選択したカードのみ表示を更新
        int newLevel = _upgradeRuntimeState.GetLevel(selectedCard);

        foreach (var card in _spawnedCards)
        {
            if(_cardToData.TryGetValue(card, out var data) && data == selectedCard)
            {
                card.Refresh(newLevel);
                card.PlaySelectedAnimation();
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

    /// <summary>
    /// キーボード/ゲームパッドのフォーカス位置を切り替える関数
    /// </summary>
    /// <param name="index"></param>
    public void SetFocusIndex(int index)
    {
        for(int i = 0; i < _spawnedCards.Count; ++i)
        {
            _spawnedCards[i].SetHighlighted(i == index);
        }
    }

    /// <summary>
    /// 指定インデックスのカードを、マウスクリックと同じ経路で選択する関数
    /// (Submit入力から呼ばれる想定)
    /// </summary>
    /// <param name="index"></param>
    public void TriggerSelect(int index)
    {
        if (index < 0 || index >= _spawnedCards.Count) return;

        S_UpgradeCard card = _spawnedCards[index];
        if(_cardToData.TryGetValue(card, out var data))
        {
            OnCardSelected(data);
        }
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
