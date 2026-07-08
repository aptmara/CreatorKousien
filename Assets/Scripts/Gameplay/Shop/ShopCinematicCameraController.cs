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
        [Tooltip("制御対象のメインカメラのCamera Component")]
        [SerializeField] private Camera _targetCamera;

        [Header("--- 1. 爆走フォーカス時代設定")]
        [Tooltip("爆走時のカメラ視野角")]
        [SerializeField] private float _cruiseFOV = 70f;
        [Tooltip("爆走時の屋台からのカメラ配置オフセット")]
        [SerializeField] private Vector3 _cruiseOffsetFromVehicle = new Vector3(0f, 4f, -12f);
        [Tooltip("爆走時の追従の滑らかさ")]
        [SerializeField] private float _followSmoothTime = 0.15f;

        [Header("--- 2. 急ブレーキ・フレーミング時代設定 ---")]
        [Tooltip("最終停止時のカメラ視野角")]
        [SerializeField] private float _finalFOV = 45f;
        [Tooltip("プレイヤーと屋台の中間地点から、カメラをどの相対位置に配置するか")]
        [SerializeField] private Vector3 _finalAngleOffsetFromCenter = new Vector3(-3f, 2.5f, -4f);
        [Tooltip("停止後の回り込みアップの移動滑らかさ")]
        [SerializeField] private float _zoomPositionSmoothTime = 0.3f;
        [Tooltip("カメラが目標回転を向くときの滑らかさ")]
        [SerializeField] private float _rotationSmoothSpeed = 6f;

        [Header("--- 3. 共通・注視点オフセット ---")]
        [Tooltip("カメラが狙うターゲット中心からの高さや左右のズレ")]
        [SerializeField] private Vector3 _lookAtOffsetFromCenter = new Vector3(0f, 1.2f, 0f);
        [Tooltip("通常画角へ戻るときの移動の滑らかさ")]
        [SerializeField] private float _returnSmoothTime = 0.4f;

        private Transform _playerTransform;
        private Transform _shopVehicleTransform;
        private ShopVehicleController _vehicleController;

        private bool _isActive = false;
        private bool _isReturning = false;
        private Vector3 _posVelocity;
        private float _fovVelocity;
        private bool _notifiedDone = false;

        // 元のカメラ位置と回転を保存する変数
        private Vector3 _targetReturnPosition;
        private Quaternion _targetReturnRotation;
        private float _targetReturnFOV;

        // カメラワークが完了したことを外部に伝えるイベント
        public event Action OnCompleteCameraWork;
        // 元の画角に戻り切ったことを伝えるイベント
        public event Action OnCompleteReturnCamera;

        public bool IsCameraWorkFinished => _notifiedDone;

        private void Start()
        {
            EnsureCameraReferences();
            Debug.Log($"[ShopCinematicCamera] Start時のメインカメラ名: {(_mainCameraTransform != null ? _mainCameraTransform.name : "NULL")}");
        }

        private void EnsureCameraReferences()
        {
            if (_mainCameraTransform == null && Camera.main != null)
            {
                _mainCameraTransform = Camera.main.transform;
            }
            if (_targetCamera == null && _mainCameraTransform != null)
            {
                _targetCamera = _mainCameraTransform.GetComponent<Camera>();
            }
        }

        /// <summary>
        /// 演出カメラワークを開始する
        /// </summary>
        public void StartCinematic(Transform player, ShopVehicleController vehicle)
        {
            EnsureCameraReferences();
            _playerTransform = player;
            _shopVehicleTransform = vehicle.transform;
            _vehicleController = vehicle;

            _isActive = true;
            _isReturning = false;
            _notifiedDone = false;
            _posVelocity = Vector3.zero;
            _fovVelocity = 0f;

            if (_targetCamera != null)
            {
                _targetReturnFOV = _targetCamera.fieldOfView;
            }

            Debug.Log("[ShopCinematicCamera] 演出カメラ起動。プレイヤーと屋台の追従を開始ぜよ。");
        }

        /// <summary>
        /// 演出カメラワークを終了し、通常カメラに戻す準備をする
        /// </summary>
        public void StopCinematicAndReturn(Vector3 originPos, Quaternion originRot)
        {
            EnsureCameraReferences();
            _targetReturnPosition = originPos;
            _targetReturnRotation = originRot;

            Debug.Log($"<color=yellow>[ShopCinematicCamera] バトル座標への復帰開始ぜよ！</color>");

            _isReturning = true;
            _posVelocity = Vector3.zero;
            _fovVelocity = 0f;
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

                if (_targetCamera != null)
                {
                    _targetCamera.fieldOfView = Mathf.SmoothDamp(
                        _targetCamera.fieldOfView,
                        _targetReturnFOV,
                        ref _fovVelocity,
                        _returnSmoothTime
                    );
                }

                // 元に戻ったら通常カメラに制御を返す
                if (Vector3.Distance(_mainCameraTransform.position, _targetReturnPosition) < 0.02f)
                {
                    _mainCameraTransform.position = _targetReturnPosition;
                    _mainCameraTransform.rotation = _targetReturnRotation;
                    if (_targetCamera != null) _targetCamera.fieldOfView = _targetReturnFOV;

                    _isActive = false;
                    _isReturning = false;

                    Debug.Log("<color=green>[ShopCinematicCamera] ぴたっと元の通常画角への復帰が完了したぜよ！</color>");
                    OnCompleteReturnCamera?.Invoke();   // 復帰完了通知
                }

                return;
            }

            if (_playerTransform == null || _shopVehicleTransform == null) return;

            // 屋台のステートによる2フェーズ切り替え
            // ------------------------------------------------------------
            if (_vehicleController.CurrentState == ShopVehicleController.VehicleState.Stationary ||
                _vehicleController.CurrentState == ShopVehicleController.VehicleState.Braking)
            {
                // phase 2: 急ブレーキ & フレーミング
                Vector3 midPoint = (_playerTransform.position + _shopVehicleTransform.position) * 0.5f;
                Vector3 targetLookTarget = midPoint + (FieldContext.Rotation * _lookAtOffsetFromCenter);

                Vector3 rotatedOffset = FieldContext.Rotation * _finalAngleOffsetFromCenter;
                Vector3 targetCameraPos = midPoint + rotatedOffset;

                // 位置を回り込み座標へ移動
                _mainCameraTransform.position = Vector3.SmoothDamp(_mainCameraTransform.position, targetCameraPos, ref _posVelocity, _zoomPositionSmoothTime);

                // 画角を通常・クローズアップFOVへ絞り込む
                if (_targetCamera != null)
                {
                    _targetCamera.fieldOfView = Mathf.SmoothDamp(_targetCamera.fieldOfView, _finalFOV, ref _fovVelocity, _zoomPositionSmoothTime);
                }

                // 注視点へLookAt回転
                Vector3 lookDir = (targetLookTarget - _mainCameraTransform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Vector3 fieldUp = FieldContext.Rotation * Vector3.up;
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, fieldUp);
                    _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, Time.deltaTime * _rotationSmoothSpeed);
                }

                // カメラワーク完了検知
                if (!_notifiedDone && Vector3.Distance(_mainCameraTransform.position, targetCameraPos) < 0.15f)
                {
                    _notifiedDone = true;
                    OnCameraWorkComplete();
                }
            }
            else
            {
                // phase 1: 爆走フォーカス
                Vector3 targetLookTarget = _shopVehicleTransform.position + (FieldContext.Rotation * _lookAtOffsetCenterCalculated());

                // 屋台に並走・追従するカメラ座標
                Vector3 rotatedBrakeOffset = FieldContext.Rotation * _cruiseOffsetFromVehicle;
                Vector3 targetCameraPos = _shopVehicleTransform.position + rotatedBrakeOffset;

                _mainCameraTransform.position = Vector3.SmoothDamp(_mainCameraTransform.position, targetCameraPos, ref _posVelocity, _followSmoothTime);

                // 広角FOVを適用してスピード感をブースト
                if (_targetCamera != null)
                {
                    _targetCamera.fieldOfView = Mathf.SmoothDamp(_targetCamera.fieldOfView, _cruiseFOV, ref _fovVelocity, _followSmoothTime);
                }

                // 屋台をロックオン
                Vector3 lookDir = (targetLookTarget - _mainCameraTransform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    Vector3 fieldUp = FieldContext.Rotation * Vector3.up;
                    Quaternion targetRot = Quaternion.LookRotation(lookDir, fieldUp);
                    _mainCameraTransform.rotation = Quaternion.Slerp(_mainCameraTransform.rotation, targetRot, Time.deltaTime * _rotationSmoothSpeed);
                }
            }
        }

        private Vector3 _lookAtOffsetCenterCalculated()
        {
            return _lookAtOffsetFromCenter;
        }

        private void OnCameraWorkComplete()
        {
            Debug.Log("[ShopCinematicCamera] 回り込みズーム完了！画角がピタッと一致したぜよ。");

            OnCompleteCameraWork?.Invoke();
        }

        /// <summary>
        /// エディットモード中の確認用
        /// </summary>
        private void OnDrawGizmos()
        {
            // 非再生中、インスペクターで数値を弄った際にSceneビューに配置予想線を描画する
            if (Application.isPlaying) return;

            EnsureCameraReferences();

            // 簡易的な中心点
            Vector3 origin = transform.position;
            Quaternion rot = FieldContext.Rotation;

            // 最終構図のカメラ位置予測
            Vector3 finalCamPos = origin + (rot * _finalAngleOffsetFromCenter);
            Vector3 finalLookTarget = origin + (rot * _lookAtOffsetFromCenter);

            // 爆走追従時のカメラ位置予測
            Vector3 cruiseCamPos = origin + (rot * _cruiseOffsetFromVehicle);

            // 最終カメラ位置を青球で表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(finalCamPos, 0.4f);
            Gizmos.DrawLine(finalCamPos, finalLookTarget);

            // 注視点を赤球で表示
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(finalLookTarget, 0.2f);

            // 爆走期追従位置を緑球で表示
            Gizmos.color = new Color(0f, 1f, 0f, 0.5f);
            Gizmos.DrawSphere(cruiseCamPos, 0.3f);
            Gizmos.DrawLine(cruiseCamPos, finalLookTarget);
        }
    }
}
