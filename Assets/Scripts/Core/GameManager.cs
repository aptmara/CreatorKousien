//=========================================================================
// File: GameManager.cs
// Author: Terada
// Description: ゲーム全体のライフサイクル管理クラス
// Created: 2026-04-13
//=========================================================================
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatorKousien.Core;
using CreatorKousien.Battle;
using CreatorKousien.Data;
using CreatorKousien.UseCase;
using CreatorKousien.Field;
using CreatorKousien.Player;
using CreatorKousien.Enemy;
using CreatorKousien.Command;
using System.Collections;
using System.Threading;
using CreatorKousien.View;

public class GameManager : MonoBehaviour
{
    private enum GameState { Title,Select,Game,Result}
    private GameState _currentState;

    [Header("ステージセットアップデータ設定")]
    [Tooltip("インスペクターで作成した BattleSetupData をアタッチしてください")]
    [SerializeField] private BattleSetupData _setupData;

    /// <summary>
    /// 仲介クラスのインスタンス生成
    /// </summary>
    GameMediator _mediator;
    /// <summary>
    /// バトルマネージャーインスタンス
    /// </summary>
    BattleManager _battleManager;
    /// <summary>
    /// ステージマネージャーインスタンス
    /// </summary>
    StageManager _stageManager;

    TurnManager _turnManager;

    //==== Field ====
    [Header("Fiela")]
    [Tooltip("ここにFieldViewのPrefabをアタッチ")]
    [SerializeField]
    FieldView _fieldViewPrefab;
    FieldView _fieldView;
    FieldService _fieldService;

    //==== EventBus ===
    GameEventBus _eventBus;

    // 
    PlayerSystem _player;
    PlayerView _playerView;

    [SerializeField]
    private TestTimerView _timerPrefab;
    
    // PlayerID(固定値)
    const int PlayerID = 1;
    EnemySystem _enemy;

    private Dictionary<int,EnemyView> _enemyViews = new Dictionary<int, EnemyView>();

    TileEffectSystem _tileEffect;
    ActionTelegraphSystem _actiontelegraph;


    private void Awake()
    {
        // シーンをまたいで生存する設定
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // イベントを解除（メモリリーク防止）
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        switch (_currentState)
        {
            case GameState.Title:
                UpdateTitleScene();
                break;
            case GameState.Select:
                UpdateSelectScene();
                break;
            case GameState.Game:
                UpdateGameScene();
                break;
            case GameState.Result:
                UpdateResultScene();
                break;
        }
    }

    /// @brief ゲームシーンがスタートした時に初期化を通す
    void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        Debug.Log(scene.name);

