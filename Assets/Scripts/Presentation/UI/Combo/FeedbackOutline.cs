// ================================================================================
// File         : FeedbackOutline.cs
// Author       : Iwai Shogo
//
// Description  : コンボテキストを囲む、または背後に配置する円形進行ゲージを制御する演出クラス
// Created      : 2026-06-08
// ================================================================================

using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    [RequireComponent(typeof(TMPro.TMP_Text))]
    public sealed class FeedbackOutline : MonoBehaviour, IComboFeedback
    {
        public enum GaugeStyle
        {
            ShrinkOutline,
            GrowOutline
        }

        [Header("--- Outline Style Settings ---")]
        [Tooltip("アウトラインゲージの表現スタイル")]
        [SerializeField] private GaugeStyle _gaugeStyle = GaugeStyle.ShrinkOutline;

        [Tooltip("ゲージ満タン時のアウトラインの太さ")]
        [SerializeField, Range(0f, 1f)] private float _maxOutlineWidth = 0.6f;

        [Tooltip("ゲージ空っぽ時のアウトラインの太さ")]
        [SerializeField, Range(0f, 1f)] private float _minOutlineWidth = 0.0f;

        [Header("Material Color Link")]
        [Tooltip("猶予残量に応じてアウトラインの色そのものも変化させるか")]
        [SerializeField] private bool _useColorGradient = true;
        [SerializeField] private Gradient _outlineColorGradient;

        private TMPro.TMP_Text _targetText;
        private Material _instancedMaterial;
        private float _originalOutlineWidth;
        private Color _originalOutlineColor;

        // シェーダープロパティのIDキャッシュ（高速化のため）
        private static readonly int ShaderOutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int ShaderOutlineColorId = Shader.PropertyToID("_OutlineColor");

        public void Initialize(RectTransform comboTextRect, TMPro.TMP_Text comboText)
        {
            _targetText = comboText;
            if (_targetText == null) return;

            // 1. 共有マテリアルを複製して、このコンボ文字専用にする
            _instancedMaterial = _targetText.fontSharedMaterial = new Material(_targetText.fontSharedMaterial);

            // 2. 初期値の退避
            if (_instancedMaterial.HasProperty(ShaderOutlineWidthId))
            {
                _originalOutlineWidth = _instancedMaterial.GetFloat(ShaderOutlineWidthId);
            }
            if (_instancedMaterial.HasProperty(ShaderOutlineColorId))
            {
                _originalOutlineColor = _instancedMaterial.GetColor(ShaderOutlineColorId);
            }
        }

        public void OnUpdate(int currentCombo, float durationRatio)
        {
            if (_instancedMaterial == null) return;

            float clampedRatio = Mathf.Clamp01(durationRatio);
            float targetWidth;

            // 1. スタイルに応じたアウトラインの太さ計算
            if (_gaugeStyle == GaugeStyle.ShrinkOutline)
            {
                // 時間が減ると細くなる
                targetWidth = Mathf.Lerp(_minOutlineWidth, _maxOutlineWidth, clampedRatio);
            }
            else
            {
                // 時間が減ると太くなる
                targetWidth = Mathf.Lerp(_maxOutlineWidth, _minOutlineWidth, clampedRatio);
            }

            // マテリアルへ太さを反映
            _instancedMaterial.SetFloat(ShaderOutlineWidthId, targetWidth);

            // 2. 色のグラデーション変化
            if (_useColorGradient)
            {
                Color targetColor = _outlineColorGradient.Evaluate(clampedRatio);

                // 無限累積モードで猶予が1.0を超えている時は、フチを少しフラッシュさせるエフェクト
                if (durationRatio > 1.0f)
                {
                    targetColor = Color.Lerp(targetColor, Color.white, Mathf.PingPong(Time.time * 4f, 0.4f));
                }

                _instancedMaterial.SetColor(ShaderOutlineColorId, targetColor);
            }

            // TMPコンポーネント側にマテリアルが更新されたことを通知して再描画
            _targetText.UpdateMeshPadding();
        }

        private void OnDisable()
        {
            if (_instancedMaterial != null)
            {
                _instancedMaterial.SetFloat(ShaderOutlineWidthId, _originalOutlineWidth);
                _instancedMaterial.SetColor(ShaderOutlineColorId, _originalOutlineColor);
                if (_targetText != null)
                {
                    _targetText.UpdateMeshPadding();
                }
            }
        }

        public void OnReset()
        {
            if (_instancedMaterial == null) return;

            // 元の状態に戻す
            _instancedMaterial.SetFloat(ShaderOutlineWidthId, _originalOutlineWidth);
            _instancedMaterial.SetColor(ShaderOutlineColorId, _originalOutlineColor);
            if (_targetText != null) _targetText.UpdateMeshPadding();
        }

        private void OnDestroy()
        {
            if (_instancedMaterial != null)
            {
                Destroy(_instancedMaterial);
            }
        }
    }
}
