//=========================================================================
// File: GameManager.cs
// Author: Terada
// Description: ゲーム全体のライフサイクル管理クラス
// Created: 2026-04-13
//=========================================================================
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatorKousien.Core;
using CreatorKousien.Battle;

public class GameManager : MonoBehaviour
{
    private enum GameState { Title,Select,Game,Result}
    private GameState _currentState;


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
        _stageManager = new StageManager();
        _stageManager.Initialize();

        // 追記項目
        /*
        Version selectView = FindFirstObjectByType<SelectSceneView>();
        if(selectView == null)
        {
            Debug.LogError("[GameManager] SelectViewが見つかりません");
            return;
        }
        selectView.Setup(_stageManager, SceneChangeStart);
        */
    }

    void InitializeGameScene()
    {
        // BattleSetupDataの取得
        /*
        BattleSetupData setup = _stageManager.GetSelectBattleSetupData();
        if(setup == null)
        {
            Debug.LogError("[GameManager] ステージデータが取得できません");
            return;
        }
        */
        Debug.Log("[GameManager] GameSceneStart");
        // StageNoに応じた読み込みとインスタンス生成
        // Mediatorの初期化
        var dispatcher = new CommandDispatcher();
        var eventBus = new GameEventBus();

        _mediator = new GameMediator();
        _mediator.Initialize(dispatcher,eventBus);
        // バトルマネージャーの初期化
        //_battleManager = new BattleManager();
        // _battleManager.Initialize();
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
