// ------------------------------------------------------------
// File		: MoveCommand.cs
// Summary	: 移動コマンドの定義
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// - 設計書に基づき、絶対座標から移動量に変換 (4/15)
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 移動の性質や種類を定義する列挙型
/// </summary>
public enum MoveType
{
    Walk,       // 通常の歩行移動
    Knockback,  // ノックバック移動
    Warp,       // ワープ移動
}


/// <summary>
/// EffectManagerからFieldServiceへ渡される移動命令のパッケージ
/// </summary>
public struct MoveCommand
{
    /// <summary>
    /// 移動対象のアクターID
    /// </summary>
    public int ActorId;


    /// <summary>
    /// 移動量 (X, Yの相対的な変化量)
    /// </summary>
    public Vector2Int Delta;

    /// <summary>
    /// 移動の種類
    /// </summary>
    public MoveType Type;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="actorId">アクターID</param>
    /// <param name="delta">移動量 (右へ１マスなら new Vector2Int(1, 0))</param>
    /// <param name="type">移動の種類</param>
    public MoveCommand(int actorId, Vector2Int delta, MoveType type = MoveType.Walk)
    {
        ActorId = actorId;      /// アクターIDを設定
        Delta = delta;
        Type = type;            /// 移動の種類を設定
    }
}
