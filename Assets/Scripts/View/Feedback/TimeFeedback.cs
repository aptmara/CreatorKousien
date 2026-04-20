// ------------------------------------------------------------
// File		: TimeFeedback.cs
// Summary	: ヒットストップやスローモーションなど、時間に関するフィードバックを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - ヒットストップ演出の作成
// ------------------------------------------------------------
using UnityEngine;

namespace CreatorKousien.View.Feedback
{
    public class TimeFeedback : MonoBehaviour
    {
        /// <summary>
        /// ヒットストップを再生するメソッド。指定した時間だけゲームの時間を遅くし、ヒットのインパクトを強調する。
        /// </summary>
        /// <param name="duration">ヒットストップの持続時間</param>
        /// <param name="timeScale">ゲームの時間スケール</param>
        public void PlayHitStop(float duration = 0.1f, float timeScale = 0.05f)
        {
            StartCoroutine(HitStopRoutine(duration, timeScale));
        }


        /// <summary>
        /// ヒットストップのコルーチン。指定した時間だけゲームの時間を遅くし、その後元に戻す。
        /// </summary>
        /// <param name="duration">ヒットストップの持続時間</param>
        /// <param name="timeScale">ゲームの時間スケール</param>
        /// <returns></returns>
        private System.Collections.IEnumerator HitStopRoutine(float duration, float timeScale)
        {
            Time.timeScale = timeScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1.0f;
        }
    }
}

