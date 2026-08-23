// ================================================================================
// File         : CollectibleTable.cs
// Author       : Iwai Shogo
//
// Description  : 
// Created      : 2026-07-11
// ================================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using Game.Core.Roguelike;
using Game.Gameplay.Roguelike.CombatPressure;

namespace Game.Data.Collectibles
{
    [CreateAssetMenu(fileName = "CollectibleTable", menuName = "Game/Collectible/Collectible Table")]
    public class CollectibleTable : ScriptableObject
    {
        [Serializable]
        public struct SpecialItemEntry
        {
            [Tooltip("特殊アイテムのデータ")]
            public CollectibleData Data;

            [Tooltip("出現確立 (%)")]
            public float DropChancePercent;
        }

        [Header("--- ベースアイテム ---")]
        [Tooltip("特殊アイテムの確率抽選に全て漏れた場合、ここからランダムに選ばれる (キャンディ等)")]
        [SerializeField] private CollectibleData[] _baseItems;

        [Header("--- 特殊アイテムの確率設定 ---")]
        [Tooltip("一定確率で出したい特殊アイテムの一覧と確率")]
        [SerializeField] private List<SpecialItemEntry> _specialItems;

        /// <summary>
        /// 全生成経路と同じ出現ウェイト補正を用いたアイテム抽選ロジック
        /// </summary>
        /// <returns>抽選されたCollectible Data</returns>
        public CollectibleData DetermineItem()
        {
            List<WeightedItem> items = BuildWeightedItems();
            float totalWeight = 0f;
            foreach (WeightedItem item in items)
                totalWeight += item.Weight;

            if (totalWeight <= 0f)
                return null;

            float selectedWeight = UnityEngine.Random.value * totalWeight;
            foreach (WeightedItem item in items)
            {
                selectedWeight -= item.Weight;
                if (selectedWeight <= 0f)
                    return item.Data;
            }

            return items.Count > 0 ? items[items.Count - 1].Data : null;
        }

        public float GetEffectiveDropPercent(CollectibleType type)
        {
            List<WeightedItem> items = BuildWeightedItems();
            float totalWeight = 0f;
            float targetWeight = 0f;
            foreach (WeightedItem item in items)
            {
                totalWeight += item.Weight;
                if (item.Data.Type == type)
                    targetWeight += item.Weight;
            }

            return totalWeight > 0f ? targetWeight / totalWeight * 100f : 0f;
        }

        public List<CollectibleData> GetAllItems()
        {
            var items = new List<CollectibleData>();
            var addedTypes = new HashSet<CollectibleType>();

            if (_baseItems != null)
            {
                foreach (CollectibleData item in _baseItems)
                    AddUnique(item, items, addedTypes);
            }

            if (_specialItems != null)
            {
                foreach (SpecialItemEntry entry in _specialItems)
                    AddUnique(entry.Data, items, addedTypes);
            }

            items.Sort((left, right) => left.Type.CompareTo(right.Type));
            return items;
        }

        public CollectibleData GetByType(CollectibleType type)
        {
            return GetAllItems().Find(item => item.Type == type);
        }

        public static string GetDisplayName(CollectibleType type)
        {
            return type switch
            {
                CollectibleType.Candy => "キャンディ",
                CollectibleType.Toge => "とげ玉",
                CollectibleType.Poison => "毒キノコ",
                CollectibleType.Ice => "氷",
                CollectibleType.Cross => "十字架",
                CollectibleType.Gummy => "グミ",
                _ => type.ToString(),
            };
        }

        private List<WeightedItem> BuildWeightedItems()
        {
            var items = new List<WeightedItem>();
            var addedTypes = new HashSet<CollectibleType>();
            var unlockedBaseItems = new List<CollectibleData>();
            float specialWeightBudget = 0f;

            if (_specialItems != null)
            {
                var countedSpecialTypes = new HashSet<CollectibleType>();
                foreach (SpecialItemEntry entry in _specialItems)
                {
                    if (!IsUnlocked(entry.Data) || !countedSpecialTypes.Add(entry.Data.Type)) continue;
                    specialWeightBudget += Mathf.Max(0f, entry.DropChancePercent);
                }
            }

            if (_baseItems != null)
            {
                foreach (CollectibleData data in _baseItems)
                {
                    if (!IsUnlocked(data) || !addedTypes.Add(data.Type)) continue;
                    unlockedBaseItems.Add(data);
                }
            }

            float baseBudget = Mathf.Max(0.01f, 100f - specialWeightBudget);
            float baseWeight = unlockedBaseItems.Count > 0 ? baseBudget / unlockedBaseItems.Count : 0f;
            foreach (CollectibleData data in unlockedBaseItems)
            {
                items.Add(new WeightedItem(
                    data,
                    baseWeight * CombatPressureSpawnWeights.GetWeight(data.Type)));
            }

            if (_specialItems != null)
            {
                foreach (SpecialItemEntry entry in _specialItems)
                {
                    if (!IsUnlocked(entry.Data) || !addedTypes.Add(entry.Data.Type)) continue;
                    float specialBaseWeight = Mathf.Max(0.01f, entry.DropChancePercent);
                    items.Add(new WeightedItem(
                        entry.Data,
                        specialBaseWeight * CombatPressureSpawnWeights.GetWeight(entry.Data.Type)));
                }
            }

            return items;
        }

        private readonly struct WeightedItem
        {
            public readonly CollectibleData Data;
            public readonly float Weight;

            public WeightedItem(CollectibleData data, float weight)
            {
                Data = data;
                Weight = weight;
            }
        }

        private static void AddUnique(
            CollectibleData item,
            ICollection<CollectibleData> items,
            ISet<CollectibleType> addedTypes)
        {
            if (item != null && addedTypes.Add(item.Type))
                items.Add(item);
        }

        private static bool IsUnlocked(CollectibleData item)
        {
            return item != null && RoguelikeUpgradeRuntime.IsCollectibleUnlocked((int)item.Type);
        }
    }
}
