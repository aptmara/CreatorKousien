// ------------------------------------------------------------
// File     : BossMouthHealth.cs
// Summary  : アングリバイト中の口のHPと成功判定を管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - アングリバイト1回分の口の最大HPと現在HPを管理する。
// - 口へ入った落とし物との衝突判定やダメージ計算は行わない。
// - 口のHPが0になったことをイベントでBossControllerへ通知する。
// - アングリバイト失敗時は、BossControllerから受付を終了させる。
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// アングリバイト中の口のHPを管理するコンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・口の最大HPと現在HP
    /// ・ダメージ受付状態
    /// ・口のHPが0になったかどうか
    /// ・HP変更イベント
    /// ・HPが0になった際の通知
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossMouthHealth : MonoBehaviour
    {
        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// 口の最大HP
        /// </summary>
        private float _maxHp;

        /// <summary>
        /// 口の現在HP
        /// </summary>
        private float _currentHp;

        /// <summary>
        /// ダメージを受け付けている状態かどうか
        /// </summary>
        private bool _isChallengeActive;

        /// <summary>
        /// 口のHPが0になったかどうか
        /// </summary>
        private bool _isDepleted;

        /// <summary>
        /// 口のHPが設定済みかどうか
        /// </summary>
        private bool _isConfigured;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 口の最大HP
        /// </summary>
        public float MaxHp => _maxHp;

        /// <summary>
        /// 口の現在HP
        /// </summary>
        public float CurrentHp => _currentHp;

        /// <summary>
        /// 現在アングリバイトのダメージ受付中かどうか
        /// </summary>
        public bool IsChallengeActive => _isChallengeActive;

        /// <summary>
        /// 口のHPが0になっているかどうか
        /// </summary>
        public bool IsDepleted => _isDepleted;

        /// <summary>
        /// 現在、口へダメージを適用できるかどうか
        /// </summary>
        public bool IsDamageable => _isConfigured && _isChallengeActive && !_isDepleted;

        /// <summary>
        /// 現在の口HP割合
        /// </summary>
        public float HealthRatio => _maxHp > 0f ? _currentHp / _maxHp : 0f;


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// 口のHPが設定または変更されたときに通知
        ///
        /// 引数:
        /// 1. HPが変化したBossMouthHealth
        /// 2. 現在HP
        /// 3. 最大HP
        /// </summary>
        public event Action<BossMouthHealth, float, float> HealthChanged;

        /// <summary>
        /// 口のHPが0になった瞬間に1度だけ通知
        /// </summary>
        public event Action<BossMouthHealth> Depleted;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// このコンポーネントが無効化されたときに呼ばれる。
        /// </summary>
        private void OnDisable()
        {
            _isChallengeActive = false;
        }


        // アングリバイト開始・終了
        // ------------------------------------------------------------

        /// <summary>
        /// アングリバイト開始時に口のHPを設定し、ダメージ受付状態にする
        /// </summary>
        /// <param name="maxHp">最大HP</param>
        public void BeginChallenge(float maxHp)
        {
            if (maxHp <= 0f)
            {
                Debug.LogWarning($"[{nameof(BossMouthHealth)}] 最大HPが0以下のため、1に設定します。");
                maxHp = 1f;
            }

            _maxHp = maxHp;
            _currentHp = maxHp;

            _isChallengeActive = true;
            _isDepleted = false;
            _isConfigured = true;

            HealthChanged?.Invoke(this, _currentHp, _maxHp);
        }


        /// <summary>
        /// アングリバイト終了時にダメージ受付状態を解除する
        /// </summary>
        public void CancelChallenge()
        {
            _isChallengeActive = false;
        }


        // ダメージ処理
        // ------------------------------------------------------------

        /// <summary>
        /// 口にダメージを与える
        /// </summary>
        /// <param name="damage">与えるダメージ量</param>
        /// <returns>ダメージを適用できたかどうか</returns>
        public bool ApplyDamage(float damage)
        {
            if (!IsDamageable)
            {
                return false;
            }

            if (damage <= 0f)
            {
                return false;
            }

            _currentHp = Mathf.Max(0f, _currentHp - damage);

            HealthChanged?.Invoke(this, _currentHp, _maxHp);

            if (_currentHp <= 0f)
            {
                DepleteMouth();
            }

            return true;
        }


        /// <summary>
        /// 口にダメージを与える
        /// </summary>
        private void DepleteMouth()
        {
            if (_isDepleted)
            {
                return;
            }

            _currentHp = 0f;
            _isDepleted = true;
            _isChallengeActive = false;

            Depleted?.Invoke(this);
        }
    }
}
