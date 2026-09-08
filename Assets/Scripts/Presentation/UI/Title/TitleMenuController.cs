// ================================================================================
// File         : TitleMenuController.cs
// Author       : Iwai Shogo
//
// Description  : タイトル画面でのメニュー操作の管理を行う。
// Created      : 2026-07-03
// ================================================================================

using Game.Core.Save;
using Game.Infrastructure.Loading;
using Game.Presentation.UI.Common;
using Game.Presentation.UI.Pause;
using Game.WaveSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace Game.Presentation.UI.Title
{
    /// <summary>
    /// タイトル画面でのメニュー操作の管理を行う
    /// </summary>
    public class TitleMenuController : MonoBehaviour
    {
        // テラダ
        [Header("====== 初期選択ボタン ======")]
        [SerializeField] private Button _startButton;

        [Tooltip("「つづきから」ボタン。セーブデータが無い時は自動でinteractable = falseになります。")]
        [SerializeField] private Button _continueButton;
        [SerializeField] private Image _continueButtonTextImage;

        [SerializeField] private Button _optionButton;

        [Header("====== オプション ======")]
        [SerializeField] private PauseMenuController _optionMenuPrefab;

        private PauseMenuController _optionMenuInstance;
        private MenuSelectionFeedbackController _selectionFeedback;
        private TitleSignboardAnimator _signboardAnimator;

        [Header("--- 遷移先のシーン ---")]
        [SerializeField] private string _selectSceneName = "StageSelect";
        [SerializeField] private string _loadingSceneName = "Loading";

        [SerializeField] private string _defaultBootName = "Boot";
        [SerializeField] private string _tutorialBootName = "TutorialBoot";

        [SerializeField] private string _openingSceneName = "Opening";

        [SerializeField] private StageDataSO _stageDataSO;

        [Header("--- つづきから設定 ---")]
        [Tooltip("セーブデータのStageIndexを解決する起点となるStage1のStageDataSO。\nOpeningFlowControllerに設定しているStage1と同じアセットを指定してください。")]
        [SerializeField] private StageDataSO _continueRootStageData;

        private void Awake()
        {
            _selectionFeedback = GetComponent<MenuSelectionFeedbackController>();
            _signboardAnimator = GetComponent<TitleSignboardAnimator>();
        }

        public void OnEnable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem.current.SetSelectedGameObject(_startButton.gameObject);

            // セーブデータが無い場合は「つづきから」を押せないようにする
            if (_continueButton != null)
            {
                bool hasSaveData = SaveManager.HasSaveData();
                _continueButton.interactable = hasSaveData;
                if (_continueButtonTextImage != null)
                {
                    _continueButtonTextImage.color = hasSaveData ? Color.white : _continueButton.colors.disabledColor;
                }
            }
        }

        private void Update()
        {
            if (_selectionFeedback != null)
            {
                if (!_selectionFeedback.enabled)
                {
                    bool signboardAnimationPlaying = _signboardAnimator != null && _signboardAnimator.IsPlaying;
                    if (signboardAnimationPlaying)
                    {
                        return;
                    }

                    _selectionFeedback.enabled = true;
                }

                bool optionMenuOpen = _optionMenuInstance != null && _optionMenuInstance.IsShowingTitleOptions;
                _selectionFeedback.SetInputEnabled(!optionMenuOpen);
            }
        }

        /// <summary>
        /// New Gameボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickNewGame()
        {
            Debug.Log("New Game が押されたぜよ。シーン遷移: " + _openingSceneName);
            // SceneManager.LoadScene(_selectSceneName);

            // Beta版での一時的な実装
            // オープニングシーンに切り替えます！ 9/4 - Asano
            SceneManager.LoadScene(_openingSceneName, LoadSceneMode.Single);
        }

        /// <summary>
        /// Load Game(つづきから)ボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickLoadGame()
        {
            GameSaveData save = SaveManager.Load();
            if (save == null)
            {
                Debug.LogWarning("[Title] セーブデータが見つからないため、つづきからはできましぇん。");
                return;
            }

            if (_continueRootStageData == null)
            {
                Debug.LogError("[Title] つづきから用のルートStageDataSO(_continueRootStageData)が設定されていません。");
                return;
            }

            StageDataSO targetStage = SaveManager.ResolveStageByIndex(_continueRootStageData, save.stageIndex);
            if (targetStage == null)
            {
                Debug.LogError("[Title] セーブデータに対応するStageを解決できませんでした。");
                return;
            }

            Debug.Log($"つづきから が押されたぜよ。Stage: {targetStage.StageName} / Wave: {save.waveIndex + 1} / お金: {save.money} / 強化数: {save.upgrades?.Count ?? 0}");
            StartCoroutine(ContinueLoad(_defaultBootName, targetStage, save));
        }


        /// <summary>
        /// Tutorialボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickTutorial()
        {
            StartCoroutine(StageLoad(_tutorialBootName, _stageDataSO));
        }

        /// <summary>
        /// Optionボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickOption()
        {
            if (_optionMenuPrefab == null)
            {
                return;
            }

            if (_optionMenuInstance == null)
            {
                _optionMenuInstance = Instantiate(_optionMenuPrefab);
            }

            _optionMenuInstance.OpenTitleOptions(_optionButton);
            _selectionFeedback?.SetInputEnabled(false);
        }

        /// <summary>
        /// Exitボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickExit()
        {
            Debug.Log("Exit が押されたぜよ。ゲームを終了するぜよ。");

#if UNITY_EDITOR
            // Unityエディタ上での実行時は再生モードを終了する
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // ビルドした実際のゲームではアプリを終了する
            Application.Quit();
#endif
        }

        private IEnumerator StageLoad(string bootName, StageDataSO stage)
        {
            // Scemeをロードする
            AsyncOperation bootLoad = SceneManager.LoadSceneAsync(_selectSceneName, LoadSceneMode.Additive);
            yield return bootLoad;

            // 生成が完了次第、ステージデータを渡してロードを起動
            LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
            loadingFlowController.LoadBootScene(bootName, stage);
            // 現シーンを削除する
            Scene currentSceneName = gameObject.scene;
            SceneManager.UnloadSceneAsync(currentSceneName);
        }

        /// <summary>
        /// つづきから用。StageSelectを経由せず直接Loadingシーンへ入り、
        /// セーブされていたStage・Wave・お金・強化状況から再開する。
        /// </summary>
        private IEnumerator ContinueLoad(string bootName, StageDataSO stage, GameSaveData save)
        {
            AsyncOperation loadingLoad = SceneManager.LoadSceneAsync(_loadingSceneName, LoadSceneMode.Additive);
            yield return loadingLoad;

            LoadingFlowController loadingFlowController = UnityEngine.Object.FindFirstObjectByType<LoadingFlowController>();
            if (loadingFlowController == null)
            {
                Debug.LogError("[Title] LoadingFlowControllerが見つかりません。");
                yield break;
            }

            loadingFlowController.LoadBootScene(bootName, stage, save);

            // 現シーンを削除する
            Scene currentSceneName = gameObject.scene;
            SceneManager.UnloadSceneAsync(currentSceneName);
        }
    }
}
