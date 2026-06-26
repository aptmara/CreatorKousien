// ------------------------------------------------------------
// File		: PlayerPunchController.cs
// Summary	: プレイヤーのパンチを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-24
//
// Notes	:
// - プレイヤーのパンチを管理するクラスを作成
// ------------------------------------------------------------
using System;
using System.Collections;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public sealed class PlayerPunchController : MonoBehaviour
    {
        [Header("パンチ設定")]
        [Tooltip("プレイヤーのコントローラー")]
        [SerializeField] private PlayerController _playerController;
        [Tooltip("パンチのアタッチメントのコントローラー")]
        [SerializeField] private PlayerAttachmentController _attachmentController;

        private AttachmentPunchAnimator _activePunchAnimator;
        private Action _pendingHitAction;
        private bool _isPunching;


        /// <summary>
        /// パンチアニメーションがヒットしたときに呼び出されるイベント
        /// </summary>
        /// <returns>パンチ中かどうか</returns>
        public bool TryPlayPunch(Action onHit)
        {
            if (_isPunching)
            {
                return false;
            }

            _isPunching = true;
            _pendingHitAction = onHit;

            _playerController.SetCanMove(false);
            _attachmentController.SetPunchForceLarge(true);

            _activePunchAnimator = _attachmentController.CurrentAttachment.GetComponentInChildren<AttachmentPunchAnimator>();

            if (_activePunchAnimator == null)
            {
                FinishPunch();
                return false;
            }

            _activePunchAnimator.PunchHit += OnPunchHit;
            _activePunchAnimator.PunchFinished += OnPunchFinished;
            _activePunchAnimator.PlayPunch();

            return true;
        }


        /// <summary>
        /// パンチが当たったときの処理
        /// </summary>
        private void OnPunchHit()
        {
            Action hitAction = _pendingHitAction;
            _pendingHitAction = null;
            hitAction?.Invoke();
        }


        /// <summary>
        /// パンチアニメーションが終了したときの処理
        /// </summary>
        private void OnPunchFinished()
        {
            FinishPunch();
        }


        /// <summary>
        /// パンチの終了処理
        /// </summary>
        private void FinishPunch()
        {
            if (_activePunchAnimator != null)
            {
                _activePunchAnimator.PunchHit -= OnPunchHit;
                _activePunchAnimator.PunchFinished -= OnPunchFinished;
            }

            _playerController.SetCanMove(true);
            _attachmentController.SetPunchForceLarge(false);

            _activePunchAnimator = null;
            _pendingHitAction = null;
            _isPunching = false;
        }
    }
}
