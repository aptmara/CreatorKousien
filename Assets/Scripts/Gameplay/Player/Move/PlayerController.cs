// ------------------------------------------------------------
// File		: PlayerController.cs
// Summary	: プレイヤーの移動を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6  ベース作成
// - 7/5  プレイヤーの挙動に応じて歩きアニメーションを変化させる機能の追加 - 浅野
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの移動を管理するクラス
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("コンポーネント設定")]
        [Tooltip("入力読み取りコンポーネント")]
        [SerializeField] private PlayerInputReader _inputReader;

        [Tooltip("移動制御コンポーネント")]
        [SerializeField] private PlayerMotor _motor;


        [Header("移動設定")]
        [Tooltip("プレイヤーが自動で回転するかどうか")]
        [SerializeField] private bool _useAutoRotationMove = false;

        [Tooltip("アタッチメントの拡大縮小機能を使用するかどうか")]
        [SerializeField] private bool _useAttachmentScaling = false;

        [Tooltip("アタッチメント拡縮をToggle方式にするかどうか。OFFならHold方式")]
        [SerializeField] private bool _useAttachmentScaleToggle = false;

        [Tooltip("アタッチメントのコントローラー")]
        [SerializeField] private PlayerAttachmentController _attachmentController;

        [Tooltip("表情(BlendShape)の制御")]
        [SerializeField] private PlayerFaceController _faceController;


        private bool _canMove = true;                           ///< プレイヤーが移動可能かどうかを示すフラグ
        private bool _isAttachmentShrunk;                       ///< アタッチメントの拡大縮小状態を示すフラグ
        private bool _prevSpreadFace;                           ///< 前フレームの表情状態を保持するフラグ

        public bool IsAttachmentShrunk => _isAttachmentShrunk;  ///< アタッチメントの拡大縮小状態を取得するプロパティ


        // 関数処理
        // ------------------------------------------------------------

        private void Start()
        {
            SetCursorLock(true);
        }

        ///<summary>
        /// 物理演算の更新処理
        /// </summary>
        private void FixedUpdate()
        {
            if (!_canMove)
            {
                return;
            }

            // Facadeとして入力を取得し、Motorに移動処理を渡す
            PlayerInputState input = _inputReader.CurrentInput;

            if (_attachmentController != null && _useAttachmentScaling)
            {
                if (_useAttachmentScaleToggle)
                {
                    if (_inputReader.ConsumeAttachmentScalePressed())
                    {
                        _isAttachmentShrunk = !_isAttachmentShrunk;
                    }
                }
                else
                {
                    _isAttachmentShrunk = input.AttachmentScaleHeld;
                }

                _attachmentController.SetShrunk(_isAttachmentShrunk);
            }
            else
            {
                _isAttachmentShrunk = false;

                if (_attachmentController != null)
                {
                    _attachmentController.SetShrunk(false);
                }
            }

            UpdateSpreadFace();

            if (_useAutoRotationMove)
            {
                _motor.MoveWithAutoRotation(input.MoveDirection, _isAttachmentShrunk);
            }
            else
            {
                _motor.Move(input.MoveDirection, _isAttachmentShrunk);
                _motor.Rotate(input.LookDirection, _isAttachmentShrunk);
            }
        }


        private void UpdateSpreadFace()
        {
            if (_faceController == null)
            {
                return;
            }

            // ----- アタッチメントの拡大縮小状態に応じて表情を更新 -----
            bool spreading = _isAttachmentShrunk;

            if (spreading == _prevSpreadFace)
                return;

            _prevSpreadFace = spreading;

            // ----- 表情の更新 -----
            if (spreading)
            {
                _faceController.SetHeldFace("Spread");
            }
            else
            {
                _faceController.ClearHeldFace();
            }
        }


        /// <summary>
        /// プレイヤーの移動可否を設定する関数
        /// </summary>
        /// <param name="canMove">移動可能かどうか</param>
        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;

            if (!_canMove)
            {
                _motor.Move(Vector2.zero, false); // 移動を停止させる
            }
        }

        private void SetCursorLock(bool enable)
        {
            if (enable)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

        }
    }
}
