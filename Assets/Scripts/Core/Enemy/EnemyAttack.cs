// 制作者: 越智晴彦
using Game.Core.Events;
using UnityEngine;

namespace Game.Core.Enemy
{
    public class EnemyAttack
    {
        //! 攻撃頻度
        float _maxAttackInterval;
        float _attackInterval;
        //! 攻撃力
        float _attackPower;
        //! 状態
        bool _isActiv;

        public void Initialize(float attackPower, float attackInterval, bool isActiv)
        {
            _maxAttackInterval = attackInterval;
            _attackInterval = _maxAttackInterval;
            _attackPower = attackPower;
            _isActiv = isActiv;
        }
        // Update is called once per frame
        void Update()
        {
            if (!_isActiv) return;

            _attackInterval -= Time.deltaTime;

            if (_attackInterval <= 0.0f)
            {
                _attackInterval = _maxAttackInterval;
                Attack();
            }
        }

        public void SetActiv(bool activ) => _isActiv = activ;

        void Attack()
        {
            EventBus.Publish(new RuleBarrierAttackEvent(_attackPower));
        }
    }
}


