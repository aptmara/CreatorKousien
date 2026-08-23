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

        /// <summary>
        /// これまでに食べた個数
        /// </summary>
        public int EatenCount { get; private set; }

        /// <summary>
        /// 食べ過ぎ状態に到達したかどうか
        /// </summary>
        public bool IsOverfed { get; private set; }

        /// <summary>
        /// 膨らみ表示用の割合（0.0～1.0）
        /// </summary>
        public float FillRatio => _maxEatCount > 0 ? Mathf.Clamp01((float)EatenCount / _maxEatCount) : 0f;

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

            EatenCount++;
            _onFillChanged?.Invoke(FillRatio);

            if (EatenCount >= _maxEatCount)
            {
                IsOverfed = true;
                _onOverfed?.Invoke();
            }

            return true;
        }


        /// <summary>
        /// 状態を初期化する
        /// </summary>
        public void Reset()
        {
            EatenCount = 0;
            IsOverfed = false;
            _onFillChanged?.Invoke(FillRatio);
        }
    }
}
