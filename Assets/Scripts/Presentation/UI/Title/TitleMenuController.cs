// ================================================================================
// File         : TitleMenuController.cs
// Author       : Iwai Shogo
//
// Description  : タイトル画面でのメニュー操作の管理を行う。
// Created      : 2026-07-03
// ================================================================================

using Game.Presentation.UI.Pause;
using Game.Presentation.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
            SceneManager.LoadScene(_selectSceneName);
        }

        /// <summary>
        /// Load Gameボタンが押されたときに呼び出す（将来用）
        /// </summary>
        public void OnClickLoadGame()
        {
            Debug.Log("Load Game が押されたぜよ（だが未実装）");
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
    }
}
