// ================================================================================
// File         : SO_CollectibleFlavorNames.cs
// Description  : ショップ表示専用のアイテム名(フレーバーネーム)対応表。
//                CollectibleTable.GetDisplayName（戦闘中UI等で使用）とは独立させ、
//                ショップの「商品名」だけを自由に変更できるようにする。
// Created      : 2026-09-07
// ================================================================================
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Collectibles
{
    [CreateAssetMenu(fileName = "SO_CollectibleFlavorNames", menuName = "Game/Collectible/Flavor Names")]
    public class SO_CollectibleFlavorNames : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public CollectibleType Type;
            [Tooltip("ショップ上での商品名")]
            public string FlavorName;
        }

        [SerializeField]
        private List<Entry> _entries = new List<Entry>
        {
            new Entry { Type = CollectibleType.Candy, FlavorName = "キャンディ" },
            new Entry { Type = CollectibleType.Toge, FlavorName = "コンペートー" },
            new Entry { Type = CollectibleType.Poison, FlavorName = "ドクリンゴキャンディ" },
            new Entry { Type = CollectibleType.Ice, FlavorName = "ツメタイキャンディ" },
            new Entry { Type = CollectibleType.Gummy, FlavorName = "グミ" },
            new Entry { Type = CollectibleType.Cross, FlavorName = "オカルトキャンディ" },
        };

        public IReadOnlyList<Entry> Entries => _entries;

        public string GetFlavorName(CollectibleType type)
        {
            foreach (Entry entry in _entries)
            {
                if (entry.Type == type)
                    return entry.FlavorName;
            }

            return CollectibleTable.GetDisplayName(type);
        }

        public void SetFlavorName(CollectibleType type, string flavorName)
        {
            for (int index = 0; index < _entries.Count; index++)
            {
                if (_entries[index].Type != type)
                    continue;

                Entry entry = _entries[index];
                entry.FlavorName = flavorName;
                _entries[index] = entry;
                return;
            }

            _entries.Add(new Entry { Type = type, FlavorName = flavorName });
        }
    }
}
