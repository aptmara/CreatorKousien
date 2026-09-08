using System.Collections;
using Game.Core.Save;
using Game.Infrastructure.Bootstrap;
using Game.Presentation.UI.Loading;
using Game.WaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Infrastructure.Loading
{
    public sealed class LoadingFlowController : MonoBehaviour
    {
        private const float MinimumLoadingDuration = 3f;

        [SerializeField] private string _bootSceneName = "Boot";
        private LoadingView _view;

        private void Awake()
        {
            _view = gameObject.AddComponent<LoadingView>();
            _view.Initialize();
        }

        /// <summary>
        /// Bootシーンをロードしてゲームを準備します。
        /// </summary>
        /// <param name="bootSceneName">ロードするBootシーン名</param>
        /// <param name="stageData">開始するStageDataSO</param>
        /// <param name="resumeData">「つづきから」のセーブデータ。新規開始の場合はnull(省略可)。</param>
        public void LoadBootScene(string bootSceneName, StageDataSO stageData, GameSaveData resumeData = null)
        {
            StartCoroutine(Load(bootSceneName, stageData, resumeData));
        }

        IEnumerator Load(string bootSceneName, StageDataSO stageData, GameSaveData resumeData)
        {
            Time.timeScale = 1f;

            float loadingStartedAt = Time.realtimeSinceStartup;

            AsyncOperation bootLoad = SceneManager.LoadSceneAsync(bootSceneName, LoadSceneMode.Additive);
            if (bootLoad == null)
            {
                Debug.LogError($"[LoadingFlowController] {bootSceneName}シーンのロード開始に失敗しました。");
                yield break;
            }

            yield return bootLoad;

            PrototypeSceneFlowController boot = Object.FindFirstObjectByType<PrototypeSceneFlowController>();
            if (boot == null)
            {
                Debug.LogError("[LoadingFlowController] PrototypeSceneFlowControllerが見つかりません。");
                yield break;
            }

            // StageSelectで選ばれたStageDataSOを渡して、対応するStageシーンを読み込ませる
            // (つづきからの場合はresumeDataも一緒に渡す)
            yield return boot.PrepareGameRoutine(stageData, resumeData);
            if (boot.PreparationFailed || !boot.IsPrepared)
            {
                Debug.LogError("[LoadingFlowController] ゲームの初期化に失敗しました。");
                yield break;
            }

            yield return null;
            //Shader.WarmupAllShaders();
            yield return null;

            float remainingDuration = MinimumLoadingDuration - (Time.realtimeSinceStartup - loadingStartedAt);
            if (remainingDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingDuration);
            }

            yield return _view.PlayGameStartRoutine();

            boot.StartPreparedGame();

            AsyncOperation bootUnload = SceneManager.UnloadSceneAsync(bootSceneName);
            if (bootUnload != null)
            {
                yield return bootUnload;
            }

            Scene loadingScene = gameObject.scene;
            Scene gameplayScene = SceneManager.GetSceneByName("GameplayShell");
            if (gameplayScene.IsValid() && gameplayScene.isLoaded)
            {
                SceneManager.SetActiveScene(gameplayScene);
            }

            SceneManager.UnloadSceneAsync(loadingScene);
        }
    }
}
