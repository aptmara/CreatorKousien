// ================================================================================
// File         : CollectibleData.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;

namespace Game.Data.Collectibles
{
    /// <summary>
    /// アイテム1種類あたりのマスターデータとして機能するScriptableObject。
    /// RuntimeでこのAsset自体を書き換えることは禁止されており、状態はHeldItemへコピーして使用します。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Collectible_New", menuName = "Game/Collectible Data")]
    public class CollectibleData : ScriptableObject
    {
        [Header("Basic Infomation")]
        [Tooltip("データの一意な識別子")]
        public string Id;

        [Tooltip("表示用Prefab参照")]
        public GameObject ViewPrefab;

        [Header("Parameters")]
        [Tooltip("アイテムの重さ")]
        public float Weight = 1.0f;

        [Tooltip("敵へ与えるダメージ量")]
        public float DamageAmount = 1.0f;

        [Tooltip("アイテムの属性")]
        public string Attribute = "None";

        [Tooltip("アイテムの形状")]
        public string Shape = "Default";

        [Tooltip("CollectionBufferをいくつ消費するか")]
        public int CapacityCost = 1;
    }
}
