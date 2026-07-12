// ================================================================================
// File         : GameOverCinematicController.cs
// Author       : Iwai Shogo
//
// Description  : ゲームオーバー時の演出を制御するコントローラー。
// Created      : 2026-07-09
//
// Note         : ゲームオーバー用プレイヤーモデルのPrefabを生成するような処理を追加します！ - Asano 2026-07-13
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

        // ゲームプレイ時のプレイヤーオブジェクトの参照（演出中に非表示にするため）
        private GameObject _realPlayerObject;

        // ゲームオーバー演出用のプレイヤー
        private GameObject _cinematicPlayerObject;
        private PlayerCartoonDeath _cinematicPlayerDeath;


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

            if (_mainCamera == null)
            {
                Debug.LogWarning("[GameOver] メインカメラが見つからないんぽ…");
                yield break;
            }

            float cameraStartFieldOfView = _mainCamera.fieldOfView;

            var playerFacade = Object.FindFirstObjectByType<PlayerFacade>();
            if (playerFacade != null)
            {
                _realPlayerObject = playerFacade.gameObject;
                _playerController = playerFacade.GetComponent<PlayerController>();
                if (_playerController != null) _playerController.SetCanMove(false);
            }
            else
            {
                // せうご…お前の意志は俺が引き継ぐぜ…
                Debug.LogWarning("[GameOver] 実プレイヤーが見つからないんぽ…");
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

                // 煙エフェクトを動的に生成
                ParticleSystem activeDust = null;
                if (_settings.CartoonDustPrefab != null)
                {
                    // 門の手前に沿った位置に煙を配置
                    Vector3 dustPos = gateCenter
                                    - (gateForward * _settings.DustEffectDistance)
                                    + (gateUp * _settings.EnemyVisualYOffset);

                    activeDust = Instantiate(_settings.CartoonDustPrefab, dustPos, Quaternion.LookRotation(gateForward, gateUp));

                    // 煙の形状を適用
                    var shape = activeDust.shape;
                    shape.shapeType = ParticleSystemShapeType.Box;
                    shape.scale = new Vector3(_settings.DustEffectWidth, 1f, 1f);

                    activeDust.Play();
                }

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

                // なだれ込み時間が終わったら煙エフェクトを止める
                if (activeDust != null)
                {
                    activeDust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                    Destroy(activeDust.gameObject, 2f);
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
            TrySpawnCinematicPlayer();

            // phase 4: カメラを引き、プレイヤーを映す
            elapsed = 0f;

            // プレイヤーの腰当たりの高さを注視点
            Vector3 playerTargetPos;

            if (_cinematicPlayerObject != null)
            {
                // 演出用プレイヤーが存在する場合は、そちらの位置を注視点にする
                playerTargetPos = _cinematicPlayerObject.transform.position;
            }
            else if (_realPlayerObject != null)
            {
                // 演出用プレイヤーが存在しない場合は、元の位置を注視点にする
                playerTargetPos = _realPlayerObject.transform.position;
            }
            else
            {
                // プレイヤーがいない場合は、カメラをそのままズームアウトする
                playerTargetPos = camStartPos;
            }

            // カメラの最終位置
            Vector3 camEndPos = playerTargetPos + _settings.PlayerCameraPositionOffset;

            // まずプレイヤー中央を見る回転を計算する
            Vector3 baseFocusPosition = playerTargetPos + Vector3.up * _settings.PlayerCameraFocusHeight;

            Vector3 baseLookDirection = baseFocusPosition - camEndPos;

            Quaternion baseCameraRotation = Quaternion.LookRotation(baseLookDirection, Vector3.up);

            // カメラから見た右方向
            Vector3 cameraRight = baseCameraRotation * Vector3.right;

            // 注視点をプレイヤーより右側へずらす
            Vector3 playerCameraTargetFocus = baseFocusPosition  + cameraRight * _settings.PlayerScreenLeftAmount;

            Quaternion camEndRot = Quaternion.LookRotation(playerCameraTargetFocus - camEndPos, Vector3.up);

            while (elapsed < _settings.ZoomOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _settings.ZoomOutDuration);
                float easedT = t * t * (3f - 2f * t); // ease in-out

                camTransform.position = Vector3.Lerp(_settings.CameraZoomPosition, camEndPos, easedT);
                camTransform.rotation = Quaternion.Slerp(Quaternion.Euler(_settings.CameraZoomRotation), camEndRot, easedT);

                // 視野角も補間する
                _mainCamera.fieldOfView = Mathf.Lerp(cameraStartFieldOfView, _settings.PlayerCameraFieldOfView, easedT);

                yield return null;
            }

            // ずれないように固定
            camTransform.position = camEndPos;
            camTransform.rotation = camEndRot;
            _mainCamera.fieldOfView = _settings.PlayerCameraFieldOfView;

            yield return new WaitForSeconds(_settings.PlayerReviveDelay);

            // phase 5: プレイヤーが跳ねて復活
            if (_cinematicPlayerDeath != null)
            {
                yield return StartCoroutine(_cinematicPlayerDeath.PlayReviveRoutine());
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


        private bool TrySpawnCinematicPlayer()
        {
            if (_realPlayerObject == null)
            {
                Debug.LogWarning("[GameOver] 実プレイヤーが見つからないため、演出用プレイヤーを生成できないぜよ");
                return false;
            }

            if (_settings == null || _settings.GameOverPlayerPrefab == null)
            {
                Debug.LogWarning("[GameOver] 演出用プレイヤーのPrefabが設定されていないぜよ");
                return false;
            }

            Vector3 spawnPos = _realPlayerObject.transform.position + _settings.GameOverPlayerSpawnOffset;
            Quaternion spawnRot = _realPlayerObject.transform.rotation;

            // 実プレイヤーと同じ位置・向きに生成
            GameObject instance = Instantiate(_settings.GameOverPlayerPrefab, spawnPos, spawnRot);

            PlayerCartoonDeath cartoonDeath = instance.GetComponent<PlayerCartoonDeath>();

            if (cartoonDeath == null)
            {
                Debug.LogWarning("[GameOver] 演出用プレイヤーにPlayerCartoonDeathコンポーネントが見つからないぜよ");

                Destroy(instance);
                return false;
            }

            // 演出用モデルの生成に成功してから、実プレイヤーを非表示にする
            _realPlayerObject.SetActive(false);

            _cinematicPlayerObject = instance;
            _cinematicPlayerDeath = cartoonDeath;

            // 座標補正を渡す
            _cinematicPlayerDeath.ConfigurePositionCorrection(_settings.GameOverPlayerRevivedPositionOffset);

            // カメラが門を映している間に潰す
            _cinematicPlayerDeath.FlattenImmediately();

            return true;
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
