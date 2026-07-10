// ================================================================================
// File         : GameOverCinematicController.cs
// Author       : Iwai Shogo
//
// Description  : ゲームオーバー時の演出を制御するコントローラー。
// Created      : 2026-07-09
// ================================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Stage;
using Game.Core.Management;

namespace Game.Presentation.GameOverCinematic
{
    /// <summary>
    /// ゲームオーバー時の演出を制御するコントローラー。
    /// </summary>
    public sealed class GameOverCinematicController : MonoBehaviour
    {
        [Header("--- 設定データ ---")]
        [SerializeField] private SO_GameOverCinematicSettings _settings;

        [Header("--- 門の蝶番オブジェクトの参照 ---")]
        [SerializeField] private Transform _leftDoorHinge;
        [SerializeField] private Transform _rightDoorHinge;

        [Header("--- 漫符・煙エフェクト ---")]
        [SerializeField] private ParticleSystem _dustParticlePrefab;
        [SerializeField] private Transform _dustSpawnPoint;

        private CameraRigController _cameraRig;
        private Camera _mainCamera;
        private PlayerController _playerController;
        private PlayerCartoonDeath _playerCartoonDeath;

        private void OnEnable()
        {
            EventBus.Subscribe<DefLineBreakReactionEvent>(OnDefenceLineBroken);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnDefenceLineBroken);
        }

        private void OnDefenceLineBroken(DefLineBreakReactionEvent ev)
        {
            StartCoroutine(PlayGameOverSequence());
        }

        private IEnumerator PlayGameOverSequence()
        {
            // 1. 各種コンポーネントの動的解決
            _cameraRig = Object.FindFirstObjectByType<CameraRigController>();
            if (_cameraRig != null) _cameraRig.SetCinematicModeActive(true);

            _mainCamera = Camera.main;

            var playerFacade = Object.FindFirstObjectByType<PlayerFacade>();
            if (playerFacade != null)
            {
                _playerController = playerFacade.GetComponent<PlayerController>();
                _playerCartoonDeath = playerFacade.GetComponent<PlayerCartoonDeath>();
                if (_playerController != null) _playerController.SetCanMove(false);
            }

            Transform camTransform = _mainCamera.transform;
            Vector3 camStartPos = camTransform.position;
            Quaternion camStartRot = camTransform.rotation;

            // phase 1: カメラが奥の門へズームイン
            float elapsed = 0f;
            while (elapsed < _settings.ZoomInDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _settings.ZoomInDuration);
                float easedT = t * t * (3f - 2f * t); // ease in-out

                camTransform.position = Vector3.Lerp(camStartPos, _settings.CameraZoomPosition, easedT);
                camTransform.rotation = Quaternion.Slerp(camStartRot, Quaternion.Euler(_settings.CameraZoomRotation), easedT);
                yield return null;
            }

            // phase 2: 門が勢いよく奥へ開く & 砂煙エフェクト
            if (_dustParticlePrefab != null && _dustSpawnPoint != null)
            {
                _dustParticlePrefab.transform.position = _dustSpawnPoint.position;
                _dustParticlePrefab.Play();
            }

            elapsed = 0f;
            while (elapsed < _settings.DoorOpenDuration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / _settings.DoorOpenDuration);
                float curveValue = _settings.DoorOpenCurve.Evaluate(rate);

                float currentAngle = curveValue * _settings.MaxOpenAngle;
                if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.Euler(0f, -currentAngle, 0f);
                if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
                yield return null;
            }

            // ここでフィールドに残っている敵のターゲットを門の奥へ強制変更するとか、なだれ込ませる演出を挟む。(TODO)
            yield return new WaitForSeconds(_settings.BaseDoorKeepOpenDuration);

            // phase 3: 門が勢いよく閉まる
            elapsed = 0f;
            while (elapsed < _settings.DoorCloseDuration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / _settings.DoorCloseDuration);
                float curveValue = _settings.DoorCloseCurve.Evaluate(rate);

                float currentAngle = (1f - curveValue) * _settings.MaxOpenAngle;
                if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.Euler(0f, -currentAngle, 0f);
                if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
                yield return null;
            }

            // TODO: 閉まった瞬間カメラシェイクとかあっても良いかも

            // 扉が閉まるのと同時に、プレイヤーをカートゥーン死亡演出に切り替える
            if (_playerCartoonDeath != null)
            {
                _playerCartoonDeath.FlattenImmediately();
            }

            // phase 4: カメラを引き、プレイヤーを映す
            elapsed = 0f;
            while (elapsed < _settings.ZoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _settings.ZoomOutDuration);
                float easedT = t * t * (3f - 2f * t); // ease in-out

                camTransform.position = Vector3.Lerp(_settings.CameraZoomPosition, camStartPos, easedT);
                camTransform.rotation = Quaternion.Slerp(Quaternion.Euler(_settings.CameraZoomRotation), camStartRot, easedT);
                yield return null;
            }

            yield return new WaitForSeconds(_settings.PlayerReviveDelay);

            // phase 5: プレイヤーが跳ねて復活
            if (_playerCartoonDeath != null)
            {
                yield return StartCoroutine(_playerCartoonDeath.PlayReviveAndDizzyRoutine());
            }

            yield return new WaitForSeconds(_settings.TransitionResultDelay);

            // phase 6: リザルトシーンへの加算ロード
            if (GameProgressionManager.Instance != null)
            {
                // GameProgressionManager.Instance.UpdateProgressionState(GameProgressionState.Result);
            }
            else
            {
                // マネージャーがいない場合のフォールバック（直接加算シーンロードなど）
                SceneManager.LoadScene("Result", LoadSceneMode.Additive);
            }
        }
    }
}
