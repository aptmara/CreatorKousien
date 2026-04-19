// ================================================================================
// File         : TestTimerView.cs
// Author       : Iwai Shogo
//
// Description  : コマンドフェーズのタイマーを視覚的に表現する。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;
using CreatorKousien.Core;

namespace CreatorKousien.View
{
    public class TestTimerView : MonoBehaviour
    {
        [Header("サイズ設定")]
        [Tooltip("タイマーが最大値の時のScaleのX値")]
        [SerializeField] private float _maxScaleX = 10f;
        [SerializeField] private Vector3 _baseScale = new Vector3(1f, 0.5f, 0.5f);

        [Header("カラー設定")]
        [Tooltip("残り時間に応じた色の変化 (右:100%の時の色, 左:0%の時の色)")]
        [SerializeField] private Gradient _timeGradient;

        private Transform _timerTransform;
        private Material _material;

        private void Awake()
        {
            _timerTransform = transform;

            // マテリアルのキャッシュ
            Renderer rend = GetComponent<Renderer>();
            if (rend != null)
            {
                _material = rend.material;
            }

            SetProgress(0f);
        }

        public void Initialize(GameEventBus eventBus)
        {
            eventBus.OnCommandPhaseStarted += ShowTimer;
            eventBus.OnCommandTimerUpdated += UpdateTimerView;
            eventBus.OnCommandTimeUp += HideTimer;
        }

        private void ShowTimer()
        {
            SetProgress(1f);
            gameObject.SetActive(true);
        }

        private void UpdateTimerView(float currentTime, float maxTime)
        {
            float progress = Mathf.Clamp01(currentTime / maxTime);
            SetProgress(progress);
        }

        private void HideTimer()
        {
            SetProgress(0f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 割合に応じてScaleと色を変更する
        /// </summary>
        private void SetProgress(float progress)
        {
            // サイズの変更
            _timerTransform.localScale = new Vector3(_maxScaleX * progress, _baseScale.y, _baseScale.z);

            // 色の変更
            if (_material != null)
            {
                _material.color = _timeGradient.Evaluate(progress);
            }
        }
    }
}
