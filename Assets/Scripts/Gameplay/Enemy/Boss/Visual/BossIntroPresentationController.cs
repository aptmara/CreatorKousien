// ------------------------------------------------------------
// File     : BossIntroPresentationController.cs
// Summary  : ボスの開幕アニメーションとボス戦中の引きカメラを管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-17
//
// Notes:
// - 開幕アニメーションはボス戦全体で1回だけ再生する。
// - カメラはボス戦開始時に引き位置へ移動する。
// - 引きカメラは通常ダウンやフェーズ変更では解除しない。
// - ボス戦が中断された場合だけ通常カメラ位置へ戻す。
// - 最終撃破時はカメラを戻さず、ゲームクリア演出へ制御を渡す。
// ------------------------------------------------------------
using System.Collections;
using Game.Data.Enemy.Boss;
using Game.Gameplay.Cameras;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボス戦開始時のお披露目演出と、
    /// ボス戦全体で使用する引きカメラを管理する。
    ///
    /// このクラスが担当するもの:
    /// ・通常カメラ位置の保存
    /// ・ボス戦用の引き位置へのカメラ移動
    /// ・開幕アニメーションの再生
    /// ・ボス戦中の引きカメラ維持
    /// ・ボス戦中断時のカメラ復帰
    /// ・最終撃破演出へのカメラ制御引き渡し
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossIntroPresentationController : MonoBehaviour
    {
        [Header("--- ボス参照 ---")]

        [SerializeField]
        [Tooltip("ボスのAnimatorと開始位置を管理するコンポーネント")]
        private BossAnimationController _animationController;


        [Header("--- カメラ参照 ---")]

        [SerializeField]
        [Tooltip("制御対象のカメラ。未設定の場合はMainCameraを自動取得する")]
        private Camera _targetCamera;

        [SerializeField]
        [Tooltip("通常のカメラ制御を停止するCameraRigController。未設定の場合は実行時に自動取得する")]
        private CameraRigController _cameraRigController;


        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// 通常状態におけるカメラのワールド位置
        /// </summary>
        private Vector3 _normalCameraPosition;

        /// <summary>
        /// 通常状態におけるカメラのワールド回転
        /// </summary>
        private Quaternion _normalCameraRotation;

        /// <summary>
        /// 通常カメラの位置と回転を保存済みかどうか
        /// </summary>
        private bool _hasSavedNormalCameraPose;

        /// <summary>
        /// ボス戦用の引きカメラが有効かどうか
        /// </summary>
        private bool _isBattleCameraActive;

        /// <summary>
        /// 現在の開幕演出に対してキャンセルが要求されたかどうか
        /// </summary>
        private bool _isCancellationRequested;

        /// <summary>
        /// 現在使用している開幕演出設定
        /// </summary>
        private BossIntroPresentationData _currentPresentationData;

        /// <summary>
        /// 通常カメラ位置へ復帰させているコルーチン
        /// </summary>
        private Coroutine _cameraRestoreRoutine;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 現在、開幕演出が再生中かどうか
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// ボス戦用の引きカメラが有効かどうか
        /// </summary>
        public bool IsBattleCameraActive => _isBattleCameraActive;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時にボス側の参照を取得する
        /// </summary>
        private void Reset()
        {
            FindLocalReferences();
        }


        /// <summary>
        /// Inspector上で値が変更されたときにボス側の参照を取得する
        /// </summary>
        private void OnValidate()
        {
            FindLocalReferences();
        }


        /// <summary>
        /// 実行開始時に必要な参照を取得する
        /// </summary>
        private void Awake()
        {
            FindLocalReferences();
            ResolveRuntimeReferences();
        }


        /// <summary>
        /// コンポーネントが無効化された場合は、
        /// ゲームクリアへの引き渡し済みでない限り通常カメラへ戻す
        /// </summary>
        private void OnDisable()
        {
            _isCancellationRequested = true;
            IsPlaying = false;

            StopCameraRestoreRoutine();

            if (_hasSavedNormalCameraPose)
            {
                RestoreCameraImmediately();
            }
        }


        // 参照取得
        // ------------------------------------------------------------

        /// <summary>
        /// 実行中のシーンからボス関連の参照を取得する
        /// </summary>
        private void FindLocalReferences()
        {
            if (_animationController == null)
            {
                _animationController = GetComponentInChildren<BossAnimationController>();
            }
        }


        /// <summary>
        /// 実行中のシーンからカメラ関連の参照を取得する
        /// </summary>
        private void ResolveRuntimeReferences()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_cameraRigController == null && _targetCamera != null)
            {
                _cameraRigController = _targetCamera.GetComponentInParent<CameraRigController>();
            }

            if (_cameraRigController == null)
            {
                _cameraRigController = Object.FindFirstObjectByType<CameraRigController>();
            }
        }


        // 開幕演出
        // ------------------------------------------------------------

        /// <summary>
        /// 開幕演出の再生を開始する
        /// </summary>
        /// <param name="baseStepData">設定</param>
        /// <param name="presentationData">演出データ</param>
        /// <returns></returns>
        public IEnumerator PlayPresentation(BossThornAttackStepData baseStepData, BossIntroPresentationData presentationData)
        {
            ResolveRuntimeReferences();

            if (presentationData == null)
            {
                Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] 開幕演出設定が null です。演出をスキップします。", this);
                yield break;
            }

            if (_targetCamera == null)
            {
                Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] 制御対象のカメラが設定されていません。演出をスキップします。", this);
                yield break;
            }

            if (presentationData.IsEnabled && _animationController == null)
            {
                Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] ボスのAnimatorを管理するコンポーネントが設定されていません。演出をスキップします。", this);
                yield break;
            }

            if (presentationData.IsEnabled && baseStepData == null)
            {
                Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] ボスの開始位置を管理するデータが設定されていません。演出をスキップします。", this);
                yield break;
            }

            // 前回のカメラ復帰処理が残っている場合は停止する
            StopCameraRestoreRoutine();

            _currentPresentationData = presentationData;
            _isCancellationRequested = false;
            IsPlaying = true;

            // ボス戦開始前の通常カメラ位置を1回だけ保存する
            SaveNormalCameraPose();

            // 通常のカメラ追従・固定処理を停止する
            if (_cameraRigController != null)
            {
                _cameraRigController.SetCinematicModeActive(true);
            }

            _isBattleCameraActive = true;

            bool didStartAnimation = false;

            if (presentationData.IsEnabled)
            {
                // 前回のAnimator姿勢やRoot Motionを初期化する
                _animationController.ResetForPhaseStart();

                // 開幕開始位置を即座に適用して、カメラ移動と同時に開幕アニメーションを開始する
                didStartAnimation = _animationController.PlayBossIntro(baseStepData, presentationData);

                if (!didStartAnimation)
                {
                    Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] 開幕アニメーションの再生に失敗しました。", this);
                }
            }

            // ボスを開幕開始位置へ移した後で、カメラをボス戦用の引き位置へ移動する
            yield return MoveCameraToBattlePosition(
                presentationData);

            if (_isCancellationRequested)
            {
                IsPlaying = false;
                yield break;
            }

            // カメラ移動中から再生している開幕アニメーションが1回分終了するまで待機する
            if (didStartAnimation)
            {
                while (!_isCancellationRequested &&
                       !_animationController.IsCurrentAnimationFinished())
                {
                    yield return null;
                }
            }

            float afterAnimationDelay = Mathf.Max(0f, presentationData.AfterAnimationDelay);

            float delayElapsedTime = 0f;

            while (!_isCancellationRequested && delayElapsedTime < afterAnimationDelay)
            {
                delayElapsedTime += Time.deltaTime;
                yield return null;
            }

            IsPlaying = false;
        }


        /// <summary>
        /// 通常カメラ位置からボス戦用の引き位置へ移動する
        /// </summary>
        /// <param name="presentationData">開幕演出設定</param>
        /// <returns>カメラ移動終了まで待機するIEnumerator</returns>
        private IEnumerator MoveCameraToBattlePosition(BossIntroPresentationData presentationData)
        {
            Transform cameraTransform = _targetCamera.transform;

            Vector3 moveStartPosition = cameraTransform.position;
            Quaternion moveStartRotation = cameraTransform.rotation;

            Vector3 targetPosition =  _normalCameraPosition + presentationData.CameraPositionOffset;

            Quaternion targetRotation = _normalCameraRotation;

            float moveDuration =
                Mathf.Max(0f, presentationData.CameraMoveDuration);

            if (moveDuration <= 0f)
            {
                cameraTransform.position = targetPosition;
                cameraTransform.rotation = targetRotation;

                yield break;
            }

            float elapsedTime = 0f;

            while (!_isCancellationRequested && elapsedTime < moveDuration)
            {
                float normalizedTime = Mathf.Clamp01(elapsedTime / moveDuration);

                float moveProgress = EvaluateCurve(presentationData.CameraBlendCurve, normalizedTime);


                cameraTransform.position = Vector3.LerpUnclamped(moveStartPosition, targetPosition, moveProgress);
                cameraTransform.rotation = Quaternion.SlerpUnclamped(moveStartRotation, targetRotation, moveProgress);
                elapsedTime += Time.deltaTime;

                yield return null;
            }

            if (!_isCancellationRequested)
            {
                cameraTransform.position = targetPosition;
                cameraTransform.rotation = targetRotation;
            }
        }


        // カメラ状態の保存
        // ------------------------------------------------------------

        /// <summary>
        /// 通常カメラの位置と回転を保存する
        /// </summary>
        private void SaveNormalCameraPose()
        {
            if (_hasSavedNormalCameraPose || _targetCamera == null)
            {
                return;
            }

            Transform cameraTransform = _targetCamera.transform;

            _normalCameraPosition = cameraTransform.position;
            _normalCameraRotation = cameraTransform.rotation;

            _hasSavedNormalCameraPose = true;
        }


        // ボス戦中断時のカメラ復帰処理
        // ------------------------------------------------------------

        /// <summary>
        /// 開幕演出の再生を中断し、通常カメラ位置へ復帰させる。
        /// </summary>
        public void CancelPresentationAndRestoreCamera()
        {
            _isCancellationRequested = true;
            IsPlaying = false;

            if (_animationController != null)
            {
                _animationController.CancelCurrentAnimation();
            }

            if (!_isBattleCameraActive || !_hasSavedNormalCameraPose)
            {
                return;
            }

            StopCameraRestoreRoutine();

            if (!gameObject.activeInHierarchy || !isActiveAndEnabled || _currentPresentationData == null || _currentPresentationData.CameraReturnDuration <= 0f)
            {
                RestoreCameraImmediately();
                return;
            }

            _cameraRestoreRoutine = StartCoroutine(RestoreCameraRoutine());
        }


        /// <summary>
        /// 設定時間を使用して通常カメラ位置へ復帰させるコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator RestoreCameraRoutine()
        {
            if (_targetCamera == null || _currentPresentationData == null)
            {
                RestoreCameraImmediately();
                yield break;
            }

            Transform cameraTransform = _targetCamera.transform;

            Vector3 restoreStartPosition = cameraTransform.position;
            Quaternion restoreStartRotation = cameraTransform.rotation;

            float returnDuration = Mathf.Max(0.01f, _currentPresentationData.CameraReturnDuration);
            float elapsedTime = 0f;

            while (elapsedTime < returnDuration)
            {
                float normalizedTime = elapsedTime / returnDuration;

                float returnProgress = EvaluateCurve(_currentPresentationData.CameraBlendCurve, normalizedTime);

                cameraTransform.position = Vector3.LerpUnclamped(restoreStartPosition, _normalCameraPosition, returnProgress);
                cameraTransform.rotation = Quaternion.SlerpUnclamped(restoreStartRotation, _normalCameraRotation, returnProgress);

                elapsedTime += Time.deltaTime;

                yield return null;
            }

            RestoreCameraImmediately();
            _cameraRestoreRoutine = null;
        }


        /// <summary>
        /// カメラを保存していた通常への位置に即座に復帰させる
        /// </summary>
        private void RestoreCameraImmediately()
        {
            if (_targetCamera != null && _hasSavedNormalCameraPose)
            {
                Transform cameraTransform = _targetCamera.transform;

                cameraTransform.position = _normalCameraPosition;
                cameraTransform.rotation = _normalCameraRotation;
            }

            if (_cameraRigController != null && _isBattleCameraActive)
            {
                _cameraRigController.SetCinematicModeActive(false);
            }

            _hasSavedNormalCameraPose = false;
            _isBattleCameraActive = false;
            _currentPresentationData = null;
            IsPlaying = false;
        }


        // 最終撃破時のカメラ制御引き渡し
        // ------------------------------------------------------------

        /// <summary>
        /// ボス戦中の引きカメラを解除し、通常カメラ位置へ復帰させる。
        /// </summary>
        public void ReleaseCameraForBattleCompletion()
        {
            _isCancellationRequested = true;
            IsPlaying = false;

            StopCameraRestoreRoutine();

            _hasSavedNormalCameraPose = false;
            _isBattleCameraActive = false;
            _currentPresentationData = null;
        }


        // 共通処理
        // ------------------------------------------------------------

        /// <summary>
        /// 指定されたAnimationCurveを使用して、正規化された時間に対応する値を評価する。
        /// </summary>
        /// <param name="curve">アニメーションカーブ</param>
        /// <param name="normalizedTime">正規化された時間</param>
        /// <returns>評価された値</returns>
        private static float EvaluateCurve(AnimationCurve curve, float normalizedTime)
        {
            float safeNormalizedTime = Mathf.Clamp01(normalizedTime);

            return curve != null ? curve.Evaluate(safeNormalizedTime) : safeNormalizedTime;
        }


        /// <summary>
        /// 実行中のカメラ復帰コルーチンを停止する
        /// </summary>
        private void StopCameraRestoreRoutine()
        {
            if (_cameraRestoreRoutine == null)
            {
                return;
            }

            StopCoroutine(_cameraRestoreRoutine);
            _cameraRestoreRoutine = null;
        }
    }
}
