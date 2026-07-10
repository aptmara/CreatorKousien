// ================================================================================
// File         : GameOverCinematicController.cs
// Author       : Iwai Shogo
//
// Description  : ゲームオーバー時の演出を制御するコントローラー。
// Created      : 2026-07-09
// ================================================================================

using System.Collections;
using System.Collections.Generic;
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

            // 敵のなだれ込み演出
            if (_settings.DummyEnemyPrefab != null && _dustSpawnPoint != null)
            {
                Vector3 gateCenter = _dustSpawnPoint.position;

                // デフォルトの方向
                Vector3 gateForward = transform.forward;
                Vector3 gateRight = transform.right;
                Vector3 gateUp = transform.up;

                var anchor = _dustSpawnPoint.GetComponentInParent<GameOverGateAnchor>();
                if (anchor != null)
                {
                    gateForward = anchor.transform.forward;
                    gateRight = anchor.transform.right;
                    gateUp = anchor.transform.up;
                }

                List<Transform> enemyTransforms = new List<Transform>();
                List<Vector3> enemyTargets = new List<Vector3>();
                List<float> enemySpeeds = new List<float>();
                List<float> enemyDelays = new List<float>();

                // 1. 遠くの固定底辺ライン上にランダムで配置
                for (int i = 0; i < _settings.DummyEnemyCount; i++)
                {
                    // スポーンライン上の横方向のランダム位置
                    float spawnHorizontalRate = Random.Range(-0.5f, 0.5f);

                    // 座標計算
                    Vector3 spawnPos = gateCenter
                                     - (gateForward * _settings.SpawnLineDistance)
                                     + (gateRight * (spawnHorizontalRate * _settings.SpawnLineWidth))
                                     + (gateUp * _settings.EnemyVisualYOffset);

                    // 生成
                    GameObject enemyObj = Instantiate(_settings.DummyEnemyPrefab, spawnPos, Quaternion.LookRotation(gateForward));

                    if (enemyObj.TryGetComponent<Rigidbody>(out var enemyRb)) enemyRb.isKinematic = true;
                    if (enemyObj.TryGetComponent<Collider>(out var enemyCol)) enemyCol.enabled = false;

                    // 目標集結ライン上の通過点を計算
                    float targetHorizontalRate = Random.Range(-0.5f, 0.5f);
                    Vector3 finalTargetPos = gateCenter
                                           + (gateForward * _settings.DisappearDepth)
                                           + (gateRight * (targetHorizontalRate * _settings.TargetLineWidth))
                                           + (gateUp * _settings.EnemyVisualYOffset);

                    // Listに保存
                    enemyTransforms.Add(enemyObj.transform);
                    enemyTargets.Add(finalTargetPos);
                    enemySpeeds.Add(_settings.BaseEnemySpeed + Random.Range(-_settings.SpeedVariation, _settings.SpeedVariation));
                    enemyDelays.Add(Random.Range(0f, _settings.MaxStartDelay));
                }

                // 2. 敵を一斉に門の奥へ走らせる
                float rushElapsed = 0f;

                while (rushElapsed < _settings.BaseDoorKeepOpenDuration)
                {
                    rushElapsed += Time.deltaTime;

                    for (int i = enemyTransforms.Count - 1; i >= 0; i--)
                    {
                        Transform enemyTrans = enemyTransforms[i];
                        if (enemyTrans == null) continue;

                        // ディレイ中は待機
                        if (rushElapsed < enemyDelays[i])
                        {
                            Vector3 lookGate = (gateCenter - enemyTrans.position).normalized;
                            if (lookGate.sqrMagnitude > 0.001f) enemyTrans.rotation = Quaternion.LookRotation(lookGate, gateUp);
                            continue;
                        }

                        // 目的地に向かって前進
                        Vector3 moveDir = (enemyTargets[i] - enemyTrans.position).normalized;
                        enemyTrans.position += moveDir * enemySpeeds[i] * Time.deltaTime;

                        if (moveDir.sqrMagnitude > 0.001f)
                        {
                            enemyTrans.rotation = Quaternion.LookRotation(moveDir);
                        }

                        // 削除ライン追従
                        Vector3 relativePos = enemyTrans.position - gateCenter;
                        float currentDepth = Vector3.Dot(relativePos, gateForward);

                        if (currentDepth >= _settings.DisappearDepth)
                        {
                            Destroy(enemyTrans.gameObject);

                            enemyTransforms.RemoveAt(i);
                            enemyTargets.RemoveAt(i);
                            enemySpeeds.RemoveAt(i);
                            enemyDelays.RemoveAt(i);
                        }
                    }
                    yield return null;
                }

                // 残った敵の片付け
                foreach (var trans in enemyTransforms)
                {
                    if (trans != null) Destroy(trans.gameObject);
                }
            }
            else
            {
                // プレハブが未設定の場合は、これまでのキープ時間だけ待機
                yield return new WaitForSeconds(_settings.BaseDoorKeepOpenDuration);
            }

            // phase 3: 門が勢いよく閉まる
            elapsed = 0f;
            while (elapsed < _settings.DoorCloseDuration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / _settings.DoorCloseDuration);
                float curveValue = _settings.DoorCloseCurve.Evaluate(rate);

                float currentAngle = curveValue * _settings.MaxOpenAngle;
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

            // プレイヤーの腰当たりの高さを注視点
            Vector3 playerTargetPos = _playerController != null ? _playerController.transform.position : camStartPos;
            Vector3 playerCameraTargetFocus = playerTargetPos + new Vector3(0f, 1.2f, 0f);

            // プレイヤー空どれくらい離れた位置にカメラを配置するか
            Vector3 focusCameraOffset = new Vector3(0f, 2.0f, -8.5f);
            Vector3 camEndPos = playerTargetPos + focusCameraOffset;

            // プレイヤーの方を向くようにカメラの回転を計算
            Quaternion camEndRot = Quaternion.LookRotation(playerCameraTargetFocus - camEndPos);

            while (elapsed < _settings.ZoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _settings.ZoomOutDuration);
                float easedT = t * t * (3f - 2f * t); // ease in-out

                camTransform.position = Vector3.Lerp(_settings.CameraZoomPosition, camEndPos, easedT);
                camTransform.rotation = Quaternion.Slerp(Quaternion.Euler(_settings.CameraZoomRotation), camEndRot, easedT);
                yield return null;
            }

            // ずれないように固定
            camTransform.position = camEndPos;
            camTransform.rotation = camEndRot;

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
                GameProgressionManager.Instance.GoToResult(isClear: false);
            }
            else
            {
                // マネージャーがいない場合のフォールバック（直接加算シーンロードなど）
                SceneManager.LoadScene("Result", LoadSceneMode.Additive);
            }
        }

        /// <summary>
        /// Stageシーンの門オブジェクトから地震を登録してもらうためのメソッド
        /// </summary>
        public void RegisterGate(GameOverGateAnchor gateAnchor)
        {
            if (gateAnchor != null)
            {
                _leftDoorHinge = gateAnchor.LeftDoorHinge;
                _rightDoorHinge = gateAnchor.RightDoorHinge;
                _dustSpawnPoint = gateAnchor.DustSpawnPoint;
            }
        }
    }
}
