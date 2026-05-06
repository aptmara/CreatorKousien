// ------------------------------------------------------------
// File		: CameraRigController.cs
// Summary	: プレイヤー追従と、Z座標に応じたカメラの画角を滑らかにブレンドするクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Cameras
{
    /// <summary>
    /// プレイヤー追従と、Z座標に応じたカメラの画角を滑らかにブレンドするクラス
    /// </summary>
    public class CameraRigController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("コンポーネント参照")]
        [Tooltip("実際に画角を動かす対象のカメラ")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("追従対象(プレイヤーのRootなど)")]
        [SerializeField] private Transform _targetTransform;

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

        private Vector3 _currentVelocity;



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
        /// カメラの位置と回転を更新する
        /// </summary>
        private void LateUpdate()
        {
            if (_targetTransform == null)
            {
                return;
            }

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

            // 5. カメラ自身を滑らかに移動・回転させる
            transform.position = Vector3.SmoothDamp(transform.position, finalTargetPos, ref _currentVelocity, _positionSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, finalTargetRot, Time.deltaTime / _rotationSmoothTime);
        }


        /// <summary>
        /// 開始時に現在の位置に応じた設定を即座に反映させるための関数
        /// </summary>
        private void UpdateCameraTransformInstant()
        {
            if (_targetTransform == null || _cameraTransform == null)
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

            _cameraTransform.position = finalTargetPos;
            _cameraTransform.rotation = Quaternion.Euler(finalTargetEuler);
        }
    }
}
