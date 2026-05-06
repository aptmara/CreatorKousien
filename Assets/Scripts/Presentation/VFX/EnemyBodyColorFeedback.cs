// 制作者: AI
using UnityEngine;
using Game.Core.Events;

namespace Game.Presentation.VFX
{
    /// <summary>
    /// 敵のHP割合に応じてモデル（Renderer）の色を変化させるコンポーネント。
    /// EnemyControllerと同じオブジェクト（またはその子）にアタッチして使用する。
    /// </summary>
    public class EnemyBodyColorFeedback : MonoBehaviour
    {
        [Tooltip("実行時に親のEnemyControllerから自動取得されるユニークなID。")]
        private string _targetEnemyId;

        [SerializeField]
        [Tooltip("色を変化させる対象のRenderer。未設定の場合は子オブジェクト全てから自動で取得します。")]
        private Renderer[] _renderers;

        [Header("HPカラー設定")]
        [SerializeField] private Color _highHpColor = Color.white;
        [SerializeField] private Color _lowHpColor = Color.red;

        private MaterialPropertyBlock _propBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Start()
        {
            var controller = GetComponentInParent<Game.Core.Enemy.EnemyController>();
            if (controller != null && !string.IsNullOrEmpty(controller.InstanceEnemyId))
            {
                _targetEnemyId = controller.InstanceEnemyId;
            }

            if (_renderers == null || _renderers.Length == 0)
            {
                _renderers = GetComponentsInChildren<Renderer>();
            }

            _propBlock = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHealthChangedEvent>(OnHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHealthChangedEvent>(OnHealthChanged);
        }

        private void OnHealthChanged(EnemyHealthChangedEvent ev)
        {
            if (string.IsNullOrEmpty(_targetEnemyId) || ev.EnemyId != _targetEnemyId) return;

            // HP割合に応じて色を補間 (1.0 = HighColor, 0.0 = LowColor)
            Color targetColor = Color.Lerp(_lowHpColor, _highHpColor, ev.Ratio);

            if (_renderers != null)
            {
                foreach (var r in _renderers)
                {
                    if (r == null) continue;
                    
                    r.GetPropertyBlock(_propBlock);
                    
                    // URPや標準シェーダーの両方に対応するため、よく使われるプロパティ名にセット
                    _propBlock.SetColor(BaseColorId, targetColor);
                    _propBlock.SetColor(ColorId, targetColor);
                    
                    r.SetPropertyBlock(_propBlock);
                }
            }
        }
    }
}
