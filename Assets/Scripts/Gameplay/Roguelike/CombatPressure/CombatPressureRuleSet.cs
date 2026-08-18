using System;
using System.Collections.Generic;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Roguelike.CombatPressure
{
    public enum CombatPressureSource
    {
        Combo,
        Status,
    }

    public enum CombatPressureMetric
    {
        ComboCount,
        AffectedEnemyCount,
        TotalStackCount,
    }

    [CreateAssetMenu(fileName = "SO_CombatPressureRuleSet", menuName = "Game/Roguelike/Combat Pressure Rule Set")]
    public sealed class CombatPressureRuleSet : ScriptableObject
    {
        [SerializeField] private CollectibleTable _collectibleTable;
        [SerializeField] private List<CombatPressureRule> _rules = new List<CombatPressureRule>();

        public IReadOnlyList<CombatPressureRule> Rules => _rules;

        public CollectibleData GetCollectible(CollectibleType type)
            => _collectibleTable != null ? _collectibleTable.GetByType(type) : null;

        public IReadOnlyList<string> ValidateRules()
        {
            var messages = new List<string>();
            var ids = new HashSet<string>();

            if (_collectibleTable == null)
                messages.Add("Collectible Tableが未設定です。");

            for (int i = 0; i < _rules.Count; i++)
            {
                CombatPressureRule rule = _rules[i];
                if (rule == null)
                {
                    messages.Add($"ルール {i + 1}: null です。");
                    continue;
                }

                if (rule.Threshold <= 0)
                    messages.Add($"{rule.DisplayName}: 閾値は1以上にしてください。");
                if (rule.Source == CombatPressureSource.Status && string.IsNullOrWhiteSpace(rule.StatusType))
                    messages.Add($"{rule.DisplayName}: 状態異常名が未設定です。");
                if (rule.SpawnCount > 0 && rule.FocusedCollectible == null)
                    messages.Add($"{rule.DisplayName}: 生成数があるため生成先モデルを設定してください。");
                if (rule.SpawnInterval < 0f || rule.BuffDuration < 0f)
                    messages.Add($"{rule.DisplayName}: 時間は0以上にしてください。");
                if (!ids.Add(rule.Id))
                    messages.Add($"{rule.DisplayName}: ルールIDが重複しています。");
            }

            return messages;
        }
    }

    [Serializable]
    public sealed class CombatPressureRule
    {
        [SerializeField] private string _id = Guid.Empty.ToString();
        [SerializeField] private string _displayName = "新しいルール";
        [SerializeField] private bool _enabled = true;
        [SerializeField] private CombatPressureSource _source = CombatPressureSource.Combo;
        [SerializeField] private string _statusType = "Poison";
        [SerializeField] private CombatPressureMetric _metric = CombatPressureMetric.ComboCount;
        [SerializeField, Min(1)] private int _threshold = 25;

        [Header("生成（ラン中に選択されなかった場合の既定値）")]
        [SerializeField] private CollectibleData _focusedCollectible;
        [SerializeField, Min(0)] private int _spawnCount = 1;
        [SerializeField, Min(0f)] private float _spawnInterval;

        [Header("一時バフ（条件を満たしている間有効）")]
        [SerializeField, Min(1f)] private float _moveSpeedMultiplier = 1f;
        [SerializeField, Min(1f)] private float _attachmentScaleMultiplier = 1f;
        [SerializeField, Min(0f)] private float _buffDuration;

        [Header("通常湧き補正（条件を満たしている間有効）")]
        [SerializeField] private CollectibleType _weightedCollectibleType;
        [SerializeField, Min(1f)] private float _normalSpawnWeightMultiplier = 1f;

        public string Id => _id;
        public string DisplayName => string.IsNullOrWhiteSpace(_displayName) ? _id : _displayName;
        public bool Enabled => _enabled;
        public CombatPressureSource Source => _source;
        public string StatusType => _statusType;
        public CombatPressureMetric Metric => _metric;
        public int Threshold => _threshold;
        public CollectibleData FocusedCollectible => _focusedCollectible;
        public int SpawnCount => _spawnCount;
        public float SpawnInterval => _spawnInterval;
        public float MoveSpeedMultiplier => _moveSpeedMultiplier;
        public float AttachmentScaleMultiplier => _attachmentScaleMultiplier;
        public float BuffDuration => _buffDuration;
        public CollectibleType WeightedCollectibleType => _weightedCollectibleType;
        public float NormalSpawnWeightMultiplier => _normalSpawnWeightMultiplier;

        public int GetMetricValue(int combo, int affectedEnemies, int totalStacks)
        {
            return _metric switch
            {
                CombatPressureMetric.ComboCount => combo,
                CombatPressureMetric.AffectedEnemyCount => affectedEnemies,
                CombatPressureMetric.TotalStackCount => totalStacks,
                _ => 0,
            };
        }

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id) || _id == Guid.Empty.ToString())
                _id = Guid.NewGuid().ToString("N");
        }
    }
}
