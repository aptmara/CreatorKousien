using System.Collections.Generic;
using System.Linq;
using Game.Core.Events;
using Game.Core.Management;
using Game.Core.Roguelike;
using Game.Data.Collectibles;
using Game.Data.Player;
using Game.Gameplay.Roguelike;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ウェーブ間ショップを、3候補から1つ選ぶドラフトとして制御する。
/// </summary>
public class S_UpgradeSelectionUI : MonoBehaviour
{
    private enum SelectionPhase
    {
        Draft,
        CollectibleFocus,
        Closing,
    }

    private enum DraftKind
    {
        Standard,
        Contract,
        Evolution,
    }

    private readonly struct DraftCandidate
    {
        public readonly UpgradeData Data;
        public readonly int LevelGain;
        public readonly bool IsDeepening;

        public DraftCandidate(UpgradeData data, int levelGain = 1, bool isDeepening = false)
        {
            Data = data;
            LevelGain = Mathf.Max(1, levelGain);
            IsDeepening = isDeepening;
        }
    }

    [Header("データ参照")]
    [SerializeField] private SO_RoguelikeBalanceConfig _balanceConfig;
    [SerializeField] private SO_UpgradePool _upgradePool;
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;
    [SerializeField] private CollectibleTable _collectibleTable;

    [Header("結果反映先")]
    [SerializeField] private S_RoguelikeResultController _resultController;

    [Header("UI")]
    [SerializeField] private S_UpgradeCard _cardPrefab;
    [SerializeField] private Transform _cardParent;
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Button _exitButton;
    [SerializeField] private GameObject _exitUIRoot;
    [SerializeField] private Image _backGround;
    [SerializeField] private Image _upgradeBoard;
    [SerializeField] private MoneyData _moneyData;
    [SerializeField] private S_RoguelikeMoneyUI _moneyUI;
    [SerializeField] private S_UpgradeDetail _upgradeDetail;

    [Header("表示設定")]
    [SerializeField] private bool _showMoneyUI;
    [SerializeField] private bool _showExitButton;

    [Header("ドラフト設定")]
    [SerializeField, Min(1)] private int _cardidateCount = 3;
    [SerializeField, Min(0)] private int _rerollBaseCost = 50;

    [Header("グリッド設定")]
    [SerializeField, Min(1)] private int _columnCount = 3;

    private readonly List<S_UpgradeCard> _spawnedCards = new List<S_UpgradeCard>();
    private readonly Dictionary<S_UpgradeCard, UpgradeData> _cardToData =
        new Dictionary<S_UpgradeCard, UpgradeData>();
    private readonly Dictionary<S_UpgradeCard, DraftCandidate> _cardToCandidate =
        new Dictionary<S_UpgradeCard, DraftCandidate>();
    private readonly Dictionary<S_UpgradeCard, CollectibleData> _cardToCollectible =
        new Dictionary<S_UpgradeCard, CollectibleData>();

    private SelectionPhase _phase;
    private UpgradeData _pendingFocusedUpgrade;
    private Image _spawnedBoard;
    private Image _spawnedBackground;
    private TextMeshProUGUI _phaseHeader;
    private RectTransform _buildStrip;
    private DraftKind _draftKind;
    private WaveRewardDefinition _currentReward;

    public int CardCount => _spawnedCards.Count;
    public int ColumnCount => _columnCount;

    private void OnEnable()
    {
        if (_balanceConfig == null)
            _balanceConfig = SO_RoguelikeBalanceConfig.LoadDefault();
        if (_balanceConfig != null)
        {
            _upgradePool = _balanceConfig.UpgradePool;
            _collectibleTable = _balanceConfig.CollectibleTable;
        }

        EventBus.Subscribe<UpgradeSelectionRequestedEvent>(OnUpgradeSelectionRequested);
        if (_exitButton == null)
        {
            Debug.LogError("[S_UpgradeSelectionUI] リロールボタンが未設定です。");
            return;
        }

        _exitButton.onClick.AddListener(OnRerollButtonPressed);
        _exitUIRoot.SetActive(_showExitButton);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<UpgradeSelectionRequestedEvent>(OnUpgradeSelectionRequested);
        if (_exitButton != null)
            _exitButton.onClick.RemoveListener(OnRerollButtonPressed);
    }

