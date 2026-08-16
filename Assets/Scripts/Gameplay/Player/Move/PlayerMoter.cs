// ------------------------------------------------------------
// File		: PlayerMoter.cs
// Summary	: プレイヤーの移動を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6 ベース作成
// - 5/12 回転量の大元作成 - 滝谷
// - 5/24 移動関連2パターン作り切り替えれるように修正 - 浅野
// - 6/19 PlayerRuntimeDataを参照、移動速度をステータスに基づいて変化させる機能の追加 - 浅野
// - 7/5  プレイヤーの回転挙動を修正 - 浅野
// - 7/11 プレイヤーがクリア時に勝手に移動するバグ修正 - 浅野
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Player.Progression;
using Game.Gameplay.Stage;
using UnityEngine.UI;

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
        [Header("Normal状態の移動設定")]
        [Tooltip("アタッチメントが通常状態のときの移動速度")]
        [SerializeField] private float _normalMoveSpeed = 10.0f;

        [Tooltip("アタッチメントが通常状態のときの回転補間速度")]
        [SerializeField] private float _normalRotationSpeed = 25.0f;

        [Tooltip("アタッチメントが通常状態のときの回転入力速度")]
        [SerializeField] private float _normalTurnSpeed = 500.0f;

        [Tooltip("アタッチメントが通常状態のときの自動回転速度")]
        [SerializeField] private float _normalAutoRotationDegreesPerSecond = 700f;


        [Header("Shrink状態の移動設定")]
        [Tooltip("アタッチメント縮小中の移動速度")]
        [SerializeField] private float _shrunkMoveSpeed = 6.0f;

        [Tooltip("アタッチメント縮小中の回転補間速度")]
        [SerializeField] private float _shrunkRotationSpeed = 15.0f;

        [Tooltip("アタッチメント縮小中の回転入力速度")]
        [SerializeField] private float _shrunkTurnSpeed = 250.0f;

        [Tooltip("アタッチメント縮小中の自動回転速度")]
        [SerializeField] private float _shrunkAutoRotationDegreesPerSecond = 350f;


        [Header("その他の設定")]
        [Tooltip("移動の基準となるカメラ(未設定時は自動取得)")]
        [SerializeField] private Camera _mainCamera;



        private Rigidbody _rigidbody;           ///< プレイヤーのRigidbodyコンポーネント

        private PlayerRuntimeData _runtimeData; ///< プレイヤーのランタイムデータ（ステータスなど）

        private Vector3 _desiredFaceDir;        ///< 向きたい方向

        private System.Func<Vector3, Vector3, Vector3> _autoRotationDirectionConstraint;  ///< 外部から設定される自動回転方向の補正処理

        private Vector3 Up => FieldContext.IsReady ? FieldContext.Up : Vector3.up;   ///< フィールドの上方向

        /// <summary>
        /// プレイヤーのランタイムデータを設定する関数
        /// </summary>
        /// <param name="runtimeData">プレイヤーのランタイムデータ</param>
        public void SetRuntimeData(PlayerRuntimeData runtimeData)
        {
            _runtimeData = runtimeData;
        }

        /// <summary>
        /// 現在の移動速度倍率(null の場合は1.0)
        /// </summary>
        private float MoveSpeedMultiplier => _runtimeData != null ? _runtimeData.MoveSpeedMultiplier : 1f;

        private float _targetYaw;

        /// <summary>
        /// 現在の移動速度を取得するプロパティ
        /// </summary>
        public Vector3 Velocity
        {
            get { return _rigidbody.linearVelocity; }
        }



        /// <summary>
        /// 自動回転方向の補正処理を設定する
        /// </summary>
        internal void SetAutoRotationDirectionConstraint(System.Func<Vector3, Vector3, Vector3> constraint)
        {
            _autoRotationDirectionConstraint = constraint;
        }


        /// <summary>
        /// 自動回転方向の補正処理を解除する
        /// </summary>
        internal void ClearAutoRotationDirectionConstraint(System.Func<Vector3, Vector3, Vector3> constraint)
        {
            if (_autoRotationDirectionConstraint == constraint)
            {
                _autoRotationDirectionConstraint = null;
            }
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

            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // カメラが未設定の場合はシーン内のメインカメラを自動取得する
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            _targetYaw = transform.eulerAngles.y;
        }

        private void Start()
        {
            // 初期回転をフィールドのupに合わせる
            Vector3 up = Up;

            Vector3 planeForward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            Vector3 faceDir = Quaternion.AngleAxis(_targetYaw, up) * planeForward;
            Quaternion rot = Quaternion.LookRotation(faceDir, up);

            _rigidbody.rotation = rot;
            transform.rotation = rot;
        }


        /// <summary>
        /// 指定された入力ベクトルに基づいてプレイヤーを移動・回転させる関数
        /// </summary>
        /// <param name="moveInput">移動入力ベクトル</param>
        public void Move(Vector2 moveInput, bool isAttachmentShrunk)
        {
            float moveSpeed = (isAttachmentShrunk ? _shrunkMoveSpeed : _normalMoveSpeed) * MoveSpeedMultiplier;

            Vector3 up = Up;
            Vector3 vel = _rigidbody.linearVelocity;
            float alongUp = Vector3.Dot(vel, up);

            // 入力がほぼ無い場合は水平移動を停止する
            if (moveInput.sqrMagnitude < 0.01f)
            {
                _rigidbody.linearVelocity = up * alongUp;
                return;
            }

            // カメラの向きを「フィールド面」に投影
            Vector3 cameraForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, up).normalized;

            Vector3 targetDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;
            Vector3 planarVelocity = targetDirection * moveSpeed;

            // 面内移動 + 上下成分維持
            _rigidbody.linearVelocity = planarVelocity + up * alongUp;
        }

        /// <summary>
        /// 回転入力に基づいてプレイヤーを回転させる関数
        /// </summary>
        /// <param name="lookInput">回転入力ベクトル</param>
        public void Rotate(Vector2 lookInput, bool isAttachmentShrunk)
        {
            float rotationSpeed = isAttachmentShrunk ? _shrunkRotationSpeed : _normalRotationSpeed;
            float turnSpeed = isAttachmentShrunk ? _shrunkTurnSpeed : _normalTurnSpeed;

            if (lookInput.sqrMagnitude < 0.01f)
            {
                return;
            }

            _targetYaw += lookInput.x * turnSpeed * Time.fixedDeltaTime;

            Vector3 up = Up;

            // フィールドの上方向
            Vector3 planeForward = Vector3.ProjectOnPlane(Vector3.forward, up).normalized;
            Vector3 faceDir = Quaternion.AngleAxis(_targetYaw, up) * planeForward;

            Quaternion targetRotation = Quaternion.LookRotation(faceDir, up);
            _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }


        /// <summary>
        /// 移動と回転を同時に行う関数
        /// </summary>
        /// <param name="moveInput">移動入力ベクトル</param>
        public void MoveWithAutoRotation(Vector2 moveInput, bool isAttachmentShrunk)
        {
            float moveSpeed = (isAttachmentShrunk ? _shrunkMoveSpeed : _normalMoveSpeed) * MoveSpeedMultiplier;
            float autoRotationSpeed = isAttachmentShrunk ? _shrunkAutoRotationDegreesPerSecond : _normalAutoRotationDegreesPerSecond;

            Vector3 up = Up;
            Vector3 vel = _rigidbody.linearVelocity;
            float alongUp = Vector3.Dot(vel, up);

            // カメラの向きを「フィールド面」に投影
            Vector3 cameraForward = Vector3.ProjectOnPlane(_mainCamera.transform.forward, up).normalized;
            Vector3 cameraRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, up).normalized;

            bool hasInput = moveInput.sqrMagnitude >= 0.01f;

            if (hasInput)
            {
                // 入力方向を「向きたい方向」として記憶
                _desiredFaceDir = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

                // 移動は入力中のみ
                _rigidbody.linearVelocity = _desiredFaceDir * moveSpeed + up * alongUp;
            }
            else
            {
                // 入力が無いときは水平移動だけ止める
                _rigidbody.linearVelocity = up * alongUp;
            }

            // 手を開いている状態なら向ききる挙動をしない
            bool completeAfterRelease = !isAttachmentShrunk;

            bool shouldRotate = _desiredFaceDir.sqrMagnitude > 0.001f && (hasInput || completeAfterRelease);

            // 向きたい方向がある場合、または外部制限が現在向きを補正する場合は自動回転する
            if (shouldRotate || _autoRotationDirectionConstraint != null)
            {
                Vector3 currentDirection = Vector3.ProjectOnPlane(_rigidbody.rotation * Vector3.forward, up).normalized;
                Vector3 requestedDirection = shouldRotate ? _desiredFaceDir : currentDirection;
                Vector3 rotationDirection = GetLimitedAutoRotationDirection(requestedDirection, up);

                if (!shouldRotate && Vector3.Angle(currentDirection, rotationDirection) < 0.01f)
                {
                    return;
                }

                Quaternion targetRotation = Quaternion.LookRotation(rotationDirection, up);
                Quaternion nextRotation = Quaternion.RotateTowards(_rigidbody.rotation, targetRotation, autoRotationSpeed * Time.fixedDeltaTime);
                _rigidbody.MoveRotation(nextRotation);
            }
        }


        /// <summary>
        /// 自動回転制限を反映した向きを取得する
        /// </summary>
        private Vector3 GetLimitedAutoRotationDirection(Vector3 direction, Vector3 up)
        {
            if (_autoRotationDirectionConstraint == null)
            {
                return direction;
            }

            return _autoRotationDirectionConstraint(direction, up);
        }


        /// <summary>
        /// プレイヤーの物理挙動を停止する関数
        /// </summary>
        public void FreezePhysics()
        {
            if (_rigidbody == null)
            {
                return;
            }

            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.isKinematic = true;
        }


        /// <summary>
        /// プレイヤーの物理演算の凍結を解除する関数
        /// </summary>
        public void UnfreezePhysics()
        {
            if (_rigidbody == null)
            {
                return;
            }
            _rigidbody.isKinematic = false;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }


        /// <summary>
        /// プレイヤーの移動を停止する関数
        /// </summary>
        public void StopMove()
        {
            // 移動を停止する
            if (_rigidbody == null)
            {
                return;
            }

            Vector3 up = Up;
            Vector3 velocity = _rigidbody.linearVelocity;
            float alongUp = Vector3.Dot(velocity, up);

            _rigidbody.linearVelocity = up * alongUp;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
