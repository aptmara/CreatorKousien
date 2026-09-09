// ------------------------------------------------------------
// File		: BocchaDefeatPresenter.cs
// Summary	: 撃破時にカメラを寄せ、落とし物をめっちゃ出してからボスを沈める！
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - これも勝手に作るので、甲斐に無しでって言われたらぼつになります；；
// - 落とし物はプレイヤーのいないほうにだすので、リザルトの邪魔にならないようにします！
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Collectibles;
using Game.Gameplay.Cameras;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Player;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// キング・ボッチャの撃破演出
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaDefeatPresenter : MonoBehaviour, IBossDefeatPresentation
    {
        [Header("==== 参照 ====")]
        [SerializeField] private BossBattleFlowController _flowController;
        [SerializeField] private BocchaSwayMover _swayMover;
        [SerializeField] private BocchaFeverBody _feverBody;


        [Header("==== カメラ ====")]

        [SerializeField]
        [Tooltip("動かすカメラ")]
        private Camera _targetCamera;

        [SerializeField]
        [Tooltip("カメラリグ")]
        private CameraRigController _cameraRigController;

        [SerializeField]
        [Tooltip("撃破時に寄せるカメラ設定")]
        private StaticCameraConfig _defeatCameraConfig;

        [SerializeField]
        private CameraRigController.ProjectionMode _cameraProjectionMode = CameraRigController.ProjectionMode.Perspective;

        [SerializeField]
        [Min(0f)]
        [Tooltip("撃破時にカメラを寄せるまでの時間")]
        private float _cameraBlendDuration = 1.0f;

        [SerializeField]
        private AnimationCurve _cameraBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        [Header("==== 撃破の瞬間 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("撃破してからカメラを寄せ始めるまでの待ち")]
        private float _startDelay = 0.4f;

        [SerializeField]
        [Tooltip("撃破時のVFX")]
        private GameObject _defeatVfxPrefab;

        [SerializeField]
        [Tooltip("お邪魔アイテムを消すときの煙")]
        private GameObject _hazardClearVfxPrefab;

        [SerializeField]
        [Tooltip("VFXの位置補正")]
        private Vector3 _vfxOffset = new Vector3(0f, 0f, 0f);

        [SerializeField]
        [Min(0f)]
        private float _vfxLifeTime = 3.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("撃破時のカメラの揺れの長さ")]
        private float _shakeDuration = 0.5f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("撃破時のカメラの揺れの強さ")]
        private float _shakeStrength = 0.5f;


        [Header("==== 落とし物 ====")]

        [SerializeField]
        [Tooltip("落とし物のプレハブ")]
        private List<CollectibleData> _burstDataList = new List<CollectibleData>();

        [SerializeField]
        [Min(0f)]
        [Tooltip("吹き出す総数")]
        private int _burstCount = 40;

        [SerializeField]
        [Min(0f)]
        [Tooltip("1個ずつ出す間隔")]
        private float _burstInterval = 0.04f;

        [SerializeField]
        [Tooltip("噴出位置のオフセット")]
        private Vector3 _burstOriginOffset = new Vector3(0f, 2f, 0f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("上方向へ吹き上げる速さ")]
        private float _burstUpSpeed = 14.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("プレイヤーの反対咆哮へ飛ばす速さ")]
        private float _burstAwaySpeed = 8.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ばらつきのでかさ")]
        private float _burstSpread = 4.0f;

        [SerializeField]
        [Min(1)]
        [Tooltip("噴出を何回の波に分けるか。1で今までどおりの連続噴出")]
        private int _burstWaveCount = 3;

        [SerializeField]
        [Min(0f)]
        [Tooltip("波と波の間の溜め")]
        private float _burstWaveInterval = 0.35f;

        [SerializeField]
        [Tooltip("波の頭で出すVFX。口から吹き出す感じにする")]
        private GameObject _burstVfxPrefab;

        [SerializeField]
        [Min(0f)]
        [Tooltip("波の頭でのカメラの揺れの長さ")]
        private float _burstShakeDuration = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("波の頭でのカメラの揺れの強さ")]
        private float _burstShakeStrength = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("1個吐き出すたびに跳ねる量")]
        private float _spitKickAmount = 0.85f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("跳ねが収まるまでの時間")]
        private float _spitKickDecay = 0.12f;


        [Header("==== ボスの落下 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("演出が終わってから沈み終わるまでの待ち")]
        private float _fallDelay = 0.3f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ボスが沈む深さ")]
        private float _fallDepth = 20.0f;

        [SerializeField]
        [Min(0.1f)]
        [Tooltip("沈み切るまでの時間")]
        private float _fallDuration = 1.5f;

        [SerializeField]
        private AnimationCurve _fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);


        [Header("==== 震え ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("噴出中にボスが震える幅")]
        private float _shakeAmplitude = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("震えの速さ")]
        private float _shakeSpeed = 40.0f;


        [Header("==== 沈む直前の一撃 ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("倒れ始める瞬間の大きなカメラの揺れの長さ")]
        private float _impactShakeDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("その揺れの強さ")]
        private float _impactShakeStrength = 1.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("揺れてから沈み始めるまでの溜め")]
        private float _impactHold = 0.3f;

        [Header("==== 入力停止 ====")]

        [SerializeField]
        [Tooltip("演出中にプレイヤーの操作を止めるかどうか")]
        private bool _lockPlayerInput = true;


        [Header("==== 寄りカメラ ====")]

        [SerializeField]
        [Tooltip("噴出中に少し寄せるカメラ設定。未設定なら寄らない")]
        private StaticCameraConfig _burstCameraConfig;

        [SerializeField]
        [Min(0f)]
        [Tooltip("寄せるのにかける時間")]
        private float _burstCameraBlendDuration = 0.8f;


        [Header("==== 落下への引き渡し ====")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("落下を始めてから戦闘終了を通知するまでの待ち。ここを過ぎるとリザルト演出へ入る")]
        private float _fallHandoffDelay = 0.4f;


        [Header("==== スロー演出 ====")]

        [SerializeField]
        [Range(0.01f, 1f)]
        [Tooltip("撃破した瞬間の時間の進み方。0.25で25%のスロー")]
        private float _slowMotionTimeScale = 0.25f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("スローを維持する時間（実時間）")]
        private float _slowMotionDuration = 1.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("通常速度へ戻すのにかける時間（実時間）")]
        private float _slowMotionBlendBack = 0.5f;


        private CollectibleSpawner _spawner;
        private Transform _playerTransform;
        private bool _isPlaying;
        private bool _hasAppliedSlowMotion;
        private float _previousFixedDeltaTime = 0.02f;

        // 演出中に無効化したプレイヤー操作系。元々無効だったものは含めない
        private readonly List<Behaviour> _lockedInputBehaviours = new List<Behaviour>();

        private void Awake()
        {
            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }

            if (_swayMover == null)
            {
                _swayMover = GetComponent<BocchaSwayMover>();
            }

            if (_feverBody == null)
            {
                _feverBody = GetComponentInChildren<BocchaFeverBody>();
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if (_cameraRigController == null && _targetCamera != null)
            {
                _cameraRigController = _targetCamera.GetComponentInParent<CameraRigController>();
            }

            if (_cameraRigController)
            {
                _cameraRigController = FindFirstObjectByType<CameraRigController>();
            }
        }


        // 演出中に破棄されても時間の進み方を取り残さないための保険
        private void OnDisable()
        {
            RestoreTimeScale();
            SetPlayerInputLocked(false);
        }


        private void OnDestroy()
        {
            RestoreTimeScale();
            SetPlayerInputLocked(false);
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 撃破演出を開始する
        /// </summary>
        public IEnumerator PlayDefeat()
        {
            if (_isPlaying) yield break;

            _isPlaying = true;

            // 演出中は殴られても何も起きないようにする
            _feverBody?.SetInvincible(true);
            _swayMover?.SetMoveEnable(false);

            SetPlayerInputLocked(true);

            // フィールドのお邪魔も片付ける
            BocchaHazardRegistry.DespawnAll(_hazardClearVfxPrefab, _vfxLifeTime);

            PlayVfx(_defeatVfxPrefab);

            if (_startDelay > 0f)
            {
                yield return new WaitForSeconds(_startDelay);
            }

            // 寄せながら噴き出したいので、カメラのブレンドは待たない
            StartCoroutine(BlendCameraTo(_burstCameraConfig, _burstCameraBlendDuration));

            // 震えながら落とし物を吹き出す
            yield return StartCoroutine(BurstCollectibles());

            // 大きく一度だけ揺らして倒れ始める
            EventBus.Publish(new CameraShakeRequestedEvent(_impactShakeDuration, _impactShakeStrength, _impactShakeStrength, 22.0f));

            if (_impactHold > 0.0f)
            {
                yield return new WaitForSeconds(_impactHold);
            }

            // 沈みながらリザルトへ引き継ぎたいので、落下の完了は待たない
            StartCoroutine(FallDown());

            if (_fallHandoffDelay > 0.0f)
            {
                yield return new WaitForSeconds(_fallHandoffDelay);
            }

            // ここを抜けるとStopBattleが呼ばれ、既存のクリア演出へ引き継がれる
            SetPlayerInputLocked(false);
        }


        /// <summary>
        /// プレイヤーの操作を止める・戻す
        /// </summary>
        /// <param name="isLocked">止めるかどうか</param>
        private void SetPlayerInputLocked(bool isLocked)
        {
            if (!_lockPlayerInput) return;

            if (isLocked)
            {
                _lockedInputBehaviours.Clear();

                // 入力を読む側ではなく、それを使って動かす側を止める
                foreach (PlayerController controller in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
                {
                    if (controller == null || !controller.enabled) continue;

                    controller.enabled = false;
                    _lockedInputBehaviours.Add(controller);
                }

                foreach (PlayerPunchController punch in FindObjectsByType<PlayerPunchController>(FindObjectsSortMode.None))
                {
                    if (punch == null || !punch.enabled) continue;

                    punch.enabled = false;
                    _lockedInputBehaviours.Add(punch);
                }

                // 入力を切っただけでは走りのまま止まるので、明示的にアイドルへ戻す
                foreach (PlayerAnimationController animation in FindObjectsByType<PlayerAnimationController>(FindObjectsSortMode.None))
                {
                    if (animation != null) animation.PlayIdle();
                }

                return;
            }

            // 元々無効だったものは記録していないので、勝手に有効化しない
            foreach (Behaviour behaviour in _lockedInputBehaviours)
            {
                if (behaviour != null) behaviour.enabled = true;
            }

            _lockedInputBehaviours.Clear();
        }

        /// <summary>
        /// 落とし物をプレイヤーの反対方向へ吹き出す
        /// </summary>
        private IEnumerator BurstCollectibles()
        {
            CollectibleSpawner spawner = ResolveSpawner();

            if (spawner == null || _burstDataList.Count == 0 || _burstCount <= 0)
            {
                yield break;
            }

            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 awayDirection = ResolveAwayDirection(up);
            Vector3 basePosition = transform.position;

            int waveCount = Mathf.Max(1, _burstWaveCount);
            int perWave = Mathf.Max(1, _burstCount / waveCount);
            int spawned = 0;

            for (int wave = 0; wave < waveCount; ++wave)
            {
                // 波の頭で「ドンッ」と出す
                if (_burstVfxPrefab != null)
                {
                    Destroy(Instantiate(_burstVfxPrefab, basePosition + _burstOriginOffset, Quaternion.identity), _vfxLifeTime);
                }

                if (_burstShakeDuration > 0.0f && _burstShakeStrength > 0.0f)
                {
                    EventBus.Publish(new CameraShakeRequestedEvent(
                        _burstShakeDuration, _burstShakeStrength, _burstShakeStrength, 35.0f));
                }

                // 最後の波は残り全部を出し切る
                int countThisWave = wave == waveCount - 1 ? _burstCount - spawned : perWave;

                for (int i = 0; i < countThisWave; ++i)
                {

                    CollectibleData data = _burstDataList[Random.Range(0, _burstDataList.Count)];

                    if (data != null)
                    {
                        Vector3 origin = basePosition + _burstOriginOffset;

                        Vector3 velocity = up * _burstUpSpeed
                            + awayDirection * _burstAwaySpeed
                            + Random.insideUnitSphere * _burstSpread;

                        spawner.SpawnWithVelocity(data, origin, velocity, 1.0f, true);
                    }

                    spawned++;

                    // 吐いた瞬間に跳ねて、待っている間に収まる
                    yield return StartCoroutine(PlaySpitKick(basePosition));
                }

                // 波の間は震えを止めて溜める
                if (wave < waveCount - 1)
                {
                    transform.position = basePosition;

                    if (_burstWaveInterval > 0.0f)
                    {
                        yield return new WaitForSeconds(_burstWaveInterval);
                    }
                }
            }

            // 震えを止めて元の位置へ戻す
            transform.position = basePosition;
        }


        /// <summary>
        /// プレイヤーから離れる方向を求める
        /// </summary>
        /// <param name="up">フィールドの上方向</param>
        private Vector3 ResolveAwayDirection(Vector3 up)
        {
            if (_playerTransform == null)
            {
                PlayerFacade playerFacade = FindFirstObjectByType<PlayerFacade>();

                if (playerFacade != null)
                {
                    _playerTransform = playerFacade.transform;
                }
                else
                {
                    GameObject player = GameObject.FindGameObjectWithTag("Player");

                    if (player != null)
                    {
                        _playerTransform = player.transform;
                    }
                }
            }

            if (_playerTransform == null)
            {
                return FieldContext.IsReady ? FieldContext.Rotation * Vector3.forward : Vector3.forward;
            }

            Vector3 away = transform.position - _playerTransform.position;

            // 上下成分を取り除いて水平方向だけにする
            away -= Vector3.Dot(away, up) * up;

            return away.sqrMagnitude > 0.001f ? away.normalized : (FieldContext.IsReady ? FieldContext.Rotation * Vector3.forward : Vector3.forward);
        }


        /// <summary>
        /// ボスを沈める演出を開始する
        /// </summary>
        private IEnumerator FallDown()
        {
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;

            Vector3 startPosition = transform.position;
            Vector3 endPosition = startPosition - up * _fallDepth;

            float elapsed = 0f;

            while (elapsed < _fallDuration)
            {
                float t = _fallCurve.Evaluate(Mathf.Clamp01(elapsed / _fallDuration));
                transform.position = Vector3.LerpUnclamped(startPosition, endPosition, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = endPosition;
        }


        private IEnumerator BlendCameraTo(StaticCameraConfig config, float duration)
        {
            if (config == null || _cameraRigController == null)
            {
                yield break;
            }

            // 通常のカメラ追従を止める
            _cameraRigController.SetCinematicModeActive(true);

            StaticCameraConfig.ProjectionSettings settings = config.GetSettings(_cameraProjectionMode);
            Transform cameraTransform = _targetCamera.transform;

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float startFieldOfView = _targetCamera.fieldOfView;
            float startOrthographicSize = _targetCamera.orthographicSize;

            Vector3 targetPosition = settings.Position;
            Quaternion targetRotation = Quaternion.Euler(settings.Rotation);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = _cameraBlendCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

                cameraTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                cameraTransform.rotation = Quaternion.LerpUnclamped(startRotation, targetRotation, t);
                _targetCamera.fieldOfView = Mathf.LerpUnclamped(startFieldOfView, settings.FieldOfView, t);
                _targetCamera.orthographicSize = Mathf.LerpUnclamped(startOrthographicSize, settings.OrthographicSize, t);

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 最終的な位置と回転を設定
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
            _targetCamera.fieldOfView = settings.FieldOfView;
            _targetCamera.orthographicSize = settings.OrthographicSize;
        }


        private void PlayVfx(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(prefab, transform.position + _vfxOffset, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }


        private CollectibleSpawner ResolveSpawner()
        {
            if (_spawner != null)
            {
                return _spawner;
            }

            _spawner = FindFirstObjectByType<CollectibleSpawner>();
            return _spawner;
        }


        /// <summary>
        /// スローモーションを開始する
        /// </summary>
        private void ApplySlowMotion()
        {
            if (_hasAppliedSlowMotion || _slowMotionTimeScale >= 1.0f || _slowMotionDuration <= 0.0f)
                return;

            _previousFixedDeltaTime = Time.fixedDeltaTime;

            Time.timeScale = _slowMotionTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            _hasAppliedSlowMotion = true;
        }


        /// <summary>
        /// スローモーションを徐々に解除する
        /// </summary>
        private IEnumerator ReleaseSlowMotion()
        {
            if (!_hasAppliedSlowMotion)
                yield break;

            float startScale = Time.timeScale;
            float elapsed = 0.0f;

            while (elapsed < _slowMotionBlendBack)
            {
                elapsed += Time.unscaledDeltaTime;

                float t = Mathf.Clamp01(elapsed / _slowMotionBlendBack);

                Time.timeScale = Mathf.Lerp(startScale, 1.0f, t);
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                yield return null;
            }

            RestoreTimeScale();
        }


        /// <summary>
        /// 1個吐き出した反動でビクッと跳ね、次を吐くまでに収まる
        /// </summary>
        /// <param name="basePosition">跳ねる前の位置</param>
        private IEnumerator PlaySpitKick(Vector3 basePosition)
        {
            if (_spitKickAmount <= 0.0f)
            {
                if (_burstInterval > 0.0f) yield return new WaitForSeconds(_burstInterval);

                yield break;
            }

            // 反動の向きはランダム。上下は控えめにして浮いて見えないようにする
            Vector3 kick = Random.insideUnitSphere * _spitKickAmount;
            kick.y *= 0.4f;

            float elapsed = 0.0f;
            float decay = Mathf.Max(0.01f, _spitKickDecay);

            while (elapsed < _burstInterval)
            {
                float t = Mathf.Clamp01(elapsed / decay);

                transform.position = basePosition + Vector3.Lerp(kick, Vector3.zero, t);

                elapsed += Time.deltaTime;

                yield return null;
            }

            transform.position = basePosition;
        }


        /// <summary>
        /// 時間の進み方を必ず元へ戻す。破棄・無効化されても取り残さないための保険
        /// </summary>
        private void RestoreTimeScale()
        {
            if (!_hasAppliedSlowMotion)
                return;

            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = _previousFixedDeltaTime > 0.0f ? _previousFixedDeltaTime : 0.02f;

            _hasAppliedSlowMotion = false;
        }
    }
}
