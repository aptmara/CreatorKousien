// ------------------------------------------------------------
// File		: CameraFeedback.cs
// Summary	: カメラの揺れやズームなど、カメラに関するフィードバックを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-20
//
// Notes	:
// - カメラの揺れを作成
// ------------------------------------------------------------
using UnityEngine;


namespace CreatorKousien.View.Feedback
{
    public class CameraFeedback : MonoBehaviour
    {
        private Vector3 _originalCameraPos;

        /// <summary>
        /// 初期化処理
        /// </summary>
        public void Initialize()
        {
            if (Camera.main != null)
            {
                _originalCameraPos = Camera.main.transform.position;
            }
        }


        /// <summary>
        /// カメラの揺れを再生するメソッド。
        /// </summary>
        /// <param name="duration">揺れの持続時間</param>
        /// <param name="magnitude">揺れの強さ</param>
        public void PlayShake(float duration = 0.15f, float magnitude = 0.4f)
        {
            StartCoroutine(CameraShakeRoutine(duration, magnitude));
        }


        /// <summary>
        /// カメラの揺れのコルーチン。指定した時間だけカメラをランダムに揺らし、その後元の位置に戻す。
        /// </summary>
        /// <param name="duration">揺れの持続時間</param>
        /// <param name="magnitude">揺れの強さ</param>
        /// <returns></returns>
        private System.Collections.IEnumerator CameraShakeRoutine(float duration, float magnitude)
        {
            if (Camera.main == null)
            {
                yield break;
            }
            Transform camTransform = Camera.main.transform;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                camTransform.position = _originalCameraPos + new Vector3(x, y, 0);
                elapsed += Time.deltaTime;
                yield return null;
            }
            camTransform.position = _originalCameraPos;
        }
    }
}

