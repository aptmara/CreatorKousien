// 制作者: 越智晴彦 ※山内陽のプログラムを改変したもの
using System;
using UnityEngine;
using Game.Core.Events;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵のバリア管理（MonoBehaviour）。
    /// UpdateループでゲージをTime.deltaTime分増加させ、
    /// ApplyGaugeDamageで減算する。
    /// EventBusへは直接発行せず、コールバック経由でEnemyControllerに委ねる。
    /// これによりEnemyBarrierGauge自体はEventBusに非依存になり、単体テストが書きやすくなる。
    /// </summary>
    public class EnemyBarrierGauge : MonoBehaviour
    {
        private float _maxGauge;
        private float _currentGauge;
        private float _healPower;
        private bool _isActive;
        private bool _isHeal;
        private float _maxHealWaitTime;
        private float _healWaitTime;
        [SerializeField]
        private GameObject _barrierObject = null;

        /// <summary>0.0〜1.0 の正規化ゲージ量（UI用）</summary>
        public float Ratio => _maxGauge > 0f ? _currentGauge / _maxGauge : 0f;

        /// <summary>現在のゲージ量（生値）</summary>
        public float CurrentGauge => _currentGauge;

        /// <summary>最大ゲージ量（生値）</summary>
        public float MaxGauge => _maxGauge;

        /// <summary>
        /// ApplyGaugeDamageによりゲージが0以下になった際に発火するコールバック。
        /// EnemyControllerがダウン遷移を行う。
        /// </summary>
        public Action OnGaugeBroken;

        /// <summary>
        /// ゲージ量が変化した際に発火するコールバック（UI用EventBus発行はEnemyControllerが行う）。
        /// 引数: currentGauge, maxGauge
        /// </summary>
        public Action<float, float> OnGaugeChanged;

        /// <summary>
        /// 初期化。EnemyControllerのAwakeまたはInitializeから呼ぶ。
        /// </summary>
        /// <param name="enemyId">識別ID</param>
        /// <param name="maxGauge">最大ゲージ量</param>
        public void Initialize(string enemyId, float maxGauge, float healRegenWaitTime, float healPower)
        {

            bool hasBarrier = _barrierObject != null;

            _maxGauge = maxGauge;
            _currentGauge = hasBarrier ? maxGauge : 0.0f;
            _isActive = hasBarrier;
            _maxHealWaitTime = healRegenWaitTime;
            _healPower = healPower;

            ResetGauge();
            ResetHeal();
        }

        /// <summary>
        /// 更新処理、継続回復等はここで行われる
        /// </summary>
        public void Update()
        {
            if (!_isActive) return;

            UpdateHealInterval();
            if(_isHeal) GaugeHeal(_healPower * Time.deltaTime);
        }
        /// <summary>
        /// MAXチェックを一時停止/再開する。
        /// ダウン中は停止、復帰後に再開する。
        /// </summary>
        /// <param name="active">true: 動作中, false: 停止</param>
        public void SetActive(bool active) => _isActive = active;

        /// <summary>
        /// ゲージダメージを適用する。
        /// ゲージが0以下になった場合はOnGaugeBrokenを発火する。
        /// </summary>
        /// <param name="rawDamage">加工前のゲージダメージ量</param>
        public void ApplyGaugeDamage(float rawDamage)
        {
            if (!_isActive && !_barrierObject) return;

            // バリアにダメージを与える
            _currentGauge -= rawDamage;
            _currentGauge = Mathf.Max(0f, _currentGauge);

            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
            // 回復待機する
            WaitHeal();

            if (_currentGauge <= 0f)
            {
                _isActive = false;
                OnGaugeBroken?.Invoke();
                _barrierObject?.SetActive(false);
            }
        }

        /// <summary>
        /// ゲージを0にリセットする。ダウン後の復帰時に使用。
        /// </summary>
        public void ResetGauge()
        {
            if (!_barrierObject) return;

            _currentGauge = _maxGauge;
            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
            _barrierObject.SetActive(true);
            ResetHeal();
        }

        /// <summary>
        /// ゲージを回復する
        /// </summary>
        /// <param name="healValue"></param>
        public void GaugeHeal(float healValue)
        {
            _currentGauge += healValue;
            _currentGauge = Mathf.Min(_maxGauge, _currentGauge);
            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
        }

        public void UpdateHealInterval()
        {
            // バリアが非アクティブだったり既に回復中であれば抜ける
            if(!_isActive) return;
            if(_isHeal) return;
            _healWaitTime -= Time.deltaTime;
            _healWaitTime = Mathf.Max(_healWaitTime, 0.0f);

            if( _healWaitTime <= 0.0f)
            {
                _isHeal = true;
            }
        }

        /// <summary>
        /// 回復状態を初期状態リセットする(現在は普通に回復)
        /// </summary>
        void ResetHeal()
        {
            _isHeal = true;
            _healWaitTime = 0.0f;
        }

        // ダメージなどを受けた際にインターバル分回復を止める
        void WaitHeal()
        {
            _isHeal = false;
            _healWaitTime = _maxHealWaitTime;
        }
    }
}
