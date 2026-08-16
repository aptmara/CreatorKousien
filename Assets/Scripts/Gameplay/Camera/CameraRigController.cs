// ------------------------------------------------------------
// File		: CameraRigController.cs
// Summary	: プレイヤー追従と、Z座標に応じたカメラの画角を滑らかにブレンドするクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
// Updated	: 2026-06-02 (固定カメラ・Ortho / Persp動的切り替え対応)
// Updated  : 2026-07-07 (ショップ演出用の一時無効化フラグを拡張)
// Updated  : 2026-08-16 (次のStageへ進む際に、カメラの位置を即座に更新する処理を追加)
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;
using Game.Core.Events;
using UnityEngine.Rendering.Universal;

namespace Game.Gameplay.Cameras
{
    /// <summary>
    /// プレイヤー追従と、Z座標に応じたカメラの画角を滑らかにブレンドするクラス
    /// </summary>
    public class CameraRigController : MonoBehaviour
    {
        // 演出モード中かどうかを管理するフラグ
        private bool _isCinematicMode = false;

        public enum ProjectionMode
        {
            Orthographic,
            Perspective,
            tiltFloorPers,
        }

        [Header("モード設定")]
        [Tooltip("チェックを入れると、昔のプレイヤー追従・ブレンド挙動に戻ります")]
        [SerializeField] private bool _useLegacyFollowMode = false;

        [Header("コンポーネント参照")]
        [Tooltip("実際に制御する対象のカメラ (未設定時はMainCameraを自動取得)")]
        [SerializeField] private Camera _targetCamera;

        #region Legacy Fields
        // 変数宣言
        // -----------------------------------------------------------
        [Space(10)]
        [Header("---------- Legacy ----------")]
        [Header("コンポーネント参照")]
        [Tooltip("実際に画角を動かす対象のカメラ")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("追従対象(プレイヤーのRootなど)")]
        [SerializeField] private Transform _targetTransform;

        // 最後に適用した固定カメラ設定
        private StaticCameraConfig _lastStaticConfig;
        private ProjectionMode _lastProjectionMode;


        [Header("X座標固定設定")]
        [Tooltip("カメラが固定されるX座標（左右に動かさないため）")]
        [SerializeField] private float _fixedX = 0f;

        [Header("フェーズ1：手前で落とす時（ワールド固定）")]
        [Tooltip("このZ座標より手前なら、カメラのY/Z座標と回転を完全に固定します")]
        [SerializeField] private float _dropLineZ = 0f;

        [Tooltip("固定するカメラのワールドY座標（高さ）")]
        [SerializeField] private float _dropWorldY = 15f;

        [Tooltip("固定するカメラのワールドZ座標（引き具合）")]
        [SerializeField] private float _dropWorldZ = -12f;

        [Tooltip("固定するカメラの回転（オイラー角。崖下を見下ろす角度など）")]
        [SerializeField] private Vector3 _dropEulerAngles = new Vector3(50f, 0f, 0f);

        [Header("フェーズ2：奥で集める時（プレイヤー相対追従）")]
        [Tooltip("このZ座標より奥なら、プレイヤーのZ/Y軸に完全追従します")]
        [SerializeField] private float _collectLineZ = 15f;

        [Tooltip("プレイヤーからの相対位置（Xは無視されます。前方を広く見たい場合はZをマイナスに）")]
        [SerializeField] private Vector3 _collectOffset = new Vector3(0f, 10f, -8f);

        [Tooltip("追従時のカメラの回転（オイラー角。少し前を向く角度など）")]
        [SerializeField] private Vector3 _collectEulerAngles = new Vector3(35f, 0f, 0f);

        [Header("補間設定")]
        [Tooltip("カメラが目標位置に移動する滑らかさ (0に近いほど機敏)")]
        [SerializeField] private float _positionSmoothTime = 0.2f;

        [Tooltip("カメラが目標回転に変化する滑らかさ (秒)")]
        [SerializeField] private float _rotationSmoothTime = 0.3f;

        [Header("フォーカス設定")]
        [Tooltip("フォーカス時の回転の滑らかさ")]
        [SerializeField] private float _focusRotationSmoothTime = 0.1f;

        [Tooltip("フォーカスを開始する敵との距離")]
        [SerializeField] private float _focusTriggerDistance = 15f;

