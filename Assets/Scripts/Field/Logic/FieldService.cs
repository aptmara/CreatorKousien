// ------------------------------------------------------------
// File		: FieldService.cs
// Summary	: 盤面の状態を更新するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-12
//
// Notes	:
// - 特殊床の生成を追加 (4/13)
// - MoveCommandを導入して移動の種類を区別できるように (4/15)
// - Fieldに自陣、敵陣の概念を追加 (4/15)
// ------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;

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

    private Dictionary<int, Vector2Int> _actorPositions = new Dictionary<int, Vector2Int>(); /// キャラクターIDとその位置のマッピング

    /// <summary>
    /// Characterが移動した際に発行されるイベント。引数は「移動したキャラクターのID」「移動先のX座標」「移動先のY座標」
    /// </summary>
    public event Action<int, int, int> OnActorMoved;


    /// <summary>
    /// ステージデータを元に盤面を初期化し、障害物などを配置
    /// </summary>
    /// <param name="stageData">盤面サイズや初期障害物情報を含む静的データ</param>
    public void Initialize(StageData stageData)
    {
        // ----- 1. 盤面サイズの決定と作成 -----
        _fieldState = new FieldState(stageData.Width, stageData.Height);
        _actorPositions.Clear();   // キャラクター位置のマッピングも初期化


        // 重複配置を避けるための「空きマス候補」リスト
        List<Vector2Int> availableCells = new List<Vector2Int>();


        // ----- 2. デフォルト床で全マスを初期化 -----
        for (int x = 0; x < stageData.Width; x++)
        {
            for (int y = 0; y < stageData.Height; y++)
            {
                var cell = _fieldState.GetCell(x, y);
                cell.CurrentTile = stageData.DefaultTile; // デフォルト床を設定

                // デフォルト床が設定されていれば、その通行可否を適用
                if (stageData.DefaultTile != null)
                {
                    cell.IsPassable = stageData.DefaultTile.CanStand;
                }

                availableCells.Add(new Vector2Int(x, y)); // 全マスを「空きマス候補」に追加
            }
        }


        // ----- 3. 特殊床のランダム配置 -----
        if (stageData.SpecialTileRules != null)
        {
            foreach (var rule in stageData.SpecialTileRules)
            {
                // 確率の抽選 (0.0 - 1.0)
                if (UnityEngine.Random.value <= rule.SpawnProbability)
                {
                    for (int i = 0; i < rule.SpawnCount; i++)
                    {
                        if (availableCells.Count == 0)
                            break;

                        // ランダムなマスを選ぶ
                        int randomIndex = UnityEngine.Random.Range(0, availableCells.Count);
                        Vector2Int pos = availableCells[randomIndex];

                        // 床を特殊床に変更
                        var cell = _fieldState.GetCell(pos.x, pos.y);
                        cell.CurrentTile = rule.SpecialTile;

                        if (rule.SpecialTile != null)
                        {
                            cell.IsPassable = rule.SpecialTile.CanStand; // 特殊床の通行可否を適用
                        }

                        // 重複上書きなしの仕様を満たすため、候補リストから除外
                        availableCells.RemoveAt(randomIndex);
                    }
                }
            }
        }


        // ----- 4. 障害物の配置 -----
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
    /// ActorIDから現在の座標を取得するメソッド。存在しないIDの場合は(-1, -1)を返す
    /// </summary>
    /// <param name="actorId"></param>
    /// <returns></returns>
    public Vector2Int GetActorPosition(int actorId)
    {
        if (_actorPositions.TryGetValue(actorId, out Vector2Int pos))
        {
            return pos;
        }
        return new Vector2Int(-1, -1); // 存在しないアクターIDの場合は無効な座標を返す
    }


    /// <summary>
    /// 移動の実行リクエスト
    /// </summary>
    /// <param name="command">移動の種類とDeltaを含むコマンド</param>
    /// <param name="currentPos">移動元の現在位置</param>
    /// <returns>移動が成功した場合はtrue、それ以外はfalse</returns>
    public bool TryMoveActor(MoveCommand command)
    {
        Vector2Int currentPos = GetActorPosition(command.ActorId);
        if (currentPos.x == -1)
        {
            return false;   // アクターIDが存在しない場合は移動不可
        }

        Vector2Int targetPos;

        //----- 1. Movetypeに応じてDeltaを変換 -----
        if (command.Type == MoveType.Warp)
        {
            targetPos = command.Delta; // ワープはDeltaを絶対座標として扱う
        }
        else
        {
            // 通常移動とノックバックはDeltaを相対座標として扱う
            targetPos = currentPos + command.Delta;
        }

        // ----- 2. 移動可能か判定 -----
        if (CanMoveTo(command.ActorId, targetPos.x, targetPos.y))
        {
            // 移動が可能な場合、占有状態を更新
            UpdateOccupancy(command.ActorId, currentPos.x, currentPos.y, targetPos.x, targetPos.y);
            // 移動イベントを発行
            OnActorMoved?.Invoke(command.ActorId, targetPos.x, targetPos.y);
            return true;    // 移動成功
        }

        return false;   // 移動不可の場合はfalseを返す
    }


    /// <summary>
    /// 指定した座標に移動可能かどうかを判定するロジック
    /// </summary>
    /// <param name="actorId">移動を試みるキャラクターのID</param>
    /// <param name="targetX">目的地のX座標</param>
    /// <param name="targetY">目的地のY座標</param>
    /// <returns>移動可能な場合は true, それ以外は false</returns>
    public bool CanMoveTo(int actorId, int targetX, int targetY)
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

        // ----- 4. テリトリーチェック -----
        bool isPlayer = (actorId == 1);         // 仮にID=1がプレイヤーとする
        int borderX = _fieldState.Width / 2;    // 盤面の中央を境界とする例

        if (isPlayer && targetX >= borderX)
        {
            return false;   // プレイヤーは右半分に移動できない
        }
        if (!isPlayer && targetX < borderX)
        {
            return false;   // 敵は左半分に移動できない
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
    public void UpdateOccupancy(int moverId, int fromX, int fromY, int toX, int toY)
    {
        // ----- 1. 占有状態の解除 -----
        if (fromX >= 0 && fromY >= 0)   // 移動元が有効な座標であれば占有解除を試みる
        {
            var fromCell = _fieldState.GetCell(fromX, fromY);
            if (fromCell != null && fromCell.OccupierId == moverId)
            {
                fromCell.OccupierId = -1;   // 移動元の占有を解除
            }
        }

        // ----- 2. 占有状態の設定 -----
        if (toX >= 0 && toY >= 0)
        {
            var toCell = _fieldState.GetCell(toX, toY);
            toCell.OccupierId = moverId;    // 移動先を占有

            _actorPositions[moverId] = new Vector2Int(toX, toY); // キャラクター位置のマッピングも更新
        }
    }
}
