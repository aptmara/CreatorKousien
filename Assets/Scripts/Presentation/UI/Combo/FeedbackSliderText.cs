// ================================================================================
// File         : FeedbackSliderText.cs
// Author       : Iwai Shogo
//
// Description  : 文字を2重に配置し、RectMask2Dのサイズをコントロールすることで文字自体をゲージとして機能させる演出クラス
// Created      : 2026-06-08
// ================================================================================

using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Combo
{
    public sealed class FeedbackSliderText : MonoBehaviour, IComboFeedback
    {
        [Header("--- Mask Settings ---")]
        [Tooltip("RectMask2DコンポーネントがついているMaskContainerオブジェクト")]
        [SerializeField] private RectMask2D _rectMask;

        private float _maxHeight;
        private RectTransform _maskRectTransform;

        public void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText)
        {
            if (_rectMask == null)
            {
                _rectMask = GetComponentInChildren<RectMask2D>();
            }

            if (_rectMask != null)
            {
                _maskRectTransform = _rectMask.GetComponent<RectTransform>();
                // マスクの本来の最大高さを取得
                _maxHeight = _maskRectTransform.rect.height;

                // 開始時は空
                SetMaskPaddingRatio(0f);
            }
        }

        public void OnUpdate(int currentCombo, float durationRatio)
        {
            if (_rectMask == null) return;

            // 猶予割合（0.0 ～ 1.0）
            float clampedRatio = Mathf.Clamp01(durationRatio);

            // 割合に応じてマスクをかける
            SetMaskPaddingRatio(clampedRatio);
        }

        public void OnReset()
        {
            SetMaskPaddingRatio(0f);
        }

        /// <summary>
        /// マスクの高さを割合でカットする処理
        /// </summary>
        private void SetMaskPaddingRatio(float ratio)
        {
            if (_rectMask == null) return;

            // 方向反転ロジック
            float bottomPadding = _maxHeight * (1f - ratio);

            Vector4 padding = new Vector4(0f, 0f, 0f, bottomPadding);
            _rectMask.padding = padding;
        }

        private void OnDisable()
        {
            // オフにされたときは、中身が全部見えるように100%に戻しておく
            if (_rectMask != null)
            {
                _rectMask.padding = Vector4.zero;
            }
        }
    }
}
