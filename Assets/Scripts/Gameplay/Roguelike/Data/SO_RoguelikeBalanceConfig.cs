using System;
using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.Data.Player;
using Game.Core.Roguelike;
using Game.Gameplay.Roguelike.CombatPressure;
using Game.WaveSystem;
using UnityEngine;

namespace Game.Gameplay.Roguelike
{
    public enum WaveRewardKind
    {
        Standard,
        Contract,
        Evolution,
        None,
    }

    [Serializable]
    public sealed class WaveRewardDefinition
    {
        [SerializeField, Min(1)] private int _clearedWave = 1;
        [SerializeField] private WaveRewardKind _rewardKind = WaveRewardKind.Standard;
        [SerializeField, Min(1)] private int _candidateCount = 3;
        [SerializeField, Min(0)] private int _evolutionCandidateCount = 2;
        [SerializeField] private bool _allowDeepening;
        [SerializeField, Min(1)] private int _deepeningLevelGain = 2;

        public int ClearedWave => _clearedWave;
        public WaveRewardKind RewardKind => _rewardKind;
        public int CandidateCount => Mathf.Max(1, _candidateCount);
        public int EvolutionCandidateCount => Mathf.Max(0, _evolutionCandidateCount);
        public bool AllowDeepening => _allowDeepening;
        public int DeepeningLevelGain => Mathf.Max(1, _deepeningLevelGain);
    }

    [Serializable]
    public sealed class RoguelikeDraftTuning
    {
        [SerializeField, Min(1)] private int _defaultCandidateCount = 3;
        [SerializeField, Min(0)] private int _rerollBaseCost = 50;
        [SerializeField, Min(0f)] private float _ownedLevelWeight = 1.4f;
        [SerializeField, Min(0f)] private float _synergyWeightBonus = 2f;
        [SerializeField, Min(0f)] private float _suppressedWeightMultiplier = 0.2f;
        [SerializeField, Min(0.001f)] private float _minimumWeight = 0.05f;

        public int DefaultCandidateCount => Mathf.Max(1, _defaultCandidateCount);
        public int RerollBaseCost => Mathf.Max(0, _rerollBaseCost);
        public float OwnedLevelWeight => Mathf.Max(0f, _ownedLevelWeight);
        public float SynergyWeightBonus => Mathf.Max(0f, _synergyWeightBonus);
        public float SuppressedWeightMultiplier => Mathf.Max(0f, _suppressedWeightMultiplier);
        public float MinimumWeight => Mathf.Max(0.001f, _minimumWeight);

        /// <summary>
        /// ドラフト抽選の実重み計算。S_UpgradeSelectionUIの実装と一致させること
        /// （バランスエディタの検証シミュレーションもここを共用する）。
        /// </summary>
        public float GetCandidateWeight(
            UpgradeData data,
            int ownedLevel,
            UpgradeSynergyTag ownedTags,
            UpgradeSynergyTag suppressedTags)
        {
            if (data == null)
                return 0f;

            float weight = Mathf.Max(0.001f, data.DraftWeight) + Mathf.Max(0, ownedLevel) * OwnedLevelWeight;
            if ((data.GetEffectiveTags() & ownedTags) != 0)
                weight += SynergyWeightBonus;
            if ((data.GetEffectiveTags() & suppressedTags) != 0)
                weight *= SuppressedWeightMultiplier;

            return Mathf.Max(MinimumWeight, weight);
        }
    }

    [Serializable]
    public sealed class CombatPressureProgressionTuning
    {
        [Header("全ルール共通")]
        [SerializeField, Range(0f, 0.9f)] private float _thresholdReductionPerLevel = 0.15f;

        [Header("コンボ")]
        [SerializeField, Min(1)] private int _comboRecoveryUnlockLevel = 2;
        [SerializeField, Min(0f)] private float _comboRecoverySecondsPerHit = 0.25f;
        [SerializeField, Min(1)] private int _comboEchoUnlockLevel = 3;
        [SerializeField, Min(1)] private int _comboEchoHitInterval = 3;

        [Header("毒累計")]
        [SerializeField, Min(1)] private int _poisonDefeatUnlockLevel = 3;
        [SerializeField, Min(0)] private int _poisonDefeatSpawnCount = 4;

        [Header("凍結累計")]
        [SerializeField, Min(1)] private int _iceBreakFirstLevel = 2;
        [SerializeField, Min(0)] private int _iceBreakFirstCount = 3;
        [SerializeField, Min(1)] private int _iceBreakSecondLevel = 3;
        [SerializeField, Min(0)] private int _iceBreakSecondCount = 6;

        [Header("取得時の上空降下プレビュー")]
        [SerializeField, Min(0)] private int _previewBaseCount = 2;
        [SerializeField, Min(0)] private int _previewLevelCap = 3;

        public void Apply()
        {
            CombatPressureProgression.Configure(
                _thresholdReductionPerLevel,
                _comboRecoveryUnlockLevel,
                _comboRecoverySecondsPerHit,
                _comboEchoUnlockLevel,
                _comboEchoHitInterval,
                _poisonDefeatUnlockLevel,
                _poisonDefeatSpawnCount,
                _iceBreakFirstLevel,
                _iceBreakFirstCount,
                _iceBreakSecondLevel,
                _iceBreakSecondCount,
                _previewBaseCount,
                _previewLevelCap);
        }
    }

    [CreateAssetMenu(fileName = "SO_RoguelikeBalance_Default", menuName = "Game/Roguelike/Balance Config")]
    public sealed class SO_RoguelikeBalanceConfig : ScriptableObject
    {
        public const string DefaultResourcePath = "Roguelike/SO_RoguelikeBalance_Default";

        [Header("Master Data")]
        [SerializeField] private StageDataSO _stageData;
        [SerializeField] private SO_UpgradePool _upgradePool;
        [SerializeField] private CollectibleTable _collectibleTable;
        [SerializeField] private CombatPressureRuleSet _combatPressureRuleSet;

        [Header("Wave Rewards")]
        [SerializeField] private List<WaveRewardDefinition> _waveRewards = new List<WaveRewardDefinition>();

        [Header("Draft")]
        [SerializeField] private RoguelikeDraftTuning _draft = new RoguelikeDraftTuning();

        [Header("Combat Pressure Level Progression")]
        [SerializeField] private CombatPressureProgressionTuning _combatPressureProgression = new CombatPressureProgressionTuning();

        public StageDataSO StageData => _stageData;
        public SO_UpgradePool UpgradePool => _upgradePool;
        public CollectibleTable CollectibleTable => _collectibleTable;
        public CombatPressureRuleSet CombatPressureRuleSet => _combatPressureRuleSet;
        public IReadOnlyList<WaveRewardDefinition> WaveRewards => _waveRewards;
        public RoguelikeDraftTuning Draft => _draft;
        public CombatPressureProgressionTuning CombatPressureProgression => _combatPressureProgression;

        public WaveRewardDefinition GetRewardForWave(int clearedWave)
        {
            for (int index = 0; index < _waveRewards.Count; index++)
            {
                WaveRewardDefinition reward = _waveRewards[index];
                if (reward != null && reward.ClearedWave == clearedWave)
                    return reward;
            }

            return null;
        }

        public static SO_RoguelikeBalanceConfig LoadDefault()
            => Resources.Load<SO_RoguelikeBalanceConfig>(DefaultResourcePath);
    }
}
