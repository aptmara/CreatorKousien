// ------------------------------------------------------------
// File		: OpeningSlideView.cs
// Summary	: オープニングスライドのビュー
//
// Author	: [浅野勇生]
// Created	: 2026-09-04
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

namespace Game.Presentation.Opening
{
    /// <summary>
    /// スライド1枚分の演出
    /// 固有の動きを付けたいスライドは、このクラスを継承して各Routineを上書きする
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class OpeningSlideView : MonoBehaviour
    {
        [Header("--- スライダー共通 ---")]
        [SerializeField, Min(0f)] private float _enterDuration = 0.4f;
        [SerializeField, Min(0f)] private float _exitDuration = 0.3f;

        private CanvasGroup _group;

        /// <summary>
        /// 継承先からも使うCanvas Group
        /// </summary>
        protected CanvasGroup Group
        {
            get
            {
                if (_group == null)
                {
                    _group = GetComponent<CanvasGroup>();
                }
                return _group;
            }
        }

        protected float EnterDuration => _enterDuration;
        protected float ExitDuration => _exitDuration;


        /// <summary>
        /// 登場演出。これが終わってからテキストが出る
        /// </summary>
        public virtual IEnumerator PlayEnterRoutine()
        {
            yield return FadeGroupRoutine(0f, 1f, _enterDuration);
        }


        /// <summary>
        /// 退場演出。ループする演出の停止もここで行う
        /// </summary>
        public virtual IEnumerator PlayExitRoutine()
        {
            yield return FadeGroupRoutine(1f, 0f, _exitDuration);
        }


        protected IEnumerator FadeGroupRoutine(float from, float to, float duration)
        {
            Group.alpha = from;

            if (duration <= 0f)
            {
                Group.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Group.alpha = Mathf.Lerp(from, to, OpeningEase.SmoothStep(elapsed / duration));
                yield return null;
            }

            Group.alpha = to;
        }
    }
}
