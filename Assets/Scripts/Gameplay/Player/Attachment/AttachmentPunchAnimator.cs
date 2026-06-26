// ------------------------------------------------------------
// File		: AttachmentPunchAnimator.cs
// Summary	: アタッチメントのパンチアニメーションを制御するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-24
//
// Notes	:
// - アタッチメントのパンチアニメーションを再生する
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public sealed class AttachmentPunchAnimator : MonoBehaviour
    {
        private static readonly int PunchHash = Animator.StringToHash("Punch");     ///< AnimatorのPunchトリガーのハッシュ値

        [Header("アニメーション設定")]
        [Tooltip("Animatorコンポーネント")]
        [SerializeField] private Animator _animator;

        /// <summary>
        /// パンチがヒットしたときに呼び出されるイベント
        /// </summary>
        public event Action PunchHit;

        /// <summary>
        /// パンチアニメーションが終了したときに呼び出されるイベント
        /// </summary>
        public event Action PunchFinished;


        /// <summary>
        /// Animatorコンポーネントを取得する
        /// </summary>
        private void Reset()
        {
            _animator = GetComponentInChildren<Animator>();
        }

        /// <summary>
        /// パンチアニメーションを再生する
        /// </summary>
        public void PlayPunch()
        {
            if (_animator == null)
            {
                return;
            }

            _animator.ResetTrigger(PunchHash);
            _animator.SetTrigger(PunchHash);
        }


        /// <summary>
        /// パンチがヒットしたときに呼び出されるイベント
        /// </summary>
        public void AnimEvent_PunchHit()
        {
            PunchHit?.Invoke();
        }


        /// <summary>
        /// パンチアニメーションが終了したときに呼び出されるイベント
        /// </summary>
        public void AnimEvent_PunchFinished()
        {
            PunchFinished?.Invoke();
        }
    }
}
