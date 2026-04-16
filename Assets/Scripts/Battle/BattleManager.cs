// ------------------------------------------------------------
// File		: BattleManager.cs
// Summary	: 戦闘の管理を行うクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - PlayerとEnemyの攻撃の計算や、戦闘の進行を管理するクラス。GameManagerから呼び出される想定
// - 設計書にもとづいて設計するので一旦全部コメントアウトします。
// ------------------------------------------------------------
using UnityEngine;
using System;
using System.Collections.Generic;
//using CreatorKousien.Data;
//using CreatorKousien.Enemy;

//[Serializable]
//public class BattleStageSetup
//{
//    [Tooltip("ステージの名前")]
//    public string StageName = "Stage 1";
//    [Tooltip("盤面データ")]
//    public StageData StageData;
//    [Tooltip("プレイヤーデータ")]
//    public PlayerData PlayerData;
//    [Tooltip("敵のデータリスト")]
//    public EnemyData[] EnemiesToSpawn;
//}


///// <summary>
///// バトル関連の処理を管理するクラス
///// </summary>
public class BattleManager : MonoBehaviour
{
    //    [Header("データベース")]
    //    [Tooltip("全ステージの設定リスト")]
    //    [SerializeField] private BattleStageSetup[] _stageDatabases;                        // 全ステージの設定リスト

    //    [Header("ビューへの参照")]
    //    [Tooltip("盤面を描画するFieldView")]
    //    [SerializeField] private FieldView _fieldView;                                      // 盤面を描画するFieldView

    //    [Header("カメラの設定")]
    //    [Tooltip("カメラの高さ")]
    //    [SerializeField] private float _cameraHeight = 8.0f;                                 // カメラの高さ
    //    [Tooltip("カメラのZ軸オフセット")]
    //    [SerializeField] private float _cameraOffsetZ = -5.0f;                               // 少し手前から見下ろすためのズレ


    //    // ----- 各システムのインスタンス -----
    //    private FieldService _fieldService;                                                 // FieldServiceのインスタンス
    //    private TileEffectSystem _tileEffectSystem;                                         // TileEffectSystemのインスタンス
    //    private PlayerSystem _playerSystem;                                                 // PlayerSystemのインスタンス

    //    // 敵システム関連
    //    private AttackTelegraphSystem _telegraphSystem;                                     // タイル効果のシステム
    //    private List<EnemyRuntimeData> _enemyRuntimes = new List<EnemyRuntimeData>();       // 戦闘中の敵のランタイムデータリスト
    //    private List<EnemyAI> _enemyAIs = new List<EnemyAI>();                              // 戦闘中の敵のAIリスト

    //    private int _nextActorId = 2;                                                       // 次に生成されるアクターのID


