// ================================================================================
// File         : FeedbackColor.cs
// Author       : Iwai Shogo
//
// Description  : 猶予時間の残量に応じてコンボテキストのカラーをグラデーション・明滅変化させる演出クラス。
// Created      : 2026-06-08
// ================================================================================
using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    public sealed class FeedbackColor : MonoBehaviour, IComboFeedback
    {
        [Header("--- Color Gradient Settings ---")]
        [Tooltip("猶予がたっぷりある時（1.0以上）の色")]
        [SerializeField] private Color _safeColor = new Color(0.2f, 0.9f, 1.0f);

        [Tooltip("通常の猶予（1.0付近）の時の色")]
        [SerializeField] private Color _normalColor = new Color(1.0f, 0.85f, 0.2f);

        [Tooltip("猶予が尽きそうな時のベース色")]
        [SerializeField] private Color _dangerColor = new Color(1.0f, 0.2f, 0.2f);

        [Header("Flash Settings")]
        [Tooltip("赤色明滅に切り替わる閾値")]
        [SerializeField, Range(0f, 1f)] private float _flashThreshold = 0.25f;

        [Tooltip("ピンチ時の明滅スピード")]
        [SerializeField] private float _flashSpeed = 15.0f;

        private TMPro.TMP_Text _targetText;
        private Color _originalColor;

        public void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText)
        {
            _targetText = comboText;
            if (_targetText != null)
            {
                _originalColor = _targetText.color;
            }
        }

        public void OnUpdate(int currentCombo, float durationRatio)
        {
            if (_targetText == null) return;

            Color finalColor;

            if (durationRatio > 1.0f)
            {
                // 無限累積モード等で1.0を超えている時は、SafeColorとNormalColorの間で少し輝かせる
                float overshootLerp = Mathf.PingPong(Time.time * 2f, 0.3f);
                finalColor = Color.Lerp(_normalColor, _safeColor, overshootLerp + 0.7f);
            }
            else if (durationRatio > _flashThreshold)
            {
                // 通常の減少区間：黄色から赤へ徐々に変化
                float t = (durationRatio - _flashThreshold) / (1f - _flashThreshold);
                finalColor = Color.Lerp(_dangerColor, _normalColor, t);
            }
            else
            {
                // ピンチ区間：赤と白を高速明滅させる
                float flash = Mathf.PingPong(Time.time * _flashSpeed, 1f);
                finalColor = Color.Lerp(_dangerColor, Color.white, flash);
            }

            _targetText.color = finalColor;
        }

        public void OnReset()
        {
            if (_targetText == null) return;
            _targetText.color = _originalColor;
        }
    }
}
