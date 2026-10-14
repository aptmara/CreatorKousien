/**
 * File:BossBalanceIntroPresenter.cs
 * 
 * 天秤ボスの登場演出を制御、パターンを二つ作成し、固定、ランダムを実現、
 * 
 * 寺田
 */
using System;
using System.Collections;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// テンビン登場演出のパターン
    /// </summary>
    public enum BalanceIntroPattern
    {
        Pattern1,
        Pattern2,
        Random,
    }

    public enum BalanceIntroCameraAction
    {
        None,

        ZoomIn,
    }

    [Serializable]
    public sealed class BalanceIntroStep
    {
        [SerializeField]
        [Tooltip("Inspectorで識別するための名前")]
        private string _stepName = "Step";

        [Header("====== 位置(画面基準　X=右 / Y=上 / Z=奥。定位置からのオフセット) =====")]

        [SerializeField]
        [Tooltip("ONならステップ開始時に「開始オフセット」へ瞬間移動する")]
        private bool _snapToStartOffset;

        [SerializeField]
        private Vector3 _startOffset;

        [SerializeField]
        private Vector3 _targetOffset;

        [SerializeField]
        [Tooltip("開始位置から目標位置へ動く時間")]
        private float _moveDuration = 0.8f;

        [SerializeField]
        private AnimationCurve _moveCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [SerializeField, Min(0.0f)]
        [Tooltip("到着してから次のステップへ移るまでの時間")]
        private float _holdDuration;

        [Header("===== 皿(腕を揺らして皿を上下させる) =====")]
        [SerializeField, Tooltip("このステップ中、皿を上下に振る")]
        private bool _bobPans;

        [SerializeField, Min(0.0f)]
        [Tooltip("腕のふり幅(度数法)")]
        private float _bobAngle = 22.0f;

        [SerializeField, Min(0.1f)]
        [Tooltip("１秒当たりの往復回数")]
        private float _bobFrequency = 2.4f;

        [Header("====== 腕の回転 ======")]
        [SerializeField, Tooltip("移動中に腕を何回回転させるか(整数推奨)")]
        private float _spinRotations;

        [Header("===== カメラ ======")]
        [SerializeField]
        private BalanceIntroCameraAction _cameraAction = BalanceIntroCameraAction.None;

        [SerializeField, Min(0.0f)]
        [Tooltip("カメラを寄せる時間")]
        private float _cameraBlendDuration = 0.0f;

        [Header("===== 到着時の揺れ ======")]
        [SerializeField,Min(0.0f)]
        [Tooltip("到着した瞬間のカメラの揺れ(0は揺さぶりなし)")]
        private float _arriveShake;

        //======== 公開 ==========
        public string StepName => _stepName;
        public bool SnapToStartOffset => _snapToStartOffset;
        public Vector3 StartOffset => _startOffset;
        public Vector3 TargetOffset => _targetOffset;
        public float MoveDuration => _moveDuration;
        public float HoldDuration => _holdDuration;
        public bool BobPans => _bobPans;
        public float BobAngle => _bobAngle;
        public float BobFrequency => _bobFrequency;
        public float SpinRotations => _spinRotations;
        public BalanceIntroCameraAction CameraAction => _cameraAction;
        public float CameraBlendDuration => _cameraBlendDuration;
        public float ArriveShake => _arriveShake;

        public float EvaluateMove(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            return _moveCurve != null ? _moveCurve.Evaluate(t) : t;
        }

        public static BalanceIntroStep Create(
            string name,bool snap,Vector3 start,Vector3 target,
            float move,float hold,bool bob  = false,float spin = 0.0f,
            BalanceIntroCameraAction camera = BalanceIntroCameraAction.None,
            float cameraBlend = 0.0f,
            float shake = 0.0f,bool overshoot = false)
        {
            BalanceIntroStep step = new BalanceIntroStep();

            step._stepName = name;
            step._snapToStartOffset = snap;
            step._startOffset = start;
            step._targetOffset = target;
            step._moveDuration = move;
            step._holdDuration = hold;
            step._bobPans = bob;
            step._spinRotations = spin;
            step._cameraAction = camera;
            step._cameraBlendDuration = cameraBlend;
            step._arriveShake = shake;

            if(overshoot)
            {
                step._moveCurve = new AnimationCurve(
                    new Keyframe(0.0f,0.0f),
                    new Keyframe(0.7f,1.1f),
                    new Keyframe(1.0f,1.0f)
                    );
            }

            return step;
        }
    }

    /// <summary>
    /// テンビンボスの登場演出管理
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossBalanceIntroPresenter : MonoBehaviour,IBossIntroPresentation
    {
        private const float SideX = 9.0f;
        private const float PeekSideX = 10.5f;
        private const float HideSideX = 16.0f;
        private const float HideY = -14.0f;

        [Header("===== パターン ======")]
        [SerializeField, Tooltip("Randomなら毎回どちらかを抽選")]
        private BalanceIntroPattern _pattern = BalanceIntroPattern.Random;

        [SerializeField, Tooltip("パターン1:左下->右下から出てきて皿を振る->中央で２回転+カメラアップ")]
        private BalanceIntroStep[] _pattern1Steps = CreateDefaultPattern1();

        [SerializeField, Tooltip("パターン2:左->右->下から覗く->定位置に登場+カメラアップ")]
        private BalanceIntroStep[] _pattern2Steps = CreateDefaultPattern2();

        [SerializeField, Min(0.0f)]
        [Tooltip("最初のステップに入る前の待ち時間")]
        private float _startDelay = 0.3f;

        [SerializeField, Min(0.0f)]
        [Tooltip("カメラが戻ってから戦闘開始までの時間")]
        private float _finishDelay = 0.3f;

        [Header("===== 参照 =====")]
        [SerializeField, Tooltip("動かすルート。未設定ならこのGameObject")]
        private Transform _moveRoot;

        [SerializeField,Tooltip("テンビン本体。未設定なら子から探す")]
        private RealisticBalanceScale _scale;

        [SerializeField, Tooltip("演出中に判定を切るかどうか")]
        private bool _disableColliderDuringIntro = true;

        [SerializeField, Tooltip("戦闘中の基本位置")]
        private Vector3 _battleBasePosition;

        [Header("===== カメラ =====")]
        [SerializeField, Tooltip("動かすカメラ。本来ならMainCamera")]
        private Camera _targetCamera;

        [SerializeField, Tooltip("カメラリグ。未設定なら自動で探す")]
        private CameraRigController _cameraRigController;

        [SerializeField]
        private CameraRigController.ProjectionMode _cameraProjectionMode = CameraRigController.ProjectionMode.Perspective;

        [SerializeField]
        private AnimationCurve _cameraBlendCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [SerializeField]
        private StaticCameraConfig _cameraConfig;

        [SerializeField]
        private float _cameraMoveDuration = 1.0f;

        [SerializeField, Range(0.05f, 0.9f)]
        [Tooltip("カメラアップの強さ。0.45なら中止店までの距離が55%になる")]
        private float _zoomInRatio = 0.45f;

        [SerializeField, Tooltip("カメラアップで画面中央に置く点(定位置からのオフセット。画面基準)")]
        private Vector3 _zoomFocusOffset = new Vector3(0.0f, 1.5f, 0.0f);

        [SerializeField]
        private AnimationCurve _cameraCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        [SerializeField, Min(0.0f)]
        [Tooltip("演出後に元のカメラへ戻す時間")]
        private float _returnCameraDuration = 0.8f;

        [Header("====== 演出 =====")]
        [SerializeField, Tooltip("演出中に画面のふちを赤くするか")]
        private bool _playEdgeWarning = true;

        [SerializeField, Min(0.0f)]
        private float _arriveShakeDuration = 0.0f;

        [SerializeField, Min(1.0f)]
        private float _arriveShakeFrequency = 30.0f;

        //===== ランタイム ======================
        private BossBattleFlowController _flow;
        private bool _isPlaying;
        private bool _warningActive;
        private Vector3 _basePosition;

        //=== 腕 =========
        private float _spinAngle;
        private bool _bobActive;
        private float _bobAmplitude;
        private float _bobFreaquency;
        private float _bobWeight;
        private float _bobTime;

        //===== カメラ =======
        private bool _hasSavedCamera;
        private Vector3 _savedCamPos;
        private Quaternion _savedCamRot;
        private float _savedFov;
        private float _savedOrtho;
        private bool _rigWasCinematic;
        private bool _cameraTouched;

        private bool _camTweenActive;
        private float _camTweenElapsed;
        private float _camTweenDuration;
        private Vector3 _camFromPos;
        private Quaternion _camFromRot;
        private float _camFromFov;
        private float _camFromOrtho;
        private Vector3 _camToPos;
        private Quaternion _camToRot;
        private float _camToFov;
        private float _camToOrtho;

        // 当たり判定
        private Collider[] _colliders;
        private bool[] _colliderStates;

        public BalanceIntroPattern LastPlayedPattern { get; private set; }

        private Transform Root => _moveRoot !=  null ? _moveRoot : transform;

        private Vector3 ToWorld(Vector3 screenOffset) => _savedCamRot * screenOffset;



        private static BalanceIntroStep[] CreateDefaultPattern1()
        {
            Vector3 leftHide = new Vector3(-SideX, HideY, 0.0f);
            Vector3 leftUp = new Vector3(-SideX, -3.0f, 0.0f);
            Vector3 rightHide = new Vector3(SideX,HideY, 0.0f);
            Vector3 rightUp = new Vector3(SideX, -3.0f, 0.0f);
            Vector3 centerHide = new Vector3(0.0f, HideY, 0.0f);

            return new[]
            {
                BalanceIntroStep.Create("左下から出現",true,leftHide,leftUp,0.7f,0.9f,bob : true,shake:0.15f),
                BalanceIntroStep.Create("左下へ戻る",false,leftUp,leftHide,0.45f,0.1f,bob : true),
                BalanceIntroStep.Create("右下から出現",true,rightHide,rightUp,0.7f,0.9f,bob : true,shake:0.15f),
                BalanceIntroStep.Create("右下へ戻る",false,rightUp,rightHide,0.45f,0.1f,bob : true),
                BalanceIntroStep.Create("中央で２回転",true,centerHide,Vector3.zero,1.3f,0.7f,
                spin : 2.0f,camera:BalanceIntroCameraAction.ZoomIn,cameraBlend:1.1f,shake:0.4f,overshoot:true),
            };
        }

        private static BalanceIntroStep[] CreateDefaultPattern2()
        {
            Vector3 leftHide = new Vector3(-HideSideX, 0.0f, 0.0f);
            Vector3 leftPeek = new Vector3(-PeekSideX, 0.0f, 0.0f);
            Vector3 rightHide = new Vector3(HideSideX, 0.0f, 0.0f);
            Vector3 rightPeek = new Vector3(PeekSideX, 0.0f, 0.0f);
            Vector3 bottomHide = new Vector3(0.0f, HideY, 0.0f);
            Vector3 bottomPeek = new Vector3(0.0f, -7.0f, 0.0f);

            return new[]
            {
                BalanceIntroStep.Create("左から出現", true, leftHide, leftPeek, 0.45f, 0.5f, shake: 0.1f),
                BalanceIntroStep.Create("左へ戻る", false, leftPeek, leftHide, 0.3f, 0.2f),
                BalanceIntroStep.Create("右から出現", true, rightHide, rightPeek, 0.45f, 0.5f, shake: 0.1f),
                BalanceIntroStep.Create("右へ戻る", false, rightPeek, rightHide, 0.3f, 0.2f, bob: true),
                BalanceIntroStep.Create("下から出現", true, bottomHide, bottomPeek, 0.45f, 0.5f, shake: 0.1f),
                BalanceIntroStep.Create("下へ戻る", false, bottomHide, Vector3.zero, 0.6f, 0.8f),
                BalanceIntroStep.Create("下へ戻る", false, bottomHide, Vector3.zero, 0.6f, 0.8f,
                camera: BalanceIntroCameraAction.ZoomIn, cameraBlend: 0.6f, shake: 0.5f, overshoot: true),
            };
        }

        [ContextMenu("Reset Steps To Default")]
        private void ResetStepsToDefault()
        {
            _pattern1Steps = CreateDefaultPattern1();
            _pattern2Steps = CreateDefaultPattern2();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Awake()
        {
            ResolveReferences();
        }

        // Update is called once per frame
        void Update()
        {
            if(_isPlaying && _flow != null && !_flow.IsBattleActive)
            {
                AbortIntro();
            }
        }

        private void OnDisable()
        {
            if(_isPlaying)
            {
                AbortIntro();
            }
        }
        private void ResolveReferences()
        {
            if (_flow == null)
            {
                _flow = GetComponent<BossBattleFlowController>();
            }

            if(_scale == null)
            {
                _scale = GetComponentInChildren<RealisticBalanceScale>(true);
            }

            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }

            if(_cameraRigController  == null && _targetCamera != null)
            {
                _cameraRigController = _targetCamera.GetComponent<CameraRigController>();
            }

            if(_cameraRigController == null)
            {
                _cameraRigController = FindFirstObjectByType<CameraRigController>();
            }
        }

        public IEnumerator PlayIntro()
        {
            ResolveReferences();

            BalanceIntroStep[] steps = SelectSteps();

            if(steps == null || steps.Length == 0)
            {
                yield break;
            }

            _isPlaying = true;
            _basePosition = _battleBasePosition;
            _spinAngle = 0.0f;
            _bobActive = false;
            _bobWeight = 0.0f;
            _bobTime = 0.0f;
            _cameraTouched = false;


            if(_scale != null)
            {
                _scale.SetPresentationDriven(true);
            }

            if(_disableColliderDuringIntro)
            {
                DisableColliders();
            }

            if(_playEdgeWarning)
            {
                _warningActive = true;
                EventBus.Publish(new BossIntroWarningStartedEvent());
            }

            BalanceIntroStep first = steps[0];

            if (first != null && first.SnapToStartOffset)
            {
                Root.position = _basePosition + ToWorld(first.StartOffset);
            }
            yield return StartCoroutine(BlendCameraTo(_cameraConfig, _cameraMoveDuration));
            SaveCameraPose();
            _rigWasCinematic = _cameraRigController != null && _cameraRigController.IsCinematicModeActive;
            _cameraRigController?.SetCinematicModeActive(true);

            yield return StartCoroutine(Wait(_startDelay));

            foreach(BalanceIntroStep step in steps)
            {
                if(step == null)
                {
                    continue;
                }

                yield return StartCoroutine(RunStep(step));
            }

            _bobActive = false;

            if(_cameraTouched)
            {
                BeginCameraTween(_savedCamPos, _savedCamRot, _savedFov, _savedOrtho, _returnCameraDuration);

                while(_camTweenActive)
                {
                    TickPresentation(Time.deltaTime);
                    yield return null;
                }
            }

            yield return StartCoroutine(Wait(_finishDelay));

            Root.position = _basePosition;

            FinishIntro();
        }

        // <summary>
        /// 指定した設定へカメラをブレンドさせる
        /// </summary>
        /// <param name="config">目標のカメラ設定</param>
        /// <param name="duration">ブレンドにかける時間</param>
        private IEnumerator BlendCameraTo(StaticCameraConfig config, float duration)
        {
            if (config == null || _targetCamera == null)
            {
                yield break;
            }


            StaticCameraConfig.ProjectionSettings settings = config.GetSettings(_cameraProjectionMode);
            Transform cameraTransform = _targetCamera.transform;

            // 現在のカメラの位置・回転・画角を保存しておく
            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float startFieldOfView = _targetCamera.fieldOfView;
            float startOrthographicSize = _targetCamera.orthographicSize;

            Vector3 targetPosition = settings.Position;
            Quaternion targetRotation = Quaternion.Euler(settings.Rotation);

            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float t = _cameraBlendCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

                // LerpUnclampedを使うことで、曲線の値が0未満や1を超える場合でも補間できる
                cameraTransform.position = Vector3.LerpUnclamped(startPosition, targetPosition, t);
                cameraTransform.rotation = Quaternion.SlerpUnclamped(startRotation, targetRotation, t);
                _targetCamera.fieldOfView = Mathf.LerpUnclamped(startFieldOfView, settings.FieldOfView, t);
                _targetCamera.orthographicSize = Mathf.LerpUnclamped(startOrthographicSize, settings.OrthographicSize, t);

                elapsed += Time.deltaTime;

                yield return null;
            }

            // 最終的にターゲットの値を確実に設定する
            cameraTransform.position = targetPosition;
            cameraTransform.rotation = targetRotation;
            _targetCamera.fieldOfView = settings.FieldOfView;
            _targetCamera.orthographicSize = settings.OrthographicSize;
        }

        private BalanceIntroStep[] SelectSteps()
        {
            BalanceIntroPattern pattern = _pattern;

            if(pattern == BalanceIntroPattern.Random)
            {
                pattern = UnityEngine.Random.value < 0.5f ? BalanceIntroPattern.Pattern1 : BalanceIntroPattern.Pattern2;
            }

            LastPlayedPattern = pattern;

            return pattern == BalanceIntroPattern.Pattern1 ? _pattern1Steps : _pattern2Steps;
        }

        private IEnumerator RunStep(BalanceIntroStep step)
        {
            Transform root = Root;

            if(step.SnapToStartOffset)
            {
                root.position = _basePosition + ToWorld(step.StartOffset);
            }


            Vector3 from = root.position;
            Vector3 to = _basePosition + ToWorld(step.TargetOffset);

            _bobActive = step.BobPans;
            _bobAmplitude = step.BobAngle;
            _bobFreaquency = step.BobFrequency;

            if(step.CameraAction == BalanceIntroCameraAction.ZoomIn)
            {
                BeginZoomIn(step.CameraBlendDuration);
            }

            float spinBase = _spinAngle;
            float spinTotal = step.SpinRotations * 360.0f;
            float duration = step.MoveDuration;
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                float t = Mathf.Clamp01(elapsed / duration);

                root.position = Vector3.LerpUnclamped(from, to, step.EvaluateMove(t));

                _spinAngle = spinBase + spinTotal * Mathf.SmoothStep(0.0f, 1.0f, t);

                TickPresentation(dt);

                yield return null;
            }

            root.position = to;

            _spinAngle = Mathf.Repeat(spinBase + spinTotal, 360.0f);

            PublishShake(step.ArriveShake);

            if(step.HoldDuration > 0.0f)
            {
                yield return StartCoroutine(Wait(step.HoldDuration));
            }
        }

        private IEnumerator Wait(float seconds)
        {
            float elapsed = 0.0f;

            while (elapsed < seconds)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                TickPresentation(dt);

                yield return null;
            }

        }

        private void TickPresentation(float dt)
        {
            _bobWeight = Mathf.MoveTowards(_bobWeight, _bobActive ? 1.0f : 0.0f, dt * 6.0f);

            float bob = 0.0f;

            if(_bobWeight > 0.0f)
            {
                _bobTime += dt;
                bob = Mathf.Sin(_bobTime * Mathf.PI * 2.0f * _bobFreaquency) * _bobAmplitude * _bobWeight;
            }

            if(_scale != null)
            {
                _scale.SetPresentationAngle(_spinAngle + bob);
            }

            if (_camTweenActive && _targetCamera != null)
            {
                _camTweenElapsed += dt;

                float t = _camTweenDuration > 0.0f ? Mathf.Clamp01(_camTweenElapsed / _camTweenDuration) : 1.0f;
                float k = _cameraCurve != null ? _cameraCurve.Evaluate(t) : t;

                Transform cam = _targetCamera.transform;
                cam.position = Vector3.LerpUnclamped(_camFromPos, _camToPos, k);
                cam.rotation = Quaternion.SlerpUnclamped(_camFromRot, _camToRot, k);
                _targetCamera.fieldOfView = Mathf.LerpUnclamped(_camFromFov, _camToFov, k);
                _targetCamera.orthographicSize = Mathf.LerpUnclamped(_camFromOrtho, _camToOrtho, k);

                if(t >= 1.0f)
                {
                    _camTweenActive = false;
                }
            }
        }

        private void SaveCameraPose()
        {
            _hasSavedCamera = false;

            if(_targetCamera == null)
            {
                _savedCamRot = Quaternion.identity;
                return;
            }

            Transform cam = _targetCamera.transform;

            _savedCamPos = cam.position;
            _savedCamRot = cam.rotation;
            _savedFov = _targetCamera.fieldOfView;
            _savedOrtho = _targetCamera.orthographicSize;
            _hasSavedCamera = true;
        }

        private void BeginCameraTween(Vector3 pos,Quaternion rot,float fov,float ortho,float duration)
        {
            if (_targetCamera == null)
            {
                return;
            }

            Transform cam = _targetCamera.transform;

            _camFromPos = cam.position;
            _camFromRot = cam.rotation;
            _camFromFov = _targetCamera.fieldOfView;
            _camFromOrtho = _targetCamera.orthographicSize;

            _camToPos = pos;
            _camToRot = rot;
            _camToFov = fov;
            _camToOrtho = ortho;

            _camTweenElapsed = 0.0f;
            _camTweenDuration = Mathf.Max(0.0f, duration);
            _camTweenActive = true;
            _cameraTouched = true;

            if(_camTweenDuration <= 0.0f)
            {
                TickPresentation(0.0f);
            }
        }

        private void BeginZoomIn(float duration)
        {
            if(!_hasSavedCamera)
            {
                return;
            }

            Vector3 focus = _basePosition + ToWorld(_zoomFocusOffset);
            Vector3 forward = _savedCamRot * Vector3.forward;

            float distance = Mathf.Max(0.5f, Vector3.Dot(focus - _savedCamPos, forward));
            float ratio = Mathf.Clamp01(_zoomInRatio);

            Vector3 pos = focus - forward * (distance * (1.0f - ratio));

            BeginCameraTween(pos, _savedCamRot, _savedFov, _savedOrtho * (1.0f - ratio), duration);
        }

        private void PublishShake(float strength)
        {
            if (strength <= 0.0f || _arriveShakeDuration <= 0.0f) return;

            EventBus.Publish(new CameraShakeRequestedEvent(_arriveShakeDuration, strength, strength, _arriveShakeFrequency));
        }

        private void DisableColliders()
        {
            _colliders = Root.GetComponentsInChildren<Collider>(true);
            _colliderStates = new bool[_colliders.Length];

            for (int i = 0; i < _colliders.Length; ++i)
            {
                if (_colliders[i] == null) continue;

                _colliderStates[i] = _colliders[i].enabled;
                _colliders[i].enabled = false;
            }
        }

        private void RestoreColliders()
        {
            if (_colliders == null) return;

            for (int i = 0; i < _colliders.Length; ++i)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = _colliderStates[i];
                }
            }

            _colliders = null;
            _colliderStates = null;
        }

        private void FinishIntro()
        {
            Cleanup();
        }

        private void AbortIntro()
        {
            StopAllCoroutines();

            Cleanup();
        }

        private void Cleanup()
        {
            _isPlaying = false;
            _camTweenActive = false;
            _bobActive = false;

            if(_warningActive)
            {
                _warningActive = false;
                EventBus.Publish(new BossIntroWarningEndedEvent());
            }

            if(_scale != null)
            {
                _scale.SetPresentationDriven(false);
            }

            RestoreColliders();

            if(_targetCamera != null && _hasSavedCamera && _cameraTouched)
            {
                Transform cam = _targetCamera.transform;

                cam.position = _savedCamPos;
                cam.rotation = _savedCamRot;
                _targetCamera.fieldOfView = _savedFov;
                _targetCamera.orthographicSize = _savedOrtho;
            }

            _cameraRigController?.SetCinematicModeActive(_rigWasCinematic);

            _hasSavedCamera = false;
            _cameraTouched = false;
        }

        private void OnDrawGizmosSelected()
        {
            Camera cam = Camera.main;

            Quaternion rot = _isPlaying ? _savedCamRot : (cam != null ? cam.transform.rotation : Quaternion.identity);
            Vector3 basePos = _isPlaying ? _basePosition : Root.position;

            DrawStepGizmos(_pattern1Steps, basePos, rot, Color.cyan);
            DrawStepGizmos(_pattern2Steps, basePos, rot, Color.yellow);

            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(basePos, 0.6f);
        }

        private static void DrawStepGizmos(BalanceIntroStep[] steps,Vector3 basePos,Quaternion rot,Color color)
        {
            if (steps == null) return;

            Gizmos.color = color;

            foreach(BalanceIntroStep step in steps)
            {
                if(step == null) continue;

                Vector3 start = basePos + rot * step.StartOffset;
                Vector3 target = basePos + rot * step.TargetOffset;

                Gizmos.DrawWireSphere(target, 0.4f);

                if(step.SnapToStartOffset)
                {
                    Gizmos.DrawLine(start, target);
                    Gizmos.DrawWireCube(start, Vector3.one * 0.3f);
                }
            }
        }
    }

}