        [Tooltip("ヒット時のフォーカス持続時間")]
        [SerializeField] private float _hitFocusDuration = 0.5f;

        private Vector3 _currentVelocity;
        private Transform _focusTarget;
        private float _focusTimer;
        #endregion


        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 初期位置を設定
        /// </summary>
        private void Start()
        {
            // 初期位置を設定
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            UpdateCameraTransformInstant();
        }

        /// <summary>
        /// 外部から、通常の追従・固定処理を一時停止させるための関数
        /// </summary>
        public void SetCinematicModeActive(bool active)
        {
            _isCinematicMode = active;

            if (!active)
            {
                _currentVelocity = Vector3.zero;

                _focusTarget = null;
                _focusTimer = 0f;
            }
            else
            {
                // 演出モードに入るときに、SmoothDamp の内部速度などをリセットする
                _currentVelocity = Vector3.zero;
            }
        }

        /// <summary>
        /// カメラの位置と回転を更新する
        /// </summary>
        private void LateUpdate()
        {
            // 演出カメラワークが作動している間は通常のカメラ固定・追従処理をスキップ
            if (_isCinematicMode)
            {
                return;
            }

            if (_targetTransform == null)
            {
                return;
            }

            #region Legacy Update Logic
            // 1. Z座標の割合を算出 (0.0: 手前 ～ 1.0: 奥)
            float phaseRatio = Mathf.InverseLerp(_dropLineZ, _collectLineZ, _targetTransform.position.z);

            // 2. 「手前で落とす時」の目標座標と回転を計算
            Vector3 dropTargetPos = new Vector3(_fixedX, _dropWorldY, _dropWorldZ);
            Quaternion dropTargetRot = Quaternion.Euler(_dropEulerAngles);

            // 3. 「奥で集める時」の目標座標と回転を計算
            Vector3 collectTargetPos = new Vector3(_fixedX, _targetTransform.position.y + _collectOffset.y, _targetTransform.position.z + _collectOffset.z);
            Quaternion collectTargetRot = Quaternion.Euler(_collectEulerAngles);

            // 4. 割合に応じて、2つの目標をブレンド（Lerp）
            Vector3 finalTargetPos = Vector3.Lerp(dropTargetPos, collectTargetPos, phaseRatio);
            Quaternion finalTargetRot = Quaternion.Euler(Vector3.Lerp(_dropEulerAngles, _collectEulerAngles, phaseRatio));

            // 5. フォーカス対象があれば位置と回転を調整（プレイヤーとターゲットの両方を収める）
            if (_focusTimer > 0 && _focusTarget != null)
            {
                _focusTimer -= Time.deltaTime;

                // ターゲットとの中間地点を目標にする
                Vector3 playerPos = _targetTransform.position;
                Vector3 enemyPos = _focusTarget.position;
                Vector3 midPoint = (playerPos + enemyPos) * 0.5f;

                // ターゲットとの距離に応じてカメラを引き、両方を画角に入れる
                float distance = Vector3.Distance(playerPos, enemyPos);

                // 距離に基づいた高さのオフセット計算（簡易版: 距離の半分 + 基本オフセット）
                float heightOffset = Mathf.Max(_collectOffset.y, distance * 0.8f);
                float zOffset = Mathf.Min(_collectOffset.z, -distance * 0.5f);

                // 目標位置を中間地点基準で再計算
                Vector3 focusTargetPos = new Vector3(_fixedX, midPoint.y + heightOffset, midPoint.z + zOffset);
                finalTargetPos = focusTargetPos;

                // 中間地点を向く
                Vector3 lookDir = (midPoint - transform.position).normalized;
                if (lookDir != Vector3.zero)
                {
                    finalTargetRot = Quaternion.LookRotation(lookDir);
                }
            }
            else
            {
                _focusTarget = null;
                _focusTimer = 0;
            }

            // 6. カメラ自身を滑らかに移動・回転させる
            transform.position = Vector3.SmoothDamp(transform.position, finalTargetPos, ref _currentVelocity, _positionSmoothTime);

            float rotSmooth = (_focusTarget != null) ? _focusRotationSmoothTime : _rotationSmoothTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, finalTargetRot, Time.deltaTime / rotSmooth);
            #endregion
        }

