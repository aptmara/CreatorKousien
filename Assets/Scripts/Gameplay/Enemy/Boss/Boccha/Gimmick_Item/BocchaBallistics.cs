// ------------------------------------------------------------
// File		: BocchaBallistics.cs
// Summary	: ボッチャの弾道計算を行うギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - バスケのシュートのように、頂点の高さを指定して軌道を決める
// - フィールドが傾いても正しく解けるよう、重力ベクトルから上方向を導出する
// - なんか上から振ってくるのはおかしいから勝手に作ってみる！
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 放物線軌道の計算ユーティリティ!!
    /// </summary>
    public static class BocchaBallistics
    {
        /// <summary>
        /// 指定した頂点の高さで目標地点へ到達する初速を求める
        /// </summary>
        /// <param name="from">発射位置</param>
        /// <param name="to">着地させたい位置</param>
        /// <param name="apexHeight">発射位置と着地位置の高い方から、さらに何m上を頂点にするか</param>
        /// <param name="gravity">適用される重力</param>
        /// <param name="flightTime">到達までにかかる時間</param>
        /// <returns>与えるべき初速</returns>
        public static Vector3 SolveVelocityByApex(Vector3 from, Vector3 to, float apexHeight, Vector3 gravity, out float flightTime)
        {
            float g = gravity.magnitude;

            // 重力がない場合は放物線が成立しないため、直線的に飛ばす
            if (g < 0.0001f)
            {
                flightTime = 1.0f;

                return to - from;
            }

            Vector3 up = -gravity / g;

            float fromHeight = Vector3.Dot(from, up);
            float toHeight = Vector3.Dot(to, up);

            float apex = Mathf.Max(fromHeight, toHeight) + Mathf.Max(0.1f, apexHeight);

            // 頂点までの上昇時間と、頂点から着地までの落下時間
            float riseTime = Mathf.Sqrt(2.0f * (apex - fromHeight) / g);
            float fallTime = Mathf.Sqrt(2.0f * (apex - toHeight) / g);

            flightTime = Mathf.Max(0.01f, riseTime + fallTime);

            // 水平成分は上方向の成分を取り除いて求める
            Vector3 horizontal = (to - from) - up * (toHeight - fromHeight);

            return horizontal / flightTime + up * (g * riseTime);
        }


        /// <summary>
        /// 目標時間を指定して目標地点へ到達する初速を求める
        /// </summary>
        /// <param name="from">開始点</param>
        /// <param name="to">目標点</param>
        /// <param name="flightTime">飛行時間</param>
        /// <param name="gravity">重力</param>
        /// <returns>初速ベクトル</returns>
        public static Vector3 SolveVelocityByTime(Vector3 from, Vector3 to, float flightTime, Vector3 gravity)
        {
            float time = Mathf.Max(0.01f, flightTime);

            // p = p0 + v * t + 0.5 * g * t^2
            return (to - from - 0.5f * gravity * time * time) / time;
        }


        /// <summary>
        /// 指定した初速で放物線を描く軌道を描画する
        /// </summary>
        /// <param name="from">甲斐支店</param>
        /// <param name="velocity">初速</param>
        /// <param name="gravity">重力</param>
        /// <param name="flightTime">飛行時間</param>
        /// <param name="drawDuration">描画時間</param>
        /// <param name="color">色</param>
        /// <param name="segmentCount">支点数</param>
        public static void DrawTrajectory(Vector3 from, Vector3 velocity, Vector3 gravity, float flightTime, float drawDuration, Color color, int segmentCount = 16)
        {
            Vector3 previous = from;

            for (int i = 1; i <= segmentCount; ++i)
            {
                float t = flightTime * i / segmentCount;

                Vector3 current = from + velocity * t + 0.5f * gravity * t * t;

                Debug.DrawLine(previous, current, color, drawDuration);

                previous = current;
            }
        }
    }
}
