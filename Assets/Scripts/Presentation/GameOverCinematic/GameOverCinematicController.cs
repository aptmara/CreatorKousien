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
using Game.Gameplay.Collectibles;

namespace Game.Presentation.GameOverCinematic
{
    /// <summary>
    /// ゲームオーバー時の演出を制御するコントローラー。
    /// </summary>
    public sealed class GameOverCinematicController : MonoBehaviour
    {
        private const float DefaultFixedDeltaTime = 0.02f;

        [Header("--- 設定データ ---")]
        [SerializeField] private SO_GameOverCinematicSettings _settings;

        [Header("--- 門の蝶番オブジェクトの参照 ---")]
        [SerializeField] private Transform _leftDoorHinge;
        [SerializeField] private Transform _rightDoorHinge;

        [Header("--- 漫符・煙エフェクト ---")]
        [SerializeField] private ParticleSystem _dustParticlePrefab;
        [SerializeField] private Transform _dustSpawnPoint;

        [Header("--- なだれ込みカメラシェイク設定 ---")]
        [Tooltip("なだれ込み中のカメラシェイクを有効にするか")]
        [SerializeField] private bool _enableRushShake = true;
        [Tooltip("位置の揺れの強さ")]
        [SerializeField, Min(0f)] private float _shakeStrength = 0.14f;
        [Tooltip("回転の揺れの強さ")]
        [SerializeField, Min(0f)] private float _shakeRotationStrength = 1.5f;
        [Tooltip("揺れの細かさ・激しさ")]
        [SerializeField, Min(1f)] private float _shakeFrequency = 30f;

        [Header("--- 視界クリアクリーンアップ設定 ---")]
        [Tooltip("ゲームオーバー確定時にプレイヤー周囲から消去するアイテムの探索半径")]
        [SerializeField, Min(0f)] private float _itemClearRadius = 7f;

        [Header("--- バリア破壊時のスローモーション設定 ---")]
        [Tooltip("スローモーション倍率")]
        [SerializeField, Range(0.01f, 1f)] private float _breakSlowTimeScale = 0.1f;
        [Tooltip("門へのズーム前に、破壊の余韻とスローを維持する実時間（秒）")]
        [SerializeField, Min(0f)] private float _delayBeforeZoomIn = 2.5f;
        [Tooltip("カメラシェイクの強さ")]
        [SerializeField] private float _breakImpactStrength = 0.3f;
        [Tooltip("カメラシェイクの回転の強さ")]
        [SerializeField] private float _breakImpactRotation = 4.0f;

        [Header("--- カートゥーン閉扉バウンド設定 ---")]
        [Tooltip("門が閉まりきる直前の激しい反動バウンド時間（秒）")]
        [SerializeField] private float _doorSlamBounceDuration = 0.25f;
        [Tooltip("門が閉まった瞬間の行き過ぎ（食い込み）角度の最大幅")]
        [SerializeField] private float _doorOvershootAngle = 35f;

        [Header("--- 魂VFX設定 ---")]
        [Tooltip("新しく用意した単一の魂VFXプレハブ")]
        [SerializeField] private GameObject _newSoulVfxPrefab;

        [Tooltip("魂が次に湧き出るまでの一定間隔（秒）")]
        [SerializeField, Min(0.05f)] private float _soulSpawnInterval = 0.4f;

        [Tooltip("生成された各魂エフェクトが自動で消滅するまでの寿命（秒）")]
        [SerializeField, Min(0.1f)] private float _soulVfxLifetime = 4.0f;

        private CameraRigController _cameraRig;
        private Camera _mainCamera;
        private PlayerController _playerController;

        private GameObject _realPlayerObject;
        private GameObject _cinematicPlayerObject;
        private PlayerCartoonDeath _cinematicPlayerDeath;

        // パーリンノイズ用のシード値
        private float _noiseSeed;
        private bool _isSoulLoopActive = false;

        private void Awake()
        {
            _noiseSeed = Random.value * 1000f;
        }

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