    private void Start()
    {
        if (RoguelikeUpgradeRuntime.ConsumeRuntimeStateClearRequest())
            _upgradeRuntimeState.Clear();

        foreach (UpgradeRuntimeEntry entry in _upgradeRuntimeState.Entries)
        {
            if (entry?.CardData != null)
                Game.Gameplay.Roguelike.Effects.RoguelikeEffectRuntime.Register(entry.CardData, entry.Level);
        }

        _panelRoot.SetActive(false);
        OpenSelection();
    }

    public void OpenSelection()
    {
        _phase = SelectionPhase.Draft;
        _pendingFocusedUpgrade = null;
        _panelRoot?.SetActive(true);
        ClearCards();
        EnsureDecorations();

        int clearedWave = GameProgressionManager.Instance != null
            ? GameProgressionManager.Instance.CurrentWaveIndex
            : 1;
        _currentReward = _balanceConfig != null ? _balanceConfig.GetRewardForWave(clearedWave) : null;
        _draftKind = GetDraftKind(_currentReward, clearedWave);
        UpdatePhaseHeader(clearedWave);
        UpdateBuildStrip();

        _moneyUI.SpawnMoneyUI(_moneyData.moneyOnHand);
        _moneyUI.SetVisible(_showMoneyUI);
        List<UpgradeData> available = _upgradePool.GetAvailableUpgrades(_upgradeRuntimeState);
        foreach (DraftCandidate picked in PickDraftCandidates(available))
            SpawnUpgradeCard(picked);

        if (_spawnedCards.Count == 0)
            _upgradeDetail.ChangeReactionSoldOut();

        ConfigureRerollButton();
        SetFocusIndex(0);
    }

    public void OnFinishButtonPressed()
    {
        if (_phase == SelectionPhase.Closing)
            return;

        _phase = SelectionPhase.Closing;
        FinishSelection();
    }

    private void FinishSelection()
    {
        ClearCards();
        _panelRoot?.SetActive(false);
        _resultController.FinishRoguelikeScene();
    }

    public void TriggerSelectByCard(S_UpgradeCard card)
    {
        int index = _spawnedCards.IndexOf(card);
        if (index < 0)
            return;

        SetFocusIndex(index);
        TriggerSelect(index);
    }

    public void SetFocusIndex(int index)
    {
        for (int i = 0; i < _spawnedCards.Count; ++i)
            _spawnedCards[i].SetHighlighted(i == index);

        if (index < 0 || index >= _spawnedCards.Count)
            return;

        S_UpgradeCard focusedCard = _spawnedCards[index];
        if (_phase == SelectionPhase.CollectibleFocus &&
            _cardToCollectible.TryGetValue(focusedCard, out CollectibleData target))
        {
            _upgradeDetail.SpawnFocusTargetDetail(_pendingFocusedUpgrade, target);
        }
        else if (_cardToData.TryGetValue(focusedCard, out UpgradeData data))
        {
            DraftCandidate candidate = _cardToCandidate[focusedCard];
            _upgradeDetail.SpawnDetail(data, candidate.LevelGain, candidate.IsDeepening);
        }
    }

    public void TriggerSelect(int index)
    {
        if (_phase == SelectionPhase.Closing || index < 0 || index >= _spawnedCards.Count)
            return;

        S_UpgradeCard card = _spawnedCards[index];
        if (_phase == SelectionPhase.CollectibleFocus)
        {
            if (_cardToCollectible.TryGetValue(card, out CollectibleData target))
                CompleteDraft(card, _pendingFocusedUpgrade, target);
            return;
        }

        if (!_cardToCandidate.TryGetValue(card, out DraftCandidate selected))
            return;

        UpgradeData selectedCard = selected.Data;

        bool needsFocus = selectedCard.OfferType == UpgradeOfferType.CombatPressureRule &&
                          selectedCard.RequiresCollectibleFocus &&
                          !RoguelikeBuildRuntime.GetFocusedCollectibleType(selectedCard.CombatPressureRuleId).HasValue;
        if (needsFocus)
        {
            OpenCollectibleFocus(selectedCard);
            return;
        }

        CompleteDraft(card, selectedCard, null, selected.LevelGain, selected.IsDeepening);
    }

