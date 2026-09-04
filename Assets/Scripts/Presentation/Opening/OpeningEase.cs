// ------------------------------------------------------------
// File		: OpeningEase.cs
// Summary	: オープニング演出で共通して使うイージング関数
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using UnityEngine;


namespace Game.Presentation.Opening
{
    /// <summary>
    /// オープニング演出で共通して使うイージング関数
    /// </summary>
    public static class OpeningEase
    {
        /// <summary>
        /// 滑らかに加速して減速するイージング関数
        /// </summary>
        /// <param name="t">t: 0~1の範囲の値</param>
        /// <returns>変換後の値</returns>
        public static float SmoothStep(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }


        /// <summary>
        /// 滑らかに加速して減速するイージング関数（OutBack）
        /// </summary>
        /// <param name="t">t: 0~1の範囲の値</param>
        /// <returns>変換後の値</returns>
        public static float OutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float value = t - 1f;
            return 1f + c3 * value * value * value + c1 * value * value;
        }
    }
}


