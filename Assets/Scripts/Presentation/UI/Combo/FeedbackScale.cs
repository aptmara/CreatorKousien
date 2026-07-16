// ================================================================================
// File         : FeedbackScale.cs
// Author       : Iwai Shogo
//
// Description  : 猶予時間の残量に応じてコンボテキストのサイズを変動・明滅させる演出クラス。
// Updated      : 2026-07-16 (ActivePopupFeedbackBridgeのパンチスケールをインテリジェントに乗算合成)
// ================================================================================

using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    public class FeedbackScale : MonoBehaviour, IComboFeedback
    {
        [Header("--- Scale Settings ---")]
        [SerializeField] private float _maxScale = 1.3f;
        [SerializeField] private float _minScale = 0.8f;

        [Header("--- Jitter Settings ---")]
        [SerializeField, Range(0f, 1f)] private float _jitterThreshold = 0.3f;
        [SerializeField] private float _jitterStrength = 8.0f;

        private RectTransform _textRect;
        private Vector3 _originalPosition;

        private ActivePopupFeedbackBridge _bridgeCache;

        public void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText)
        {
            _textRect = comboTextRect;
            if (_textRect != null)
            {
                _originalPosition = _textRect.anchoredPosition;
            }

            // コンポーネントを動的に解決
            _bridgeCache = GetComponentInParent<ActivePopupFeedbackBridge>();
        }

        public void OnUpdate(int currentCombo, float durationRatio)
        {
            if (_textRect == null) return;

            // 1. 猶予割合によるベーススケール計算
            float clampedRatio = Mathf.Clamp01(durationRatio);
            float baseScale = Mathf.Lerp(_minScale, _maxScale, clampedRatio);

            // 2. ブリッジの瞬間巨大化パンチ倍率をブレンド
            float finalPunchMultiplier = (_bridgeCache != null) ? _bridgeCache.CurrentPunchScaleMultiplier : 1.0f;
            _textRect.localScale = Vector3.one * (baseScale * finalPunchMultiplier);

            // 3. ジッター演出
            if (durationRatio <= _jitterThreshold && durationRatio > 0f)
            {
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
