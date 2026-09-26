using Game.Infrastructure.Loading;
using Game.WaveSystem;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ToMovie : MonoBehaviour
{
    [SerializeField] string _loadingSceneName;
    [SerializeField] string _bootName;

    [SerializeField] StageDataSO _stageData;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    IEnumerator Start()
    {
        AsyncOperation loadingLoad = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
        yield return loadingLoad;

        LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
        if (loadingFlowController == null)
        {
            Debug.LogError("[Title] LoadingFlowControllerが見つかりません。");
            yield break;
        }

        loadingFlowController.LoadBootScene(_bootName, _stageData);

        

        // 現シーンを削除する
        Scene currentSceneName = gameObject.scene;
        SceneManager.UnloadSceneAsync(currentSceneName);
    }


}
