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
// - Turnが変わったことを受け取れるように変更 (4/16)
// - タイル変更イベントを追加 (4/16)
// ------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;
using CreatorKousien.Field;
using CreatorKousien.Data;

namespace CreatorKousien.Field
{
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
        /// タイルが変更されたときに発行されるイベント。引数は「X座標」「Y座標」「新しいタイルの定義」
        /// </summary>
        public event Action<int, int, TileTypeDefinition> OnTileChanged;

        private int _currentBorderX;            /// 実行中の境界線を保存しておく変数

        private StageData _currentStageData;    /// 現在のステージデータを保存しておく変数 (特殊床のスポーンルールなどで参照するため)

        private int _currentTurnCount = 0;      /// 現在のターン数を保存しておく変数 (ターンを跨ぐ床の寿命管理などで参照するため)


        // 盤面情報取得API
        // ----------------------------------------------------------------------

        /// <summary>
        /// 盤面のサイズを取得
        /// </summary>
        public Vector2Int GetFieldSize() => new Vector2Int(_fieldState.Width, _fieldState.Height);

        /// <summary>
        /// プレイヤーのテリトリーX範囲を取得。左端が0で、境界線の1つ手前までがプレイヤーのテリトリー
        /// </summary>
        /// <returns>自陣のX座標の[min, max]</returns>
        public Vector2Int GetPlayerTerritoryX() => new Vector2Int(0, _currentBorderX - 1); // プレイヤーのテリトリーX範囲

        /// <summary>
        /// 敵陣のテリトリーX範囲を取得。境界線から右端までが敵のテリトリー
        /// </summary>
        /// <returns>敵陣のX座標の[min, max]</returns>
        public Vector2Int GetEnemyTerritoryX() => new Vector2Int(_currentBorderX, _fieldState.Width - 1); // 敵のテリトリーX範囲

        /// <summary>
        /// 現在の境界線(BorderX)を取得
        /// </summary>
        /// <returns></returns>
        public int GetBorderX() => _currentBorderX;

        /// <summary>
        /// 指定座標が盤面外かどうかを判定するAPI
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsOutOfBounds(int x, int y) => _fieldState.IsOutOfBounds(x, y); // 盤面外判定API

        /// <summary>
        /// 指定座標に障害物があるかどうかを判定するAPI。盤面外も障害物とみなす
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool IsObstacle(int x, int y)
        {
            if (IsOutOfBounds(x, y))
            {
                return true;   // 盤面外は障害物とする
            }
            return !_fieldState.GetCell(x, y).IsPassable; // 通行不可のセルは障害物とする
        }

