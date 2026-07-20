using UnityEngine;

using Game.Core.Events;
using Game.Core.Management;
using Game.Core.Roguelike;
using System;


namespace Game.Core.DefenceLine
{
    /// <summary>
    /// 防衛ラインのHPゲージを管理するコンポーネント
    /// </summary>
    public class DefenseLineGauge : MonoBehaviour
    {
        public Action OnGaugeBroken;

        // 将来用にフラグを保持
        bool _isBroken = false;
        [SerializeField] float _gaugeHP = 80.0f;
        float _baseMaxGaugeHP;
        float _maxGaugeHP;

        public float CurrentHP => _gaugeHP;
        public float MaxHP => _maxGaugeHP;

        private void Awake()
        {
            _baseMaxGaugeHP = Mathf.Max(0f, _gaugeHP);
            _maxGaugeHP = _baseMaxGaugeHP;
            ApplyMaxHpUpgrade();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<RuleBarrierAttackEvent>(OnBarrierHitBatch);
            RoguelikeUpgradeRuntime.Changed += ApplyMaxHpUpgrade;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RuleBarrierAttackEvent>(OnBarrierHitBatch);
            RoguelikeUpgradeRuntime.Changed -= ApplyMaxHpUpgrade;
        }

        private void Start()
        {
            PublishHealthChanged();
        }

        private void Update()
        {
            if (_isBroken || RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond <= 0f)
            {
                return;
            }

            if (GameProgressionManager.Instance == null ||
                GameProgressionManager.Instance.CurrentState != GameProgressionState.Battle)
            {
                return;
            }

            Heal(_maxGaugeHP * RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond * Time.deltaTime);
        }

        public void Heal(float amount)
        {
            if (_isBroken || amount <= 0f || _gaugeHP >= _maxGaugeHP) return;

            float previousHp = _gaugeHP;
            _gaugeHP = Mathf.Min(_maxGaugeHP, _gaugeHP + amount);
            if (!Mathf.Approximately(previousHp, _gaugeHP))
            {
                PublishHealthChanged();
            }
        }

        private void ApplyMaxHpUpgrade()
        {
            float previousMaxHp = _maxGaugeHP;
            float upgradedMaxHp = _baseMaxGaugeHP * RoguelikeUpgradeRuntime.BarrierMaxHpMultiplier;
            float increasedAmount = Mathf.Max(0f, upgradedMaxHp - previousMaxHp);

            _maxGaugeHP = Mathf.Max(0f, upgradedMaxHp);
            _gaugeHP = Mathf.Clamp(_gaugeHP + increasedAmount, 0f, _maxGaugeHP);
            PublishHealthChanged();
        }


        /// <summary>
        /// RuleBarrierAttackEventを受信し、体力がなくなった場合にコールバックを返す
        /// </summary>
        private void OnBarrierHitBatch(RuleBarrierAttackEvent ev)
        {
            // 破壊済みなら抜ける
            if (_isBroken) return;

            // ダメージを与える
            float appliedDamage = ev.AttackPower / Mathf.Max(1f, RoguelikeUpgradeRuntime.BarrierDefenseMultiplier);
            _gaugeHP -= appliedDamage;
            _gaugeHP = Mathf.Max(_gaugeHP, 0.0f);
            PublishHealthChanged();

            Debug.Log("防衛ライン残りHP" + _gaugeHP);

            // 被弾後の残HPに応じて処理を行う
            if (_gaugeHP <= 0.0f)
            {
                // バリアが破壊されたことを返す
                OnGaugeBroken?.Invoke();

                // オブジェクトに破壊リアクションを返す
                EventBus.Publish(new DefLineBreakReactionEvent(appliedDamage, ev.AttackPosition));
                _isBroken = true;
            }
            else
            {
                float remainingHpRatio = _maxGaugeHP > 0.0f ? _gaugeHP / _maxGaugeHP : 0.0f;
                EventBus.Publish(new DefLineHitReactionEvent(appliedDamage, remainingHpRatio, ev.AttackPosition));
            }
        }

        private void PublishHealthChanged()
        {
            EventBus.Publish(new DefenseLineHealthChangedEvent(_gaugeHP, _maxGaugeHP));
        }

    }
}
