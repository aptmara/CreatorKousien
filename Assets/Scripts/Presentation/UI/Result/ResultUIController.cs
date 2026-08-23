// ================================================================================
// File         : ResultUIController.cs
// Author       : Iwai Shogo
//
// Description  : リザルト画面の文字列書き換えや、クリア・ゲームオーバーの演出切り替えに特化したUIクラス。
// Created      : 2026-07-02
//
// Notes        : ゲームクリアのUI実装します！ - 2026/07/09  Asano
// Notes        : コントローラー対応させました。 - 2026/07/14  Iwai
// Notes        : ゲームおーば演出とUI一律非表示に対応。 - 2026/07/15  Iwai
// ================================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.Core.Management;
using UnityEngine.EventSystems;

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

        [Header("--- Game Clear 演出 ---")]
        [SerializeField] private ResultClearUIAnimator _clearAnimator;

        [Header("--- Game Over 演出 ---")]
        [SerializeField] private ResultGameOverUIAnimator _gameOverAnimator;

        [Header("--- Game Over インタラクション ---")]
        [SerializeField] private Button _gameOverTitleButton;

        private bool _isGameOverActive;


        /// <summary>
        /// 進行管理から渡されたデータに基づいて画面表示を確定させる
        /// </summary>
        public void SetupResultView(GameResultSummary summary, Action onGameClearTitleCallback, Action onGameOverRetryCallback, Action onNextStageCallback)
        {
            // とりあえず演出が決まり切っていないから、文字とActiveの切り替えでベース構築するよん(TODO)
            // クリア時の演出作ります！！ - Asano

            _isGameOverActive = false;

            // 1. リトライボタンのイベント登録
            if (summary.IsGameClear)
            {
                // クリア時の演出を再生する
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(true);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(false);

                // クリア演出の再生
                _clearAnimator?.Play(onGameClearTitleCallback, onNextStageCallback, summary.HasNextStage);
            }
            else
            {
                // ゲームオーバー時の演出を再生する
                if (_gameClearVisualParent != null) _gameClearVisualParent.SetActive(false);
                if (_gameOverVisualParent != null) _gameOverVisualParent.SetActive(true);

                if (_gameOverTitleButton != null)
                {
                    _gameOverTitleButton.onClick.RemoveAllListeners();
                    _gameOverTitleButton.onClick.AddListener(() => onGameOverRetryCallback?.Invoke());

                    _isGameOverActive = true;
                }

                // ゲームオーバーアニメーションの再生
                if (_gameOverAnimator != null)
                {
                    _gameOverAnimator.Play(onGameClearTitleCallback);
                }
                else // アニメーターが設定されていない場合のフォールバック
                {
                    StartCoroutine(DelayFocusRoutine());
                }
            }
        }


        /// <summary>
        /// 演出が終わってUIが完全に表示される時間（約0.5秒〜1秒）待ってから
        /// ボタンを強制フォーカスするコルーチン
        /// </summary>
        private IEnumerator DelayFocusRoutine()
        {
            yield return new WaitForSecondsRealtime(0.5f);

            FocusOnButton();
        }

        private void Update()
        {
            if (_isGameOverActive && EventSystem.current != null)
            {
                if (EventSystem.current.currentSelectedGameObject == null)
                {
                    FocusOnButton();
                }
            }
        }

        private void FocusOnButton()
        {
            if (EventSystem.current != null && _gameOverTitleButton != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
                EventSystem.current.SetSelectedGameObject(_gameOverTitleButton.gameObject);
            }
        }
    }
}
