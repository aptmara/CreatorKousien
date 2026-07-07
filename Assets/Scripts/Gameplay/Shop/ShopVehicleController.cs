// ================================================================================
// File         : ShopVehicleController.cs
// Author       : Iwai Shogo
//
// Description  : 屋台の登場・爆走・急ブレーキ・退出の移動と、それに連動するカートゥーン歪みを総合制御するクラス。
// Created      : 2026-07-07
// ================================================================================

using Game.Gameplay.Stage;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Shop
{
    /// <summary>
    /// 屋台の登場・爆走・急ブレーキ・退出の移動と、それに連動するカートゥーン歪みを総合制御するクラス
    /// </summary>
    public class ShopVehicleController : MonoBehaviour
    {
        /// <summary>
        /// 屋台の状態を示すステート
        /// </summary>
        public enum VehicleState
        {
            Inactive,           // 非アクティブ
            EnteringWaypoints,  // 左のあぜ道を進行中
            ApproachingPlayer,  // プレイヤーを動的に追従して突入中
            Braking,            // プレイヤー前で急ブレーキ演出中
            Stationary,         // 停止中
            Exiting             // 右のあぜ道からハケ中
        }

        [Header("--- コンポーネント参照 ---")]
        [SerializeField] private CartoonDistortion _distortion;

        [Header("--- ルート設定 ---")]
        [Tooltip("左外から入ってきて、プレイヤー追従に分岐する直前までのあぜ道ポイント")]
        [SerializeField] private List<Transform> _entryWaypoints = new List<Transform>();

        [Tooltip("ショップ終了後、プレイヤーの前から右外へハケるためのあぜ道ポイント")]
        [SerializeField] private List<Transform> _exitWaypoints = new List<Transform>();

        [Header("--- 移動・緩急パラメータ ---")]
        [SerializeField] private float _moveSpeed = 15f;
        [SerializeField] private float _turnSpeed = 10f;
        [Tooltip("プレイヤーのどれくらい手前に停止するか")]
        [SerializeField] private Vector3 _stopOffsetFromPlayer = new Vector3(-2.5f, 0f, 0f);
        [Tooltip("移動の緩急をつけるカーブ")]
        [SerializeField] private AnimationCurve _accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("--- カートゥーン歪み連動設定 ---")]
        [Tooltip("巡航速度の時に平行四辺形に前傾する最大値")]
        [SerializeField] private float _maxCruiseShear = 0.6f;
        [Tooltip("急ブレーキ時に縦に伸びる最大値")]
        [SerializeField] private float _maxBrakeSquash = 0.8f;
        [Tooltip("ブレーキ後の縦伸びが元に戻るバネの硬さ(大きいほど速く往復する)")]
        [SerializeField] private float _springStiffness = 15f;
        [Tooltip("ブレーキ後の縦伸びのブレを止める減衰力(0〜1)")]
        [SerializeField] private float _springDamping = 0.2f;

        private VehicleState _currentState = VehicleState.Inactive;
        private Transform _playerTarget;
        private Vector3 _dynamicStopPosition;
        private int _currentWaypointIndex = 0;
        private Vector3 _lastPosition;
        private float _brakeSquashVelocity = 0f;
        private float _currentSquashY = 0f;

        public VehicleState CurrentState => _currentState;

        private void Start()
        {
            _lastPosition = transform.position;

            // テスト用に初期状態で非アクティブにする場合はここを調整
            if (Application.isPlaying && _currentState == VehicleState.Inactive)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 演出を開始するエントリー関数。ウェーブクリア時にこれを叩く。
        /// </summary>
        public void LaunchShopSequence(Transform playerTransform)
        {
            _playerTarget = playerTransform;
            _currentState = VehicleState.EnteringWaypoints;
            _currentWaypointIndex = 0;
            _currentSquashY = 0f;
            _brakeSquashVelocity = 0f;

            if (_entryWaypoints.Count > 0)
            {
                transform.position = _entryWaypoints[0].position;
            }
            else if (_playerTarget != null)
            {
                transform.position = _playerTarget.position + new Vector3(-20f, 0f, 0f);
            }

            gameObject.SetActive(true);
            _lastPosition = transform.position;
        }

        /// <summary>
        /// ショップUIが閉じて右にはけさせる時にこれを叩く。
        /// </summary>
        public void DismissShopSequence()
        {
            if (_currentState != VehicleState.Stationary) return;
            _currentState = VehicleState.Exiting;
            _currentWaypointIndex = 0;
        }

        private void Update()
        {
            if (_currentState == VehicleState.Inactive) return;

            // 毎フレーム、物理的な移動速度を計算してシアーにフィードバック
            Vector3 deltaMove = transform.position - _lastPosition;
            float currentFrameSpeed = deltaMove.magnitude / Time.deltaTime;
            _lastPosition = transform.position;

            switch (_currentState)
            {
                case VehicleState.EnteringWaypoints:
                    HandleWaypointMovement(_entryWaypoints, OnEntryWaypointsComplete);
                    ApplyRunningDistortion(currentFrameSpeed);
                    break;

                case VehicleState.ApproachingPlayer:
                    HandlePlayerApproach();
                    ApplyRunningDistortion(currentFrameSpeed);
                    break;

                case VehicleState.Braking:
                    HandleBrakingOscillation();
                    break;

                case VehicleState.Stationary:
                    // 完全に静止
                    _distortion.ShearX = 0f;
                    _distortion.SquashY = 0f;
                    break;

                case VehicleState.Exiting:
                    HandleWaypointMovement(_exitWaypoints, OnExitComplete);
                    ApplyRunningDistortion(currentFrameSpeed);
                    break;
            }
        }

        // 移動ロジック群
        // ============================================================

        private void HandleWaypointMovement(List<Transform> waypoints, Action onComplete)
        {
            if (waypoints == null || waypoints.Count == 0 || _currentWaypointIndex >= waypoints.Count)
            {
                onComplete?.Invoke();
                return;
            }

            Vector3 targetPos = waypoints[_currentWaypointIndex].position;

            MoveAndRotateTowards(targetPos);

            if (Vector3.Distance(transform.position, targetPos) < 0.8f)
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= waypoints.Count)
                {
                    onComplete?.Invoke();
                }
            }
        }

        private void HandlePlayerApproach()
        {
            if (_playerTarget == null)
            {
                _currentState = VehicleState.Braking;
                TriggerBrakeImpact();
                return;
            }

            // プレイヤーの源氏愛知を動的に追従した停止目標ポイントを計算
            // プレイヤーの向いている方向や位置に応じてオフセットを掛ける
            Quaternion fieldRot = FieldContext.Rotation;
            Vector3 rotatedOffset = fieldRot * _stopOffsetFromPlayer;

            _dynamicStopPosition = _playerTarget.position + rotatedOffset;

            MoveAndRotateTowards(_dynamicStopPosition);

            // 停止位置に近づいたら急ブレーキステートに移行
            if (Vector3.Distance(transform.position, _dynamicStopPosition) < 1.0f)
            {
                transform.position = _dynamicStopPosition;
                _currentState = VehicleState.Braking;
                TriggerBrakeImpact();
            }
        }

        private void MoveAndRotateTowards(Vector3 targetPos)
        {
            Vector3 diff = targetPos - transform.position;
            Vector3 fieldUp = FieldContext.Rotation * Vector3.up;

            Vector3 projectedDir = Vector3.ProjectOnPlane(diff, fieldUp).normalized;

            if (projectedDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(projectedDir, fieldUp);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * _turnSpeed);
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * _moveSpeed);
        }

        private void OnEntryWaypointsComplete()
        {
            // 左のあぜ道を走り切ったら、動的プレイヤー追従モードへ
            _currentState = VehicleState.ApproachingPlayer;
        }

        private void OnExitComplete()
        {
            // 右外にはけきったら非アクティブにして演出終了
            _currentState = VehicleState.Inactive;
            gameObject.SetActive(false);
        }

        // カートゥーン歪みロジック群
        // ============================================================

        private void ApplyRunningDistortion(float speed)
        {
            // 走っているときは、速度に比例してシアーさせる
            float speedRatio = Mathf.Clamp01(speed / _moveSpeed);
            _distortion.ShearX = speedRatio * _maxCruiseShear;
            _distortion.SquashY = 0f;
        }

        private void TriggerBrakeImpact()
        {
            // 急ブレーキの瞬間シアーをリセット
            _distortion.ShearX = 0f;
            _currentSquashY = _maxBrakeSquash;
            _brakeSquashVelocity = _springStiffness * 0.5f;
        }

        private void HandleBrakingOscillation()
        {
            // 調和振動子による、ブレーキ後の減衰バネ振動の計算
            float springForce = -_springStiffness * _currentSquashY;
            float dampingForce = -_springDamping * _brakeSquashVelocity;

            _brakeSquashVelocity += (springForce + dampingForce) * Time.deltaTime;
            _currentSquashY += _brakeSquashVelocity * Time.deltaTime;

            _distortion.SquashY = _currentSquashY;

            // 振動がほぼ収まったら、完全に静止状態へ移行
            if (Mathf.Abs(_currentSquashY) < 0.01f && Mathf.Abs(_brakeSquashVelocity) < 0.01f)
            {
                _currentSquashY = 0f;
                _distortion.SquashY = 0f;
                _currentState = VehicleState.Stationary;

                Debug.Log("[ShopVehicleController] 屋台がプレイヤー前でピタッと完全停止したぜよ！");
            }
        }
    }
}
