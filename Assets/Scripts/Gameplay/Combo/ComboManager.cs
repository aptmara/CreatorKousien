// ================================================================================
// File         : ComboManager.cs
// Author       : Iwai Shogo
//
// Description  : コンボ数と猶予状態のカウント・減算ロジックを管理するクラス。
// Created      : 2026-06-08
// ================================================================================

using System;
using UnityEngine;

namespace Game.Gameplay.Combo
{
    public sealed class ComboManager
    {
        /// <summary>
        /// 猶予時間の制限モード
        /// </summary>
        public enum DurationLimitMode
        {
            ClampToMax,     // 設定した最大猶予時間の上限有り
            InfiniteStack,  // 無限
        }

        private readonly float _addDurationPerHit;
        private readonly float _maxDuration;
        private readonly DurationLimitMode _limitMode;
        private readonly IComboBonusStrategy _bonusStrategy;

        private int _currentCombo;
        private float _remainingDuration;

        public int CurrentCombo => _currentCombo;
        public float RemainingDuration => _remainingDuration;
        public float MaxDuration => _maxDuration;

        // 寺田
        public MoneyData moneyData { get; set; }
        public SceneEventChannel EventChannel { get; set; }
        public float multiRatio { get; set; }

        /// <summary>
        /// 猶予時間の残り割合
        /// </summary>
        public float DurationRatio => _maxDuration > 0f ? _remainingDuration / _maxDuration : 0f;

        // イベント通知用コールバック
        public Action<int, float> OnComboUpdated;   // 引数: コンボ数、残り猶予割合
        public Action OnComboReset;

        /// <summary>
        /// コンボマネージャーの初期化
        /// </summary>
        public ComboManager(float addDuration, float maxDuration, DurationLimitMode mode, IComboBonusStrategy bonusStrategy = null)
        {
            _addDurationPerHit = addDuration;
            _maxDuration = maxDuration;
            _limitMode = mode;
            _bonusStrategy = bonusStrategy ?? new DefaultComboBonus();

            ResetCombo();
        }

        /// <summary>
        /// ヒット時のコンボ加算処理
        /// </summary>
        public void AddCombo(int hitCount)
        {
            if (hitCount <= 0) return;

            _currentCombo += hitCount;

            // モードに応じた猶予時間の加算処理
            float addedTime = _addDurationPerHit * hitCount;
            if (_limitMode == DurationLimitMode.ClampToMax)
            {
                // 上限あり
                _remainingDuration = Mathf.Min(_maxDuration, _remainingDuration + addedTime);
            }
            else
            {
                // 上限なし
                if (_remainingDuration <= 0f) _remainingDuration = _maxDuration;
                _remainingDuration += addedTime;
            }

            OnComboUpdated?.Invoke(_currentCombo, DurationRatio);
        }

        /// <summary>
        /// 時間経過による減算処理
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (_currentCombo == 0) return;

            _remainingDuration -= deltaTime;

            if (_remainingDuration <= 0f)
            {
                moneyData.AddMoney(_currentCombo);
                ResetCombo();
            }
            else
            {
                OnComboUpdated?.Invoke(_currentCombo, DurationRatio);
            }
        }

        /// <summary>
        /// コンボを強制リセットする
        /// </summary>
        public void ResetCombo()
        {
            // 平方根カーブを用いたインフレ倍率
            float dynamicMagni = multiRatio + (Mathf.Sqrt(_currentCombo) * 0.04f);

            // 倍率制限を適用
            if (dynamicMagni > 0.5f)
            {
                dynamicMagni = 0.5f;
            }
            EventChannel?.ExecuteEvent(dynamicMagni + 1.0f);
            _currentCombo = 0;
            _remainingDuration = 0f;
            OnComboReset?.Invoke();
        }

        /// <summary>
        /// 拡張用: 現在のコンボによる攻撃力倍率を取得する
        /// </summary>
        public float GetCurrentDamageMultiplier()
        {
            return _bonusStrategy.GetDamageMultiplier(_currentCombo);
        }
    }
}
