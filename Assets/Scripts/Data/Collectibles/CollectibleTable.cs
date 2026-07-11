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
        /// 独立確率判定を用いたアイテム抽選ロジック
        /// </summary>
        /// <returns>抽選されたCollectible Data</returns>
        public CollectibleData DetermineItem()
        {
            // 1. 特殊アイテムを1つずつ確率判定していく
            if (_specialItems != null)
            {
                foreach (var entry in _specialItems)
                {
                    if (entry.Data == null) continue;

                    // 0.0 ~ 100.0 の範囲でランダム値を生成
                    float randomValue = UnityEngine.Random.Range(0.0f, 100.0f);
                    if (randomValue <= entry.DropChancePercent)
                    {
                        // 確率判定に成功した場合、そのアイテムを返す
                        return entry.Data;
                    }
                }
            }

            // 2. 特殊アイテムが出なかった場合、ベースアイテムからランダムに選ぶ
            if (_baseItems != null && _baseItems.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, _baseItems.Length);
                return _baseItems[randomIndex];
            }

            // 3. アイテムが存在しない場合は null を返す
            return null;
        }
    }
}