    //    /// <summary>
    //    /// GameManagerから呼ばれる、バトルの初期化エントリポイント
    //    /// </summary>
    //    /// <param name="stageId">実行するステージのID</param>
          public void Initialize(int stageId = 1)
          {
        //        if (_stageDatabases == null || stageId < 0 || stageId >= _stageDatabases.Length)
        //        {
        //            Debug.LogError($"[BattleManager] Stage ID {stageId} のデータが見つかりません！");
        //            return;
        //        }

        //        var setupData = _stageDatabases[stageId];
        //        Debug.Log($"<color=yellow>[BattleManager] {setupData.StageName} のセットアップを開始します...</color>");

        //        // ----- 1. バトル共通システムの初期化 -----
        //        _telegraphSystem = new AttackTelegraphSystem();

        //        // ----- 2. 盤面の初期化 -----
        //        SetupField(setupData.StageData);

        //        // ----- 3. プレイヤーの初期化 -----
        //        SpawnAndInitPlayer(setupData.PlayerData, setupData.StageData.PlayerStartPosition);

        //        // ----- 4. 敵の初期化 -----
        //        SpawnAndInitEnemies(setupData.EnemiesToSpawn);

        //        // ----- 5. カメラの位置調整 -----
        //        AdjustCameraPosition(setupData.StageData);

        //        Debug.Log($"<color=green>[BattleManager] {setupData.StageName} のセットアップが完了しました！</color>");

        //        // TODO: バトル開始のイベントを発行するなど、必要な処理を追加
        //    }


        //    /// <summary>
        //    /// ステージデータを元に盤面を初期化する処理
        //    /// </summary>
        //    /// <param name="stageData"></param>
        //    private void SetupField(StageData stageData)
        //    {
        //        _fieldService = new FieldService();
        //        _fieldService.Initialize(stageData);
        //        _tileEffectSystem = new TileEffectSystem(_fieldService.State);

        //        _fieldView.BuildView(_fieldService.State, stageData);
        //        _fieldService.OnTileChanged += _fieldView.UpdateCellTileModel;
        //    }


        //    /// <summary>
        //    /// プレイヤーのスポーンと初期化を行う処理
        //    /// </summary>
        //    /// <param name="playerData"></param>
        //    /// <param name="startPos"></param>
        //    private void SpawnAndInitPlayer(PlayerData playerData, Vector2Int startPos)
        //    {
        //        if (playerData == null || playerData.PlayerPrefab == null)
        //        {
        //            Debug.LogError("[BattleManager] プレイヤーデータが不正です！");
        //            return;
        //        }

        //        // ----- 1. Playerの実体化 -----
        //        GameObject playerObj = Instantiate(playerData.PlayerPrefab);
        //        playerObj.name = "PlayerCharacter";
        //        var playerView = playerObj.GetComponentInChildren<PlayerView>(true);

        //        // ----- 2. PlayerSystemの初期化 -----
        //        _playerSystem = new PlayerSystem();
        //        _playerSystem.Initialize(playerData, startPos);

        //        // ----- 3. FieldServiceとのイベントの紐づけ -----
        //        _fieldService.OnActorMoved += (actorId, x, y) =>
        //        {
        //            if (actorId == _playerSystem.RuntimeData.ActorId)
        //            {
        //                Vector2Int pos = new Vector2Int(x, y);
        //                _playerSystem.SyncPosition(pos);
        //                _fieldView.HighlightCell(x, y);
        //                playerView.SetStandingTile(_fieldView.GetCellView(pos));
        //            }
        //        };

        //        _playerSystem.OnPositionChanged += (gridPos) =>
        //        {
        //            Vector3 actualPos = _fieldView.GetCellWorldPosition(gridPos.x, gridPos.y);
        //            playerView.UpdateTargetPosition(actualPos);
        //        };
        //        _playerSystem.OnDeathEvent += () =>
        //        {
        //            playerView.OnDeath();
        //        };

        //        // ----- 4. プレイヤーの初期位置にスポーン -----
        //        Vector3 startWorldPos = _fieldView.GetCellWorldPosition(startPos.x, startPos.y);
        //        playerView.Initialize(startWorldPos);
        //        playerView.SetStandingTile(_fieldView.GetCellView(startPos));

        //        _fieldService.UpdateOccupancy(_playerSystem.RuntimeData.ActorId, -1, -1, startPos.x, startPos.y);
        //        _tileEffectSystem.TriggerOnEnter(_playerSystem.RuntimeData.ActorId, startPos.x, startPos.y);
        //        _fieldView.HighlightCell(startPos.x, startPos.y);
              }



        //    private void SpawnAndInitEnemies(EnemyData[] enemies)
        //    {
        //        if (enemies == null || enemies.Length == 0)
        //        {
        //            Debug.LogWarning("[BattleManager] 敵のデータが見つかりません。敵はスポーンされません。");
        //            return;
        //        }

        //        // ----- 1. 敵陣の空きますリストを作成 -----
        //        Vector2Int enemyTerritoryX = _fieldService.GetEnemyTerritoryX();
        //        int fieldHeight = _fieldService.GetFieldSize().y;
        //        List<Vector2Int> availableCells = new List<Vector2Int>();

        //        for (int x = enemyTerritoryX.x; x <= enemyTerritoryX.y; x++)
        //        {
        //            for (int y = 0; y < fieldHeight; y++)
        //            {
        //                if (!_fieldService.IsObstacle(x, y) && _fieldService.GetOccupierId(x, y) == -1)
        //                {
        //                    availableCells.Add(new Vector2Int(x, y));
        //                }
        //            }
        //        }

        //        // ----- 2. 敵のスポーンと初期化 -----
        //        foreach (var enemyData in enemies)
        //        {
        //            if (availableCells.Count == 0)
        //            {
        //                Debug.LogWarning("[BattleManager] 敵をスポーンする空きセルが不足しています。これ以上敵をスポーンできません。");
        //                break;
        //            }

        //            // ランダムに空きセルを選択してスポーン
        //            int randomIndex = UnityEngine.Random.Range(0, availableCells.Count);
        //            Vector2Int spawnPos = availableCells[randomIndex];
        //            availableCells.RemoveAt(randomIndex);

        //            int actorId = _nextActorId;
        //            _nextActorId++;

        //            // EnemyRuntimeDataの作成と保存
        //            EnemyRuntimeData runtimeData = new EnemyRuntimeData
        //            {
        //                ActorId = actorId,
        //                EnemyId = enemyData.EnemyId,
        //                Position = spawnPos,
        //                CurrentHp = enemyData.MaxHp
        //            };
        //            _enemyRuntimes.Add(runtimeData);

        //            // EnemyAIの作成と保存
        //            EnemyAI ai = new EnemyAI(runtimeData, enemyData, _telegraphSystem);
        //            _enemyAIs.Add(ai);

        //            // Prefabの実体化
        //            if (enemyData.EnemyPrefab != null)
        //            {
        //                GameObject enemyObj = Instantiate(enemyData.EnemyPrefab);
        //                enemyObj.name = $"Enemy_{enemyData.EnemyName}_{actorId}";
        //                enemyObj.transform.position = _fieldView.GetCellWorldPosition(spawnPos.x, spawnPos.y);
        //            }