        if (scene.name == "Title")
        {
            Debug.Log("TitleSceneがロードされた");
            
        }
        else if(scene.name == "Select")
        {
            InitializeSelectScene();
        }
        else if (scene.name == "Game")
        {
            InitializeGameScene();
        }
        else if(scene.name == "Result")
        {
            InitializeResultScene();
        }
    }

    void SceneChangeStart(string sceneName)
    {
        // 終了処理
        SceneManager.LoadScene(sceneName);
    }

    void InitializeSelectScene()
    {
        Debug.Log("[GameManager] SelectSceneStart");
        _stageManager = new StageManager();
        _stageManager.Initialize();

        // SelectSceneViewを取得
        var selectView = FindFirstObjectByType<SelectSceneView>();
        if(selectView == null)
        {
            Debug.LogError("[GameManager] SelectViewが見つかりません");
            return;
        }
        // Setup
        selectView.Setup(_stageManager, SceneChangeStart);
        
    }

    void InitializeGameScene()
    {
        Debug.Log("[GameManager] GameSceneStart");
        
        // BattleSetupDataの取得
        _setupData = _stageManager.GetSelectedBattleSetupData();
        if(_setupData == null)
        {
            Debug.LogError("[GameManager] ステージデータが取得できません");
            return;
        }

        // バトルマネージャーの初期化
        _battleManager = new BattleManager();

        // FieldView初期化
        _fieldView = Instantiate(_fieldViewPrefab);
        
        // FieldServiceの生成
        _fieldService = new FieldService();
        _fieldService.Initialize(_setupData.StageData);
        _fieldView.BuildView(_fieldService.State, _setupData.StageData);

        // 各種システムの生成
        //==== Player Systemの生成・初期化 ====

        _player = new PlayerSystem();
        Vector2Int pPos = _setupData.StageData.PlayerStartPosition;

        _player.Initialize(_setupData.PlayerData, pPos);
        _fieldService.UpdateOccupancy(_player.RuntimeData.ActorId, -1, -1, pPos.x, pPos.y);

        //_fieldView = new FieldView();
        Vector3 startWorldPos = _fieldView.GetCellWorldPosition(pPos.x, pPos.y);

        // PlayerObjectの生成
        GameObject playerobj = Instantiate(_setupData.PlayerData.PlayerPrefab,startWorldPos,Quaternion.identity);
        _playerView = playerobj.GetComponent<PlayerView>();
        _playerView.Initialize(startWorldPos);
        _playerView.SetStandingTile(_fieldView.GetCellView(pPos));
        _fieldView.HighlightCell(pPos.x, pPos.y);

        //====//====//====//====//====//====//====

        //==== Enemy System ====
        _enemy = new EnemySystem(_actiontelegraph);

        foreach(var enemyInfo in _setupData.Enemies)
        {
            // 敵をSetupDataの情報にしたがって生成
            _enemy.SpawnEnemy(enemyInfo.ActorId, enemyInfo.EnemyData, enemyInfo.SpawnPosition);

            _fieldService.UpdateOccupancy(enemyInfo.ActorId,-1,-1,enemyInfo.SpawnPosition.x,enemyInfo.SpawnPosition.y);

            Vector3 worldPos = _fieldView.GetCellWorldPosition(enemyInfo.SpawnPosition.x, enemyInfo.SpawnPosition.y);

            GameObject enemyObj = Instantiate(enemyInfo.EnemyData.EnemyPrefab, worldPos, Quaternion.identity);

            EnemyView view = enemyObj.GetComponent<EnemyView>();
            view.Initialize(enemyInfo.ActorId, worldPos,_fieldView.GetCellView(enemyInfo.SpawnPosition));
            _enemyViews.Add(enemyInfo.ActorId, view);
        }

        //====//====//====//====//====//====


        // TileEffect生成
        _tileEffect = new TileEffectSystem(_fieldService.State);
        // ActionTelegraph生成
        _actiontelegraph = new ActionTelegraphSystem();

        //==== EventBusの設定 ====
        InitEventBus();

        // Dispatcher生成
        var dispatcher = new CommandDispatcher();
        // 各種UseCaseの生成
        MoveUseCase moveUseCase = new MoveUseCase(_fieldService,_tileEffect,_eventBus);
        AttackUseCase attackUseCase = new AttackUseCase(_battleManager, _fieldService,_player,_enemy, dispatcher, _eventBus);
        EnemyActionUseCase enemyUseCase = new EnemyActionUseCase(_enemy, _fieldService, _player,_actiontelegraph, dispatcher);
        // 各種Commandの登録
        dispatcher.Register<MoveCommand>(moveUseCase.Execute);
        dispatcher.Register<AttackCommand>(attackUseCase.Execute);
        dispatcher.Register<EnemyActionCommand>(enemyUseCase.Execute);

        _turnManager = gameObject.GetComponent<TurnManager>();
        _turnManager.Initialize(dispatcher, _eventBus);

        // Mediatorの初期化
        _mediator = new GameMediator();
        _mediator.Initialize(dispatcher,_eventBus,_turnManager);
    }

    void InitializeResultScene()
    {

    }

    void UpdateTitleScene()
    {

    }
    void UpdateSelectScene()
    {

    }

    void UpdateGameScene()
    {

    }
    void UpdateResultScene()
    {

    }


    private void InitEventBus()
    {

        // 4 イベントの配線
        // ------------------------------------------------------------
        _eventBus = new GameEventBus();

        // プレファブからタイマーを生成
        if (_timerPrefab != null)
        {
            // 画面上部に生成
            Vector3 timerPosition = new Vector3(4.375f, 3, 0);

            var timerInstance = Instantiate(_timerPrefab, timerPosition, Quaternion.identity);
            timerInstance.Initialize(_eventBus);
        }

        // 移動リクエストの集中管理
        _eventBus.OnActorMoveRequested += (actorId, targetGridPos) =>
        {
            // グリッド座標をワールド座標に変換する
            Vector3 worldPos = _fieldView.GetCellWorldPosition(targetGridPos.x, targetGridPos.y);
            GridCellView cellView = _fieldView.GetCellView(targetGridPos);

            // プレイヤーの場合
            if (actorId == _player.RuntimeData.ActorId)
            {
                _playerView.UpdateTargetPosition(worldPos);
                _playerView.SetStandingTile(cellView);
                _fieldView.HighlightCell(targetGridPos.x, targetGridPos.y);
            }
            // エネミーの場合
            else if (_enemyViews.TryGetValue(actorId, out var eView))
            {
                eView.MoveTo(worldPos);
                eView.SetStandingTile(cellView);
            }
        };


        // 被ダメージ通知を受け取ったら、PlayerViewの被弾エフェクトを鳴らす！！
        _eventBus.OnDamageTaken += (targetId, damage) =>
        {
            if (targetId == _player.RuntimeData.ActorId) _playerView.PlayDamageEffect(damage);
            else if (_enemyViews.TryGetValue(targetId, out var eView)) eView.PlayDamageEffect();
        };

        // 死亡通知を受け取ったら、PlayerViewの死亡エフェクトを鳴らす！！
        _eventBus.OnActorDeath += (targetId) =>
        {
            if (targetId == _player.RuntimeData.ActorId) _playerView.PlayDeathEffect();
            else if (_enemyViews.TryGetValue(targetId, out var eView))
            {
                eView.PlayDeathEffect();
                _enemyViews.Remove(targetId); // 辞書から削除
            }
        };


        // 攻撃範囲表示のリクエストを受け取ったら、FieldViewに盤面を光らせるように指示する！
        _eventBus.OnAttackAreaExecuted += (targetCells) =>
        {
            // 1. 盤面をオレンジ色に光らせる！
            _fieldView.ShowAttackArea(targetCells, true);

            // 2. 0.3秒後に消すコルーチンを回す
            StartCoroutine(HideAttackAreaRoutine(targetCells));
        };


        // 戻って来た通知を受け取って画面を動かす
        _eventBus.OnTelegraphRequested += (targetCells, isWarning) =>
        {
            _fieldView.ShowTelegraph(targetCells, isWarning);
        };
        _eventBus.OnAttackHit += (targetActorId) =>
        {
            Debug.Log($"<color=red>[View] ActorID:{targetActorId} に攻撃ヒットエフェクトを再生！</color>");
        };

        _fieldService.OnActorMoved += (actorId, x, y) =>
        {
            if (actorId == _player.RuntimeData.ActorId)
            {
                _player.SyncPosition(new Vector2Int(x, y));

                Vector3 targetWorldPos = _fieldView.GetCellWorldPosition(x, y);
                _playerView.UpdateTargetPosition(targetWorldPos);
                _playerView.SetStandingTile(_fieldView.GetCellView(new Vector2Int(x, y)));

                _fieldView.HighlightCell(x, y);
            }
        };

        _eventBus.OnActionLogicCompleted += (actorId) =>
        {
            StartCoroutine(WaitAnimationAndProceed());
        };
    }

    /// <summary>
    /// 盤面の光を消すコルーチン。攻撃エフェクトが残る時間を待ってから、FieldViewに光を消すように指示する。
    /// </summary>
    /// <param name="targetCells"></param>
    /// <returns></returns>
    private System.Collections.IEnumerator HideAttackAreaRoutine(List<Vector2Int> targetCells)
    {
        // 攻撃のエフェクトが残る時間
        yield return new WaitForSeconds(0.3f);

        // 盤面の光を消す
        _fieldView.ShowAttackArea(targetCells, false);
    }

    private IEnumerator WaitAnimationAndProceed()
    {
        // 0.5秒のアニメーション待機をシミュレート
        yield return new WaitForSeconds(0.5f);

        // Mediator経由で安全にTurnManagerへ報告
        _mediator.CompleteCurrentActionAnimation();
    }
}
