// ------------------------------------------------------------
// File		: OpeningTextPresenter.cs
// Summary	: 座布団と本文テキストの表示を担当
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.Presentation.Opening
{
    /// <summary>
    /// 座布団と本文テキストの表示を担当
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class OpeningTextPresenter : MonoBehaviour
    {
        [Header("--- 参照 ---")]
        [Tooltip("座布団のルート。RectTransformとCanvasGroupを持つ")]
        [SerializeField] private CanvasGroup _plateGroup;
        [SerializeField] private RectTransform _plateRect;
        [SerializeField] private TextMeshProUGUI _bodyText;

        [Tooltip("▼のルート")]
        [SerializeField] private CanvasGroup _arrowGroup;


        [Header("--- 座布団の出入り ---")]
        [SerializeField, Min(0f)] private float _plateFadeDuration = 0.28f;
        [SerializeField] private float _plateSlideDistance = 60f;


        [Header("--- 本文の出入り ---")]
        [SerializeField, Min(0f)] private float _lineFadeInDuration = 0.12f;
        [SerializeField, Min(0f)] private float _lineFadeOutDuration = 0.18f;


        [Header("--- ▼のバウンド ---")]
        [SerializeField, Min(0.01f)] private float _arrowBounceCycle = 0.8f;
        [SerializeField] private float _arrowBounceHeight = 6f;

        private RectTransform _arrowRect;
        private Vector2 _plateShownPosition;
        private Vector2 _arrowBasePosition;
        private Coroutine _arrowRoutine;

        private void Awake()
        {
            _plateShownPosition = _plateRect.anchoredPosition;

            if (_arrowGroup != null)
            {
                _arrowRect = _arrowGroup.GetComponent<RectTransform>();
                _arrowBasePosition = _arrowRect.anchoredPosition;
            }

            HideImmediate();
        }


        /// <summary>
        /// 座布団と本文を即座に非表示にする
        /// </summary>
        public void HideImmediate()
        {
            _plateGroup.alpha = 0f;
            _bodyText.text = string.Empty;
            _bodyText.alpha = 1f;

            if (_arrowGroup != null)
            {
                _arrowGroup.alpha = 0f;
            }
        }


        /// <summary>
        /// 座布団を下からスライドさせながら表示する
        /// </summary>
        /// <returns></returns>
        public IEnumerator ShowPlateRoutine()
        {
            Vector2 hiddenPosition = _plateShownPosition + Vector2.down * _plateSlideDistance;
            yield return MovePlateRoutine(hiddenPosition, _plateShownPosition, 0f, 1f);
        }


        /// <summary>
        /// 座布団を下へスライドさせながら隠す
        /// </summary>
        /// <returns></returns>
        public IEnumerator HidePlateRoutine()
        {
            StopArrow();
            Vector2 hiddenPosition = _plateShownPosition + Vector2.down * _plateSlideDistance;
            yield return MovePlateRoutine(_plateShownPosition, hiddenPosition, 1f, 0f);
        }


        /// <summary>
        /// 一行を、フェードイン→一文字ずつ表示→入力待ち→フェードアウトの順番！！
        /// </summary>
        /// <param name="line">表示する文章</param>
        /// <param name="script">表示速度など</param>
        /// <param name="submitPressed">決定入力がこのフレームに押されたか</param>
        /// <returns></returns>
        public IEnumerator ShowLineRoutine(string line, OpeningSlideScript script, Func<bool> submitPressed)
        {
            // 先に全文セットしてレイアウトを確定！！
            _bodyText.text = line;
            _bodyText.maxVisibleCharacters = 0;
            _bodyText.ForceMeshUpdate();
            int totalCharacters = _bodyText.textInfo.characterCount;

            yield return FadeTextRoutine(0f, 1f, _lineFadeInDuration);

            bool typingSkipped = false;

            if (script.CharInterval > 0f)
            {
                int visibleCount = 0;
                float nextCharacterIn = 0f;

                while (visibleCount < totalCharacters)
                {
                    if (submitPressed != null && submitPressed())
                    {
                        typingSkipped = true;
                        break;
                    }

                    nextCharacterIn -= Time.unscaledDeltaTime;
                    while (nextCharacterIn <= 0f && visibleCount < totalCharacters)
                    {
                        visibleCount++;
                        nextCharacterIn += script.CharInterval;
                        PlayTypeSe(script, visibleCount);
                    }

                    _bodyText.maxVisibleCharacters = visibleCount;
                    yield return null;
                }
            }

            _bodyText.maxVisibleCharacters = totalCharacters;

            // 全文表示に使った入力で、そのまま次の行へ進まないよう、1フレーム空ける！
            if (typingSkipped)
            {
                yield return null;
            }

            StartArrow();
            yield return WaitAdvanceRoutine(script, submitPressed);
            StopArrow();

            // 次の行の頭で入力を拾いなおさないように1フレームあける
            yield return null;

            yield return FadeTextRoutine(1f, 0f, _lineFadeOutDuration);
        }


        private IEnumerator WaitAdvanceRoutine(OpeningSlideScript script, Func<bool> submitPressed)
        {
            float elapsed = 0f;

            while (true)
            {
                if (submitPressed != null && submitPressed())
                {
                    yield break;
                }

                if (script.AutoAdvanceDelay > 0f)
                {
                    elapsed += Time.unscaledDeltaTime;
                    if (elapsed > script.AutoAdvanceDelay)
                    {
                        yield break;
                    }
                }

                yield return null;
            }
        }


        private void PlayTypeSe(OpeningSlideScript script, int visibleCount)
        {
            if (string.IsNullOrEmpty(script.TypeSeName))
            {
                return;
            }

            // 配列を増やしたときに0で保存されることがあるので、ここで守る
            int interval = Mathf.Max(1, script.TypeSeInterval);
            if (visibleCount % interval != 0)
            {
                return;
            }

            if (SoundManager.instance != null)
            {
                SoundManager.instance.PlaySE(script.TypeSeName);
            }
        }


        private void StartArrow()
        {
            if (_arrowGroup == null)
            {
                return;
            }

            StopArrow();
            _arrowRoutine = StartCoroutine(ArrowBounceRoutine());
        }


        private void StopArrow()
        {
            if (_arrowRoutine != null)
            {
                StopCoroutine(_arrowRoutine);
                _arrowRoutine = null;
            }

            if (_arrowGroup != null)
            {
                _arrowGroup.alpha = 0f;
                _arrowRect.anchoredPosition = _arrowBasePosition;
            }
        }


        private IEnumerator ArrowBounceRoutine()
        {
            _arrowGroup.alpha = 1f;
            float elapsed = 0f;

            while (true)
            {
                elapsed += Time.unscaledDeltaTime;
                float phase = Mathf.Sin(elapsed / _arrowBounceCycle * Mathf.PI * 2f);
                _arrowRect.anchoredPosition = _arrowBasePosition + Vector2.up * (phase * _arrowBounceHeight);
                yield return null;
            }
        }


        private IEnumerator MovePlateRoutine(Vector2 from, Vector2 to, float fromAlpha, float toAlpha)
        {
            float elapsed = 0f;

            while (elapsed < _plateFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = OpeningEase.SmoothStep(Mathf.Clamp01(elapsed / _plateFadeDuration));
                _plateGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
                _plateRect.anchoredPosition = Vector2.Lerp(from, to, t);
                yield return null;
            }

            _plateGroup.alpha = toAlpha;
            _plateRect.anchoredPosition = to;
        }


        private IEnumerator FadeTextRoutine(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                _bodyText.alpha = to;
                yield break;
            }


            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _bodyText.alpha = Mathf.Lerp(from, to, OpeningEase.SmoothStep(Mathf.Clamp01(elapsed / duration)));

                yield return null;
            }

            _bodyText.alpha = to;
        }
    }
}