    public int IndexOfCard(S_UpgradeCard card) => _spawnedCards.IndexOf(card);

    private void OnUpgradeSelectionRequested(UpgradeSelectionRequestedEvent ev)
    {
        Debug.Log($"[S_UpgradeSelectionUI] 強化選択リクエスト受信(Lv.{ev.Level})");
        OpenSelection();
    }

    private List<DraftCandidate> PickDraftCandidates(List<UpgradeData> available)
    {
        return _draftKind switch
        {
            DraftKind.Contract => PickRuleCandidates(
                available.Where(item => item.OfferType == UpgradeOfferType.Contract).ToList()),
            DraftKind.Evolution => PickEvolutionCandidates(available),
            _ => PickStandardCandidates(available),
        };
    }

    private List<DraftCandidate> PickStandardCandidates(List<UpgradeData> available)
    {
        var remaining = available.Where(item =>
            item.OfferType == UpgradeOfferType.Standard ||
            item.OfferType == UpgradeOfferType.CombatPressureRule ||
            item.OfferType == UpgradeOfferType.Relic).ToList();
        var result = new List<DraftCandidate>();

        List<UpgradeData> acquired = remaining
            .Where(item => _upgradeRuntimeState.GetLevel(item) > 0)
            .ToList();
        if (acquired.Count > 0)
        {
            AddWeightedCandidate(acquired, remaining, result);
        }
        else
        {
            List<UpgradeData> buildStarters = remaining
                .Where(item => item.OfferType == UpgradeOfferType.CombatPressureRule)
                .ToList();
            AddWeightedCandidate(buildStarters, remaining, result);
        }

        List<UpgradeData> relics = remaining
            .Where(item => item.OfferType == UpgradeOfferType.Relic)
            .ToList();
        AddWeightedCandidate(relics, remaining, result);

        UpgradeSynergyTag ownedTags = GetOwnedTags();
        List<UpgradeData> synergisticNew = remaining
            .Where(item => _upgradeRuntimeState.GetLevel(item) == 0)
            .Where(item => ownedTags == UpgradeSynergyTag.None || (item.GetEffectiveTags() & ownedTags) != 0)
            .ToList();
        if (result.Count < CandidateCount)
            AddWeightedCandidate(synergisticNew, remaining, result);

        while (result.Count < CandidateCount && remaining.Count > 0)
            AddWeightedCandidate(remaining, remaining, result);

        return result;
    }

    private List<DraftCandidate> PickRuleCandidates(List<UpgradeData> source)
    {
        var result = new List<DraftCandidate>();
        var remaining = new List<UpgradeData>(source);
        while (result.Count < CandidateCount && remaining.Count > 0)
            AddWeightedCandidate(remaining, remaining, result);
        return result;
    }