        //            // 盤面に占有を通知
        //            _fieldService.UpdateOccupancy(actorId, -1, -1, spawnPos.x, spawnPos.y);
        //            Debug.Log($"[BattleManager] 敵 '{enemyData.EnemyName}' (ID: {actorId}) を位置 ({spawnPos.x}, {spawnPos.y}) にスポーンしました。");
        //        }
        //    }


        //    private void AdjustCameraPosition(StageData stageData)
        //    {
        //        // ----- 1. シーン内のカメラを取得 -----
        //        Camera mainCam = Camera.main;

        //        if (mainCam == null)
        //        {
        //            Debug.LogError("[BattleManager] シーン内にメインカメラが見つかりません！");
        //            return;
        //        }

        //        // ----- 2. 盤面の中央座標を計算-----
        //        float cellSize = stageData.CellSize;
        //        int width = stageData.Width;
        //        int height = stageData.Height;
        //        float borderGap = stageData.BorderGap;

        //        // X軸の中心
        //        // 端から端までの長さの半分を計算
        //        float totalWidth = (width * cellSize) + borderGap;
        //        float centerX = (totalWidth / 2f) - (cellSize / 2f);

        //        // Z軸の中心
        //        float centerZ = -((height - 1) * cellSize) / 2f;

        //        // ----- 3. カメラの位置決定 -----
        //        Vector3 targetCameraPos = new Vector3(centerX, _cameraHeight, centerZ + _cameraOffsetZ);

        //        // ----- 4. カメラの位置を設定 -----
        //        mainCam.transform.position = targetCameraPos;

        //        // ----- 5. カメラの向きを調整（必要に応じて） -----
        //        Vector3 lookAtTarget = new Vector3(centerX, 0, centerZ);
        //        mainCam.transform.LookAt(lookAtTarget);
        //    }


        //    /// <summary>
        //    /// 外部公開API
        //    /// </summary>
        //    public void ProcessTurnChange()
        //    {
        //        Debug.Log("[BattleManager] ターン進行処理を実行します");

        //        // ----- 1. プレイヤーのターン処理 -----
        //        _fieldService.ProcessTurnChange();

        //        // ----- 2. 敵のターン処理 -----
        //        // _telegraphSystem.ProcessTurn();

        //        // ----- 3. その他 -----
        //    }


        //    /// <summary>
        //    /// キャラクターの移動コマンドを処理するAPI
        //    /// </summary>
        //    /// <param name="command"></param>
        //    /// <returns></returns>
        //    public bool TryExecuteMove(MoveCommand command)
        //    {
        //        if (_fieldService.TryMoveActor(command))
        //        {
        //            var runtime = (command.ActorId == 1) ? _playerSystem.RuntimeData : GetEnemyRuntime(command.ActorId);

        //            if (runtime != null)
        //            {
        //                // TODO: ここではPlayerの想定だが、Enemyでも共通の処理になる
        //                _tileEffectSystem.TriggerOnExit(command.ActorId, runtime.Position.x - command.Delta.x, runtime.Position.y - command.Delta.y);
        //                _tileEffectSystem.TriggerOnEnter(command.ActorId, runtime.Position.x, runtime.Position.y);
        //            }
        //            return true;
        //        }
        //        return false;
        //    }


        //    /// <summary>
        //    /// キャラクターの攻撃/スキル実行要求を処理するAPI
        //    /// </summary>
        //    public void ExecuteAttack(/*AttackCommand command*/)
        //    {
        //        // TODO: 攻撃コマンドの処理。攻撃の計算,命中判定などを行う
        //        Debug.Log("[BattleManager] 攻撃コマンドを処理します");
        //    }


        //    /// <summary>
        //    /// キャラクターの床効果の発動を処理するAPI
        //    /// </summary>
        //    public void EvaluateTurnEndTileEffects()
        //    {
        //        // 全Characterの足元の床効果を発動させる
        //        Vector2Int playerPos = _playerSystem.RuntimeData.Position;
        //        _tileEffectSystem.TriggerOnTurnEnd(_playerSystem.RuntimeData.ActorId, playerPos.x, playerPos.y);

        //        foreach (var enemy in _enemyRuntimes)
        //        {
        //            Vector2Int enemyPos = enemy.Position;
        //            _tileEffectSystem.TriggerOnTurnEnd(enemy.ActorId, enemyPos.x, enemyPos.y);
        //        }
        //    }



        //    // ヘルパー関数
        //    // ------------------------------------------------------------

        //    /// <summary>
        //    /// 敵のランタイムデータをActorIdから取得するヘルパー関数
        //    /// </summary>
        //    /// <param name="actorId">アクターのID</param>
        //    /// <returns>指定されたアクタのランタイムデータ</returns>
        //    private EnemyRuntimeData GetEnemyRuntime(int actorId)
        //    {
        //        return _enemyRuntimes.Find(e => e.ActorId == actorId);
        //    }
    }
