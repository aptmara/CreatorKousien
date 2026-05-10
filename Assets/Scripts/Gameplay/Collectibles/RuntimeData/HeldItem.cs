// ================================================================================
// File         : HeldItem.cs
// Author       : Iwai Shogo
//
// Description  : プレイヤーが保持しているアイテムの軽量データ(Runtime Data)。
// Created      : 2026-05-06
// ================================================================================

using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// 物理オブジェクトを破棄した後にメモリ上で保持し続ける軽量データ。
    /// GameObjectへの参照は持たず、属性・形状・重さなどの保持中状態の正本となります。
    /// </summary>
    public class HeldItem
    {
        public string Id { get; private set; }
        public float Weight { get; set; }
        public float DamageAmount { get; set; }
        public string Attribute { get; set; }
        public string Shape { get; set; }
        public int CapacityCost { get; set; }

        public GameObject VisualPrefab { get; private set; }

        public CollectibleObject OriginalInstance { get; private set; }

        [Tooltip("ShapeChangeSystemで評価される転がし距離")]
        public float RollingDistance { get; set; }

        /// <summary>
        /// マスターデータから保持用データを生成します。
        /// </summary>
        public HeldItem(CollectibleData data, CollectibleObject instance)
        {
            if (data == null) return;

            Id = data.Id;
            Weight = data.Weight;
            DamageAmount = data.DamageAmount;
            Attribute = data.Attribute;
            Shape = data.Shape;
            CapacityCost = data.CapacityCost;
            VisualPrefab = data.ViewPrefab;
            RollingDistance = 0f;

            OriginalInstance = instance;
        }
    }
}