        private float Noise(float time, float offset)
        {
            return Mathf.PerlinNoise(_noiseSeed + offset, time) * 2f - 1f;
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

            // バリア破壊のカメラシェイク & スローモーション猶予
            if (_delayBeforeZoomIn > 0.0f)
            {
                Time.timeScale = _breakSlowTimeScale;
                Time.fixedDeltaTime = DefaultFixedDeltaTime * Time.timeScale;

                float breakElapsed = 0f;
                while (breakElapsed < _delayBeforeZoomIn)
                {
                    breakElapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(breakElapsed / _delayBeforeZoomIn);
                    float damping = 1f - (t * t);

                    // ノイズシェイク計算
                    float noiseTime = breakElapsed * _shakeFrequency * 1.5f;
                    float offsetX = Noise(noiseTime, 100f) * _breakImpactStrength * damping;
                    float offsetY = Noise(noiseTime, 115f) * _breakImpactStrength * damping;
                    Vector3 shakeOffset = (camTransform.right * offsetX) + (camTransform.up * offsetY);

                    Quaternion shakeRotOffset = Quaternion.Euler(
                        Noise(noiseTime, 130f) * _breakImpactRotation * damping,
                        Noise(noiseTime, 145f) * _breakImpactRotation * damping,
                        Noise(noiseTime, 160f) * _breakImpactRotation * damping
                    );

                    camTransform.position = camStartPos + shakeOffset;
                    camTransform.rotation = camStartRot * shakeRotOffset;
                    yield return null;
                }

                // スローモーション解除
                Time.timeScale = 1f;
                Time.fixedDeltaTime = DefaultFixedDeltaTime;
            }

            // 門を最大開放角度まで開く
            if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.Euler(0f, _settings.MaxOpenAngle, 0f);
            if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.Euler(0f, -_settings.MaxOpenAngle, 0f);
            
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

            // phase 2: 砂煙エフェクト
            if (_dustParticlePrefab != null && _dustSpawnPoint != null)
            {
                _dustParticlePrefab.transform.position = _dustSpawnPoint.position;
                _dustParticlePrefab.Play();
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

                // プレイヤー周囲のカメラを遮るアイテムを消去する
                if (_realPlayerObject != null)
                {
                    ClearCollectiblesAroundPlayer(_realPlayerObject.transform.position, _itemClearRadius);
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

                // ズームイン完了時のカメラベース位置を記録
                Vector3 baseBrakeCamPos = camTransform.position;
                Quaternion baseBrakeCamRot = camTransform.rotation;

                while (rushElapsed < _settings.BaseDoorKeepOpenDuration)
                {
                    rushElapsed += Time.deltaTime;

                    // なだれ込みカメラシェイク計算処理
                    if (_enableRushShake)
                    {
                        float noiseTime = rushElapsed * _shakeFrequency;

                        // 左右上下の揺れ
                        float offsetX = Noise(noiseTime, 0f) * _shakeStrength;
                        float offsetY = Noise(noiseTime, 15f) * _shakeStrength;
                        Vector3 shakeOffset = (camTransform.right * offsetX) + (camTransform.up * offsetY);

                        // 回転のランダムグリッチ揺れ
                        Quaternion shakeRotOffset = Quaternion.Euler(
                            Noise(noiseTime, 30f) * _shakeRotationStrength,
                            Noise(noiseTime, 45f) * _shakeRotationStrength,
                            Noise(noiseTime, 60f) * _shakeRotationStrength
                        );

                        camTransform.position = baseBrakeCamPos + shakeOffset;
                        camTransform.rotation = baseBrakeCamRot * shakeRotOffset;
                    }

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

            // phase 3: 門がカートゥーン風に閉まる
            elapsed = 0f;
            while (elapsed < _settings.DoorCloseDuration)
            {
                elapsed += Time.deltaTime;
                float rate = Mathf.Clamp01(elapsed / _settings.DoorCloseDuration);
                float curveValue = _settings.DoorCloseCurve.Evaluate(rate);

                float currentAngle = Mathf.Lerp(_settings.MaxOpenAngle, 0f, curveValue);
                if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
                if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.Euler(0f, -currentAngle, 0f);
                yield return null;
            }

            // バタンと閉まった瞬間の反動
            float bounceElapsed = 0f;
            while (bounceElapsed < _doorSlamBounceDuration)
            {
                bounceElapsed += Time.deltaTime;
                float t = bounceElapsed / _doorSlamBounceDuration;
                float damping = 1f - t;

                float bounceAngle = Mathf.Sin(t * Mathf.PI * 3f) * _doorOvershootAngle * damping;

                if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.Euler(0f, bounceAngle, 0f);
                if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.Euler(0f, -bounceAngle, 0f);
                yield return null;
            }

            // 扉をゼロ座標で固定
            if (_leftDoorHinge != null) _leftDoorHinge.localRotation = Quaternion.identity;
            if (_rightDoorHinge != null) _rightDoorHinge.localRotation = Quaternion.identity;

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

            if (_newSoulVfxPrefab != null && _cinematicPlayerObject != null)
            {
                StartCoroutine(SoulSpawnLoopRoutine());
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
        /// 設定されたインターバルごとに魂エフェクトをプレイヤー位置から生成し続けるコルーチン
        /// </summary>
        private IEnumerator SoulSpawnLoopRoutine()
        {
            _isSoulLoopActive = true;

            while (_isSoulLoopActive && _cinematicPlayerObject != null)
            {
                Vector3 basePos = (_cinematicPlayerObject != null) ? _cinematicPlayerObject.transform.position : Vector3.zero;

                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-0.4f, 0.4f),
                    0.2f,
                    UnityEngine.Random.Range(-0.4f, 0.4f)
                );

                Vector3 spawnPos = basePos + randomOffset;

                // 魂エフェクトを生成
                GameObject soulInstance = Instantiate(_newSoulVfxPrefab, spawnPos, Quaternion.identity);

                ParticleSystem[] particles = soulInstance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (ParticleSystem ps in particles)
                {
                    var main = ps.main;
                    main.useUnscaledTime = true;
                }

                // エフェクトの寿命が来たら自動破棄
                Destroy(soulInstance, _soulVfxLifetime);

                yield return new WaitForSecondsRealtime(_soulSpawnInterval);
            }
        }


        private void OnDestroy()
        {
            _isSoulLoopActive = false;
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

            // 手を強制的に非表示
            if (_realPlayerObject.TryGetComponent<PlayerAttachmentController>(out var attachmentController))
            {
                attachmentController.ForceDestroyAttachment();
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
        /// プレイヤー周囲の指定半径内にある収集物を強制的にデスポーンしてプールへ戻す
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        private void ClearCollectiblesAroundPlayer(Vector3 center, float radius)
        {
            Collider[] hitColliders = Physics.OverlapSphere(center, radius);
            int clearCount = 0;

            foreach (var col in hitColliders)
            {
                // タグによる判定
                if (col.CompareTag("Collectable") || col.gameObject.name.Contains("Collectible"))
                {
                    CollectibleObject collectible = col.GetComponentInParent<CollectibleObject>();
                    if (collectible != null && collectible.gameObject.activeInHierarchy)
                    {
                        collectible.Despawn();
                        clearCount++;
                    }
                }
            }

            if (clearCount > 0)
            {
                Debug.Log($"[GameOver] プレイヤー周囲の収集物を{clearCount}個デスポーンしてプールへ戻したぜよ");
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
