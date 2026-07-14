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

        [Tooltip("ゲームクリアから流用する魂のRectTransform配列")]
        [SerializeField] private RectTransform[] _souls;

        [Tooltip("3D空間のプレイヤー位置に対応させる、魂の発生基点となるUI上の位置")]
        [SerializeField] private RectTransform _player3DAnchor;

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

        [Header("--- 魂の湧き出し・ループ設定 ---")]
        [Tooltip("魂が次に湧き出るまでの一定間隔（秒）")]
        [SerializeField] private float _soulSpawnInterval = 0.35f;
        [Tooltip("魂の上昇速度")]
        [SerializeField] private float _soulRiseSpeed = 160f;
        [Tooltip("魂のフェードアウト（消滅）速度")]
        [SerializeField] private float _soulFadeOutSpeed = 1.2f;

        private Vector2 _mainBoardPos;
        private Vector2 _smallBoardPos;
        private Vector2[] _charInitialPositions;
        private CanvasGroup[] _charCanvasGroups;
        private CanvasGroup _smallBoardCanvasGroup;

        // 魂ループ管理用変数
        private CanvasGroup[] _soulCanvasGroups;
        private Vector2[] _soulActivePositions;
        private float[] _soulSpeeds; // 個別の速度変化用

        private bool _isShakingActive;
        private bool _isSoulsActive;

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

            if (_souls != null)
            {
                _soulCanvasGroups = new CanvasGroup[_souls.Length];
                _soulActivePositions = new Vector2[_souls.Length];
                _soulSpeeds = new float[_souls.Length];
                for (int i = 0; i < _souls.Length; i++)
                {
                    if (_souls[i] != null) _soulCanvasGroups[i] = GetOrAddCanvasGroup(_souls[i]);
                }
            }
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
            _isSoulsActive = false;

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

            if (_souls != null)
            {
                for (int i = 0; i < _souls.Length; i++)
                {
                    if (_souls[i] != null)
                    {
                        _souls[i].gameObject.SetActive(false);
                        if (_soulCanvasGroups[i] != null) _soulCanvasGroups[i].alpha = 0f;
                    }
                }
            }
        }

        private IEnumerator PlayRoutine()
        {
            // 1. 画面全体のダーク絶望フェード開始
            StartCoroutine(PlayDarkFadeRoutine());

            // 2. メインボードが落下
            yield return PlayMainBoardDropRoutine();

            // 3. 魂の永続湧き出しループを開始
            StartCoroutine(PlaySoulsRiseLoopRoutine());

            // 4. GameOverの文字が1文字ずつじわ〜っっっと表示
            _isShakingActive = true;
            yield return PlayCharsFadeInRoutine();

            // 5. 小さいボードを表示
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

        private IEnumerator PlaySoulsRiseLoopRoutine()
        {
            if (_souls == null || _souls.Length == 0 || _player3DAnchor == null) yield break;
            _isSoulsActive = true;

            int currentPoolIndex = 0;

            while (_isSoulsActive)
            {
                RectTransform currentSoul = _souls[currentPoolIndex];
                CanvasGroup currentCg = _soulCanvasGroups[currentPoolIndex];

                if (currentSoul != null && currentCg != null)
                {
                    Vector2 spawnPos = _player3DAnchor.anchoredPosition;
                    _soulActivePositions[currentPoolIndex] = spawnPos + new Vector2(UnityEngine.Random.Range(-35f, 30f), UnityEngine.Random.Range(-15f, 15f));
                    _soulSpeeds[currentPoolIndex] = _soulRiseSpeed * UnityEngine.Random.Range(0.85f, 1.25f); // 速度のゆらぎ

                    currentSoul.anchoredPosition = _soulActivePositions[currentPoolIndex];
                    currentCg.alpha = 1f;
                    currentSoul.gameObject.SetActive(true);
                }

                // インデックスを循環
                currentPoolIndex = (currentPoolIndex + 1) % _souls.Length;

                yield return new WaitForSecondsRealtime(_soulSpawnInterval);
            }
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

            if (_isSoulsActive && _souls != null)
            {
                for (int i = 0; i < _souls.Length; i++)
                {
                    if (_souls[i] == null || !_souls[i].gameObject.activeSelf) continue;

                    _soulActivePositions[i].y += _soulSpeeds[i] * deltaTime;
                    float waveX = Mathf.Sin(unscaledTime * 2.5f + i) * 18f;

                    _souls[i].anchoredPosition = new Vector2(_soulActivePositions[i].x + waveX, _soulActivePositions[i].y);

                    // 徐々にフェードアウト
                    if (_soulCanvasGroups[i] != null)
                    {
                        _soulCanvasGroups[i].alpha -= _soulFadeOutSpeed * deltaTime;
                        if (_soulCanvasGroups[i].alpha <= 0f)
                        {
                            _souls[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        private void OnDestroy()
        {
            _isSoulsActive = false;
        }

        private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            CanvasGroup cg = target.GetComponent<CanvasGroup>();
            if (cg == null) cg = target.gameObject.AddComponent<CanvasGroup>();
            return cg;
        }
    }
}
