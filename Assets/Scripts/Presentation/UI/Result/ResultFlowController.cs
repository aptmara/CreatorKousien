// ================================================================================
// File         : ResultFlowController.cs
// Author       : Iwai Shogo
//
// Description  : 加算ロードされたリザルトシーン全体の進行とイベントを統括する
// Created      : 2026-07-02
// ================================================================================

using UnityEngine;
using Game.Core.Management;

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

        void Start()
        {
            if (_uiController == null) _uiController = GetComponentInChildren<ResultUIController>();

            // GameProgressionManager からパッキングされたデータを引き抜く
            if (GameProgressionManager.Instance != null && GameProgressionManager.Instance.ResultSummary != null )
            {
                GameResultSummary data = GameProgressionManager.Instance.ResultSummary;

                // UIクラスに対して、表示情報の更新と演出切り替えを委譲
                _uiController.SetupResultView(data, OnRetryButtonClicked);
            }
            else
            {
                Debug.LogWarning("[Result] プレイデータ(ResultSummary)が見つかりません。デバッグ起動用の仮表示ぜよ。。。");
                _uiController.SetupResultView(new GameResultSummary(true, 2, 30f), OnRetryButtonClicked);
            }
        }

        private void OnRetryButtonClicked()
        {
            Debug.Log("[Result] リトライ要求！再起動フロー実行ぜよ。");

            // クリーンアップはマネージャーに委譲
            GameResetManager.TriggerFullReset();
        }
    }
}