        /// <summary>
        /// 指定された設定データとモードに基づいて、カメラを固定配置する。
        /// </summary>
        /// <param name="config">カメラ配置アセット</param>
        /// <param name="mode">投影モード</param>
        public void SetupStaticCamera(StaticCameraConfig config, ProjectionMode mode)
        {
            // レガシーモードがオンの場合は、暴発防止でスキップ
            if (_useLegacyFollowMode) return;

            if (config == null)
            {
                Debug.LogError("[CameraRigController] 設定アセットが空です。");
                return;
            }

            // Stage移行後に同じ画角へ戻せるよう、適用した設定を覚えておく
            _lastStaticConfig = config;
            _lastProjectionMode = mode;

            if (_targetCamera == null) _targetCamera = Camera.main;
            if (_targetCamera == null) return;

            // アセットから指定モードの構造体データを取得
            StaticCameraConfig.ProjectionSettings settings = config.GetSettings(mode);

            // 1. 投影法の切り替え
            _targetCamera.orthographic = (mode == ProjectionMode.Orthographic);

            // 2. モード固有値の適用
            if (_targetCamera.orthographic)
            {
                _targetCamera.orthographicSize = settings.OrthographicSize;
            }
            else
            {
                _targetCamera.fieldOfView = settings.FieldOfView;
            }

            // 3. トランスフォームの適用
            Transform targetXform = (_cameraTransform != null) ? _cameraTransform : transform;
            targetXform.position = settings.Position;
            targetXform.eulerAngles = settings.Rotation;

            // 4. PostProcessingwをOnにする
            UniversalAdditionalCameraData UniCameraData = _targetCamera.GetUniversalAdditionalCameraData();
            UniCameraData.renderPostProcessing = true;
        }


        /// <summary>
        /// 最後に適用した固定カメラ設定を復元する
        /// </summary>
        public void RestoreStaticCamera()
        {
            if (_lastStaticConfig == null)
            {
                return;
            }

            SetupStaticCamera(_lastStaticConfig, _lastProjectionMode);
        }


        /// <summary>
        /// 統合シーン側から追従対象を差し替える。
        /// </summary>
        /// <param name="targetTransform">追従する対象Transform</param>
        public void SetTarget(Transform targetTransform)
        {
            _targetTransform = targetTransform;
            if (_useLegacyFollowMode) UpdateCameraTransformInstant();
        }

        /// <summary>
        /// 敵などに一定時間フォーカスを合わせる
        /// </summary>
        /// <param name="target">フォーカス対象</param>
        /// <param name="duration">時間</param>
        public void SetFocusTarget(Transform target, float duration)
        {
            if (_useLegacyFollowMode) return;
            _focusTarget = target;
            _focusTimer = duration;
        }

        /// <summary>
        /// 開始時に現在の位置に応じた設定を即座に反映させるための関数
        /// </summary>
        private void UpdateCameraTransformInstant()
        {
            if (_targetTransform == null)
            {
                return;
            }

            float phaseRatio = Mathf.InverseLerp(_dropLineZ, _collectLineZ, _targetTransform.position.z);

            // 「手前で落とす時」の目標座標と回転を計算
            Vector3 dropTargetPos = new Vector3(_fixedX, _dropWorldY, _dropWorldZ);
            Vector3 collectTargetPos = new Vector3(_fixedX, _targetTransform.position.y + _collectOffset.y, _targetTransform.position.z + _collectOffset.z);

            // 割合に応じて、2つの目標をブレンド（Lerp）
            Vector3 finalTargetPos = Vector3.Lerp(dropTargetPos, collectTargetPos, phaseRatio);
            Vector3 finalTargetEuler = Vector3.Lerp(_dropEulerAngles, _collectEulerAngles, phaseRatio);

            transform.position = finalTargetPos;
            transform.rotation = Quaternion.Euler(finalTargetEuler);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnHitBatch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnHitBatch);
        }

        private void OnHitBatch(EnemyHitBatchEvent ev)
        {
            if (!_useLegacyFollowMode || ev.EnemyTransform == null || _targetTransform == null) return;

            // プレイヤーと敵が近い場合のみツーショット・フォーカスをかける
            float dist = Vector3.Distance(_targetTransform.position, ev.EnemyTransform.position);
            if (dist <= _focusTriggerDistance)
            {
                SetFocusTarget(ev.EnemyTransform, _hitFocusDuration);
            }
        }
    }
}
