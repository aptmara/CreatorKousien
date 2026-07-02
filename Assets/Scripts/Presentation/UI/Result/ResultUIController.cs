// ================================================================================
// File         : ResultUIController.cs
// Author       : Iwai Shogo
//
// Description  : リザルト画面の文字列書き換えや、クリア・ゲームオーバーの演出切り替えに特化したUIクラス。
// Created      : 2026-07-02
// ================================================================================

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core.Management;

namespace Game.Presentation.UI.Result
{
    /// <summary>
    /// リザルト画面の文字列書き換えや、クリア・ゲームオーバーの演出切り替えに特化したUIクラス
    /// </summary>
    public class ResultUIController : MonoBehaviour
    {
        [Header("--- 演出オブジェクトの切り替え用 ---")]
        [SerializeField] private GameObject _gameClearVisualParent;
        [SerializeField] private GameObject _gameOverVisualParent;

        [Header("--- テキスト情報表示用 ---")]
        [SerializeField] private TextMeshProUGUI _resultStatusText;
        [SerializeField] private TextMeshProUGUI _waveCountText;
        [SerializeField] private TextMeshProUGUI _defenseLineHpText;

        [Header("--- インタラクション ---")]
        [SerializeField] private Button _retryButton;

        /// <summary>
        /// 進行管理から渡されたデータに基づいて画面表示を確定させる
        /// </summary>
        public void SetupResultView(GameResultSummary summary, Action onRetryCallback)
        {
            // 1. とりあえず演出が決まり切っていないから、文字とActiveの切り替えでベース構築するよん(TODO)
            if (summary.IsGameClear)
            {
                if (_resultStatusText != null) _resultStatusText.text = "GAME CLEAR !!";
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(true);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(false);
            }
            else
            {
                if (_resultStatusText != null) _resultStatusText.text = "GAME OVER ^^";
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(false);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(true);
            }

            // 2. 渡された全データをテキストに反映
            if (_waveCountText != null)
            {
                _waveCountText.text = $"Wave: {summary.LastClearedWaveIndex + 1}";
            }

            if (_defenseLineHpText != null)
            {
                _defenseLineHpText.text = $"Defense Line Current HP: {summary.RemainingDefenseLineHp:F1}";
            }

            // 3. リトライボタンのイベント登録
            if (_retryButton != null)
            {
                _retryButton.onClick.RemoveAllListeners();
                _retryButton.onClick.AddListener(() => onRetryCallback?.Invoke());
            }
        }
    }
}
