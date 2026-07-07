// ================================================================================
// File         : ShopCinematicCameraController.cs
// Author       : Iwai Shogo
//
// Description  : ショップ演出専用のカメラワークを制御する独立クラス。
// Created      : 2026-07-07
// ================================================================================

using UnityEngine;
using System;

namespace Game.Gameplay.Shop
{
    /// <summary>
    /// ショップ演出専用のカメラワークを制御する独立クラス
    /// </summary>
    public class ShopCinematicCameraController : MonoBehaviour
    {
        [Header("--- コンポーネント参照 ---")]
        [Tooltip("制御対象のメインカメラのTransform")]
        [SerializeField] private Transform _mainCameraTransform;

        [Header("--- カメラワーク・スピード設定 ---")]
        [Tooltip("爆走時の追従の滑らかさ (0: 機敏 1: 滑らか)")]
        [SerializeField] private float _followSmoothTime = 0.2f;
        [Tooltip("停止後の回り込みアップの移動滑らかさ (0: 機敏 1: 滑らか)")]
        [SerializeField] private float _zoomPositionSmoothTime = 0.4f;
        [Tooltip("カメラが目標回転を向くときの滑らかさ")]
        [SerializeField] private float _rotationSmoothSpeed = 5f;

        [Header("--- 最後のドアップの固定画角設定 ---")]
        [Tooltip("プレイヤーと屋台の中間地点から、カメラをどの相対位置に配置するか")]
        [SerializeField] private Vector3 _finalAngleOffsetFromCenter = new Vector3(-3f, 2.5f, -4f);

        private Transform _playerTransform;
        private Transform _shopVehicleTransform;
        private ShopVehicleController _vehicleController;

        private bool _isActive = false;
        private Vector3 _posVelocity;
        private bool _notifiedDone = false;

        // カメラワークが完了したことを外部に伝えるイベント
        public event Action OnCompleteCameraWork;

        private void Start()
        {
            if (_mainCameraTransform == null && Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
        }

        /// <summary>
        /// 演出カメラワークを開始する
        /// </summary>
        public void StartCinematic(Transform player, ShopVehicleController vehicle)
        {
            _playerTransform = player;
            _shopVehicleTransform = vehicle.transform;
            _vehicleController = vehicle;

            _isActive = true;
            _notifiedDone = false;
            _posVelocity = Vector3.zero;
        }

        /// <summary>
        /// 演出カメラワークを終了し、通常カメラに戻す準備をする
        /// </summary>
        public void StopCinematic()
        {
            _isActive = false;
        }

        private void LateUpdate()
        {
            if (!_isActive || _mainCameraTransform == null || _playerTransform == null || _shopVehicleTransform == null) return;

            // 1. 常にプレイヤーと屋台の中心地点を動的計算
            Vector3 midPoint = (_playerTransform.position + _shopVehicleTransform.position) * 0.5f;

            // 屋台のステートに応じて挙動を分岐させる
            if (_vehicleController.CurrentState == ShopVehicleController.VehicleState.Stationary ||
                _vehicleController.CurrentState == ShopVehicleController.VehicleState.Braking)
            {
                // 停止・ブレーキ
                Vector3 targetCameraPos = midPoint + _finalAngleOffsetFromCenter;

                // 目標の固定位置へ
                _mainCameraTransform.position = Vector3.SmoothDamp(
                    _mainCameraTransform.position,
                    targetCameraPos,
                    ref _posVelocity,
                    _zoomPositionSmoothTime
                );

                // カメラの向きは常に中間地点をLookAt
                Vector3 lookDir = (midPoint - _mainCameraTransform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, Time.deltaTime * _rotationSmoothSpeed);
                }

                // カメラの座標が目標に近づき同じ構図になった瞬間検知
                if (!_notifiedDone && Vector3.Distance(_mainCameraTransform.position, targetCameraPos) < 0.05f)
                {
                    _notifiedDone = true;
                    OnCameraWorkComplete();
                }
            }
            else
            {
                Vector3 targetCameraPos = new Vector3(midPoint.x, _mainCameraTransform.position.y, midPoint.z - 10f);

                _mainCameraTransform.position = Vector3.SmoothDamp(
                    _mainCameraTransform.position,
                    targetCameraPos,
                    ref _posVelocity,
                    _followSmoothTime
                );

                Vector3 lookDir = (midPoint - _mainCameraTransform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir);
                    _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, Time.deltaTime * _rotationSmoothSpeed);
                }
            }
        }

        private void OnCameraWorkComplete()
        {
            Debug.Log("[ShopCinematicCamera] 回り込みズーム完了！画角がピタッと一致したぜよ。");

            OnCompleteCameraWork?.Invoke();
        }
    }
}
