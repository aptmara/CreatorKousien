// ================================================================================
// File         : ShopCinematicCameraController.cs
// Author       : Iwai Shogo
//
// Description  : ショップ演出専用のカメラワークを制御する独立クラス。
// Created      : 2026-07-07
// ================================================================================

using Game.Gameplay.Stage;
using System;
using UnityEngine;

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
        [Tooltip("通常画角へ戻るときの移動の滑らかさ")]
        [SerializeField] private float _returnSmoothTime = 0.4f;

        [Header("--- 最後のドアップの固定画角設定 ---")]
        [Tooltip("プレイヤーと屋台の中間地点から、カメラをどの相対位置に配置するか")]
        [SerializeField] private Vector3 _finalAngleOffsetFromCenter = new Vector3(-3f, 2.5f, -4f);

        private Transform _playerTransform;
        private Transform _shopVehicleTransform;
        private ShopVehicleController _vehicleController;

        private bool _isActive = false;
        private bool _isReturning = false;
        private Vector3 _posVelocity;
        private bool _notifiedDone = false;

        // 元のカメラ位置と回転を保存する変数
        private Vector3 _targetReturnPosition;
        private Quaternion _targetReturnRotation;

        // カメラワークが完了したことを外部に伝えるイベント
        public event Action OnCompleteCameraWork;

        // 元の画角に戻り切ったことを伝えるイベント
        public event Action OnCompleteReturnCamera;

        public bool IsCameraWorkFinished => _notifiedDone;

        private void Start()
        {
            if (_mainCameraTransform == null && Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
            Debug.Log($"[ShopCinematicCamera] Start時のメインカメラ名: {(_mainCameraTransform != null ? _mainCameraTransform.name : "NULL")}");
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
            _isReturning = false;
            _notifiedDone = false;
            _posVelocity = Vector3.zero;

            Debug.Log("[ShopCinematicCamera] 演出カメラ起動。プレイヤーと屋台の追従を開始ぜよ。");
        }

        /// <summary>
        /// 演出カメラワークを終了し、通常カメラに戻す準備をする
        /// </summary>
        public void StopCinematicAndReturn(Vector3 originPos, Quaternion originRot)
        {
            if (_mainCameraTransform == null && Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }

            _targetReturnPosition = originPos;
            _targetReturnRotation = originRot;

            Debug.Log($"<color=yellow>[ShopCinematicCamera] 確実なバトル座標への復帰を開始！ 目標Pos: {_targetReturnPosition}, 目標Rot: {_targetReturnRotation.eulerAngles}</color>");

            _isReturning = true;
            _posVelocity = Vector3.zero;
        }

        private void LateUpdate()
        {
            if (!_isActive || _mainCameraTransform == null) return;

            // 元の通常画角へのイージング
            if (_isReturning)
            {
                // 保存しておいた元の座標へ滑らかに戻す
                _mainCameraTransform.position = Vector3.SmoothDamp(
                    _mainCameraTransform.position,
                    _targetReturnPosition,
                    ref _posVelocity,
                    _returnSmoothTime
                );

                // 回転
                _mainCameraTransform.rotation = Quaternion.Slerp(
                     _mainCameraTransform.rotation,
                     _targetReturnRotation,
                     Time.deltaTime * _rotationSmoothSpeed
                );

                Debug.Log($"[ShopCinematicCamera] 復帰中... 現在地: {_mainCameraTransform.position} -> 目標: {_targetReturnPosition} (残り距離: {Vector3.Distance(_mainCameraTransform.position, _targetReturnPosition)})");

                // 元に戻ったら通常カメラに制御を返す
                if (Vector3.Distance(_mainCameraTransform.position, _targetReturnPosition) < 0.02f)
                {
                    _mainCameraTransform.position = _targetReturnPosition;
                    _mainCameraTransform.rotation = _targetReturnRotation;

                    _isActive = false;
                    _isReturning = false;

                    Debug.Log("<color=green>[ShopCinematicCamera] ぴたっと元の通常画角への復帰が完了したぜよ！</color>");
                    OnCompleteReturnCamera?.Invoke();   // 復帰完了通知
                }

                return;
            }

            if (_playerTransform == null || _shopVehicleTransform == null) return;

            // 1. 常にプレイヤーと屋台の中心地点を動的計算
            Vector3 midPoint = (_playerTransform.position + _shopVehicleTransform.position) * 0.5f;

            // 屋台のステートに応じて挙動を分岐させる
            if (_vehicleController.CurrentState == ShopVehicleController.VehicleState.Stationary ||
                _vehicleController.CurrentState == ShopVehicleController.VehicleState.Braking)
            {
                // 停止・ブレーキ
                Vector3 rotatedOffset = FieldContext.Rotation * _finalAngleOffsetFromCenter;
                Vector3 targetCameraPos = midPoint + rotatedOffset;

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
                    Vector3 fieldUp = FieldContext.Rotation * Vector3.up;
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, fieldUp);
                    _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, Time.deltaTime * _rotationSmoothSpeed);
                }

                // カメラの座標が目標に近づき同じ構図になった瞬間検知
                if (!_notifiedDone && Vector3.Distance(_mainCameraTransform.position, targetCameraPos) < 0.15f)
                {
                    _notifiedDone = true;
                    OnCameraWorkComplete();
                }
            }
            else
            {
                // プレイヤーと屋台の中間点を追従
                Vector3 fieldForward = FieldContext.Rotation * Vector3.forward;
                Vector3 fieldUp = FieldContext.Rotation * Vector3.up;

                // 中間地点から傾いた床の手前側にカメラを引き離す
                Vector3 targetCameraPos = midPoint - (fieldForward * 10f) + (fieldUp * 5f);

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
