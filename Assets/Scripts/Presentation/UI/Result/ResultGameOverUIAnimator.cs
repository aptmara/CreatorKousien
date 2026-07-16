// ================================================================================
// File         : ResultGameOverUIAnimator.cs
// Author       : Iwai Shogo
//
// Description  : ゲームオーバー時の不気味なアニメーションを制御するクラス。
// Created      : 2026-07-15
// ================================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Game.Presentation.UI.Result
{
    public class ResultGameOverUIAnimator : MonoBehaviour
    {
        [Header("--- UIオブジェクト参照 ---")]
        [Tooltip("メインボード（GameOver文字の座布団）のRectTransform")]
        [SerializeField] private RectTransform _mainBoard;

        [Tooltip("小さいボード（ボタンの座布団）のRectTransform")]
        [SerializeField] private RectTransform _smallBoard;

        [Tooltip("Back to Titleボタン")]
        [SerializeField] private Button _titleButton;

        [Tooltip("GameOverを構成する1文字ずつのRectTransform配列")]
        [SerializeField] private RectTransform[] _charTransforms;


        [Header("--- 演出アセット参照 ---")]
        [Tooltip("画面全体を絶望色に染めるダークパネルのCanvasGroup")]
        [SerializeField] private CanvasGroup _darkFadePanel;


        [Header("--- アニメーション速度設定 ---")]
        [Tooltip("メインボードの落下時間")]
        [SerializeField] private float _mainBoardDropDuration = 0.65f;
        [Tooltip("メインボードの落下開始オフセットY")]
        [SerializeField] private float _mainBoardDropOffsetY = 900f;

        [Tooltip("1文字あたりのフェードイン時間")]
        [SerializeField] private float _charFadeDuration = 0.5f;
        [Tooltip("次の文字がフェードインし始めるまでのディレイ")]
        [SerializeField] private float _charFadeInterval = 0.15f;

        [Tooltip("小さいボードの表示フェード時間")]
        [SerializeField] private float _smallBoardFadeDuration = 0.4f;
        [SerializeField] private float _darkFadeDuration = 0.8f;


        [Header("--- 不気味な揺れ＆グリッチ設定 ---")]
        [SerializeField] private Vector2 _shakeAmplitude = new Vector2(6f, 10f);
        [SerializeField] private Vector2 _shakeSpeed = new Vector2(3.0f, 2.2f);
        [Range(0f, 1f)][SerializeField] private float _glitchChance = 0.05f;
        [SerializeField] private float _glitchStrength = 25f;


        private Vector2 _mainBoardPos;
        private Vector2 _smallBoardPos;
        private Vector2[] _charInitialPositions;
        private CanvasGroup[] _charCanvasGroups;
        private CanvasGroup _smallBoardCanvasGroup;

        private bool _isShakingActive;

        private void Awake()
        {
            CaptureFinalPositions();
        }

        private void CaptureFinalPositions()
        {
            if (_mainBoard != null) _mainBoardPos = _mainBoard.anchoredPosition;
            if (_smallBoard != null) _smallBoardPos = _smallBoard.anchoredPosition;

            if (_charTransforms != null)
            {
                _charInitialPositions = new Vector2[_charTransforms.Length];
                _charCanvasGroups = new CanvasGroup[_charTransforms.Length];
                for (int i = 0; i < _charTransforms.Length; i++)
                {
                    if (_charTransforms[i] != null)
                    {
                        _charInitialPositions[i] = _charTransforms[i].anchoredPosition;
                        _charCanvasGroups[i] = GetOrAddCanvasGroup(_charTransforms[i]);
                    }
                }
            }

            if (_smallBoard != null) _smallBoardCanvasGroup = GetOrAddCanvasGroup(_smallBoard);
        }

        public void Play(Action onTitleClicked)
        {
            StopAllCoroutines();
            ResetView();

            if (_titleButton != null)
            {
                _titleButton.interactable = false;
                _titleButton.onClick.RemoveAllListeners();
                _titleButton.onClick.AddListener(() => onTitleClicked?.Invoke());
            }

            StartCoroutine(PlayRoutine());
        }

        private void ResetView()
        {
            _isShakingActive = false;

            if (_darkFadePanel != null)
            {
                _darkFadePanel.alpha = 0f;
                _darkFadePanel.gameObject.SetActive(true);
            }

            if (_mainBoard != null) _mainBoard.anchoredPosition = _mainBoardPos + Vector2.up * _mainBoardDropOffsetY;

            if (_smallBoard != null)
            {
                _smallBoard.gameObject.SetActive(true);
                if (_smallBoardCanvasGroup != null) _smallBoardCanvasGroup.alpha = 0f;
            }

            if (_charCanvasGroups != null)
            {
                for (int i = 0; i < _charCanvasGroups.Length; i++)
                {
                    if (_charCanvasGroups[i] != null) _charCanvasGroups[i].alpha = 0f;
                }
            }
        }

        private IEnumerator PlayRoutine()
        {
            // 1. 画面全体のダーク絶望フェード開始
            StartCoroutine(PlayDarkFadeRoutine());

            // 2. メインボードが落下
            yield return PlayMainBoardDropRoutine();

            // 3. GameOverの文字が1文字ずつじわ〜っっっと表示
            _isShakingActive = true;
            yield return PlayCharsFadeInRoutine();

            // 4. 小さいボードを表示
            yield return PlaySmallBoardAppearRoutine();

            // ボタンの有効化
            if (_titleButton != null)
            {
                _titleButton.interactable = true;
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                    EventSystem.current.SetSelectedGameObject(_titleButton.gameObject);
                }
            }
        }

        private IEnumerator PlayDarkFadeRoutine()
        {
            if (_darkFadePanel == null) yield break;
            float time = 0f;
            while (time < _darkFadeDuration)
            {
                time += Time.unscaledDeltaTime;
                _darkFadePanel.alpha = Mathf.Clamp01(time / _darkFadeDuration);
                yield return null;
            }
        }

        private IEnumerator PlayMainBoardDropRoutine()
        {
            if (_mainBoard == null) yield break;
            Vector2 start = _mainBoardPos + Vector2.up * _mainBoardDropOffsetY;
            float time = 0f;
            while (time < _mainBoardDropDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = Mathf.Sin(time / _mainBoardDropDuration * Mathf.PI * 0.5f);
                _mainBoard.anchoredPosition = Vector2.LerpUnclamped(start, _mainBoardPos, t);
                yield return null;
            }
            _mainBoard.anchoredPosition = _mainBoardPos;
        }

        private IEnumerator PlayCharsFadeInRoutine()
        {
            if (_charTransforms == null) yield break;
            for (int i = 0; i < _charTransforms.Length; i++)
            {
                if (_charCanvasGroups[i] != null) StartCoroutine(FadeCharIn(_charCanvasGroups[i]));
                yield return new WaitForSecondsRealtime(_charFadeInterval);
            }
            yield return new WaitForSecondsRealtime(_charFadeDuration);
        }

        private IEnumerator FadeCharIn(CanvasGroup cg)
        {
            float time = 0f;
            while (time < _charFadeDuration)
            {
                time += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(time / _charFadeDuration);
                yield return null;
            }
            cg.alpha = 1f;
        }

        private IEnumerator PlaySmallBoardAppearRoutine()
        {
            if (_smallBoardCanvasGroup == null) yield break;
            float time = 0f;
            while (time < _smallBoardFadeDuration)
            {
                time += Time.unscaledDeltaTime;
                _smallBoardCanvasGroup.alpha = Mathf.Clamp01(time / _smallBoardFadeDuration);
                yield return null;
            }
        }

        private void Update()
        {
            float unscaledTime = Time.unscaledTime;
            float deltaTime = Time.unscaledDeltaTime;

            // 文字揺れ＆グリッチ
            if (_isShakingActive && _charTransforms != null)
            {
                bool triggerGlitch = UnityEngine.Random.value < _glitchChance;
                for (int i = 0; i < _charTransforms.Length; i++)
                {
                    if (_charTransforms[i] == null) continue;

                    float phase = i * 0.8f;
                    float offsetX = Mathf.Sin(unscaledTime * _shakeSpeed.x + phase) * _shakeAmplitude.x;
                    float offsetY = Mathf.Cos(unscaledTime * _shakeSpeed.y + phase) * _shakeAmplitude.y;

                    if (triggerGlitch)
                    {
                        offsetX += UnityEngine.Random.Range(-_glitchStrength, _glitchStrength);
                        offsetY += UnityEngine.Random.Range(-_glitchStrength, _glitchStrength);
                    }

                    _charTransforms[i].anchoredPosition = _charInitialPositions[i] + new Vector2(offsetX, offsetY);
                }
            }
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
