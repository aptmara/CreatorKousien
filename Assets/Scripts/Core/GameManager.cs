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
    FieldView _fieldView;
    FieldService _fieldService;

    //==== EventBus ===
    GameEventBus _eventBus;

    // 
    PlayerSystem _player;
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
        Debug.Log("[GameManager] GameSceneStart");
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

        // 各種システムの生成
        //==== Player Systemの生成・初期化 ====

        _player = new PlayerSystem();
        Vector2Int pPos = _setupData.StageData.PlayerStartPosition;

        _player.Initialize(_setupData.PlayerData, pPos);
        _fieldService.UpdateOccupancy(_player.RuntimeData.ActorId, -1, -1, pPos.x, pPos.y);

        _fieldView = new FieldView();
        Vector3 startWorldPos = _fieldView.GetCellWorldPosition(pPos.x, pPos.y);

        // PlayerObjectの生成
        GameObject playerobj = Instantiate(_setupData.PlayerData.PlayerPrefab,startWorldPos,Quaternion.identity);
        PlayerView playerView = playerobj.GetComponent<PlayerView>();
        playerView.Initialize(startWorldPos);
        playerView.SetStandingTile(_fieldView.GetCellView(pPos));
        _fieldView.HighlightCell(pPos.x, pPos.y);

        //====//====//====//====//====//====//====

        //==== Enemy System ====

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

        // Dispatcher生成
        var dispatcher = new CommandDispatcher();

        // TileEffect生成
        _tileEffect = new TileEffectSystem(_fieldService.State);
        // ActionTelegraph生成
        _actiontelegraph = new ActionTelegraphSystem();

        // 各種UseCaseの生成
        MoveUseCase moveUseCase = new MoveUseCase(_fieldService,_tileEffect,_eventBus);
        AttackUseCase attackUseCase = new AttackUseCase(_battleManager, _fieldService,_player,_enemy, dispatcher, _eventBus);
        EnemyActionUseCase enemyUseCase = new EnemyActionUseCase(_enemy, _fieldService, _player,_actiontelegraph, dispatcher);
        // 各種Commandの登録
        dispatcher.Register<MoveCommand>(moveUseCase.Execute);
        dispatcher.Register<AttackCommand>(attackUseCase.Execute);
        dispatcher.Register<EnemyActionCommand>(enemyUseCase.Execute);

        _turnManager = gameObject.AddComponent<TurnManager>();
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

}