    private List<DraftCandidate> PickEvolutionCandidates(List<UpgradeData> available)
    {
        var result = new List<DraftCandidate>();
        var evolutions = available
            .Where(item => item.OfferType == UpgradeOfferType.Evolution)
            .ToList();

        int evolutionCandidateCount = _currentReward != null
            ? _currentReward.EvolutionCandidateCount
            : 2;
        while (result.Count < Mathf.Min(evolutionCandidateCount, CandidateCount) && evolutions.Count > 0)
            AddWeightedCandidate(evolutions, evolutions, result);

        List<UpgradeData> deepeningPool = _upgradeRuntimeState.Entries
            .Where(entry => entry?.CardData != null)
            .Where(entry => entry.Level < entry.CardData.MaxLevel)
            .Where(entry => entry.CardData.OfferType == UpgradeOfferType.Standard ||
                            entry.CardData.OfferType == UpgradeOfferType.CombatPressureRule)
            .Select(entry => entry.CardData)
            .ToList();
        UpgradeData deepening = _currentReward == null || _currentReward.AllowDeepening
            ? PickWeighted(deepeningPool)
            : null;
        if (deepening != null && result.Count < CandidateCount)
        {
            int remainingLevels = deepening.MaxLevel - _upgradeRuntimeState.GetLevel(deepening);
            int levelGain = _currentReward != null ? _currentReward.DeepeningLevelGain : 2;
            result.Add(new DraftCandidate(deepening, Mathf.Min(levelGain, remainingLevels), true));
        }

        while (result.Count < CandidateCount && evolutions.Count > 0)
            AddWeightedCandidate(evolutions, evolutions, result);

        for (int index = result.Count - 1; index > 0; index--)
        {
            int swap = Random.Range(0, index + 1);
            (result[index], result[swap]) = (result[swap], result[index]);
        }

        return result;
    }

    private void AddWeightedCandidate(
        IReadOnlyList<UpgradeData> source,
        ICollection<UpgradeData> remaining,
        ICollection<DraftCandidate> candidates)
    {
        UpgradeData picked = PickWeighted(source);
        if (picked == null)
            return;

        candidates.Add(new DraftCandidate(picked));
        remaining.Remove(picked);
    }

    private UpgradeData PickWeighted(IReadOnlyList<UpgradeData> source)
    {
        if (source == null || source.Count == 0)
            return null;

        float totalWeight = 0f;
        for (int index = 0; index < source.Count; index++)
            totalWeight += GetCandidateWeight(source[index]);

        float selectedWeight = Random.value * totalWeight;
        for (int index = 0; index < source.Count; index++)
        {
            selectedWeight -= GetCandidateWeight(source[index]);
            if (selectedWeight <= 0f)
                return source[index];
        }

        return source[source.Count - 1];
    }

    private float GetCandidateWeight(UpgradeData data)
    {
        if (data == null)
            return 0f;

        int level = _upgradeRuntimeState.GetLevel(data);
        RoguelikeDraftTuning tuning = _balanceConfig != null ? _balanceConfig.Draft : new RoguelikeDraftTuning();
        return tuning.GetCandidateWeight(data, level, GetOwnedTags(), GetSuppressedTags());
    }

    private UpgradeSynergyTag GetOwnedTags()
    {
        UpgradeSynergyTag tags = UpgradeSynergyTag.None;
        foreach (UpgradeRuntimeEntry entry in _upgradeRuntimeState.Entries)
        {
            if (entry?.CardData != null)
                tags |= entry.CardData.GetEffectiveTags();
        }
        return tags;
    }

    private UpgradeSynergyTag GetSuppressedTags()
    {
        UpgradeSynergyTag tags = UpgradeSynergyTag.None;
        foreach (UpgradeRuntimeEntry entry in _upgradeRuntimeState.Entries)
        {
            if (entry?.CardData != null && entry.CardData.OfferType == UpgradeOfferType.Contract)
                tags |= entry.CardData.SuppressedTags;
        }
        return tags;
    }

    private void SpawnUpgradeCard(DraftCandidate candidate)
    {
        UpgradeData cardData = candidate.Data;
        S_UpgradeCard card = Instantiate(_cardPrefab, _cardParent);
        card.Setup(
            cardData,
            _upgradeRuntimeState.GetLevel(cardData),
            candidate.LevelGain,
            candidate.IsDeepening);
        _spawnedCards.Add(card);
        _cardToData[card] = cardData;
        _cardToCandidate[card] = candidate;
    }

