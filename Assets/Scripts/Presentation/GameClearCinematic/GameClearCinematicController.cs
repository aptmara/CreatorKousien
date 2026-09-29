// ------------------------------------------------------------
// File		: GameClearCinematicController.cs
// Summary	: ゲームクリア時のシネマティックを制御するクラス
//
// Author	: [浅野 勇生]
// Created    : 2026-07-09
// Update	d    : 2026-08-16 クリア時にエフェクトを追加するために、プレイヤーのアニメーションをクリア状態に設定する処理を追加 - 浅野
//
// Notes	:
// - 基盤制作
// ------------------------------------------------------------
using System.Collections;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Stage;
using Game.Presentation.GameOverCinematic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Presentation.GameClearCinematic
{
    /// <summary>
    /// ゲームクリア時のシネマティックを制御するクラス
    /// </summary>
    public sealed class GameClearCinematicController : MonoBehaviour
    {
        private const float DefaultFixedDeltaTime = 0.02f;                      ///< デフォルトのFixedUpdateの時間間隔

        [Header("設定SO")]
        [SerializeField] private SO_GameClearCinematicSettings _settings;       ///< ゲームクリアシネマティックの設定SO


        [Header("参照")]
        [Tooltip("カメラリグコントローラー")]
        [SerializeField] private CameraRigController _cameraRigController;

        [Tooltip("カメラのターゲット")]
        [SerializeField] private Camera _targetCamera;

        [Tooltip("プレイヤーの座標")]
        [SerializeField] private Transform _playerTransform;

        [Tooltip("プレイヤーのコントローラー")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("プレイヤーアニメーションコントローラー")]
        [SerializeField] private PlayerAnimationController _playerAnimationController;


        [Header("白フラッシュ")]
        [SerializeField] private CanvasGroup _flashCanvasGroup;


        [Header("プレイヤー表情コントローラー")]
        [SerializeField] private PlayerFaceController _playerFaceController;


        [Header("クリア時の表情")]
        [SerializeField] private string _clearStartFaceName = "ClearStart";      ///< クリア開始時の表情名

        [Header("クリア時のエフェクト")]
        [Tooltip("エフェクトのプレハブ")]
        [SerializeField] private GameObject _clearFaceVfxPrefab;

        [Tooltip("エフェクトを出す位置。未設定ならプレイヤーの位置に出す")]
        [SerializeField] private Transform _clearFaceVfxPoint;

        [Tooltip("どや顔になってからエフェクトを出すまでの待ち時間")]
        [SerializeField] private float _clearFaceVfxDelay = 0.15f;

        [Tooltip("エフェクトを片づけるまでの待ち時間")]
        [SerializeField] private float _clearFaceVfxLifetime = 0.5f;

        [Header("クリア後の門退出")]
        [SerializeField] private bool _playGateExitSequence = true;
        [SerializeField, Min(0.1f)] private float _gateWalkSpeed = 10f;
        [SerializeField, Min(0f)] private float _distancePastGate = 5f;
        [SerializeField, Min(0.01f)] private float _gateArrivalThreshold = 0.2f;
        [SerializeField, Min(0.01f)] private float _gateOpenDuration = 0.6f;
        [SerializeField, Min(0f)] private float _gateCloseDelay = 0.3f;
        [SerializeField, Min(0.01f)] private float _gateCloseDuration = 0.6f;
        [SerializeField, Min(0f)] private float _gateCameraPullbackDistance = 4f;

        [Header("クリア後の空カメラ")]
        [SerializeField, Range(0f, 89f)] private float _skyLookAngle = 65f;
        [SerializeField, Min(0.01f)] private float _skyLookDuration = 1.5f;
        [SerializeField, Min(0f)] private float _skyHoldDuration = 2f;


        private float _noiseSeed;                                                ///< ノイズシード値

        private Transform _lastHitEnemyTransform;                               ///< 最後にヒットした敵（＝トドメを刺した敵）のTransform

        private Transform _droppingEnemyTransform;                              ///< 撃破落下を開始した敵のTransform（こちらを優先して使用する）

        private GameOverGateAnchor _gateAnchor;

        private bool _isGateExitSequencePlaying;

        private bool _isShortcutSequencePlaying;

        private void Awake()
        {
            _noiseSeed = Random.value * 1000f;
            ResolveReferences();
            SetFlashAlpha(0f);
        }


        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHitForFocus);
            EventBus.Subscribe<EnemyDefeatDropStartedEvent>(OnEnemyDefeatDropStarted);
        }


        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHitForFocus);
            EventBus.Unsubscribe<EnemyDefeatDropStartedEvent>(OnEnemyDefeatDropStarted);
        }


        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.cKey.wasPressedThisFrame)
            {
                return;
            }

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                StartCoroutine(PlayGateExitSequenceFromShortcut());
            }
        }


        /// <summary>
        /// 撃破落下を開始した敵を記録しておき、クリア演出のフォーカス対象として最優先で使用する
        /// </summary>
        /// <param name="ev">撃破落下開始イベント</param>
        private void OnEnemyDefeatDropStarted(EnemyDefeatDropStartedEvent ev)
        {
            if (ev.EnemyTransform != null)
            {
                _droppingEnemyTransform = ev.EnemyTransform;
            }
        }


        /// <summary>
        /// 最後にヒットした敵を記録しておき、クリア演出のフォーカス対象にする
        /// </summary>
        /// <param name="ev">敵ヒットイベント</param>
        private void OnEnemyHitForFocus(EnemyHitBatchEvent ev)
        {
            if (ev.EnemyTransform != null)
            {
                _lastHitEnemyTransform = ev.EnemyTransform;
            }
        }


        /// <summary>
        /// ゲームクリアシネマティックの演出を再生するコルーチン
        /// </summary>
        /// <returns></returns>
        public IEnumerator PlayRoutine()
        {
            // 参照の解決
            ResolveReferences();

            if (_settings == null)
            {
                Debug.LogWarning("[GameClearCinematic] Settingsが未設定です。演出をスキップします。");
                yield break;
            }

            if (_targetCamera == null || _playerTransform == null)
            {
                Debug.LogWarning("[GameClearCinematic] カメラまたはプレイヤーの参照が未設定です。演出をスキップします。");
                yield break;
            }

            // カメラのTransformを取得
            Transform cameraTransform = _targetCamera.transform;

            if (_cameraRigController != null)
            {
                _cameraRigController.SetCinematicModeActive(true);
            }

            if (_playerController != null)
            {
                _playerController.FreezePhysics();
            }

            _playerAnimationController?.PlayIdle();

            _playerController?.RetractClearAttachment();

            // 撃破落下中の敵が分かる場合は、最後にヒットした敵よりもそちらを優先してフォーカスする
            if (_droppingEnemyTransform != null)
            {
                _lastHitEnemyTransform = _droppingEnemyTransform;
            }

            Debug.Log($"[DropDebug] クリア演出開始 target={(_lastHitEnemyTransform != null ? _lastHitEnemyTransform.name : "null")} time={Time.time:F2}"); // TODO: 動作確認後に削除

            // 倒した敵が分かる場合は敵フォーカス演出、分からない場合は従来のスロー演出を再生する
            if (_lastHitEnemyTransform != null)
            {
                yield return StartCoroutine(PlayEnemyFocusPart(cameraTransform));
                yield return StartCoroutine(PlayEnemyFallWatchPart(cameraTransform));
            }
            else
            {
                yield return StartCoroutine(PlaySlowMotionPart(cameraTransform));
            }

            yield return StartCoroutine(PlayPlayerZoomPart(cameraTransform));

            yield return StartCoroutine(PlayPlayerClearMotion(cameraTransform));

            if (_playGateExitSequence && _gateAnchor != null && _playerController != null)
            {
                yield return StartCoroutine(PlayGateExitSequenceOnce(cameraTransform));
            }

            SetFlashAlpha(0f);
        }

        private IEnumerator PlayGateExitSequenceFromShortcut()
        {
            if (_isShortcutSequencePlaying || _isGateExitSequencePlaying)
            {
                yield break;
            }

            ResolveReferences();
            if (_settings == null || _targetCamera == null || _playerTransform == null || _playerController == null || _gateAnchor == null)
            {
                Debug.LogWarning("[GameClearCinematic] Shift+C演出に必要な参照が見つかりません。");
                yield break;
            }

            _isShortcutSequencePlaying = true;

            Vector3 playerStartPosition = _playerTransform.position;
            Quaternion playerStartRotation = _playerTransform.rotation;
            Vector3 cameraStartPosition = _targetCamera.transform.position;
            Quaternion cameraStartRotation = _targetCamera.transform.rotation;
            bool wasCinematicModeActive = _cameraRigController != null
                && _cameraRigController.IsCinematicModeActive;

            if (_cameraRigController != null)
            {
                _cameraRigController.SetCinematicModeActive(true);
            }

            _playerController.FreezePhysics();
            yield return StartCoroutine(PlayPlayerZoomPart(_targetCamera.transform));
            yield return StartCoroutine(PlayPlayerClearMotion(_targetCamera.transform));
            yield return StartCoroutine(PlayGateExitSequenceOnce(_targetCamera.transform));

            _playerController.WarpTo(playerStartPosition, playerStartRotation);
            _playerController.RestoreAttachmentFromClear();
            _playerController.UnfreezePhysics();
            _playerAnimationController?.PlayIdle();
            _playerFaceController?.ResetFace();

            _targetCamera.transform.SetPositionAndRotation(cameraStartPosition, cameraStartRotation);
            _cameraRigController?.SetCinematicModeActive(wasCinematicModeActive);
            _isShortcutSequencePlaying = false;
        }

        private IEnumerator PlayPlayerClearMotion(Transform cameraTransform)
        {
            _playerController?.PrepareClearAttachmentAnimation();

            if (!string.IsNullOrEmpty(_clearStartFaceName))
            {
                _playerFaceController?.SetFace(_clearStartFaceName);
            }

            _playerAnimationController?.PlayClear();
            _playerController?.PlayPreparedClearAttachmentAnimation();
            StartCoroutine(PlayClearFaceVfxRoutine());

            yield return StartCoroutine(WaitKeepingPlayerFacingCamera(_settings.AfterClearAnimationDelay, cameraTransform));
        }

        private IEnumerator PlayGateExitSequenceOnce(Transform cameraTransform)
        {
            if (_isGateExitSequencePlaying)
            {
                yield break;
            }

            _isGateExitSequencePlaying = true;
            yield return StartCoroutine(PlayGateExitSequence(cameraTransform));
            _isGateExitSequencePlaying = false;
        }

        private IEnumerator PlayGateExitSequence(Transform cameraTransform)
        {
            _playerController.RestoreAttachmentFromClear();
            _playerAnimationController?.PlayIdle();

            Coroutine openRoutine = _gateAnchor.SetDoorOpen(true, _gateOpenDuration);
            if (openRoutine != null)
            {
                yield return openRoutine;
            }

            Vector3 targetPosition = _gateAnchor.GetPassThroughTarget(_playerTransform.position, _distancePastGate);
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            float thresholdSqr = _gateArrivalThreshold * _gateArrivalThreshold;
            Vector3 playerStartPosition = _playerTransform.position;
            Vector3 cameraStartPosition = cameraTransform.position;
            Vector3 pullbackDirection = Vector3.ProjectOnPlane(cameraStartPosition - playerStartPosition, up).normalized;
            float totalTravelDistance = Vector3.ProjectOnPlane(targetPosition - playerStartPosition, up).magnitude;

            _playerController.BeginScriptedMovement();
            while (Vector3.ProjectOnPlane(targetPosition - _playerTransform.position, up).sqrMagnitude > thresholdSqr)
            {
                _playerController.MoveScriptedTowards(targetPosition, _gateWalkSpeed);
                float remainingDistance = Vector3.ProjectOnPlane(targetPosition - _playerTransform.position, up).magnitude;
                float progress = totalTravelDistance > 0.001f
                    ? 1f - Mathf.Clamp01(remainingDistance / totalTravelDistance)
                    : 1f;
                cameraTransform.position = cameraStartPosition
                    + pullbackDirection * (_gateCameraPullbackDistance * EaseInOut(progress));
                cameraTransform.rotation = GetLookAtPlayerRotation(cameraTransform.position);
                yield return new WaitForFixedUpdate();
            }
            _playerController.EndScriptedMovement();

            if (_gateCloseDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_gateCloseDelay);
            }

            Coroutine closeRoutine = _gateAnchor.SetDoorOpen(false, _gateCloseDuration);
            if (closeRoutine != null)
            {
                yield return closeRoutine;
            }

            yield return StartCoroutine(PlaySkyLookRoutine(cameraTransform));
        }

        private IEnumerator PlaySkyLookRoutine(Transform cameraTransform)
        {
            Quaternion startRotation = cameraTransform.rotation;
            Vector3 lookDirection = Quaternion.AngleAxis(-_skyLookAngle, cameraTransform.right) * cameraTransform.forward;
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, up);

            float elapsed = 0f;
            while (elapsed < _skyLookDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseInOut(Mathf.Clamp01(elapsed / _skyLookDuration));
                cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                yield return null;
            }

            cameraTransform.rotation = targetRotation;
            if (_skyHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(_skyHoldDuration);
            }
        }

        public void RegisterGate(GameOverGateAnchor gateAnchor)
        {
            _gateAnchor = gateAnchor;
        }



        private IEnumerator PlayClearFaceVfxRoutine()
        {
            if (_clearFaceVfxPrefab == null)
            {
                yield break;
            }

            // 表情が切り替わってから少し溜めてからエフェクトを出す
            if (_clearFaceVfxDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_clearFaceVfxDelay);
            }

            Transform spawnPoint = _clearFaceVfxPoint != null ? _clearFaceVfxPoint : _playerTransform;

            if (spawnPoint == null)
            {
                Debug.LogWarning("[GameClearCinematic] エフェクトの出す位置が未設定で、プレイヤーのTransformも取得できません。エフェクトを再生できません。");
                yield break;
            }

            // プレイヤーの子にしてエフェクトを生成する
            GameObject vfxInstance = Instantiate(_clearFaceVfxPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);

            if (_clearFaceVfxLifetime <= 0f)
            {
                yield break;
            }

            // スロー演出中でも同じ秒数で片付くよう、実時間で待つ
            yield return new WaitForSecondsRealtime(_clearFaceVfxLifetime);

            if (vfxInstance != null)
            {
                Destroy(vfxInstance);
            }
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// スローモーションを開始し、倒した敵へカメラを寄せるコルーチン
        /// </summary>
        /// <param name="cameraTransform">カメラのTransform</param>
        /// <returns></returns>
        private IEnumerator PlayEnemyFocusPart(Transform cameraTransform)
        {
            Time.timeScale = _settings.SlowMotionTimeScale;
            Time.fixedDeltaTime = DefaultFixedDeltaTime * Time.timeScale;

            // スローに入った直後は現在の画角のまま少し見せてから、カメラを寄せ始める
            float startDelay = Mathf.Max(0f, _settings.EnemyFocusStartDelay);
            float delayElapsed = 0f;

            while (delayElapsed < startDelay && _lastHitEnemyTransform != null)
            {
                delayElapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;

            float duration = Mathf.Max(0.01f, _settings.EnemyFocusDuration);
            float elapsed = 0f;

            while (elapsed < duration && _lastHitEnemyTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;

                float easedT = EaseInOut(Mathf.Clamp01(elapsed / duration));

                // 落下中の敵を毎フレーム追いかけて目標を更新する
                Vector3 targetPosition = GetEnemyCameraPosition();
                Quaternion targetRotation = GetLookAtEnemyRotation(targetPosition);

                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                yield return null;
            }
        }


        /// <summary>
        /// フラッシュを焚きながら、敵が落下していくのを見届けるコルーチン
        /// </summary>
        /// <param name="cameraTransform">カメラのTransform</param>
        /// <returns></returns>
        private IEnumerator PlayEnemyFallWatchPart(Transform cameraTransform)
        {
            float duration = _settings.EnemyFallWatchDuration;
            float elapsed = 0f;

            while (elapsed < duration && _lastHitEnemyTransform != null)
            {
                elapsed += Time.unscaledDeltaTime;

                // カメラ位置は固定し、落下していく敵を向きだけで追い続ける
                Quaternion lookRotation = GetLookAtEnemyRotation(cameraTransform.position);
                Quaternion shakeRotation = CalculateShakeRotation(elapsed);

                cameraTransform.rotation = lookRotation * shakeRotation;

                SetFlashAlpha(CalculateFlashAlpha(elapsed));

                yield return null;
            }

            SetFlashAlpha(0f);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
        }


        /// <summary>
        /// 敵を写すカメラ座標を取得する
        /// </summary>
        /// <returns>カメラ座標</returns>
        private Vector3 GetEnemyCameraPosition()
        {
            Quaternion fieldRotation = FieldContext.IsReady ? FieldContext.Rotation : Quaternion.identity;
            return _lastHitEnemyTransform.position + fieldRotation * _settings.EnemyCameraOffset;
        }


        /// <summary>
        /// 敵を見つめる回転を取得する
        /// </summary>
        /// <param name="cameraPosition">カメラ座標</param>
        /// <returns>敵を見つめる回転</returns>
        private Quaternion GetLookAtEnemyRotation(Vector3 cameraPosition)
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Quaternion fieldRotation = FieldContext.IsReady ? FieldContext.Rotation : Quaternion.identity;
            Vector3 lookTarget = _lastHitEnemyTransform.position + fieldRotation * _settings.EnemyLookAtOffset;

            Vector3 lookDirection = lookTarget - cameraPosition;

            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return Quaternion.LookRotation(Vector3.down, up);
            }

            return Quaternion.LookRotation(lookDirection.normalized, up);
        }


        /// <summary>
        /// スローモーション部分のコルーチンを再生する
        /// </summary>
        /// <param name="cameraTransform">カメラの座標</param>
        /// <returns></returns>
        private IEnumerator PlaySlowMotionPart(Transform cameraTransform)
        {
            Time.timeScale = _settings.SlowMotionTimeScale;
            Time.fixedDeltaTime = DefaultFixedDeltaTime * Time.timeScale;

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;

            Quaternion fieldRotation = FieldContext.IsReady ? FieldContext.Rotation : Quaternion.identity;
            Vector3 targetPosition = startPosition + fieldRotation * _settings.SlowMotionCameraOffset;

            float duration = _settings.SlowMotionDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = EaseInOut(t);

                Vector3 basePosition = Vector3.Lerp(startPosition, targetPosition, easedT);
                Vector3 shakePosition = CalculateShakePosition(cameraTransform, elapsed);
                Quaternion shakeRotation = CalculateShakeRotation(elapsed);

                cameraTransform.position = basePosition + shakePosition;
                cameraTransform.rotation = startRotation * shakeRotation;

                SetFlashAlpha(CalculateFlashAlpha(elapsed));

                yield return null;
            }

            cameraTransform.position = targetPosition;
            cameraTransform.rotation = startRotation;
            SetFlashAlpha(0f);

            Time.timeScale = 1f;
            Time.fixedDeltaTime = DefaultFixedDeltaTime;
        }



        /// <summary>
        /// プレイヤーのカメラ座標と回転に向かって補間するコルーチン
        /// </summary>
        /// <param name="cameraTransform">カメラ座標</param>
        /// <returns></returns>
        private IEnumerator PlayPlayerZoomPart(Transform cameraTransform)
        {
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;

            // プレイヤーのカメラ座標と回転に向かって補間する時間を設定する
            float duration = Mathf.Max(0.01f, _settings.ZoomDuration);
            float elapsed = 0f;

            // プレイヤーのカメラ座標と回転に向かって補間する
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = EaseInOut(t);

                Vector3 targetPosition = GetPlayerCameraPosition();
                Quaternion targetRotation = GetLookAtPlayerRotation(targetPosition);

                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
                cameraTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

                TurnPlayerTowardCamera(cameraTransform.position);

                yield return null;
            }


            // 最終的にプレイヤーのカメラ座標と回転を設定する
            cameraTransform.position = GetPlayerCameraPosition();
            cameraTransform.rotation = GetLookAtPlayerRotation(cameraTransform.position);

            TurnPlayerTowardCamera(cameraTransform.position);
        }



        /// <summary>
        /// プレイヤーをカメラの方向に向けたまま、指定された時間待機するコルーチン
        /// </summary>
        /// <param name="duration">時間</param>
        /// <param name="cameraTransform">カメラのTransform</param>
        /// <returns></returns>
        private IEnumerator WaitKeepingPlayerFacingCamera(float duration, Transform cameraTransform)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                TurnPlayerTowardCamera(cameraTransform.position);
                yield return null;
            }
        }



        /// <summary>
        /// プレイヤーのカメラ座標を取得する
        /// </summary>
        /// <returns>カメラ座標</returns>
        private Vector3 GetPlayerCameraPosition()
        {
            Quaternion fieldRotation = FieldContext.IsReady ? FieldContext.Rotation : Quaternion.identity;
            return _playerTransform.position + fieldRotation * _settings.PlayerCameraOffset;
        }


        /// <summary>
        /// プレイヤーを見つめる回転を取得する
        /// </summary>
        /// <param name="cameraPosition">カメラ座標</param>
        /// <returns>プレイヤーを見つめる回転</returns>
        private Quaternion GetLookAtPlayerRotation(Vector3 cameraPosition)
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Quaternion fieldRotation = FieldContext.IsReady ? FieldContext.Rotation : Quaternion.identity;
            Vector3 lookTarget = _playerTransform.position + fieldRotation * _settings.PlayerLookAtOffset;

            Vector3 lookDirection = lookTarget - cameraPosition;
            if (lookDirection.sqrMagnitude < 0.001f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(lookDirection.normalized, up);
        }


        /// <summary>
        /// プレイヤーをカメラの方向に向ける
        /// </summary>
        /// <param name="cameraPosition">カメラ座標</param>
        private void TurnPlayerTowardCamera(Vector3 cameraPosition)
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 toCamera = cameraPosition - _playerTransform.position;
            Vector3 flatDirection = Vector3.ProjectOnPlane(toCamera, up);

            if (flatDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized, up);
            float turnRate = _settings.PlayerTurnSpeed * Time.unscaledDeltaTime;

            _playerTransform.rotation = Quaternion.Slerp(_playerTransform.rotation, targetRotation, turnRate);
        }


        /// <summary>
        /// カメラの揺れ位置を計算する
        /// </summary>
        /// <param name="cameraTransform">カメラの座標</param>
        /// <param name="elapsed">経過時間</param>
        /// <returns>揺れ位置</returns>
        private Vector3 CalculateShakePosition(Transform cameraTransform, float elapsed)
        {
            float noiseTime = elapsed * _settings.ShakeFrequency;

            float x = Noise(noiseTime, 0f);
            float y = Noise(noiseTime, 10f);

            return (cameraTransform.right * x + cameraTransform.up * y) * _settings.ShakeStrength;
        }


        /// <summary>
        /// カメラの揺れ回転を計算する
        /// </summary>
        /// <param name="elapsed">経過時間</param>
        /// <returns>回転</returns>
        private Quaternion CalculateShakeRotation(float elapsed)
        {
            float noiseTime = elapsed * _settings.ShakeFrequency;
            float strength  = _settings.ShakeRotationStrength;

            return Quaternion.Euler(
                Noise(noiseTime, 20f) * strength,
                Noise(noiseTime, 30f) * strength,
                Noise(noiseTime, 40f) * strength
            );
        }


        /// <summary>
        /// フラッシュのアルファ値を計算する
        /// </summary>
        /// <param name="elapsed">経過時間</param>
        /// <returns>アルファ値</returns>
        private float CalculateFlashAlpha(float elapsed)
        {
            float alpha = 0f;

            // 設定SOからフラッシュタイミングを取得
            SO_GameClearCinematicSettings.FlashTiming[] timings = _settings.FlashTimings;

            if (timings == null)
            {
                return 0f;
            }

            // 各フラッシュタイミングをチェックして、現在の経過時間に応じたアルファ値を計算する
            for (int i = 0; i < timings.Length; i++)
            {
                float start = timings[i].StartTime;
                float end = start + timings[i].Duration;

                if (elapsed < start || elapsed > end)
                {
                    continue;
                }

                // 経過時間に応じたアルファ値を計算
                float t = Mathf.InverseLerp(start, end, elapsed);
                float currentAlpha = Mathf.Lerp(timings[i].Alpha, 0f, t);
                alpha = Mathf.Max(alpha, currentAlpha);
            }

            return alpha;
        }


        /// <summary>
        /// ノイズ値を取得する
        /// </summary>
        /// <param name="time">時間</param>
        /// <param name="offset">オフセット</param>
        /// <returns>ノイズ値</returns>
        private float Noise(float time, float offset)
        {
            return Mathf.PerlinNoise(_noiseSeed + offset, time) * 2f - 1f;
        }


        /// <summary>
        /// 白フラッシュのアルファ値を設定する
        /// </summary>
        /// <param name="alpha">アルファ値</param>
        private void SetFlashAlpha(float alpha)
        {
            if (_flashCanvasGroup == null)
            {
                return;
            }

            _flashCanvasGroup.alpha = Mathf.Clamp01(alpha);
        }


        /// <summary>
        /// イージング関数（EaseInOut）を使用して、0から1までの値を滑らかに変化させる
        /// </summary>
        /// <param name="t">変化させる値</param>
        /// <returns>変化した値</returns>
        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }



        /// <summary>
        /// 参照の解決
        /// </summary>
        private void ResolveReferences()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_cameraRigController == null)
            {
                _cameraRigController = Object.FindFirstObjectByType<CameraRigController>();
            }

            if (_playerTransform == null)
            {
                PlayerFacade facade = Object.FindFirstObjectByType<PlayerFacade>();
                if (facade != null)
                {
                    _playerTransform = facade.transform;
                }
            }

            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                }
            }

            if (_playerController == null && _playerTransform != null)
            {
                _playerController = _playerTransform.GetComponentInChildren<PlayerController>();
            }

            if (_playerAnimationController == null && _playerTransform != null)
            {
                _playerAnimationController = _playerTransform.GetComponentInChildren<PlayerAnimationController>();
            }

            if (_playerFaceController == null && _playerTransform != null)
            {
                _playerFaceController = _playerTransform.GetComponentInChildren<PlayerFaceController>();
            }

            if (_gateAnchor == null)
            {
                _gateAnchor = Object.FindFirstObjectByType<GameOverGateAnchor>();
            }
        }
    }
}
