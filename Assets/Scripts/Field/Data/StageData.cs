// ------------------------------------------------------------
// File		: StageData.cs
// Summary	: ステージのデータを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-12
//
// Notes	:
// - 仕様により変更、追加される可能性があります。
// ------------------------------------------------------------
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_StageData", menuName = "CreatorKousien/Data/StageData")]
public class StageData : ScriptableObject
{
    [Header("フィールドサイズ")]
    [Tooltip("フィールドの幅")]
    [SerializeField] private int _width = 3;    // フィールドの幅

    [Tooltip("フィールドの高さ")]
    [SerializeField] private int _height = 3;   // フィールドの高さ


    /// <summary>
    /// フィールドの幅
    /// </summary>
    public int Width => _width;

    /// <summary>
    /// フィールドの高さ
    /// </summary>
    public int Height => _height;


    [Header("初期配置")]
    [Tooltip("障害物の初期配置")]
    [SerializeField] private Vector2Int[] _obstaclePositions; // 障害物の初期配置

    /// <summary>
    /// 障害物の初期配置を取得
    /// </summary>
    public Vector2Int[] ObstaclePositions => _obstaclePositions;

    [Tooltip("プレイヤーの初期座標")]
    [SerializeField] private Vector2Int _playerStartPosition; // プレイヤーの初期座標

    /// <summary>
    /// プレイヤーの初期座標を取得
    /// </summary>
    public Vector2Int PlayerStartPosition => _playerStartPosition;


    [Header("タイルセッティング")]
    [Tooltip("タイルの配置情報")]
    [SerializeField] private TileTypeDefinition _defaultTile; // タイルの配置情報

    /// <summary>
    /// タイルの配置情報を取得
    /// </summary>
    public TileTypeDefinition DefaultTile => _defaultTile;


    [Tooltip("特殊タイルのスポーンルール")]
    [SerializeField] private SpecialTileSpawnRule[] _specialTileRules;

    /// <summary>
    /// 特殊タイルのスポーンルールを取得
    /// </summary>
    public SpecialTileSpawnRule[] SpecialTileRules => _specialTileRules;


    [Serializable]
    public class SpecialTileSpawnRule
    {
        public TileTypeDefinition SpecialTile;      /// スポーンさせる特殊タイルの種類
        [Range(0,1)] public float SpawnProbability; /// 0から1の範囲で、特殊タイルがスポーンする確率
        public int SpawnCount;                      /// ステージ開始時にスポーンさせる特殊タイルの数
    }
}
