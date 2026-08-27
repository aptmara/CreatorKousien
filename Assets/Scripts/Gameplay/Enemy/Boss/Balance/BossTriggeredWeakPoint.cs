// Scripts/Gameplay/Enemy/Boss/Balance/BossTriggeredWeakPoint.cs
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 当てるとボスをDown状態へ移行させる弱点オブジェクト。
    /// 天秤の皿など、外部から露出/非露出を切り替えたいケースを想定。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossHitReceiver))]
    public sealed class BossTriggeredWeakPoint : MonoBehaviour, IBossHittable, IBossTrayItem
    {
        [Header("==== 参照 ====")]
        [SerializeField] private BossBattleFlowController _flowController;
        [SerializeField] private Collider _hitCollider;

        [Header("==== 命中制限 =====")]
        [Tooltip("許可されたオブジェクトにしか当たり判定が無いようにするか")]
        [SerializeField] private bool _restrictRequierType = true;
        [SerializeField] private CollectibleType _requiredType = CollectibleType.BossWeak;

        [Header("==== 挙動 ====")]
        [Tooltip("最初から露出しているか(false推奨。皿が上がった時だけ露出させる)")]
        [SerializeField] private bool _startExposed = false;

        [Tooltip("命中でDownを取るかどうか")]
        [SerializeField] private bool _triggerDownOnHit = true;

        [Tooltip("連続ヒット防止用のクールダウン秒数")]
        [SerializeField, Min(0f)] private float _hitCooldown = 0.2f;

        private bool _isExposed;
        private float _nextHitAllowedTime;

        public bool IsHittable => _isExposed && Time.time >= _nextHitAllowedTime;

        private void Awake()
        {
            ApplyExposed(_startExposed);
        }

        //===== IBossTrayItem: 天秤の皿が上がった/下がった通知を受け取る =======
        public void OnTrayRaised() => ApplyExposed(true);
        public void OnTrayLowered() => ApplyExposed(false);

        private void ApplyExposed(bool exposed)
        {
            _isExposed = exposed;
            if (_hitCollider != null) _hitCollider.enabled = exposed;
        }

        //======== IBossHittable: BossHitReceiverから呼ばれる =============
        public void OnHit(float damage, Vector3 hitPosition, CollectibleObject collectible)
        {
            if (_restrictRequierType && collectible.Type != _requiredType) return;

            _nextHitAllowedTime = Time.time + _hitCooldown;

            if (_flowController == null) return;
            _flowController?.TakeDamage(damage);

            if (_triggerDownOnHit && !_flowController.IsDown)
            {
                _flowController.TriggerDown();
            }
        }
    }
}
