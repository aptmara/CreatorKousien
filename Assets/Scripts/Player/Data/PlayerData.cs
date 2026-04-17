// ------------------------------------------------------------
// File		: PlayerData.cs
// Summary	: プレイヤーの基礎能力の管理クラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// - 随時、プレイヤーの能力を追加していく予定
// ------------------------------------------------------------
using UnityEngine;


/// <summary>
/// Playerの基礎能力を定義するSO
/// </summary>
[CreateAssetMenu(fileName = "SO_PlayerData", menuName = "CreatorKousien/Data/PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("ステータス")]
    [Tooltip("プレイヤーの最大HP")]
    public int MaxHp = 100;
    [Tooltip("プレイヤーの基礎攻撃力")]
    public int BaseAttack = 10;

    [Header("見た目")]
    [Tooltip("フィールド上に生成するプレイヤーの見た目")]
    public GameObject PlayerPrefab;
}
