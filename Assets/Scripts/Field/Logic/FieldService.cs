// ------------------------------------------------------------
// File		: FieldService.cs
// Summary	: 盤面の状態を更新するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-12
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 盤面の座標計算、移動可否判定、占有更新などのロジックを担当するクラス
/// </summary>
public class FieldService
{
    private FieldState _fieldState; /// 盤面の状態

    /// <summary>
    /// 現在の盤面状態へのアクセスプロパティ
    /// </summary>
    public FieldState State => _fieldState;


    /// <summary>
    /// ステージデータを元に盤面を初期化し、障害物などを配置
    /// </summary>
    /// <param name="stageData">盤面サイズや初期障害物情報を含む静的データ</param>
    public void Initialize(StageData stageData)
    {
        // ----- 1. 盤面サイズの決定と作成 -----
        _fieldState = new FieldState(stageData.Width, stageData.Height);

        // ----- 2. 障害物の配置 -----
        if (stageData.ObstaclePositions != null)
        {
            // 障害物の配置情報がある場合、盤面に反映
            foreach (var pos in stageData.ObstaclePositions)
            {
                var cell = _fieldState.GetCell(pos.x, pos.y);
                if (cell != null)
                {
                    cell.IsPassable = false; // 障害物は通行不可
                }
            }
        }
    }


    /// <summary>
    /// 指定した座標に移動可能かどうかを判定するロジック
    /// </summary>
    /// <param name="targetX">目的地のX座標</param>
    /// <param name="targetY">目的地のY座標</param>
    /// <returns>移動可能な場合は true, それ以外は false</returns>
    public bool CanMoveTo(int targetX, int targetY)
    {
        // ----- 1. 盤面外判定 -----
        if (_fieldState.IsOutOfBounds(targetX, targetY))
        {
            return false;   // 盤面外は移動不可
        }

        var cell = _fieldState.GetCell(targetX, targetY);

        // ----- 2. 通行可否判定 -----
        if (!cell.IsPassable)
        {
            return false;   // 障害物があるセルは移動不可
        }

        // ----- 3. 占有物判定 -----
        if (cell.OccupierId != -1)
        {
            return false;   // 他のオブジェクトが占有しているセルは移動不可
        }

        return true;
    }


    /// <summary>
    /// キャラクターの占有位置を更新。移動が確定した際に呼び出し。
    /// </summary>
    /// <param name="moverId">移動するキャラクターのID</param>
    /// <param name="fromX">  移動元のX座標</param>
    /// <param name="fromY">  移動元のY座標</param>
    /// <param name="toX">    移動先のX座標</param>
    /// <param name="toY">    移動先のY座標</param>
    /// <returns>更新が成功した場合はtrue、それ以外はfalse</returns>
    public bool UpdateOccupancy(int moverId, int fromX, int fromY, int toX, int toY)
    {
        // ----- 1. 移動できるかの判定 -----
        if (!CanMoveTo(toX, toY))
        {
            return false;               // 移動不可なら更新しない
        }

        // ----- 2. 占有状態の解除 -----
        var fromCell = _fieldState.GetCell(fromX, fromY);
        if (fromCell != null && fromCell.OccupierId == moverId)
        {
            fromCell.OccupierId = -1;   // 移動元の占有を解除
        }

        // ----- 3. 占有状態の設定 -----
        var toCell = _fieldState.GetCell(toX, toY);
        toCell.OccupierId = moverId;    // 移動先を占有

        return true;                    // 更新成功
    }
}
