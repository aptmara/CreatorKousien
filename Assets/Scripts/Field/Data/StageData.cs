// ------------------------------------------------------------
// File		: StageData.cs
// Summary	: ステージのデータを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-12
//
// Notes	:
// - 仕様により変更、追加される可能性があります。
// - エディタ拡張のために障害物をListに変更 (4/15)
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Field;

namespace CreatorKousien.Data
{
    [CreateAssetMenu(fileName = "SO_StageData", menuName = "CreatorKousien/Data/StageData")]
    public class StageData : ScriptableObject
    {
        [Header("フィールドサイズ")]
        [Tooltip("フィールドの幅")]
        [SerializeField] private int _width = 6;        // フィールドの幅

        [Tooltip("フィールドの高さ")]
        [SerializeField] private int _height = 3;       // フィールドの高さ

        [Tooltip("自陣と敵陣の境界線。このX座標未満が自陣になる")]
        [SerializeField] private int _borderX = 3;      // 自陣と敵陣の境界線

        [Tooltip("1セルのサイズ")]
        public float CellSize = 1.0f;

        /// <summary>
        /// フィールドの幅
        /// </summary>
        public int Width => _width;

        /// <summary>
        /// フィールドの高さ
        /// </summary>
        public int Height => _height;

        /// <summary>
        /// ボードのX座標の境界線。このX座標未満が自陣になる
        /// </summary>
        public int BorderX => _borderX;


        [Header("境界線のビジュアル設定")]
        [Tooltip("自陣と敵陣の物理的な隙間の広さ！")]
        public float BorderGap = 0.5f;                  // 自陣と敵陣の物理的な隙間の広さ

        [Tooltip("境界線の隙間に置くオブジェクト")]
        public GameObject BorderPrefab;                 // 境界線の隙間に置くオブジェクト


        [Header("初期配置")]
        [Tooltip("障害物の初期配置")]
        public List<Vector2Int> ObstaclePositions = new List<Vector2Int>(); // 障害物の初期配置

        [Tooltip("プレイヤーの初期座標")]
        [SerializeField] private Vector2Int _playerStartPosition;           // プレイヤーの初期座標

        /// <summary>
        /// プレイヤーの初期座標を取得
        /// </summary>
        public Vector2Int PlayerStartPosition => _playerStartPosition;


        [Header("タイルセッティング")]
        [Tooltip("自陣のデフォルト床")]
        public TileTypeDefinition PlayerDefaultTile;   // 自陣のデフォルト床
        [Tooltip("敵陣のデフォルト床")]
        public TileTypeDefinition EnemyDefaultTile;    // 敵陣のデフォルト床

        [Header("ランダム生成ルール(自陣)")]
        public SpecialTileSpawnRule[] PlayerSpecialTileRules;               // 自陣の特殊タイルのスポーンルール


        [Header("ランダム生成ルール(敵陣)")]
        public SpecialTileSpawnRule[] EnemySpecialTileRules;                // 敵陣の特殊タイルのスポーンルール


        [Serializable]
        public class SpecialTileSpawnRule
        {
            [Header("スポーンする特殊タイルの情報")]
            [Tooltip("スポーンさせる特殊タイルの種類")]
            public TileTypeDefinition SpecialTile;      /// スポーンさせる特殊タイルの種類
            [Tooltip("0から1の範囲で、特殊タイルがスポーンする確率")]
            [Range(0,1)] public float SpawnProbability; /// 0から1の範囲で、特殊タイルがスポーンする確率

            [Header("生成数(Min - Max)")]
            [Tooltip("スポーンさせる特殊タイルの最小数")]
            public int MinSpawnCount = 1;               /// スポーンさせる特殊タイルの最小数
            [Tooltip("スポーンさせる特殊タイルの最大数")]
            public int MaxSpawnCount = 1;               /// スポーンさせる特殊タイルの最大数

            [Header("ターンのルール")]
            [Tooltip("何ターン生き残るか(0ターンは永続床)")]
            public int LifespanTurns = 1;               /// 何ターン生き残るか(0ターンは永続床)

            [Tooltip("何ターンごとに生成の抽選を行うか(1ターンごとなら1、2ターンごとなら2)")]
            public int SpawnIntervalTurns = 1;          /// 何ターンごとに生成の抽選を行うか(1ターンごとなら1、2ターンごとなら2)
        }
    }
}

