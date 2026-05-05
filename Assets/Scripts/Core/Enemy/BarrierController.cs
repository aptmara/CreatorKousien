// 制作者: 山内陽
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// IBarrierの標準実装（割合軽減バリア）。
    /// 将来的に属性対応バリアや多層バリアへ差し替える際は
    /// IBarrierを実装した別クラスを作り、EnemyDefinitionに型指定フィールドを追加する。
    /// </summary>
    public class BarrierController : MonoBehaviour, IBarrier
    {
        [SerializeField]
        [Tooltip("ゲージダメージの軽減割合。0=軽減なし, 1=完全無効。EnemyDefinitionで上書きされる。")]
        [Range(0f, 1f)]
        private float _damageReductionRate = 0.5f;

        private bool _isActive;

        /// <inheritdoc/>
        public bool IsActive => _isActive;

        /// <summary>
        /// EnemyDefinitionの値で初期化する。
        /// </summary>
        /// <param name="hasBarrier">バリアの有無</param>
        /// <param name="reductionRate">ダメージ軽減割合（0〜1）</param>
        public void Initialize(bool hasBarrier, float reductionRate)
        {
            _isActive = hasBarrier;
            _damageReductionRate = reductionRate;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// バリア有効時: rawDamage × (1 - reductionRate) を返す。
        /// 無効時はこのメソッドが呼ばれる想定ではないが、呼ばれた場合は rawDamage をそのまま返す。
        /// </remarks>
        public float ProcessGaugeDamage(float rawDamage)
        {
            return rawDamage * (1f - _damageReductionRate);
        }

        /// <inheritdoc/>
        public void SetActive(bool active) => _isActive = active;
    }
}
