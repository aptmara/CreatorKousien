// ================================================================================
// File         : ResultFlowController.cs
// Author       : Iwai Shogo
//
// Description  : 加算ロードされたリザルトシーン全体の進行とイベントを統括する
// Created      : 2026-07-02
//
// Notes        : ゲームクリア時のボタン押下時のリトライフローを実装します！ - 2026/07/09  Asano
// ================================================================================

using UnityEngine;
using Game.Core.Management;
using UnityEngine.SceneManagement;

namespace Game.Presentation.UI.Result
{
    /// <summary>
    /// 加算ロードされたリザルトシーン全体の進行とイベントを統括する
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResultFlowController : MonoBehaviour
    {
        [Header("--- 制御対象UIへの参照 ---")]
        [SerializeField] private ResultUIController _uiController;

        [Header("--- クリア時のボタン押下でロードするシーン ---")]
        [SerializeField] private string _titleSceneName = "Title";


        void Start()
        {
            if (_uiController == null) _uiController = GetComponentInChildren<ResultUIController>();

            // GameProgressionManager からパッキングされたデータを引き抜く
            if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.ResultSummary != null )
            {
                GameResultSummary data = GameProgressionManager.Instance.ResultSummary;

                // UIクラスに対して、表示情報の更新と演出切り替えを委譲
                _uiController.SetupResultView(data, OnGameClearTitleButtonClicked, OnGameOverRetryButtonClicked);
            }
            else
            {
                Debug.LogWarning("[Result] プレイデータ(ResultSummary)が見つかりません。デバッグ起動用の仮表示ぜよ。。。");
                _uiController.SetupResultView(
                    new GameResultSummary(true, 2, 30f),
                    OnGameClearTitleButtonClicked,
                    OnGameOverRetryButtonClicked);
            }
        }


        /// <summary>
        /// ゲームクリア時のタイトルへ戻るボタン押下時の処理
        /// </summary>
        private void OnGameClearTitleButtonClicked()
        {
            Debug.Log("[Result] タイトルへ戻るぜよ");

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            SceneManager.LoadScene(_titleSceneName, LoadSceneMode.Single);
        }


        /// <summary>
        /// ゲームオーバー時のリトライボタン押下時の処理
        /// </summary>
        private void OnGameOverRetryButtonClicked()
        {
            Debug.Log("[Result] リトライ要求ぜよ");

            GameResetManager.TriggerFullReset();
        }
    }
}
