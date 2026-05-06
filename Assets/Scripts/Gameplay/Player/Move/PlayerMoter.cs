// ------------------------------------------------------------
// File		: PlayerMoter.cs
// Summary	: プレイヤーの移動を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6 ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの移動・回転・接地処理を管理するクラス
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerMotor : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("移動設定")]
        [Tooltip("プレイヤーの移動速度")]
        [SerializeField] private float _moveSpeed = 6.0f;

        [Tooltip("プレイヤーの回転速度")]
        [SerializeField] private float _rotationSpeed = 15.0f;

        [Header("その他の設定")]
        [Tooltip("移動の基準となるカメラ(未設定時は自動取得)")]
        [SerializeField] private Camera _mainCamera;

        private Rigidbody _rigidbody;       ///< プレイヤーのRigidbodyコンポーネント

        /// <summary>
        /// 現在の移動速度を取得するプロパティ
        /// </summary>
        public Vector3 Velocity
        {
            get { return _rigidbody.linearVelocity; }
        }



        // 関数処理
        // ------------------------------------------------------------

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // 物理挙動を滑らかに制御する設定
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;

            // カメラが未設定の場合はシーン内のメインカメラを自動取得する
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }
        }


        /// <summary>
        /// 指定された入力ベクトルに基づいてプレイヤーを移動・回転させる関数
        /// </summary>
        /// <param name="moveInput">移動入力ベクトル</param>
        public void Move(Vector2 moveInput)
        {
            // 入力がほぼ無い場合は水平移動を停止する
            if (moveInput.sqrMagnitude < 0.01f)
            {
                _rigidbody.linearVelocity = new Vector3(0f, _rigidbody.linearVelocity.y, 0f);
                return;
            }

            // カメラの向きを取得
            Vector3 cameraForward = _mainCamera.transform.forward;
            cameraForward.y = 0f;                                   // 上下方向の傾きは無視
            cameraForward.Normalize();

            Vector3 cameraRight = _mainCamera.transform.right;
            cameraRight.y = 0f;
            cameraRight.Normalize();

            // 入力ベクトルをカメラ基準のワールド方向に変換
            Vector3 targetDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            // 移動の適用
            Vector3 targetVelocity = targetDirection * _moveSpeed;
            targetVelocity.y = _rigidbody.linearVelocity.y;         // 落下速度は維持する
            _rigidbody.linearVelocity = targetVelocity;

            // 回転の適用
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            _rigidbody.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.fixedDeltaTime));
        }
    }
}