        /// <summary>
        /// 指定座標に誰が占有しているかを取得するAPI。盤面外は-1（占有者なし）とみなす
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int GetOccupierId(int x, int y)
        {
            if (IsOutOfBounds(x, y))
            {
                return -1;  // 盤面外は占有者なしとする
            }
            return _fieldState.GetCell(x, y).OccupierId; // セルの占有者IDを返す
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
        /// 指定されたマスのリストに存在するキャラクターのIDを全て取得するメソッド
        /// </summary>
        /// <param name="targetCells">検索したいマスのリスト</param>
        /// <returns>マス内にいたキャラクターのIDのリスト</returns>
        public List<int> GetActorsInCells(List<Vector2Int> targetCells)
        {
            List<int> hitActorIds = new List<int>();

            foreach (var pos in targetCells)
            {
                // そのマスにいるキャラクターのIDを取得する関数を呼び出す
                int actorId = GetOccupierId(pos.x, pos.y);

                // 誰かがいて、まだリストに追加されていなければ追加する
                if (actorId != -1 && !hitActorIds.Contains(actorId))
                {
                    hitActorIds.Add(actorId);
                }
            }

            return hitActorIds;
        }





        // ステージ初期化・ターン進行処理・その他判定
        // ----------------------------------------------------------------------

        /// <summary>
        /// ステージデータを元に盤面を初期化し、障害物などを配置
        /// </summary>
        /// <param name="stageData">盤面サイズや初期障害物情報を含む静的データ</param>
        public void Initialize(StageData stageData)
        {
            // ----- 1. 盤面サイズの決定と作成 -----
            _fieldState = new FieldState(stageData.Width, stageData.Height);
            _actorPositions.Clear();                // キャラクター位置のマッピングも初期化
            _currentBorderX = stageData.BorderX;    // ステージデータから境界線のX座標を保存
            _currentStageData = stageData;          // ステージデータも保存しておく（特殊床のスポーンルールで参照するため）


            // 重複配置を避けるための「空きマス候補」リスト
            List<Vector2Int> playerCells = new List<Vector2Int>();   // プレイヤーが配置可能なマスのリスト
            List<Vector2Int> enemyCells = new List<Vector2Int>();   // 敵が配置可能なマスのリスト


            // ----- 2. デフォルト床で全マスを初期化 -----
            for (int x = 0; x < stageData.Width; x++)
            {
                for (int y = 0; y < stageData.Height; y++)
                {
                    var cell = _fieldState.GetCell(x, y);
                    bool isPlayerSide = (x < _currentBorderX);

                    // 自陣と敵陣で異なるデフォルト床を設定
                    TileTypeDefinition defaultTile = isPlayerSide ? stageData.PlayerDefaultTile : stageData.EnemyDefaultTile;

                    cell.CurrentTile = defaultTile; // デフォルト床を配置

                    cell.DefaultTile = defaultTile; // デフォルト床の定義も保存

                    // デフォルト床が設定されていれば、その通行可否を適用
                    if (defaultTile != null)
                    {
                        cell.IsPassable = defaultTile.CanStand;
                    }

                    if (x < _currentBorderX)
                    {
                        playerCells.Add(new Vector2Int(x, y)); // プレイヤー側のマス
                    }
                    else
                    {
                        enemyCells.Add(new Vector2Int(x, y));  // 敵側のマス
                    }
                }
            }


            // ----- 3. 特殊床のランダム配置 -----
            ApplySpawnRules(stageData.PlayerSpecialTileRules, playerCells, stageData.PlayerStartPosition, 0); // プレイヤー側の特殊床配置
            ApplySpawnRules(stageData.EnemySpecialTileRules, enemyCells, new Vector2Int(-1, -1), 0);   // 敵側の特殊床配置


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
        /// ターン進行処理
        /// </summary>
        public void ProcessTurnChange()
        {
            _currentTurnCount++;                                     // ターン数をカウント

            List<Vector2Int> playerCells = new List<Vector2Int>();   // プレイヤーが配置可能なマスのリスト
            List<Vector2Int> enemyCells = new List<Vector2Int>();    // 敵が配置可能なマスのリスト

            // ----- 1. 既存の特殊床を全てリセット -----
            for (int x = 0; x < _fieldState.Width; x++)
            {
                for (int y = 0; y < _fieldState.Height; y++)
                {
                    var cell = _fieldState.GetCell(x, y);
                    bool isPlayerSide = (x < _currentBorderX);

                    // 床の寿命が切れているかどうかを判定
                    bool justDied = false;

                    if (cell.RemainingLifespan > 0)
                    {
                        cell.RemainingLifespan--; // 寿命を減らす

                        if (cell.RemainingLifespan == 0)
                        {
                            ChangeCellTile(x, y, cell.DefaultTile); // 床を消す
                            justDied = true;                        // 寿命が切れたことを記録
                        }
                    }

                    // 空きマスの判定を「デフォルト床かどうか」にする
                    bool isDefaultTile = (cell.CurrentTile == cell.DefaultTile);

                    if (cell.IsPassable && cell.OccupierId == -1 && !_currentStageData.ObstaclePositions.Contains(new Vector2Int(x, y)))
                    {
                        if (isDefaultTile && !justDied)
                        {
                            if (isPlayerSide)
                            {
                                playerCells.Add(new Vector2Int(x, y)); // プレイヤー側の空きマス
                            }
                            else
                            {
                                enemyCells.Add(new Vector2Int(x, y));  // 敵側の空きマス
                            }
                        }
                    }
                }
            }

            // ----- 2. ターンが変わったことを受け取って、特殊床のスポーンルールを再適用 -----
            Vector2Int playerPos = GetActorPosition(1); // 仮にID=1がプレイヤーとする
            ApplySpawnRules(_currentStageData.PlayerSpecialTileRules, playerCells, playerPos, _currentTurnCount); // プレイヤー側の特殊床配置
            ApplySpawnRules(_currentStageData.EnemySpecialTileRules, enemyCells, new Vector2Int(-1, -1), _currentTurnCount);   // 敵側の特殊床配置
        }


        /// <summary>
        /// マスのデータを置き換え、同時に見た目の更新イベントを発行するヘルパー
        /// </summary>
        /// <param name="x">置き換え先のX座標</param>
        /// <param name="y">置き換え先のY座標</param>
        /// <param name="newTile">新しいタイルの定義</param>
        private void ChangeCellTile(int x, int y, TileTypeDefinition newTile)
        {
            var cell = _fieldState.GetCell(x, y);
            cell.CurrentTile = newTile; // 新しい床を配置
            if (newTile != null)
            {
                cell.IsPassable = newTile.CanStand; // 新しい床の通行可否を適用
            }

            OnTileChanged?.Invoke(x, y, newTile); // タイル変更イベントを発行
        }


        /// <summary>
        /// 特殊床のスポーンルールを適用して、盤面に特殊床をランダム配置するロジック
        /// </summary>
        /// <param name="rules">スポーンルール</param>
        /// <param name="availableCells">配置可能なセルのリスト</param>
        /// <param name="safeZoneCenter">安全地帯の中心座標</param>
        /// <param name="currentTurn">現在のターン数</param>
        private void ApplySpawnRules(StageData.SpecialTileSpawnRule[] rules, List<Vector2Int> availableCells, Vector2Int safeZoneCenter, int currentTurn)
        {
            if (rules == null)
            {
                return; // ルールがないか、候補がない場合は何もしない
            }

            foreach (var rule in rules)
            {
                // ----- 1.新頻度のチェック -----
                if (currentTurn > 0 && _currentTurnCount % rule.SpawnIntervalTurns != 0)
                    continue; // ターン間隔の条件を満たさない場合はスキップ

                if (UnityEngine.Random.value <= rule.SpawnProbability)
                {
                    int targetSpawnCount = UnityEngine.Random.Range(rule.MinSpawnCount, rule.MaxSpawnCount + 1); // スポーン数をランダムに決定

                    for (int i = 0; i < targetSpawnCount; i++)
                    {
                        if (availableCells.Count == 0)
                        {
                            break; // 配置可能なセルがなくなったら終了
                        }

                        int randomIndex = -1;
                        bool found = false;

                        for (int attempt = 0; attempt < 10; attempt++)
                        {
                            int tempIndex = UnityEngine.Random.Range(0, availableCells.Count);
                            Vector2Int candidatePos = availableCells[tempIndex];

                            // 自分自身の足元じゃないか判定
                            if (safeZoneCenter.x >= 0 && safeZoneCenter.y >= 0)
                            {
                                if (candidatePos != safeZoneCenter)
                                {
                                    found = true;
                                    randomIndex = tempIndex;
                                    break;
                                }
                            }
                            else
                            {
                                found = true;
                                randomIndex = tempIndex;
                                break;
                            }
                        }

                        if (!found)
                        {
                            continue;
                        }

                        Vector2Int pos = availableCells[randomIndex];
                        var cell = _fieldState.GetCell(pos.x, pos.y);

                        cell.DefaultTile = (pos.x < _currentBorderX) ? _currentStageData.PlayerDefaultTile : _currentStageData.EnemyDefaultTile;    // デフォルト床を設定
                        cell.RemainingLifespan = rule.LifespanTurns;                                                                                // 寿命を設定

                        ChangeCellTile(pos.x, pos.y, rule.SpecialTile);                                                                             // セルのタイルをルールで指定された特殊床に変更
                        availableCells.RemoveAt(randomIndex);                                                                                       // 配置したセルは候補から削除
                    }
                }
            }
        }




        /// <summary>
        /// UseCaseから、キャラクターが移動したことを受け取るためのメソッド
        /// </summary>
        /// <param name="actorId">移動したキャラクターのID</param>
        /// <param name="x">移動先のX座標</param>
        /// <param name="y">移動先のY座標</param>
        public void NotifyActorMoved(int actorId, int x, int y)
        {
            OnActorMoved?.Invoke(actorId, x, y);
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

            if (isPlayer && targetX >= _currentBorderX)
            {
                return false;   // プレイヤーは右半分に移動できない
            }
            if (!isPlayer && targetX < _currentBorderX)
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
}
