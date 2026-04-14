// ------------------------------------------------------------
// File		: PlayerSystem.cs
// Summary	: プレイヤーのシステムクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// - 設計に応じて随時更新
// ------------------------------------------------------------
using UnityEngine;
using System;

public class PlayerSystem
{
    /// <summary>
    /// プレイヤーの実行時のデータ情報
    /// </summary>
    public PlayerRuntimeData RuntimeData { get; private set; }

    // ----- Viewへ状態変化を伝えるためのイベント -----
    /// <summary>
    /// 座標位置が変化したときのイベント
    /// </summary>
    public event Action<Vector2Int> OnPositionChanged;

    /// <summary>
    /// 死んだときのイベント
    /// </summary>
    public event Action OnDeathEvent;

    private const int PLAYER_ACTOR_ID = 1;          /// プレイヤーのアクタ―ID（固定値）

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="playerData">プレイヤーの基本データ</param>
    public void Initialize(PlayerData playerData, Vector2Int startPosition)
    {
        RuntimeData = new PlayerRuntimeData(PLAYER_ACTOR_ID, playerData.MaxHp, startPosition);
    }


    /// <summary>
    /// 移動処理
    /// </summary>
    /// <param name="newX">移動先のX座標</param>
    /// <param name="newY">移動先のY座標</param>
    public void SyncPosition(Vector2Int newPosition)
    {
        RuntimeData.Position = newPosition;
        OnPositionChanged?.Invoke(newPosition); // Viewへ伝達
    }


    /// <summary>
    /// HPを増減させる処理
    /// </summary>
    /// <param name="amount">増減値</param>
    public void ChangeHp(int amount)
    {
        RuntimeData.CurrentHp = Mathf.Clamp(RuntimeData.CurrentHp + amount, 0, RuntimeData.MaxHp);
    }


    /// <summary>
    /// 死んだときに呼び出される処理
    /// </summary>
    public void OnDeath()
    {
        Debug.Log("[Player System] 死亡処理が呼び出されました.");
        OnDeathEvent?.Invoke(); // Viewへ伝達
    }
}
