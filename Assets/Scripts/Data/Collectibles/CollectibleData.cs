// ================================================================================
// File         : CollectibleData.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// Updated      : 2026-07-06 (6種類のアイテムの実装)
// ================================================================================

using UnityEngine;

namespace Game.Data.Collectibles
{
    /// <summary>
    /// アイテムのタイプ
    /// </summary>
    public enum CollectibleType
    {
        Candy,  // キャンディ
        Toge,   // とげ玉
        Poison, // 毒キノコ
        Ice,    // 氷
        Cross,  // 十字架
        Gummy,  // グミ
        BossWeak,
    }

    /// <summary>
    /// アイテム1種類あたりのマスターデータとして機能するScriptableObject。
    /// RuntimeでこのAsset自体を書き換えることは禁止されており、状態はHeldItemへコピーして使用します。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Collectible_New", menuName = "Game/Collectible/Collectible Data")]
    public class CollectibleData : ScriptableObject
    {
        [Header("--- Basic Infomation ---")]
        [Tooltip("データの一意な識別子")]
        public string Id;

        [Tooltip("アイテムのタイプ")]
        public CollectibleType Type;

        [Tooltip("表示用Prefab参照")]
        public GameObject ViewPrefab;


        [Header("--- Parameters ---")]
        [Tooltip("アイテムの重さ")]
        public float Weight = 1.0f;

        [Tooltip("敵へ与える基礎ダメージ")]
        public float DamageAmount = 1.0f;

        [Tooltip("アイテムの属性")]
        public string Attribute = "None";

        [Tooltip("アイテムの形状")]
        public string Shape = "Default";

        [Tooltip("CollectionBufferをいくつ消費するか")]
        public int CapacityCost = 1;


        [Header("--- Hit Cooldown Settings ---")]
        [Tooltip("このアイテム種別特有の連続ヒットクールダウン（グミ等は短く設定）")]
        public float SameItemCooldown = 0.25f;

        [Header("VFX")]
        public CollectibleHitVfxPattern HitPattern;

        [Header("--- とげ玉専用 ---")]
        public float BarrierDamageAmount = 3.0f;

        [Header("--- 毒キノコ専用 ---")]
        public float PoisonDuration = 5.0f;
        public float PoisonMinDamage = 1.0f;
        public float PoisonMaxDamage = 10.0f;

        [Header("--- 氷専用 ---")]
        [Range(0f, 100f)]
        public float FreezeProbability = 5.0f;  // 凍結確率 (%)
        public float FreezeDuration = 5.0f;
        public int FreezeHitDurability = 20;    // 凍結耐久ヒット数
        public float FreezeBreakDamage = 30.0f; // 凍結破壊ダメージ

        [Header("--- 十字架専用 ---")]
        public GameObject LaserPrefab;  // 発射するレーザーのPrefab

        [Header("--- グミ専用 ---")]
        [Tooltip("グミ用の物理材質")]
        public PhysicsMaterial GummyPhysicsMaterial;
        public int MaxBounceChainCount = 10;

        [Header("=== 天秤弱点専用 ===")]
        [Tooltip("天秤弱点用の効果時間")]
        public float WeakenDuration = 3.0f;
    }
}
