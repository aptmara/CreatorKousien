//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_ShopMenuUI.cs
// brief  : 常設MENU型ショップの制御。
//          移動速度/バリア耐久力/出現数/腕拡大の基本強化と、
//          キャンディ(アイテム出現率アップ)を常時一覧表示し、
//          コインを消費して個別に購入できるようにする。
//
// auther : Asano Yuki
// date   : 2026/09/07 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Roguelike;
using Game.Data.Player;
using Game.Gameplay.Roguelike;
using Game.Gameplay.Roguelike.Effects;
using UnityEngine;
using UnityEngine.UI;

public class S_ShopMenuUI : MonoBehaviour
{
    // 上段に常設表示する基本強化のId(移動速度/バリア耐久力/出現数/腕拡大)
    private static readonly string[] TopUpgradeIds = { "2", "20", "5", "1" };

    [Header("データ参照")]
    [SerializeField] private SO_RoguelikeBalanceConfig _balanceConfig;
    [SerializeField] private SO_UpgradePool _upgradePool;
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;

    [Header("結果反映先")]
    [SerializeField] private S_RoguelikeResultController _resultController;

    [Header("UI")]
    [SerializeField] private S_UpgradeCard _cardPrefab;
    [Tooltip("移動速度/バリア耐久力/出現数/腕拡大を並べる親")]
    [SerializeField] private Transform _topGridParent;
    [Tooltip("キャンディ(アイテム出現率アップ)を並べる親")]
    [SerializeField] private Transform _consumableGridParent;
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("次のウェーブへ進むボタン")]
    [SerializeField] private Button _departButton;
    [SerializeField] private MoneyData _moneyData;
    [SerializeField] private S_RoguelikeMoneyUI _moneyUI;
    [SerializeField] private S_UpgradeDetail _upgradeDetail;

    [Header("装飾")]
    [Tooltip("店の背景(Canvas_Backへ配置)")]
    [SerializeField] private Image _backGround;
    [Tooltip("強化パネルの台座/看板(パネル最背面へ配置)")]
    [SerializeField] private Image _upgradeBoard;

    private readonly List<S_UpgradeCard> _spawnedCards = new List<S_UpgradeCard>();
    private Image _spawnedBoard;
    private Image _spawnedBackground;

    private void OnEnable()
    {
        if (_balanceConfig == null)
            _balanceConfig = SO_RoguelikeBalanceConfig.LoadDefault();
        if (_balanceConfig != null)
            _upgradePool = _balanceConfig.UpgradePool;

        EventBus.Subscribe<UpgradeSelectionRequestedEvent>(OnUpgradeSelectionRequested);

        if (_departButton != null)
            _departButton.onClick.AddListener(OnDepartButtonPressed);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UpgradeSelectionRequestedEvent>(OnUpgradeSelectionRequested);
        if (_departButton != null)
            _departButton.onClick.RemoveListener(OnDepartButtonPressed);
    }

    private void Start()
    {
        if (RoguelikeUpgradeRuntime.ConsumeRuntimeStateClearRequest())
            _upgradeRuntimeState.Clear();

        foreach (UpgradeRuntimeEntry entry in _upgradeRuntimeState.Entries)
        {
            if (entry?.CardData != null)
                RoguelikeEffectRuntime.Register(entry.CardData, entry.Level);
        }

        _panelRoot?.SetActive(false);
        OpenMenu();
    }

    public void OpenMenu()
    {
        _panelRoot?.SetActive(true);
        ClearCards();
        EnsureDecorations();

        _moneyUI.SpawnMoneyUI(_moneyData.moneyOnHand);
        _moneyUI.SetVisible(true);

        if (_upgradePool == null)
        {
            Debug.LogError("[S_ShopMenuUI] 強化プールが未設定です。");
            return;
        }

        foreach (string id in TopUpgradeIds)
        {
            UpgradeData data = _upgradePool.GetById(id);
            if (data != null)
                SpawnCard(data, _topGridParent);
            else
                Debug.LogWarning($"[S_ShopMenuUI] Id='{id}'の強化がプールに見つかりません。");
        }

        List<UpgradeData> consumables = _upgradePool.Upgrades
            .Where(item => item != null && item.Category == UpgradeCategory.Consumable)
            .OrderBy(item => item.DisplayName)
            .ToList();
        foreach (UpgradeData data in consumables)
            SpawnCard(data, _consumableGridParent);

        // 店主(詳細パネル)はホバー前から常時表示しておく
        if (_spawnedCards.Count > 0 && _spawnedCards[0].CardData != null)
            _upgradeDetail.SpawnDetail(_spawnedCards[0].CardData, 1, false);
    }

    private void SpawnCard(UpgradeData data, Transform parent)
    {
        Transform spawnParent = parent != null ? parent : _topGridParent;
        S_UpgradeCard card = Instantiate(_cardPrefab, spawnParent);
        card.Setup(data, _upgradeRuntimeState.GetLevel(data));
        card.HoverEnter += OnCardHoverEnter;
        card.Clicked += OnCardClicked;
        _spawnedCards.Add(card);
    }

    private void OnCardHoverEnter(S_UpgradeCard card)
    {
        if (card.CardData != null)
            _upgradeDetail.SpawnDetail(card.CardData, 1, false);
    }

    private void OnCardClicked(S_UpgradeCard card)
    {
        UpgradeData data = card.CardData;
        if (data == null)
            return;

        int currentLevel = _upgradeRuntimeState.GetLevel(data);
        if (currentLevel >= data.MaxLevel)
            return;

        int cost = data.GetCost(currentLevel);
        if (_moneyData.moneyOnHand < cost)
        {
            _upgradeDetail.ChangeReactionNotEnouthMoney();
            return;
        }

        _moneyData.SubtractMoney(cost);
        _moneyUI.ChangeMoneyUI(_moneyData.moneyOnHand);

        if (!_resultController.SelectUpgrade(data, null, 1))
        {
            // 反映に失敗した場合はコインを払い戻す(異常系のフォールバック)
            _moneyData.SubtractMoney(-cost);
            _moneyUI.ChangeMoneyUI(_moneyData.moneyOnHand);
            return;
        }

        int newLevel = _upgradeRuntimeState.GetLevel(data);
        card.Refresh(newLevel);
        _upgradeDetail.ChangeReactionOnPurchase(data);
    }

    private void OnUpgradeSelectionRequested(UpgradeSelectionRequestedEvent ev)
    {
        OpenMenu();
    }

    private void OnDepartButtonPressed()
    {
        ClearCards();
        _panelRoot?.SetActive(false);
        _resultController.FinishRoguelikeScene();
    }

    private void EnsureDecorations()
    {
        if (_spawnedBoard == null && _upgradeBoard != null)
        {
            _spawnedBoard = Instantiate(_upgradeBoard, _panelRoot.transform);
            _spawnedBoard.transform.SetAsFirstSibling();
        }

        if (_spawnedBackground != null || _backGround == null)
            return;

        Canvas backCanvas = GameObject.Find("Canvas_Back")?.GetComponent<Canvas>();
        if (backCanvas != null)
        {
            _spawnedBackground = Instantiate(_backGround, backCanvas.transform);
            _spawnedBackground.transform.SetAsFirstSibling();
        }
    }

    private void ClearCards()
    {
        foreach (S_UpgradeCard card in _spawnedCards)
        {
            if (card == null)
                continue;
            card.HoverEnter -= OnCardHoverEnter;
            card.Clicked -= OnCardClicked;
            Destroy(card.gameObject);
        }
        _spawnedCards.Clear();
    }
}
