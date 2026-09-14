// ------------------------------------------------------------
// File		: BakuStomach.cs
// Summary	: バクが食べた量を管理する純粋なクラス
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// バクの食べた量を管理！
    /// </summary>
    public class BakuStomach
    {
        private int _maxEatCount = 1;
        private Action<float> _onFillChanged;
        private Action _onOverfed;

        // 食べた量。消化で減っていくため小数で保持する
        private float _eatenAmount;

        /// <summary>
        /// これまでに食べた個数
        /// </summary>
        public int EatenCount => Mathf.FloorToInt(_eatenAmount);

        /// <summary>
        /// 食べ過ぎ状態に到達したかどうか
        /// </summary>
        public bool IsOverfed { get; private set; }

        /// <summary>
        /// 膨らみ表示用の割合（0.0～1.0）
        /// </summary>
        public float FillRatio => _maxEatCount > 0 ? Mathf.Clamp01(_eatenAmount / _maxEatCount) : 0f;

        /// <summary>
        /// 初期化。生成時に絶対呼ぶ！！！絶対！
        /// </summary>
        /// <param name="maxEatCount">食べられる最大量</param>
        /// <param name="onFillChanged">膨らみ表示用の割合が変化した時のコールバック</param>
        /// <param name="onOverfed">食べ過ぎ状態に到達した時のコールバック</param>
        public void Initialize(int maxEatCount, Action<float> onFillChanged, Action onOverfed)
        {
            _maxEatCount = Mathf.Max(1, maxEatCount);
            _onFillChanged = onFillChanged;
            _onOverfed = onOverfed;
            Reset();
        }


        /// <summary>
        /// 食べれるかどうかを確認して、食えたらくう！
        /// </summary>
        /// <returns>食えなかったらfalse</returns>
        public bool TryEat()
        {
            if (IsOverfed)
            {
                return false;
            }

            _eatenAmount += 1f;
            _onFillChanged?.Invoke(FillRatio);

            if (_eatenAmount >= _maxEatCount)
            {
                IsOverfed = true;
                _onOverfed?.Invoke();
            }

            return true;
        }


        /// <summary>
        /// 食べた分を消化して減らす。
        /// 破裂が確定した後は消化しない（もう助からない）。
        /// </summary>
        /// <param name="amount">消化する量[個]</param>
        public void Digest(float amount)
        {
            if (IsOverfed) return;
            if (amount <= 0f) return;
            if (_eatenAmount <= 0f) return;

            _eatenAmount = Mathf.Max(0f, _eatenAmount - amount);

            _onFillChanged?.Invoke(FillRatio);
        }


        /// <summary>
        /// 状態を初期化する
        /// </summary>
        public void Reset()
        {
            _eatenAmount = 0f;
            IsOverfed = false;
            _onFillChanged?.Invoke(FillRatio);
        }
    }
}
