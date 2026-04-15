// ------------------------------------------------------------
// File		: TileTypeDefinition.cs
// Summary	: タイルの種類の定義クラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;
/// <summary>
/// 床の静的な定義データ
/// </summary>
[CreateAssetMenu(fileName = "SO_Tile_", menuName = "CreatorKousien/Data/TileTypeDefinition")]
public class TileTypeDefinition : ScriptableObject
{
    [Header("基本情報")]
    [Tooltip("タイルのID。ユニークな値を設定してください")]
    public int TileId;
    [Tooltip("タイルの表示名")]
    public string TileName;
    [Tooltip("この床の上にキャラクターが立てるかどうか")]
    public bool CanStand = true;


    [Header("タイルの効果")]
    [Tooltip("タイルの発動条件とその効果")]
    public TileEffectProfile EffectProfile;


    [Header("タイルの見た目")]
    [Tooltip("タイルのモデルPrefab")]
    public GameObject ModelPrefab;
    [Tooltip("タイルのエフェクトPrefab")]
    public GameObject EffectPrefab;
}
