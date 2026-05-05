// 制作者: 山内陽
using UnityEngine;
using UnityEngine.UI;
using Game.Core.Events;
using System.Collections;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 敵の頭上に表示するゲージUI。
    /// ゲージダメージの適用やダウン状態の表示を行う。
    /// </summary>
    public class EnemyGaugeView : MonoBehaviour
    {
        [SerializeField] private string _targetEnemyId;
        [SerializeField] private Slider _gaugeSlider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Color _normalColor = Color.yellow;
        [SerializeField] private Color _downColor = Color.red;

        private float _currentGauge = 1.0f; // 1.0 = Full

        public void Initialize(string enemyId)
        {
            _targetEnemyId = enemyId;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Subscribe<EnemyDownStartedEvent>(OnDownStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Unsubscribe<EnemyDownStartedEvent>(OnDownStarted);
        }

        private void OnHitBatch(EnemyHitBatchEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            // 仮のゲージ減算（本来は敵側のロジック管理だが、プロトとして見た目だけ反映）
            _currentGauge -= ev.GaugeDamage * 0.1f; 
            _currentGauge = Mathf.Clamp01(_currentGauge);
            
            if (_gaugeSlider != null)
            {
                _gaugeSlider.value = _currentGauge;
            }
        }

        private void OnDownStarted(EnemyDownStartedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            if (_fillImage != null)
            {
                _fillImage.color = _downColor;
            }
            
            StartCoroutine(ResetGaugeAfterDown(ev.Duration));
        }

        private IEnumerator ResetGaugeAfterDown(float duration)
        {
            yield return new WaitForSeconds(duration);
            
            _currentGauge = 1.0f;
            if (_gaugeSlider != null) _gaugeSlider.value = _currentGauge;
            if (_fillImage != null) _fillImage.color = _normalColor;
        }
    }
}
