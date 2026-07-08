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
        public event Action OnBrakeAnimationComplete;

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
        [Tooltip("プレイヤーの原点が足元ではなく中心にある場合の高さの補正")]
        [SerializeField] private float _playerHeightOffset = -1.91f;
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
        private bool _isBrakeAnimationPlaying = false;

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
            if (_currentState != VehicleState.Stationary && _currentState != VehicleState.Braking)
            {
                return;
            }

            StartCoroutine(DismissAnimationRoutine());
        }

        /// <summary>
        /// 退出時のカートゥーン演出
        /// </summary>
        private IEnumerator DismissAnimationRoutine()
        {
            // 位置リセット
            _lastPosition = transform.position;
            Vector3 basePosition = transform.position;

            // phase 1: 1回目の小ジャンプ (0.2s)
            // ----------------------------------------------------------------
            float dur1 = 0.2f;
            float elaps1 = 0f;
            while (elaps1 < dur1)
            {
                elaps1 += Time.deltaTime;
                float t = elaps1 / dur1;
                float height = Mathf.Sin(t * Mathf.PI) * 0.4f;
                transform.position = basePosition + (FieldContext.Rotation * Vector3.up * height);

                // 空中にいるときは少し縦伸び
                _distortion.SquashY = height * 0.5f;
                yield return null;
            }

            // 着地で一瞬潰す
            _distortion.SquashY = -0.2f;
            yield return new WaitForSeconds(0.05f);

            // phase 2: 2回目の大ジャンプ (0.25s)
            // ----------------------------------------------------------------
            float dur2 = 0.25f;
            float elaps2 = 0f;
            while (elaps2 < dur2)
            {
                elaps2 += Time.deltaTime;
                float t = elaps2 / dur2;
                float height = Mathf.Sin(t * Mathf.PI) * 0.8f;
                transform.position = basePosition + (FieldContext.Rotation * Vector3.up * height);

                _distortion.SquashY = height * 0.6f;
                yield return null;
            }

            // 着地リセット & 潰す
            transform.position = basePosition;
            _distortion.SquashY = -0.3f;

            // phase 3: 一瞬溜める予備動作
            // ----------------------------------------------------------------
            float durBrake = 0.15f;
            float elapsBrake = 0f;
            while (elapsBrake < durBrake)
            {
                elapsBrake += Time.deltaTime;
                float t = elapsBrake / durBrake;

                _distortion.ShearX = Mathf.Lerp(0f, -0.6f, t);
                _distortion.SquashY = Mathf.Lerp(-0.3f, -0.2f, t);
                yield return null;
            }

            // phase 4: 爆走退出開始ぜよ！
            // ----------------------------------------------------------------

            // 本来のハケ移動ステートへ移行
            _currentState = VehicleState.Exiting;
            _currentWaypointIndex = 0;
            _lastPosition = transform.position;
        }

        private void Update()
        {
            if (_currentState == VehicleState.Inactive) return;

            // 毎フレーム、物理的な移動速度を計算してシアーにフィードバック
            Vector3 deltaMove = transform.position - _lastPosition;

            float currentFrameSpeed = 0f;
            if (Time.deltaTime > 0f)
            {
                currentFrameSpeed = deltaMove.magnitude / Time.deltaTime;

                if (float.IsNaN(currentFrameSpeed) || float.IsInfinity(currentFrameSpeed))
                {
                    currentFrameSpeed = 0f;
                }
            }

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
                    if (!_isBrakeAnimationPlaying)
                    {
                        StartCoroutine(BrakeAnimationRoutine());
                    }
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

            // プレイヤーの向いている方向や位置に応じてオフセットを掛ける
            Quaternion fieldRot = FieldContext.Rotation;
            Vector3 rotatedOffset = fieldRot * _stopOffsetFromPlayer;

            Vector3 fieldUp = fieldRot * Vector3.up;
            _dynamicStopPosition = _playerTarget.position + rotatedOffset + (fieldUp * _playerHeightOffset);

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
            if (speed > _moveSpeed * 2f)
            {
                Debug.LogWarning($"[VehicleWarning] 速度が異常値になっとるぜよ！ Speed: {speed}");
            }

            float speedRatio = Mathf.Clamp01(speed / _moveSpeed);
            _distortion.ShearX = speedRatio * -_maxCruiseShear;
            _distortion.SquashY = 0f;
        }

        private void TriggerBrakeImpact()
        {
            _currentState = VehicleState.Braking;
        }

        /// <summary>
        /// 前傾縦伸び -> 平べったく(反動) -> 通常に戻る -> 終了
        /// </summary>
        private IEnumerator BrakeAnimationRoutine()
        {
            _isBrakeAnimationPlaying = true;

            // phase 1: 前傾しながら縦伸び (0s ~ 0.15s)
            float duration1 = 0.15f;
            float elapsed1 = 0f;
            while (elapsed1 < duration1)
            {
                elapsed1 += Time.deltaTime;
                float t = elapsed1 / duration1;

                _distortion.ShearX = Mathf.Lerp(0f, 0.6f, t);
                _distortion.SquashY = Mathf.Lerp(0f, 0.5f, t);
                yield return null;
            }

            // phase 2: 反動でぺちゃんこになる (0.15s ~ 0.35s)
            float duration2 = 0.20f;
            float elapsed2 = 0f;
            while (elapsed2 < duration2)
            {
                elapsed2 += Time.deltaTime;
                float t = elapsed2 / duration2;

                _distortion.ShearX = Mathf.Lerp(0.6f, -0.2f, t);
                _distortion.SquashY = Mathf.Lerp(0.5f, -0.4f, t);
                yield return null;
            }

            // phase 3: 通常サイズに戻る (0.35s ~ 0.5s)
            float duration3 = 0.15f;
            float elapsed3 = 0f;
            while (elapsed3 < duration3)
            {
                elapsed3 += Time.deltaTime;
                float t = elapsed3 / duration3;

                // 通常形状へ戻す
                _distortion.ShearX = Mathf.Lerp(-0.2f, 0f, t);
                _distortion.SquashY = Mathf.Lerp(-0.4f, 0f, t);
                yield return null;
            }

            // 完全に初期値にリセット
            _distortion.ShearX = 0f;
            _distortion.SquashY = 0f;

            Debug.Log("[VehicleBrake] カートゥーン急ブレーキ演出が完璧に終了ぜよ！");

            // ステートを停止中に変更
            _currentState = VehicleState.Stationary;
            _isBrakeAnimationPlaying = false;

            // イベント発火
            OnBrakeAnimationComplete?.Invoke();
        }

        private void OnDisable()
        {
            Debug.Log($"[VehicleDisable] ShopVehicleController が非アクティブ化されました！その時の値 -> SquashY: {_distortion.SquashY}, ShearX: {_distortion.ShearX}", this);
        }
    }
}
