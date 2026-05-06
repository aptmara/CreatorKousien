// 制作者: 山内陽
using UnityEngine;
using UnityEngine.UI;
using Game.Core.Events;
using System.Collections;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 敵の攻撃ゲージをUIに表示するコンポーネント。
    /// EnemyAttackGaugeが発行するEnemyGaugeChangedEventを購読し、
    /// Sliderの値をそのまま反映する（自前でゲージ計算を行わない）。
    /// </summary>
    public class EnemyGaugeView : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("表示対象の敵のEnemyId。EnemyDefinition.EnemyIdと一致させる。")]
        private string _targetEnemyId;

        [SerializeField] private Slider _gaugeSlider;
        [SerializeField] private Image _fillImage;

        [Header("通常/ダウン状態のFill色")]
        [SerializeField] private Color _normalColor = Color.yellow;
        [SerializeField] private Color _downColor   = Color.red;

        /// <summary>
        /// 動的生成時にEnemyIdを外部から設定する。
        /// Inspector設定の場合は呼び出し不要。
        /// </summary>
        /// <param name="enemyId">EnemyDefinition.EnemyId</param>
        public void Initialize(string enemyId)
        {
            _targetEnemyId = enemyId;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyGaugeChangedEvent>(OnGaugeChanged);
            EventBus.Subscribe<EnemyDownStartedEvent>(OnDownStarted);
            EventBus.Subscribe<EnemyGaugeBrokenEvent>(OnGaugeBroken);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyGaugeChangedEvent>(OnGaugeChanged);
            EventBus.Unsubscribe<EnemyDownStartedEvent>(OnDownStarted);
            EventBus.Unsubscribe<EnemyGaugeBrokenEvent>(OnGaugeBroken);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnGaugeChanged(EnemyGaugeChangedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            if (_gaugeSlider != null)
                _gaugeSlider.value = ev.Ratio;
        }

        private void OnGaugeBroken(EnemyGaugeBrokenEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            // ゲージ破壊時はSliderを0にリセット
            if (_gaugeSlider != null)
                _gaugeSlider.value = 0f;
        }

        private void OnDownStarted(EnemyDownStartedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            if (_fillImage != null)
                _fillImage.color = _downColor;

            StartCoroutine(ResetColorAfterDown(ev.Duration));
        }

        private void OnDefeated(EnemyDefeatedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;
            gameObject.SetActive(false);
        }

        private IEnumerator ResetColorAfterDown(float duration)
        {
            yield return new WaitForSeconds(duration);

            // 撃破されていなければ色を通常に戻す（非表示なら何もしない）
            if (gameObject.activeSelf && _fillImage != null)
                _fillImage.color = _normalColor;
        }
    }
}

