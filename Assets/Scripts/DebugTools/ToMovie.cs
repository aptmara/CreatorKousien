using Game.Infrastructure.Loading;
using System.Collections;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.WaveSystem;

public class ToMovie : MonoBehaviour
{
    [SerializeField] string _loadSceneName;
    [SerializeField] string _bootName;
    [SerializeField] string _currentSceneName;
    [SerializeField] StageDataSO _stageData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(ToMovieScene());
    }


    private IEnumerator ToMovieScene()
    {
        // Scemeをロードする
        AsyncOperation bootLoad = SceneManager.LoadSceneAsync(_loadSceneName, LoadSceneMode.Additive);
        yield return bootLoad;

        // 生成が完了次第、ステージデータを渡してロードを起動
        LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
        loadingFlowController.LoadBootScene(_bootName, _stageData);
        // 現シーンを削除する
        Scene currentSceneName = gameObject.scene;
        SceneManager.UnloadSceneAsync(_currentSceneName);
    }
}