    private void OpenCollectibleFocus(UpgradeData selectedUpgrade)
    {
        _phase = SelectionPhase.CollectibleFocus;
        _pendingFocusedUpgrade = selectedUpgrade;
        ClearCards();

        if (_collectibleTable == null)
        {
            Debug.LogError("[S_UpgradeSelectionUI] CollectibleTableが未設定です。");
            OpenSelection();
            return;
        }

        foreach (CollectibleData target in _collectibleTable.GetAllItems())
        {
            S_UpgradeCard card = Instantiate(_cardPrefab, _cardParent);
            card.SetupFocusTarget(target, selectedUpgrade.Icon);
            _spawnedCards.Add(card);
            _cardToCollectible[card] = target;
        }

        ConfigureRerollButton();
        SetFocusIndex(0);
    }

    private void CompleteDraft(
        S_UpgradeCard selectedVisual,
        UpgradeData selectedUpgrade,
        CollectibleData focusedCollectible,
        int levelGain = 1,
        bool isDeepening = false)
    {
        if (!_resultController.SelectUpgrade(selectedUpgrade, focusedCollectible, levelGain))
            return;

        _phase = SelectionPhase.Closing;
        Debug.Log($"[S_UpgradeSelectionUI] '{selectedUpgrade.DisplayName}'を取得。");
        _upgradeDetail.SpawnDetail(selectedUpgrade, levelGain, isDeepening, false);
        selectedVisual.PlaySelectedAnimation(FinishSelection);
    }

    private void OnRerollButtonPressed()
    {
        if (_phase == SelectionPhase.Closing)
            return;

        if (_phase == SelectionPhase.CollectibleFocus)
        {
            OpenSelection();
            return;
        }

        int cost = RoguelikeUpgradeRuntime.GetRerollCost(RerollBaseCost);
        if (_moneyData.moneyOnHand < cost)
        {
            _upgradeDetail.ChangeReactionNotEnouthMoney();
            return;
        }

        _moneyData.SubtractMoney(cost);
        _moneyUI.ChangeMoneyUI(_moneyData.moneyOnHand);
        OpenSelection();
    }

