using UnityEngine;

using Game.Core.Events;
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

        public float CurrentHP => _gaugeHP;

        private void OnEnable()
        {
            EventBus.Subscribe<RuleBarrierAttackEvent>(OnBarrierHitBatch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<RuleBarrierAttackEvent>(OnBarrierHitBatch);
        }

        private void Initialized(float gaugeHP)
        {
            _gaugeHP = gaugeHP;
        }


        /// <summary>
        /// RuleBarrierAttackEventを受信し、体力がなくなった場合にコールバックを返す
        /// </summary>
        private void OnBarrierHitBatch(RuleBarrierAttackEvent ev)
        {
            // 破壊済みなら抜ける
            if (_isBroken) return;

            // ダメージを与える
            _gaugeHP -= ev.AttackPower;
            _gaugeHP = Mathf.Max(_gaugeHP, 0.0f);

            Debug.Log("防衛ライン残りHP" + _gaugeHP);

            // 被弾後の残HPに応じて処理を行う
            if (_gaugeHP <= 0.0f)
            {
                // バリアが破壊されたことを返す
                OnGaugeBroken?.Invoke();

                // オブジェクトに破壊リアクションを返す
                EventBus.Publish(new DefLineBreakReactionEvent());
                _isBroken = true;
            }
            else
            {
                EventBus.Publish(new DefLineHitReactionEvent());
            }
        }

    }
}
