using Game.Infrastructure.Loading;
using Game.WaveSystem;
using System;
using UnityEditor.Build.Content;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;


public class StageSelectController : MonoBehaviour
{
    [SerializeField] private string _titleSceneName = "Title";
    [SerializeField] private string _loadingSceneName = "Loading";

    [SerializeField] private Button _backButton;

    [SerializeField] private Button _stageButton;

    private bool isSelectedStage = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void LoadGameScene(StageDataSO stage)
    {
        if (isSelectedStage) return;
        isSelectedStage = true;

        // ロードシーンを生成
        Debug.Log(stage.StageName + "が選ばれました");
        Debug.Log("Loadを開始 シーン遷移: " + _loadingSceneName);
        StartCoroutine(StageLoad(stage));

    }

    public　void OnBackButtonClicked()
    {
        if (isSelectedStage) return;
        isSelectedStage = true;
        // タイトルシーンへ移行
        Debug.Log("タイトルシーンへ移行します。");
        SceneManager.LoadScene(_titleSceneName);
    }

    private IEnumerator StageLoad(StageDataSO stage)
    {
        // Scemeをロードする
        AsyncOperation bootLoad = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
        yield return bootLoad;

        // 生成が完了次第、ステージデータを渡してロードを起動
        LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
        loadingFlowController.LoadBootScene(stage);
        // 現シーンを削除する
        Scene currentSceneName = gameObject.scene;
        SceneManager.UnloadSceneAsync(currentSceneName);
    }
}
