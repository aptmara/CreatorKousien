// 制作者: 山内陽
using UnityEngine;
using UnityEngine.UI;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 敵の本体HPバーを表示するUIコンポーネント。
    /// EnemyHealthChangedEvent / EnemyDefeatedEvent を購読して表示を更新する。
    /// EnemyGaugeView と同一Canvasに配置し、同一のtargetEnemyIdを設定して使う。
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        [Tooltip("表示対象の敵のEnemyId。実行時に親のEnemyControllerから自動取得されるユニークなID。")]
        private string _targetEnemyId;

        private void Start()
        {
            var controller = GetComponentInParent<Game.Core.Enemy.EnemyController>();
            if (controller != null && !string.IsNullOrEmpty(controller.InstanceEnemyId))
            {
                _targetEnemyId = controller.InstanceEnemyId;
            }
        }

        [SerializeField] private Slider _hpSlider;
        [SerializeField] private Image _hpFillImage;

        [Header("HP割合によるFill色変化")]
        [SerializeField] private Color _highHpColor = new Color(0.2f, 0.8f, 0.2f); // 緑
        [SerializeField] private Color _midHpColor  = new Color(0.9f, 0.7f, 0.1f); // 黄
        [SerializeField] private Color _lowHpColor  = new Color(0.9f, 0.2f, 0.2f); // 赤

        /// <summary>
        /// 動的生成時にEnemyIdを外部から設定する。
        /// Inspector設定の場合はこのメソッド呼び出しは不要。
        /// </summary>
        /// <param name="enemyId">EnemyDefinition.EnemyId</param>
        public void Initialize(string enemyId)
        {
            _targetEnemyId = enemyId;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnHealthChanged(EnemyHealthChangedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;

            if (_hpSlider != null)
                _hpSlider.value = ev.Ratio;

            // HP割合に応じてFill色をグラデーション。0.5以上は高→中、0.5未満は中→低でLerp。
            if (_hpFillImage != null)
            {
                _hpFillImage.color = ev.Ratio >= 0.5f
                    ? Color.Lerp(_midHpColor, _highHpColor, (ev.Ratio - 0.5f) * 2f)
                    : Color.Lerp(_lowHpColor, _midHpColor, ev.Ratio * 2f);
            }
        }

        private void OnDefeated(EnemyDefeatedEvent ev)
        {
            if (ev.EnemyId != _targetEnemyId) return;
            // 撃破時はHPバーを非表示にする（Presenterとして表示制御のみ担う）
            gameObject.SetActive(false);
        }
    }
}
