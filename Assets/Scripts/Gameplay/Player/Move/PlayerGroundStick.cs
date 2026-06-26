// ------------------------------------------------------------
// File		: PlayerGroundStick.cs
// Summary	: プレイヤーが地面に接地している間、移動入力に応じてスティックする
//
// Author	: [浅野 勇生]
// Created	: 2026-06-23
//
// Notes	:
// - プレイヤーがアイテムの上に乗り上げないように、地面に設置するスクリプト
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Stage;

namespace Game.Gameplay.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerGroundStick : MonoBehaviour
    {
        [Header("地面探索")]
        [Tooltip("地面レイヤー(床のみ。アイテムは絶対入れない)")]
        [SerializeField] private LayerMask _groundMask;
        [Tooltip("床探索の原点からの上方向オフセット")]
        [SerializeField] private float _probeUpOffset = 0.5f;
        [Tooltip("床探索のRayの長さ")]
        [SerializeField] private float _probeDistance = 3.0f;

        [Header("フロート(浮上)設定")]
        [Tooltip("床から浮かせる目標高さ(原点〜床)")]
        [SerializeField] private float _rideHeight = 1.0f;
        [Tooltip("バネの強さ")]
        [SerializeField] private float _spring = 120f;
        [Tooltip("減衰(揺れ止め。臨界≒2*sqrt(spring))")]
        [SerializeField] private float _damper = 22f;

        private Rigidbody _rb;
        private bool _grounded;
        private RaycastHit _lastHit;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.maxDepenetrationVelocity = 3f; // めり込み吹っ飛び防止
            _rb.freezeRotation = true;          // 衝突で転ばない
        }

        private void FixedUpdate()
        {
            // 床探索
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;

            // ----- 1. 床探索のRayを飛ばす -----
            Vector3 origin = _rb.position + up * _probeUpOffset;
            _grounded = Physics.Raycast(origin, -up, out _lastHit,
                _probeUpOffset + _probeDistance, _groundMask, QueryTriggerInteraction.Ignore);

            if (!_grounded) return; // 床なし(崖際)はそのまま落下

            // 床からの現在高さ と up方向の速度
            float gap = Vector3.Dot(_rb.position - _lastHit.point, up);
            float upVel = Vector3.Dot(_rb.linearVelocity, up);

            // ----- 2. 目標高さまでのバネ力を加える -----
            float force = (_rideHeight - gap) * _spring - upVel * _damper;
            _rb.AddForce(up * force, ForceMode.Acceleration);
        }

        private void OnDrawGizmosSelected()
        {
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null) return;
            Vector3 pos = Application.isPlaying ? rb.position : transform.position;
            Vector3 up = (Application.isPlaying && FieldContext.IsReady) ? FieldContext.Up : transform.up;

            // ----- 床探索のRayを描画 -----
            Vector3 start = pos + up * _probeUpOffset;
            Vector3 end = start - up * (_probeUpOffset + _probeDistance);
            Gizmos.color = _grounded ? Color.green : Color.red;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.05f);

            // ----- 目標高さの位置を描画 -----
            Gizmos.color = Color.cyan; // 目標の浮上高さ
            Vector3 ride = (Application.isPlaying && _grounded) ? _lastHit.point + up * _rideHeight : pos;
            Gizmos.DrawWireSphere(ride, 0.1f);
        }
    }
}
