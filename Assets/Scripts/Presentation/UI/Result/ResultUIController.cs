// ================================================================================
// File         : ResultUIController.cs
// Author       : Iwai Shogo
//
// Description  : リザルト画面の文字列書き換えや、クリア・ゲームオーバーの演出切り替えに特化したUIクラス。
// Created      : 2026-07-02
//
// Notes        : ゲームクリアのUI実装します！ - 2026/07/09  Asano
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

        [Header("--- Game Clear 演出 ---")]
        [SerializeField] private ResultClearUIAnimator _clearAnimator;

        [Header("--- Game Over インタラクション ---")]
        [SerializeField] private Button _gameOverRetryButton;

        [Header("--- Game Clear 時に隠す既存UI ---")]
        [SerializeField] private GameObject[] _hideOnGameClearObjects;



        /// <summary>
        /// 進行管理から渡されたデータに基づいて画面表示を確定させる
        /// </summary>
        public void SetupResultView(GameResultSummary summary, Action onGameClearTitleCallback, Action onGameOverRetryCallback)
        {
            // とりあえず演出が決まり切っていないから、文字とActiveの切り替えでベース構築するよん(TODO)
            // クリア時の演出作ります！！ - Asano

            // 1. ゲームクリア時に隠すUIを非表示にする
            foreach (GameObject obj in _hideOnGameClearObjects)
            {
                if (obj != null)
                {
                    obj.SetActive(!summary.IsGameClear);
                }
            }


            // 2. 渡された全データをテキストに反映
            if (_waveCountText != null)
            {
                _waveCountText.text = $"Wave: {summary.LastClearedWaveIndex + 1}";
                _waveCountText.gameObject.SetActive(!summary.IsGameClear);
            }

            if (_defenseLineHpText != null)
            {
                _defenseLineHpText.text = $"Defense Line Current HP: {summary.RemainingDefenseLineHp:F1}";
                _defenseLineHpText.gameObject.SetActive(!summary.IsGameClear);
            }


            // 3. リトライボタンのイベント登録
            if (summary.IsGameClear)
            {
                // クリア時の演出を再生する
                if (_resultStatusText != null) _resultStatusText.text = "GAME CLEAR";
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(true);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(false);

                _clearAnimator?.Play(onGameClearTitleCallback);
            }
            else
            {
                // ゲームオーバー時の演出を再生する
                if (_resultStatusText != null) _resultStatusText.text = "GAME OVER ^^";
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(false);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(true);

                if (_gameOverRetryButton != null)
                {
                    _gameOverRetryButton.onClick.RemoveAllListeners();
                    _gameOverRetryButton.onClick.AddListener(() => onGameOverRetryCallback?.Invoke());
                }
            }
        }
    }
}
