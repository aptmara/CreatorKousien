// 制作者: 山内陽
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の本体HP管理（純粋クラス）。
    /// ダメージ適用・撃破判定・HP変化通知の責務のみを持つ。
    /// 「ダウン中のみダメージを受ける」制御はEnemyControllerが呼び出し前に判断する。
    /// </summary>
    public class EnemyHealth
    {
        private string _enemyId;
        private float _maxHp;
        private float _currentHp;

        /// <summary>現在のHP（0〜MaxHp）</summary>
        public float CurrentHp => _currentHp;

        /// <summary>最大HP</summary>
        public float MaxHp => _maxHp;

        /// <summary>HPが0以下かどうか</summary>
        public bool IsDefeated => _currentHp <= 0f;

        /// <summary>
        /// HP変化時に発火するコールバック。
        /// 引数: currentHp, maxHp
        /// </summary>
        public Action<float, float> OnHealthChanged;

        /// <summary>HP0到達時に発火するコールバック。1度だけ呼ばれる。</summary>
        public Action OnDefeated;

        /// <summary>
        /// 初期化。インスタンス生成後に必ず呼ぶ。
        /// </summary>
        /// <param name="enemyId">EnemyDefinitionで定義したEnemyId</param>
        /// <param name="maxHp">最大HP</param>
        public void Initialize(string enemyId, float maxHp, Action<float, float> OnHelthChange, Action OnDefeated)
        {
            _enemyId = enemyId;
            _maxHp = maxHp;
            _currentHp = maxHp;

            this.OnDefeated = OnDefeated;
            this.OnHealthChanged = OnHelthChange;

            this.OnHealthChanged(_currentHp, _maxHp);

        }

        /// <summary>
        /// 本体ダメージを適用する。
        /// 既に撃破済みの場合は何もしない。
        /// HP0到達時にOnDefeatedを1度だけ発火する。
        /// </summary>
        /// <param name="damage">適用するダメージ量（正値）</param>
        public void ApplyBodyDamage(float damage)
        {
            if (IsDefeated) return;

            _currentHp -= damage;
            _currentHp = Mathf.Max(0f, _currentHp);

            OnHealthChanged?.Invoke(_currentHp, _maxHp);

            if (_currentHp <= 0f)
            {
                OnDefeated?.Invoke();
            }
        }
    }
}
