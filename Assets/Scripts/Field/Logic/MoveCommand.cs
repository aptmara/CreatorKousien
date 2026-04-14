// ------------------------------------------------------------
// File		: MoveCommand.cs
// Summary	: 移動コマンドの定義
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// -
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
    /// 移動先のX座標
    /// </summary>
    public int TargetX;

    /// <summary>
    /// 移動先のY座標
    /// </summary>
    public int TargetY;

    /// <summary>
    /// 移動の種類
    /// </summary>
    public MoveType Type;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="actorId">アクターID</param>
    /// <param name="targetX">移動先のX座標</param>
    /// <param name="targetY">移動先のY座標</param>
    /// <param name="type">移動の種類</param>
    public MoveCommand(int actorId, int targetX, int targetY, MoveType type = MoveType.Walk)
    {
        ActorId = actorId;      /// アクターIDを設定
        TargetX = targetX;      /// 移動先のX座標を設定
        TargetY = targetY;      /// 移動先のY座標を設定
        Type = type;            /// 移動の種類を設定
    }
}
