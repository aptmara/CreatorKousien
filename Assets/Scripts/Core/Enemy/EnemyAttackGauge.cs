// 制作者: 山内陽
using System;
using UnityEngine;
using Game.Core.Events;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の攻撃ゲージ管理（MonoBehaviour）。
    /// UpdateループでゲージをTime.deltaTime分増加させ、
    /// ApplyGaugeDamageで減算する。
    /// EventBusへは直接発行せず、コールバック経由でEnemyControllerに委ねる。
    /// これによりEnemyAttackGauge自体はEventBusに非依存になり、単体テストが書きやすくなる。
    /// </summary>
    public class EnemyAttackGauge : MonoBehaviour
    {
        private string _enemyId;
        private float _maxGauge;
        private float _gaugeIncreaseRate;
        private float _currentGauge;
        private bool _isActive;
        private IBarrier _barrier;

        /// <summary>0.0〜1.0 の正規化ゲージ量（UI用）</summary>
        public float Ratio => _maxGauge > 0f ? _currentGauge / _maxGauge : 0f;

        /// <summary>現在のゲージ量（生値）</summary>
        public float CurrentGauge => _currentGauge;

        /// <summary>最大ゲージ量（生値）</summary>
        public float MaxGauge => _maxGauge;

        /// <summary>
        /// ゲージがMAX（_maxGauge）に到達した際に発火するコールバック。
        /// EnemyControllerが攻撃行動＆リセット処理を行う。
        /// </summary>
        public Action OnGaugeMaxReached;

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
        /// <param name="gaugeIncreaseRate">毎秒の自然増加量</param>
        /// <param name="barrier">適用するバリア実装（nullなら軽減なし）</param>
        public void Initialize(string enemyId, float maxGauge, float gaugeIncreaseRate, IBarrier barrier = null)
        {
            _enemyId = enemyId;
            _maxGauge = maxGauge;
            _gaugeIncreaseRate = gaugeIncreaseRate;
            _currentGauge = 0f;
            _barrier = barrier;
            _isActive = true;
        }

        /// <summary>
        /// ゲージの増加・MAXチェックを一時停止/再開する。
        /// ダウン中は停止、復帰後に再開する。
        /// </summary>
        /// <param name="active">true: 動作中, false: 停止</param>
        public void SetActive(bool active) => _isActive = active;

        private void Update()
        {
            if (!_isActive) return;

            _currentGauge += _gaugeIncreaseRate * Time.deltaTime;

            if (_currentGauge >= _maxGauge)
            {
                _currentGauge = _maxGauge;
                // MAX到達はコールバック経由でControllerへ通知。Update内で複数回発火しないようにSetActive(false)をControllerが行う。
                _isActive = false;
                OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
                OnGaugeMaxReached?.Invoke();
                return;
            }

            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
        }

        /// <summary>
        /// ゲージダメージを適用する。バリアが有効なら軽減後の値を使う。
        /// ゲージが0以下になった場合はOnGaugeBrokenを発火する。
        /// </summary>
        /// <param name="rawDamage">加工前のゲージダメージ量</param>
        public void ApplyGaugeDamage(float rawDamage)
        {
            if (!_isActive) return;

            // バリアが有効ならProcessGaugeDamageで軽減後の値を取得
            float actualDamage = (_barrier != null && _barrier.IsActive)
                ? _barrier.ProcessGaugeDamage(rawDamage)
                : rawDamage;

            _currentGauge -= actualDamage;
            _currentGauge = Mathf.Max(0f, _currentGauge);

            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);

            if (_currentGauge <= 0f)
            {
                _isActive = false;
                OnGaugeBroken?.Invoke();
            }
        }

        /// <summary>
        /// ゲージを0にリセットする。ダウン後の復帰時に使用。
        /// </summary>
        public void ResetGauge()
        {
            _currentGauge = 0f;
            OnGaugeChanged?.Invoke(_currentGauge, _maxGauge);
        }
    }
}
