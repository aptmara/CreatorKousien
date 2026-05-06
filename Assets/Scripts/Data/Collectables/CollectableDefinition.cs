// ================================================================================
// File         : CollectableDifinition.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;

namespace Game.Data.Collectables
{
    /// <summary>
    /// アイテム1種類あたりのマスターデータとして機能するScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Collectable_New", menuName = "Game/Collectable Definition")]
    public class CollectableDefinition : ScriptableObject
    {
        [Header("Basic Infomation")]
        [Tooltip("データの一意な識別子")]
        public string Id;

        [Tooltip("UI等で表示する際の名前")]
        public string DisplayName;

        [Tooltip("アイテムのカテゴリ")]
        public CollectableCategory Category;

        [Tooltip("アイテムの属性")]
        public CollectableElement Element;

        [Header("Prefab")]
        [Tooltip("フィールド上に配置される際の実体Prefab")]
        public GameObject FieldPrefab;

        [Tooltip("落下攻撃として解放される際の演出用Prefab")]
        public GameObject ReleaseVisualPrefab;

        [Header("Parameters")]
        [Tooltip("敵本体へ与える基礎ダメージ")]
        public float BasePower = 1.0f;

        [Tooltip("敵の攻撃ゲージを削る基礎ダメージ")]
        public float BaseGaugeDamage = 1.0f;

        [Tooltip("アイテムの重さ")]
        public float Weight = 1.0f;

        [Tooltip("CollectionBufferをいくつ消費するか")]
        public int BufferCost = 1;

        [Header("Fusion Rules")]
        [Tooltip("合体の素材として使用可能かどうか")]
        public bool CanFuse = true;
    }
}
