// ================================================================================
// File         : TitleMenuController.cs
// Author       : Iwai Shogo
//
// Description  : タイトル画面でのメニュー操作の管理を行う。
// Created      : 2026-07-03
// ================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Presentation.UI.Title
{
    /// <summary>
    /// タイトル画面でのメニュー操作の管理を行う
    /// </summary>
    public class TitleMenuController : MonoBehaviour
    {
        [Header("--- 遷移先のシーン ---")]
        [SerializeField] private string _bootSceneName = "Boot";

        /// <summary>
        /// New Gameボタンが押されたときに呼び出す
        /// </summary>
        public void OnClickNewGame()
        {
            Debug.Log("New Game が押されたぜよ。シーン遷移: " + _bootSceneName);
            SceneManager.LoadScene(_bootSceneName);
        }

        /// <summary>
        /// Load Gameボタンが押されたときに呼び出す（将来用）
        /// </summary>
        public void OnClickLoadGame()
        {
            Debug.Log("Load Game が押されたぜよ（だが未実装）");
        }

        /// <summary>
        /// Optionボタンが押されたときに呼び出す（将来用）
        /// </summary>
        public void OnClickOption()
        {
            Debug.Log("Option が押されたぜよ（だが未実装）");
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
