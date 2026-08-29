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
using Game.Core.Events;
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

        [SerializeField]
        [Tooltip("画面内判定に使用するボス本体のRenderer")]
        private Renderer _bossBodyRenderer;


        [Header("--- カメラ参照 ---")]

        [SerializeField]
        [Tooltip("制御対象のカメラ。未設定の場合はMainCameraを自動取得する")]
        private Camera _targetCamera;

        [SerializeField]
        [Tooltip("通常のカメラ制御を停止するCameraRigController。未設定の場合は実行時に自動取得する")]
        private CameraRigController _cameraRigController;


        [Header("--- 開幕中のカメラ振動 ---")]

        [SerializeField]
        [Tooltip("開幕演出中, ボスが画面内にいる間カメラを振動させる")]
        private bool _isRumbleEnabled = true;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("振動パルスを発行する間隔(秒)")]
        private float _rumblePulseInterval = 0.1f;

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("振動パルスの持続時間(秒)")]
        private float _rumblePulseDuration = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("揺れの位置の強さ(単位: メートル)")]
        private float _rumblePositionStrength = 0.06f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("揺れの回転の強さ(単位: 度)")]
        private float _rumbleRotationStrength = 0.8f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("揺れの振動数(単位: Hz)")]
        private float _rumbleFrequency = 25f;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("ボスが画面端にいるときの振動倍率")]
        private float _rumbleEdgeStrengthMultiplier = 0.2f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("ボスが画面中央にいるときの振動倍率")]
        private float _rumbleCenterStrengthMultiplier = 1.8f;

        [SerializeField]
        [Range(0.25f, 4f)]
        [Tooltip("山の形。大きいほど中央だけ強くなる")]
        private float _rumbleMountainSharpness = 1.3f;

        [SerializeField]
        [Tooltip("画面端から中央へ移動するときの振動変化")]
        private AnimationCurve _rumbleCenterCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        [Range(0f, 0.5f)]
        [Tooltip("一定の揺れにならないように加える細かな強弱")]
        private float _rumbleBreathingAmount = 0.12f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("細かな強弱が一周する時間")]
        private float _rumbleBreathingCycleDuration = 0.7f;


        [Header("--- 開幕警告UI ---")]

        [SerializeField, Min(0f)]
        [Tooltip("警告開始からカメラが引き始めるまでの時間")]
        private float _warningLeadInDuration = 0.5f;

        [SerializeField, Min(0f)]
        [Tooltip("警告終了からボスが上昇し始めるまでの時間")]
        private float _warningFadeOutWaitDuration = 0.3f;


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

        /// <summary>
        /// 開幕中のカメラ振動を管理しているコルーチン
        /// </summary>
        private Coroutine _introRumbleRoutine;

        /// <summary>
        /// 開幕警告UIが表示中かどうか
        /// </summary>
        private bool _isIntroWarningActive;

        /// <summary>
        /// アニメーション開始まで隠しておくボスのRenderer
        /// </summary>
        private Renderer[] _bossVisualRenderers;



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

        /// <summary>
        /// 画面内判定に使用するボス本体のRenderer
        /// </summary>
        public Renderer BossBodyRenderer => _bossBodyRenderer;


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

            CacheBossVisualRenderers();

            // 生成された瞬間から見た目だけ非表示
            SetBossVisualsHidden(true);
        }


        /// <summary>
        /// コンポーネントが無効化された場合は、
        /// ゲームクリアへの引き渡し済みでない限り通常カメラへ戻す
        /// </summary>
        private void OnDisable()
        {
            _isCancellationRequested = true;
            IsPlaying = false;

            SetBossVisualsHidden(false);

            StopCameraRestoreRoutine();
            StopIntroRumbleRoutine();

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

            if (_bossBodyRenderer == null && _animationController != null)
            {
                _bossBodyRenderer = _animationController.GetComponentInChildren<Renderer>();
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

        /// <summary>
        /// Animator配下のボスモデルRendererを取得する
        /// </summary>
        private void CacheBossVisualRenderers()
        {
            if (_animationController == null || _animationController.Animator == null)
            {
                _bossVisualRenderers = null;
                return;
            }

            _bossVisualRenderers = _animationController.Animator.GetComponentsInChildren<Renderer>(true);
        }

        /// <summary>
        /// Animatorや当たり判定を止めず、見た目だけ非表示にする
        /// </summary>
        private void SetBossVisualsHidden(bool isHidden)
        {
            if (_bossVisualRenderers == null)
            {
                return;
            }

            foreach (Renderer bossRenderer in _bossVisualRenderers)
            {
                if (bossRenderer == null)
                {
                    continue;
                }

                bossRenderer.forceRenderingOff = isHidden;
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
            StopIntroRumbleRoutine();

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
            bool didPrepareIntroPose = false;

            // ボスを画面外の開始位置へ配置
            // ------------------------------------------------------------

            if (presentationData.IsEnabled)
            {
                _animationController.ResetForPhaseStart();

                // ボスを画面外の開始位置に移動させる
                didPrepareIntroPose = _animationController.PrepareBossIntroPose(baseStepData, presentationData);

                if (!didPrepareIntroPose)
                {
                    Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] 開幕アニメーションの開始位置の準備に失敗しました。", this);
                }
            }


            // 警告UIを先に表示
            // ------------------------------------------------------------

            if (didPrepareIntroPose)
            {
                StartIntroWarning();

                // 警告を見せてからカメラを動かす
                yield return WaitForPresentationSeconds(_warningLeadInDuration);
            }

            if (_isCancellationRequested)
            {
                EndIntroWarning();
                IsPlaying = false;
                yield break;
            }


            // 警告を表示したままカメラを引く
            // ------------------------------------------------------------

            yield return MoveCameraToBattlePosition(presentationData);

            if (_isCancellationRequested)
            {
                EndIntroWarning();
                IsPlaying = false;
                yield break;
            }


            // カメラが引き終わったら警告を消す
            // ------------------------------------------------------------

            if (didPrepareIntroPose)
            {
                // カメラが引き終わったら警告を終了する
                EndIntroWarning();

                yield return WaitForPresentationSeconds(_warningFadeOutWaitDuration);
            }

            if (_isCancellationRequested)
            {
                EndIntroWarning();
                IsPlaying = false;
                yield break;
            }


            // 警告が消えてからボスの上昇開始
            // ------------------------------------------------------------

            if (didPrepareIntroPose)
            {
                didStartAnimation = _animationController.PlayBossIntro(baseStepData, presentationData);

                if (didStartAnimation)
                {
                    // Animator.Playが反映されるまで1フレーム待つ
                    yield return WaitForPresentationSeconds(0.1f);

                    // アニメーションが開始された姿勢から表示
                    SetBossVisualsHidden(false);
                }
                else
                {
                    // 再生失敗時に非表示のまま残さない
                    SetBossVisualsHidden(false);

                    Debug.LogWarning($"[{nameof(BossIntroPresentationController)}] 開幕アニメーションの再生に失敗しました。", this);
                }
            }
            else
            {
                // 開始姿勢の準備失敗時にも非表示を解除
                SetBossVisualsHidden(false);
            }

            // ボスが画面へ入ったらカメラを振動させる
            if (_isRumbleEnabled && didStartAnimation)
            {
                _introRumbleRoutine = StartCoroutine(PlayIntroRumbleRoutine());
            }


            // 上昇アニメショーンの終了を待つ
            // ------------------------------------------------------------

            if (didStartAnimation)
            {
                while (!_isCancellationRequested && !_animationController.IsCurrentAnimationFinished())
                {
                    yield return null;
                }
            }

            if (_isCancellationRequested)
            {
                IsPlaying = false;
                yield break;
            }


            // 上昇後の余韻
            // ------------------------------------------------------------

            float afterAnimationDelay = Mathf.Max(0f, presentationData.AfterAnimationDelay);

            yield return WaitForPresentationSeconds(afterAnimationDelay);


            // イントロ終了
            // ------------------------------------------------------------

            EndIntroWarning();

            IsPlaying = false;
            StopIntroRumbleRoutine();
        }


        private float CalculateIntroRumbleMultiplier(out float mountainAmount)
        {
            mountainAmount = 0f;

            if (_targetCamera == null || _bossBodyRenderer == null)
            {
                return 0f;
            }

            Vector3 viewportPosition = _targetCamera.WorldToViewportPoint(_bossBodyRenderer.bounds.center);

            if (viewportPosition.z <= 0f)
            {
                return 0f;
            }

            float viewportX = Mathf.Clamp01(viewportPosition.x);

            // X=0で0、X=0.5で1、X=1で0になる山型
            // 負の値にならないようにmaxで0を下限にする
            mountainAmount = Mathf.Max(0.0f, Mathf.Sin(viewportX * Mathf.PI));

            // 山の尖り具合を調整
            mountainAmount = Mathf.Pow(mountainAmount, _rumbleMountainSharpness);

            return Mathf.Lerp(_rumbleEdgeStrengthMultiplier, _rumbleCenterStrengthMultiplier, mountainAmount);
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

            Vector3 targetPosition = _normalCameraPosition + presentationData.CameraPositionOffset;

            Quaternion targetRotation = _normalCameraRotation * Quaternion.Euler(presentationData.CameraEulerAnglesOffset);

            float moveDuration = Mathf.Max(0f, presentationData.CameraMoveDuration);

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

            SetBossVisualsHidden(false);

            if (_animationController != null)
            {
                _animationController.CancelCurrentAnimation();
            }

            if (!_isBattleCameraActive || !_hasSavedNormalCameraPose)
            {
                return;
            }

            StopCameraRestoreRoutine();
            StopIntroRumbleRoutine();

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
            StopIntroRumbleRoutine();

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


        // 開幕中のカメラ振動
        // ------------------------------------------------------------

        /// <summary>
        /// ボスが画面内にいる間、カメラを振動させるコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayIntroRumbleRoutine()
        {
            while (IsPlaying && !_isCancellationRequested)
            {
                if (IsBossVisibleOnScreen())
                {
                    float rumbleMultiplier = CalculateIntroRumbleMultiplier(out float centerAmount);

                    float positionStrength = _rumblePositionStrength * rumbleMultiplier;

                    float rotationStrength = _rumbleRotationStrength * rumbleMultiplier;

                    // 中央へ近づくほど振動を少し細かくする
                    float frequency = Mathf.Lerp(_rumbleFrequency * 0.7f, _rumbleFrequency * 1.15f, centerAmount);

                    EventBus.Publish(new CameraShakeRequestedEvent(
                        _rumblePulseDuration,
                        positionStrength,
                        rotationStrength,
                        frequency));

                    yield return new WaitForSeconds(_rumblePulseInterval);
                }
                else
                {
                    yield return null;
                }
            }

            _introRumbleRoutine = null;
        }


        /// <summary>
        /// 開幕演出がキャンセルされていない間だけ、指定された秒数待機する
        /// </summary>
        private IEnumerator WaitForPresentationSeconds(float duration)
        {
            float safeDuration = Mathf.Max(0f, duration);
            float elapsedTime = 0f;

            while (!_isCancellationRequested && elapsedTime < safeDuration)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }


        /// <summary>
        /// ボスがカメラの画面内に表示されているかどうかを判定する
        /// </summary>
        /// <returns></returns>
        private bool IsBossVisibleOnScreen()
        {
            if (_targetCamera == null || _bossBodyRenderer == null)
            {
                return false;
            }

            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_targetCamera);

            // ボスの描画範囲が少しでも画面に入っていれば「見えている」とみなす
            return GeometryUtility.TestPlanesAABB(frustumPlanes, _bossBodyRenderer.bounds);
        }

        /// <summary>
        /// 実行中のカメラ振動コルーチンを停止する
        /// </summary>
        private void StopIntroRumbleRoutine()
        {
            if (_introRumbleRoutine != null)
            {
                StopCoroutine(_introRumbleRoutine);
                _introRumbleRoutine = null;
            }

            // イントロ中断時にも警告を残さない
            EndIntroWarning();
        }

        /// <summary>
        /// 開幕警告UIを表示する
        /// </summary>
        private void StartIntroWarning()
        {
            if (_isIntroWarningActive)
            {
                return;
            }
            _isIntroWarningActive = true;
            EventBus.Publish(new BossThornWarningStartedEvent());
        }


        /// <summary>
        /// 開幕警告UIを非表示にする
        /// </summary>
        private void EndIntroWarning()
        {
            if (!_isIntroWarningActive)
            {
                return;
            }

            _isIntroWarningActive = false;
            EventBus.Publish(new BossThornWarningEndedEvent());
        }
    }
}
