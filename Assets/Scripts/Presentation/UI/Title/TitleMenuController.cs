// ================================================================================
// File         : TitleMenuController.cs
// Author       : Iwai Shogo
//
// Description  : タイトル画面でのメニュー操作の管理を行う。
// Created      : 2026-07-03
// ================================================================================

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
        [SerializeField] private Button _optionButton;

        [Header("====== オプション ======")]
        [SerializeField] private PauseMenuController _optionMenuPrefab;

        private PauseMenuController _optionMenuInstance;
        private MenuSelectionFeedbackController _selectionFeedback;
        private TitleSignboardAnimator _signboardAnimator;

        [Header("--- 遷移先のシーン ---")]
        [SerializeField] private string _selectSceneName = "StageSelect";

        [SerializeField] private string _defaultBootName = "Boot";
        [SerializeField] private string _tutorialBootName = "TutorialBoot";

        [SerializeField] private StageDataSO _stageDataSO;
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
            Debug.Log("New Game が押されたぜよ。シーン遷移: " + _selectSceneName);
            // SceneManager.LoadScene(_selectSceneName);

            // Beta版での一時的な実装
            StartCoroutine(StageLoad(_defaultBootName, _stageDataSO));
        }

        /// <summary>
        /// Load Gameボタンが押されたときに呼び出す（将来用）
        /// </summary>
        public void OnClickLoadGame()
        {
            Debug.Log("Load Game が押されたぜよ（だが未実装）");
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
    }
}
