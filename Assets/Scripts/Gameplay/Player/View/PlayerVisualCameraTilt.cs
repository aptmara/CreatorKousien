// ------------------------------------------------------------
// File		: PlayerVisualCameraTilt.cs
// Summary	: プレイヤーの見た目だけを本体とは別にカメラへ向けて傾けるクラス
//
// Author	: [浅野勇生]
// Created	: 2026-07-03
//
// Notes	:
// - PlayerVisualCameraTilt はプレイヤーの見た目だけを本体とは別にカメラへ向けて傾けるクラスです。
// ------------------------------------------------------------
using Game.Core.Events;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Player
{
    public class PlayerVisualCameraTilt : MonoBehaviour
    {
        [Tooltip("Yaw(向き)を取得する本体。未設定なら親")]
        [SerializeField] private Transform _yawSource;
        [Tooltip("基準カメラ。未設定ならCamera.main")]
        [SerializeField] private Camera _camera;

        [Header("傾き")]
        [Tooltip("カメラへ向ける最大ピッチ角度(度)")]
        [SerializeField, Range(0f, 89f)] private float _maxTiltAngle = 30f;
        [Tooltip("0 = 傾けない / 1 = フル。演出で調整")]
        [SerializeField, Range(0f, 1f)] private float _strength = 1f;
        [Tooltip("追従の速さ")]
        [SerializeField] private float _followSpeed = 12f;
        [Tooltip("傾く向きが逆なら反転")]
        [SerializeField] private bool _invert = false;


        /// <summary>
        ///　起動時にYawソースとカメラを設定する。未設定なら親とCamera.mainを使用する
        /// </summary>
        private void Awake()
        {
            if (_yawSource == null)
                _yawSource = transform.parent;
            if (_camera == null)
                _camera = Camera.main;

            
        }
        private void OnEnable()
        {
            EventBus.Subscribe<PlayerTiltEvent>(OnPlayerTilt);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerTiltEvent>(OnPlayerTilt);
        }

        /// <summary>
        /// 毎フレーム、カメラのピッチ角度に応じてプレイヤーの見た目を傾ける
        /// ジッター防止で LateUpdate で行う
        /// </summary>
        private void LateUpdate()
        {
            if (_yawSource == null || _camera == null)
                return;

            // 1) 本体のYawを「ワールド水平面」で立て直して取り出す
            Vector3 worldUp = Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(_yawSource.forward, worldUp);
            if (fwd.sqrMagnitude < 1e-4f)
                fwd = _yawSource.forward; // ほぼ垂直ならそのまま

            Quaternion yawRot = Quaternion.LookRotation(fwd.normalized, worldUp);


            // 2) Yawの右軸回りにだけピッチを足す = 純粋なローカルX回転
            //    これでカメラのほう向けるぜよ
            Vector3 right = yawRot * Vector3.right;
            Vector3 toCam = (_camera.transform.position - transform.position).normalized;
            //Vector3 toCamOnPlane = Vector3.ProjectOnPlane(toCam, right).normalized;

            //// worldUp から 「カメラ方向」までの角度を求める
            //float pitch = Vector3.SignedAngle(worldUp, toCamOnPlane, right);
            //if (_invert)
            //    pitch = -pitch;
            //pitch = Mathf.Clamp(pitch * _strength, -_maxTiltAngle, _maxTiltAngle);

            float elevation = Mathf.Asin(Mathf.Clamp(Vector3.Dot(toCam, worldUp), -1f, 1f)) * Mathf.Rad2Deg;
            if (_invert)
                elevation = -elevation;
            float pitch = Mathf.Clamp(elevation * _strength, -_maxTiltAngle, _maxTiltAngle);

            Quaternion target = Quaternion.AngleAxis(pitch, right) * yawRot;


            // 3) フレームレート非依存で滑らかに補間する
            float t = 1f - Mathf.Exp(-_followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, t);
        }

        public void OnPlayerTilt(PlayerTiltEvent data)
        {
            _maxTiltAngle = data.CurrentTilt;
        }

    }
}


