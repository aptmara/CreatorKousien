// ------------------------------------------------------------
// File		: GameClearCinematicController.cs
// Summary	: ゲームクリア時のシネマティックを制御するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-09
//
// Notes	:
// - 基盤制作
// ------------------------------------------------------------
using System.Collections;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Stage;
using UnityEngine;

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

        private float _noiseSeed;                                                ///< ノイズシード値

        private void Awake()
        {
            _noiseSeed = Random.value * 1000f;
            ResolveReferences();
            SetFlashAlpha(0f);
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
                _playerController.SetCanMove(false);
            }

            // プレイヤーのアニメーションをクリア状態に設定
            yield return StartCoroutine(PlaySlowMotionPart(cameraTransform));
            yield return StartCoroutine(PlayPlayerZoomPart(cameraTransform));

            _playerAnimationController?.PlayClear();

            yield return StartCoroutine(WaitKeepingPlayerFacingCamera(_settings.AfterClearAnimationDelay, cameraTransform));

            SetFlashAlpha(0f);
        }



        // 内部処理
        // ------------------------------------------------------------

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
        }
    }
}

