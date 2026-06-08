// ================================================================================
// File         : FeedbackScale.cs
// Author       : Iwai Shogo
//
// Description  : 猶予時間の残量に応じてコンボテキストのサイズを変動・明滅させる演出クラス。
// Created      : 2026-06-08
// ================================================================================

using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    public class FeedbackScale : MonoBehaviour, IComboFeedback
    {
        [Header("--- Scale Settings ---")]
        [Tooltip("猶予満タン時の最大スケール倍率")]
        [SerializeField] private float _maxScale = 1.3f;

        [Tooltip("猶予ゼロに近づいた時の最小スケール倍率")]
        [SerializeField] private float _minScale = 0.8f;

        [Header("--- Jitter Settings ---")]
        [Tooltip("文字が震え始める猶予残り割合の閾値（0.0～1.0）")]
        [SerializeField, Range(0f, 1f)] private float _jitterThreshold = 0.3f;

        [Tooltip("震えの激しさ（ピクセル単位のズレ幅）")]
        [SerializeField] private float _jitterStrength = 8.0f;

        private RectTransform _textRect;
        private Vector3 _originalPosition;

        public void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText)
        {
            _textRect = comboTextRect;
            if (_textRect != null)
            {
                _originalPosition = _textRect.anchoredPosition;
            }
        }

        public void OnUpdate(int currentCombo, float durationRatio)
        {
            if (_textRect == null) return;

            // 1. 猶予割合に応じてスケールを計算
            // 1.0以上の時はMaxScaleで固定
            float clampedRatio = Mathf.Clamp01(durationRatio);
            float targetScale = Mathf.Lerp(_minScale, _maxScale, clampedRatio);
            _textRect.localScale = Vector3.one * targetScale;

            // 2. 猶予が残り少なくなったら震わせる演出
            if (durationRatio <= _jitterThreshold && durationRatio > 0f)
            {
                // 残り時間が少ないほど震えを強くする
                float dangerFactor = 1f - (durationRatio / _jitterThreshold);
                float offsetX = Random.Range(-1f, 1f) * _jitterStrength * dangerFactor;
                float offsetY = Random.Range(-1f, 1f) * _jitterStrength * dangerFactor;

                _textRect.anchoredPosition = _originalPosition + new Vector3(offsetX, offsetY, 0f);
            }
            else
            {
                _textRect.anchoredPosition = _originalPosition;
            }
        }

        public void OnReset()
        {
            if (_textRect == null) return;
            _textRect.localScale = Vector3.one;
            _textRect.anchoredPosition = _originalPosition;
        }
    }
}
