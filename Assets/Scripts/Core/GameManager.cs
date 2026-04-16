//=========================================================================
// File: GameManager.cs
// Author: Terada
// Description: ゲーム全体のライフサイクル管理クラス
// Created: 2026-04-13
//=========================================================================
using System;
using Unity.VisualScripting;
using UnityEditor.Experimental.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
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

    private void OnDisable()
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
        Scene scene = SceneManager.GetActiveScene();

        if (scene.name == "Title")
        {
            Debug.Log("TitleSceneがロードされた");
        }
        else if (scene.name == "Select")
        {

        }
        else if (scene.name == "Game")
        {

        }
        else if (scene.name == "Result")
        {

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

    void SceneChangeStart(String sceneName)
    {
        // 修了処理
        SceneManager.LoadScene(sceneName);
    }

    void InitializeSelectScene()
    {
        _stageManager = new StageManager();
        _stageManager.Initialize();
    }

    void InitializeGameScene()
    {
        // StageNoに応じた読み込みとインスタンス生成
        // Mediatorの初期化
        _mediator = new GameMediator();
        _mediator.Initialize();
        // バトルマネージャーの初期化
        _battleManager = new BattleManager();
        _battleManager.Initialize();
        _mediator.SetBattleManager(_battleManager);
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