    private void ConfigureRerollButton()
    {
        if (_exitButton == null)
            return;

        bool isFocusSelection = _phase == SelectionPhase.CollectibleFocus;
        int rerollCost = RoguelikeUpgradeRuntime.GetRerollCost(RerollBaseCost);
        _exitButton.interactable = isFocusSelection || _spawnedCards.Count > 0;

        foreach (Text text in _exitButton.GetComponentsInChildren<Text>(true))
            text.text = isFocusSelection ? "戻る" : "再抽選";

        foreach (TextMeshProUGUI text in _exitButton.GetComponentsInChildren<TextMeshProUGUI>(true))
            text.text = isFocusSelection ? "候補選択へ戻る" : $"候補を入れ替える  {rerollCost}コイン";
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

    private static DraftKind GetDraftKind(WaveRewardDefinition reward, int clearedWave)
    {
        _ = clearedWave;
        if (reward != null)
        {
            return reward.RewardKind switch
            {
                WaveRewardKind.Contract => DraftKind.Contract,
                WaveRewardKind.Evolution => DraftKind.Evolution,
                _ => DraftKind.Standard,
            };
        }

        return DraftKind.Standard;
    }

    private int CandidateCount => _currentReward != null
        ? _currentReward.CandidateCount
        : _balanceConfig != null ? _balanceConfig.Draft.DefaultCandidateCount : _cardidateCount;

    private int RerollBaseCost => _balanceConfig != null
        ? _balanceConfig.Draft.RerollBaseCost
        : _rerollBaseCost;

    private void UpdatePhaseHeader(int clearedWave)
    {
        if (_phaseHeader == null)
        {
            var headerObject = new GameObject("RoguelikePhaseHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            headerObject.transform.SetParent(_panelRoot.transform, false);
            RectTransform rect = headerObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(210f, -34f);
            rect.sizeDelta = new Vector2(620f, 58f);
            _phaseHeader = headerObject.GetComponent<TextMeshProUGUI>();
            _phaseHeader.font = GetShopFont();
            _phaseHeader.alignment = TextAlignmentOptions.Center;
            _phaseHeader.fontStyle = FontStyles.Bold;
            _phaseHeader.fontSize = 44f;
            _phaseHeader.raycastTarget = false;
        }

        _phaseHeader.text = _draftKind switch
        {
            DraftKind.Contract => $"WAVE {clearedWave}  契約 — このランの法則を選ぶ",
            DraftKind.Evolution => $"WAVE {clearedWave}  進化 — 変質か深化を選ぶ",
            _ => $"WAVE {clearedWave}  成長・遺物",
        };
        _phaseHeader.color = _draftKind == DraftKind.Standard
            ? new Color(0.96f, 0.82f, 0.48f, 1f)
            : new Color(0.88f, 0.48f, 1f, 1f);
        _phaseHeader.transform.SetAsLastSibling();
    }

    private void UpdateBuildStrip()
    {
        if (_buildStrip == null)
        {
            var stripObject = new GameObject("RoguelikeBuildStrip", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            stripObject.transform.SetParent(_panelRoot.transform, false);
            _buildStrip = stripObject.GetComponent<RectTransform>();
            _buildStrip.anchorMin = new Vector2(0.5f, 1f);
            _buildStrip.anchorMax = new Vector2(0.5f, 1f);
            _buildStrip.anchoredPosition = new Vector2(210f, -94f);
            _buildStrip.sizeDelta = new Vector2(620f, 46f);

            HorizontalLayoutGroup layout = stripObject.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        for (int index = _buildStrip.childCount - 1; index >= 0; index--)
            Destroy(_buildStrip.GetChild(index).gameObject);

        foreach (UpgradeRuntimeEntry entry in _upgradeRuntimeState.Entries.Take(9))
        {
            if (entry?.CardData == null || entry.CardData.Icon == null)
                continue;

            var iconObject = new GameObject(
                $"BuildIcon_{entry.CardData.Id}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline),
                typeof(LayoutElement));
            iconObject.transform.SetParent(_buildStrip, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(42f, 42f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = entry.CardData.Icon;
            icon.preserveAspect = true;
            icon.color = Color.white;
            iconObject.GetComponent<Outline>().effectColor = entry.CardData.OfferType == UpgradeOfferType.Standard ||
                                                            entry.CardData.OfferType == UpgradeOfferType.CombatPressureRule
                ? new Color(1f, 0.68f, 0.22f, 1f)
                : new Color(0.88f, 0.40f, 1f, 1f);
            LayoutElement element = iconObject.GetComponent<LayoutElement>();
            element.preferredWidth = 42f;
            element.preferredHeight = 42f;

            var levelObject = new GameObject("Level", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            levelObject.transform.SetParent(iconObject.transform, false);
            RectTransform levelRect = levelObject.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.5f, 0f);
            levelRect.anchorMax = new Vector2(0.5f, 0f);
            levelRect.anchoredPosition = new Vector2(0f, -7f);
            levelRect.sizeDelta = new Vector2(52f, 20f);
            TextMeshProUGUI level = levelObject.GetComponent<TextMeshProUGUI>();
            level.font = GetShopFont();
            level.text = entry.CardData.MaxLevel > 1 ? $"Lv.{entry.Level}" : "◆";
            level.alignment = TextAlignmentOptions.Center;
            level.fontStyle = FontStyles.Bold;
            level.fontSize = 18f;
            level.color = Color.white;
            level.raycastTarget = false;
        }

        _buildStrip.transform.SetAsLastSibling();
    }

    private TMP_FontAsset GetShopFont()
    {
        if (_cardPrefab == null)
            return null;

        TextMeshProUGUI source = _cardPrefab
            .GetComponentsInChildren<TextMeshProUGUI>(true)
            .FirstOrDefault(text => text.font != null);
        return source != null ? source.font : null;
    }

    private void ClearCards()
    {
        foreach (S_UpgradeCard card in _spawnedCards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }

        _spawnedCards.Clear();
        _cardToData.Clear();
        _cardToCandidate.Clear();
        _cardToCollectible.Clear();
    }
}
