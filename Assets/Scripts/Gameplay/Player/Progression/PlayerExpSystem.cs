// ------------------------------------------------------------
// File		: PlayerExpSystem.cs
// Summary	: プレイヤーの経験値管理を行うクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - 6/19: ベース作成
// ------------------------------------------------------------
using System;
using Game.Core.Events;

namespace Game.Gameplay.Player.Progression
{
    /// <summary>
    /// 経験値の蓄積とレベルアップを管理するシステム。
    /// EventBusのExpGainedEventを購読して動作する。生成側がSubscribe/Disposeを呼ぶこと。
    /// </summary>
    public sealed class PlayerExpSystem : IDisposable
    {
        private readonly PlayerRuntimeData _data;                   ///< プレイヤーのランタイムデータへの参照
        private readonly PlayerLevelCalculator _calculator;         ///< レベル計算クラスへの参照

        private bool _isSubscribed;                                 ///< イベント購読状態のフラグ

        /// <summary>
        /// プレイヤーの経験値管理システムのコンストラクタ
        /// </summary>
        /// <param name="data">プレイヤーのランタイムデータ</param>
        /// <param name="calculator">レベル計算クラスのインスタンス</param>
        public PlayerExpSystem(PlayerRuntimeData data, PlayerLevelCalculator calculator)
        {
            _data = data;
            _calculator = calculator;

            // 初期の「次のレベル」に必要な経験値を設定
            _data.ExpToNextLevel = _calculator.RequiredExp(_data.Level);
        }


        /// <summary>
        /// 経験値獲得イベントのハンドラー。経験値を加算し、レベルアップの判定と処理を行う。
        /// 現状はプレイヤーの生成時にSubscribeを呼ぶようにしてます！Disposeも忘れずに！
        /// </summary>
        public void Subscribe()
        {
            if (_isSubscribed) return;
            EventBus.Subscribe<ExpGainedEvent>(OnExpGained);
            _isSubscribed = true;
        }


        /// <summary>
        /// 経験値獲得イベントの購読解除。Disposeパターンの一部として呼び出されることを想定。
        /// 現状はプレイヤーの破棄時に呼ぶようにしてます！
        /// </summary>
        public void Dispose()
        {
            if (!_isSubscribed) return;
            EventBus.Unsubscribe<ExpGainedEvent>(OnExpGained);
            _isSubscribed = false;
        }


        /// <summary>
        /// 経験値を加算し、レベルアップの判定と処理を行う内部メソッド。
        /// </summary>
        /// <param name="e">経験値獲得イベント</param>
        private void OnExpGained(ExpGainedEvent e)
        {
            AddExp(e.Amount);
        }


        /// <summary>
        /// 経験値を換算して加算し、しきい値到達分だけレベルアップする。
        /// 敵が持っていた素の経験値量を引数に取り、レベルアップの処理を行う予定で制作しています！
        /// </summary>
        /// <param name="rawAmount">敵が持っていた素の経験値量</param>
        public void AddExp(float rawAmount)
        {
            // 最大レベルなら何もしない
            if (_calculator.IsMaxLevel(_data.Level)) return;

            // 敵の経験値量を換算係数でかけてから加算する
            float exp = rawAmount * _data.ExpConversionRate;
            if (exp <= 0f) return;

            _data.CurrentExp += exp;

            // 閾値を超えている限りレベルアップする
            while (_data.CurrentExp >= _calculator.RequiredExp(_data.Level))
            {
                _data.CurrentExp -= _calculator.RequiredExp(_data.Level);
                _data.Level++;

                EventBus.Publish(new PlayerLeveledUpEvent(_data.Level));
                EventBus.Publish(new UpgradeSelectionRequestedEvent(_data.Level));

                // 最大レベルに達したら端数の経験値は切り捨てて打ち切る
                if (_calculator.IsMaxLevel(_data.Level))
                {
                    _data.CurrentExp = 0f;
                    break;
                }
            }

            // 次レベルまでの必要量を更新し、UIへ通知
            _data.ExpToNextLevel = _calculator.RequiredExp(_data.Level);
            EventBus.Publish(new PlayerExpChangedEvent(_data.CurrentExp, _data.ExpToNextLevel));
        }
    }

}


