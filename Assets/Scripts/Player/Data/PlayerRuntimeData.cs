// ------------------------------------------------------------
// File		: PlayerRuntimeData.cs
// Summary	: プレイヤーの実行時データを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// - 設計に応じて随時更新
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// プレイヤーの実行時状態を保持する純粋なデータクラス
/// </summary>
public class PlayerRuntimeData
{
    /// <summary>
    /// アクタ―のID（ユニークな識別子）
    /// </summary>
    public int ActorId { get; private set; }

    /// <summary>
    /// 現状のHP
    /// </summary>
    public int CurrentHp { get; set; }

    /// <summary>
    /// 最大HP
    /// </summary>
    public int MaxHp { get; private set; }

    /// <summary>
    /// 座標位置（フィールド上の位置を表す）
    /// </summary>
    public Vector2Int Position { get; set; }


    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="actorId">アクタ―のID</param>
    /// <param name="maxHp">最大HP</param>
    /// <param name="initialPosition">初期位置</param>
    public PlayerRuntimeData(int actorId, int maxHp, Vector2Int initialPosition)
    {
        ActorId = actorId;
        MaxHp = maxHp;
        CurrentHp = maxHp;              // 初期HPは最大HPと同じ
        Position = initialPosition;
    }
}
