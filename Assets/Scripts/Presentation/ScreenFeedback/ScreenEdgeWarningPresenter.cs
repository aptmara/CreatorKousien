// ------------------------------------------------------------
// File		: Assets/Scripts/Presentation/ScreenFeedback/ScreenEdgeWarningPresenter.cs
// Summary	: イバラタックル接近中に画面のフチを赤くゆっくり点滅させるPresenter
//
// Author	: [浅野勇生]
// Created	: 2026-07-17
//
// Notes	:
// -
// ------------------------------------------------------------
using System.Collections;
using Game.Core.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.ScreenFeedback
{
    /// <summary>
    /// イバラタックル接近中に画面のフチを赤くゆっくり点滅させるPresenter
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ScreenEdgeWarningPresenter : MonoBehaviour
    {
        [Header("--- 参照 ---")]

        [SerializeField]
        [Tooltip("画面全体に広げたビネット用のImage")]
        private Image _vignetteImage;

        [Header("--- 点滅設定 ---")]

        [SerializeField]
        [Tooltip("警告の色")]
        private Color _warningColor = new Color(0.85f, 0.1f, 0.1f, 1f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("点滅時の最小アルファ")]
        private float _minAlpha = 0.15f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("点滅時の最大アルファ")]
        private float _maxAlpha = 0.5f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("点滅の周期（秒）")]
        private float _blinkCycleDuration = 1.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("開始・終了時のフェード秒数")]
        private float _fadeDuration = 0.3f;

        private Coroutine _blinkRoutine;

        private void Awake()
        {
            if (_vignetteImage != null)
            {
                // 演出用UIなのでクリックを吸わないようにする
                _vignetteImage.raycastTarget = false;
                SetAlpha(0f);
            }
        }


        private void OnEnable()
        {
            EventBus.Subscribe<BossThornWarningStartedEvent>(HandleWarningStarted);

            EventBus.Subscribe<BossThornWarningEndedEvent>(HandleWarningEnded);
        }


        private void OnDisable()
        {
            EventBus.Unsubscribe<BossThornWarningStartedEvent>(HandleWarningStarted);
            EventBus.Unsubscribe<BossThornWarningEndedEvent>(HandleWarningEnded);

            StopBlink();
            SetAlpha(0f);
        }


        private void HandleWarningStarted(BossThornWarningStartedEvent _)
        {
            StopBlink();
            _blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        private void HandleWarningEnded(BossThornWarningEndedEvent _)
        {
            StopBlink();
            _blinkRoutine = StartCoroutine(FadeOutRoutine());
        }


        /// <summary>
        /// フェードインした後サイン波でゆっくり点滅
        /// </summary>
        /// <returns></returns>
        private IEnumerator BlinkRoutine()
        {
            // フェードイン
            float startAlpha = _vignetteImage != null ? _vignetteImage.color.a : 0f;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                SetAlpha(Mathf.Lerp(startAlpha, _minAlpha, elapsed / _fadeDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            // 点滅
            float blinkTime = 0f;
            while (true)
            {
                float wave = (Mathf.Sin(blinkTime / _blinkCycleDuration * Mathf.PI * 2f - Mathf.PI * 0.5f) + 1f) * 0.5f;
                SetAlpha(Mathf.Lerp(_minAlpha, _maxAlpha, wave));

                blinkTime += Time.deltaTime;
                yield return null;
            }
        }


        /// <summary>
        /// フェードアウトのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator FadeOutRoutine()
        {
            float startAlpha = _vignetteImage != null ? _vignetteImage.color.a : 0f;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                SetAlpha(Mathf.Lerp(startAlpha, 0f, elapsed / _fadeDuration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetAlpha(0f);
            _blinkRoutine = null;
        }


        /// <summary>
        /// アルファ値を設定
        /// </summary>
        /// <param name="alpha"></param>
        private void SetAlpha(float alpha)
        {
            if (_vignetteImage == null)
            {
                return;
            }

            Color color = _warningColor;
            color.a = alpha;
            _vignetteImage.color = color;
        }


        /// <summary>
        /// 警告開始イベントを受け取ったときの処理
        /// </summary>
        private void StopBlink()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }
    }
}
