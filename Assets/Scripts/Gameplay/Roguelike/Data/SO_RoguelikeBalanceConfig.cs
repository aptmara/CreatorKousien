using System;
using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.WaveSystem;
using UnityEngine;

namespace Game.Gameplay.Roguelike
{
    public enum WaveRewardKind
    {
        Standard,
        None,
    }

    [Serializable]
    public sealed class WaveRewardDefinition
    {
        [SerializeField, Min(1)] private int _clearedWave = 1;
        [SerializeField] private WaveRewardKind _rewardKind = WaveRewardKind.Standard;
        [SerializeField, Min(1)] private int _candidateCount = 3;

        public int ClearedWave => _clearedWave;
        public WaveRewardKind RewardKind => _rewardKind;
        public int CandidateCount => Mathf.Max(1, _candidateCount);
    }

    [CreateAssetMenu(fileName = "SO_RoguelikeBalance_Default", menuName = "Game/Roguelike/Balance Config")]
    public sealed class SO_RoguelikeBalanceConfig : ScriptableObject
    {
        public const string DefaultResourcePath = "Roguelike/SO_RoguelikeBalance_Default";

        [Header("Master Data")]
        [SerializeField] private StageDataSO _stageData;
        [SerializeField] private SO_UpgradePool _upgradePool;
        [SerializeField] private CollectibleTable _collectibleTable;

        [Header("Wave Rewards")]
        [SerializeField] private List<WaveRewardDefinition> _waveRewards = new List<WaveRewardDefinition>();

        public StageDataSO StageData => _stageData;
        public SO_UpgradePool UpgradePool => _upgradePool;
        public CollectibleTable CollectibleTable => _collectibleTable;
        public IReadOnlyList<WaveRewardDefinition> WaveRewards => _waveRewards;

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
