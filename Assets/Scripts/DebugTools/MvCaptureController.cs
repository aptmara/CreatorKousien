// ------------------------------------------------------------
// File     : MvCaptureController.cs
// Summary  : MV撮影用のランタイムカメラと撮影補助機能
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Enemy;
using Game.Gameplay.Cameras;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Player;
using Game.Presentation.GameOverCinematic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Game.DebugTools
{
    /// <summary>
    /// EditorとPlayerビルドの両方で使用するMV撮影用コントローラー。
    /// </summary>
    public sealed class MvCaptureController : MonoBehaviour
    {
        private enum CaptureCameraMode
        {
            ThirdPerson,
            Free
        }

        private static readonly HashSet<string> BoundaryObjectNames = new HashSet<string>
        {
            "FIELD_WALL",
            "Wall_Front",
            "Wall_Back",
            "Wall_Left",
            "Wall_Right",
            "Wall_LeftCorner",
            "Wall_RightCorner"
        };

        private static readonly HashSet<string> PlayerHighlightFeatureNames = new HashSet<string>
        {
            "Player Visible Mask",
            "Player Occluded Outline",
            "Player Occluded Fill"
        };

        [Header("三人称カメラ")]
        [SerializeField] private float _targetHeight = 1.5f;
        [SerializeField] private float _distance = 7f;
        [SerializeField] private float _minimumDistance = 2f;
        [SerializeField] private float _maximumDistance = 60f;
        [SerializeField] private float _orbitSensitivity = 0.15f;
        [SerializeField] private float _positionSmoothTime = 0.08f;

        [Header("自由カメラ")]
        [SerializeField] private float _freeMoveSpeed = 8f;
        [SerializeField] private float _fastMoveMultiplier = 3f;

        [Header("共通設定")]
        [SerializeField] private float _zoomSpeed = 1f;
        [SerializeField] private float _minimumFieldOfView = 15f;
        [SerializeField] private float _maximumFieldOfView = 100f;
        [SerializeField] private float _minimumPitch = -80f;
        [SerializeField] private float _maximumPitch = 80f;

        [Header("敵撮影")]
        [SerializeField] private EnemyDefinition[] _showcaseEnemies;

        [Header("撮影表示")]
        [SerializeField] private ScriptableRendererData _rendererData;

        private readonly Dictionary<Collider, bool> _boundaryColliderStates = new Dictionary<Collider, bool>();
        private readonly Dictionary<Canvas, bool> _canvasStates = new Dictionary<Canvas, bool>();
        private readonly Dictionary<Renderer, bool> _playerRendererStates = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Renderer, bool> _collectibleRendererStates = new Dictionary<Renderer, bool>();
        private readonly Dictionary<ScriptableRendererFeature, bool> _highlightFeatureStates = new Dictionary<ScriptableRendererFeature, bool>();
        private readonly Dictionary<GameObject, bool> _gateEffectStates = new Dictionary<GameObject, bool>();
        private readonly List<EnemyController> _showcaseEnemyInstances = new List<EnemyController>();

        private Camera _camera;
        private CameraRigController _cameraRig;
        private EnemySpawner _enemySpawner;
        private CollectibleSpawner _collectibleSpawner;
        private CollectibleRegistry _collectibleRegistry;
        private Transform _player;
        private CaptureCameraMode _cameraMode;
        private bool _isCaptureActive;
        private bool _boundariesDisabled;
        private bool _uiHidden;
        private bool _playerHidden;
        private bool _collectiblesHidden;
        private bool _collectibleSpawningPaused;
        private bool _highlightDisabled;
        private bool _enemyFocusActive;
        private bool _gateEffectHidden;
        private EnemyController _focusedEnemy;
        private Vector3 _savedCameraPosition;
        private Quaternion _savedCameraRotation;
        private bool _savedOrthographic;
        private float _savedFieldOfView;
        private float _savedOrthographicSize;
        private CursorLockMode _savedCursorLockMode;
        private bool _savedCursorVisible;
        private float _yaw;
        private float _pitch;
        private Vector3 _smoothedFocusPosition;
        private Vector3 _focusSmoothVelocity;
        private bool _hasSmoothedFocusPosition;
        private int _showcaseEnemyIndex = -1;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ExitCaptureMode();
            RestoreBoundaryColliders();
            RestoreCanvases();
            RestorePlayerRenderers();
            RestoreCollectibleRenderers();
            RestoreHighlightFeatures();
            RestoreGateEffects();
            SetCollectibleSpawningPaused(false);
            ClearShowcaseEnemies();
            _boundariesDisabled = false;
            _uiHidden = false;
            _playerHidden = false;
            _collectiblesHidden = false;
            _highlightDisabled = false;
            _enemyFocusActive = false;
            _gateEffectHidden = false;
            _focusedEnemy = null;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f9Key.wasPressedThisFrame)
            {
                ToggleCameraMode(CaptureCameraMode.Free);
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                ToggleCameraMode(CaptureCameraMode.ThirdPerson);
            }

            if (keyboard.f7Key.wasPressedThisFrame)
            {
                SetBoundariesDisabled(!_boundariesDisabled);
            }

            if (keyboard.f8Key.wasPressedThisFrame)
            {
                SetUiHidden(!_uiHidden);
            }

            if (keyboard.f10Key.wasPressedThisFrame)
            {
                int direction = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? -1 : 1;
                SpawnSingleShowcaseEnemy(direction);
            }

            if (keyboard.f11Key.wasPressedThisFrame)
            {
                SpawnShowcaseEnemyLineup();
            }

            if (keyboard.f12Key.wasPressedThisFrame)
            {
                ClearShowcaseEnemies();
            }

            bool controlPressed = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
            if (controlPressed && keyboard.digit1Key.wasPressedThisFrame)
            {
                SetPlayerHidden(!_playerHidden);
            }

            if (controlPressed && keyboard.digit2Key.wasPressedThisFrame)
            {
                SetCollectiblesHidden(!_collectiblesHidden);
            }

            if (controlPressed && keyboard.digit3Key.wasPressedThisFrame)
            {
                DeleteCollectibles();
            }

            if (controlPressed && keyboard.digit4Key.wasPressedThisFrame)
            {
                SetCollectibleSpawningPaused(!_collectibleSpawningPaused);
            }

            if (controlPressed && keyboard.digit5Key.wasPressedThisFrame)
            {
                SetHighlightDisabled(!_highlightDisabled);
            }

            if (controlPressed && keyboard.digit6Key.wasPressedThisFrame)
            {
                ToggleFocusTarget();
            }

            if (controlPressed && keyboard.digit7Key.wasPressedThisFrame)
            {
                SetGateEffectHidden(!_gateEffectHidden);
            }

            if (controlPressed && _enemyFocusActive && keyboard.leftArrowKey.wasPressedThisFrame)
            {
                SwitchFocusedEnemy(-1);
            }

            if (controlPressed && _enemyFocusActive && keyboard.rightArrowKey.wasPressedThisFrame)
            {
                SwitchFocusedEnemy(1);
            }

            if (_playerHidden)
            {
                HidePlayerRenderers();
            }

            if (_collectiblesHidden)
            {
                HideCollectibleRenderers();
            }

            if (!_isCaptureActive)
            {
                return;
            }
        }

        private void LateUpdate()
        {
            if (!_isCaptureActive || !ResolveRuntimeReferences())
            {
                return;
            }

            UpdateRotationInput();
            UpdateZoomInput();

            if (_cameraMode == CaptureCameraMode.ThirdPerson)
            {
                UpdateThirdPersonCamera();
            }
            else
            {
                UpdateFreeCamera();
            }
        }

        private void ToggleCameraMode(CaptureCameraMode mode)
        {
            if (_isCaptureActive && _cameraMode == mode)
            {
                ExitCaptureMode();
                return;
            }

            if (!_isCaptureActive)
            {
                EnterCaptureMode(mode);
                return;
            }

            _cameraMode = mode;
            InitializeAnglesFromCamera();
            if (mode == CaptureCameraMode.ThirdPerson)
            {
                InitializeThirdPersonFocus();
            }
            LogCameraMode();
        }

        private void EnterCaptureMode(CaptureCameraMode mode)
        {
            if (!ResolveRuntimeReferences())
            {
                Debug.LogWarning("[MV Capture] MainCameraまたはプレイヤーが見つかりません。");
                return;
            }

            Transform cameraTransform = _camera.transform;
            _savedCameraPosition = cameraTransform.position;
            _savedCameraRotation = cameraTransform.rotation;
            _savedOrthographic = _camera.orthographic;
            _savedFieldOfView = _camera.fieldOfView;
            _savedOrthographicSize = _camera.orthographicSize;
            _savedCursorLockMode = Cursor.lockState;
            _savedCursorVisible = Cursor.visible;

            _cameraRig?.SetCinematicModeActive(true);
            _camera.orthographic = false;
            _cameraMode = mode;
            _isCaptureActive = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            InitializeAnglesFromCamera();
            InitializeThirdPersonFocus();
            LogCameraMode();
        }

        private void ExitCaptureMode()
        {
            if (!_isCaptureActive)
            {
                return;
            }

            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_savedCameraPosition, _savedCameraRotation);
                _camera.orthographic = _savedOrthographic;
                _camera.fieldOfView = _savedFieldOfView;
                _camera.orthographicSize = _savedOrthographicSize;
            }

            _cameraRig?.SetCinematicModeActive(false);
            Cursor.lockState = _savedCursorLockMode;
            Cursor.visible = _savedCursorVisible;
            _isCaptureActive = false;
            _focusSmoothVelocity = Vector3.zero;
            _hasSmoothedFocusPosition = false;
            Debug.Log("[MV Capture] カメラOFF");
        }

        private void LogCameraMode()
        {
            string modeName = _cameraMode == CaptureCameraMode.ThirdPerson ? "三人称" : "自由カメラ";
            Debug.Log($"[MV Capture] {modeName} ON  F9:自由カメラ F6:三人称 F7:境界 F8:UI Mouse:回転 Wheel:ズーム");
        }

        private bool ResolveRuntimeReferences()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_cameraRig == null)
            {
                _cameraRig = Object.FindFirstObjectByType<CameraRigController>(FindObjectsInactive.Include);
            }

            if (_player == null)
            {
                PlayerFacade facade = Object.FindFirstObjectByType<PlayerFacade>(FindObjectsInactive.Exclude);
                if (facade != null)
                {
                    _player = facade.transform;
                }
            }

            return _camera != null && _player != null;
        }

        private void InitializeAnglesFromCamera()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 eulerAngles = _camera.transform.eulerAngles;
            _yaw = eulerAngles.y;
            _pitch = NormalizeAngle(eulerAngles.x);
        }

        private void InitializeThirdPersonFocus()
        {
            Transform focusTarget = ResolveFocusTarget();
            if (focusTarget == null)
            {
                _hasSmoothedFocusPosition = false;
                return;
            }

            _smoothedFocusPosition = focusTarget.position + Vector3.up * _targetHeight;
            _focusSmoothVelocity = Vector3.zero;
            _hasSmoothedFocusPosition = true;
        }

        private void UpdateRotationInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue();
            _yaw += delta.x * _orbitSensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * _orbitSensitivity, _minimumPitch, _maximumPitch);
        }

        private void UpdateZoomInput()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            float rawScroll = mouse.scroll.ReadValue().y;
            if (Mathf.Approximately(rawScroll, 0f))
            {
                return;
            }

            // Input Systemのホイール値は環境により1単位または120単位になるため、1目盛り基準へ揃える。
            float scroll = Mathf.Abs(rawScroll) >= 120f
                ? rawScroll / 120f
                : Mathf.Sign(rawScroll);

            if (_cameraMode == CaptureCameraMode.ThirdPerson)
            {
                _distance = Mathf.Clamp(_distance - scroll * _zoomSpeed, _minimumDistance, _maximumDistance);
            }
            else
            {
                _camera.fieldOfView = Mathf.Clamp(
                    _camera.fieldOfView - scroll * _zoomSpeed * 2f,
                    _minimumFieldOfView,
                    _maximumFieldOfView);
            }
        }

        private void UpdateThirdPersonCamera()
        {
            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Transform focusTarget = ResolveFocusTarget();
            if (focusTarget == null)
            {
                return;
            }

            Vector3 targetFocusPosition = focusTarget.position + Vector3.up * _targetHeight;
            float smoothTime = Mathf.Max(0.001f, _positionSmoothTime);

            if (!_hasSmoothedFocusPosition)
            {
                InitializeThirdPersonFocus();
            }
            else
            {
                _smoothedFocusPosition = Vector3.SmoothDamp(
                    _smoothedFocusPosition,
                    targetFocusPosition,
                    ref _focusSmoothVelocity,
                    smoothTime,
                    Mathf.Infinity,
                    Time.unscaledDeltaTime);
            }

            Vector3 cameraPosition = _smoothedFocusPosition - rotation * Vector3.forward * _distance;
            _camera.transform.SetPositionAndRotation(cameraPosition, rotation);
        }

        private void UpdateFreeCamera()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector3 input = Vector3.zero;
            if (keyboard.upArrowKey.isPressed) input.z += 1f;
            if (keyboard.downArrowKey.isPressed) input.z -= 1f;
            if (keyboard.rightArrowKey.isPressed) input.x += 1f;
            if (keyboard.leftArrowKey.isPressed) input.x -= 1f;
            if (keyboard.pageUpKey.isPressed) input.y += 1f;
            if (keyboard.pageDownKey.isPressed) input.y -= 1f;

            float speed = _freeMoveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                speed *= _fastMoveMultiplier;
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _camera.transform.rotation = rotation;

            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            Vector3 movement = rotation * input * (speed * Time.unscaledDeltaTime);
            _camera.transform.position += movement;
        }

        private void SetBoundariesDisabled(bool disabled)
        {
            if (!disabled)
            {
                int restoredCount = _boundaryColliderStates.Count;
                RestoreBoundaryColliders();
                _boundariesDisabled = false;
                Debug.Log($"[MV Capture] 境界Collider ON ({restoredCount}個復元)");
                return;
            }

            Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform target in transforms)
            {
                if (!BoundaryObjectNames.Contains(target.name))
                {
                    continue;
                }

                Collider[] colliders = target.GetComponentsInChildren<Collider>(true);
                foreach (Collider targetCollider in colliders)
                {
                    if (!_boundaryColliderStates.ContainsKey(targetCollider))
                    {
                        _boundaryColliderStates.Add(targetCollider, targetCollider.enabled);
                    }

                    targetCollider.enabled = false;
                }
            }

            _boundariesDisabled = true;
            Debug.Log($"[MV Capture] 境界Collider OFF ({_boundaryColliderStates.Count}個無効化)");
        }

        private void RestoreBoundaryColliders()
        {
            foreach (KeyValuePair<Collider, bool> state in _boundaryColliderStates)
            {
                if (state.Key != null)
                {
                    state.Key.enabled = state.Value;
                }
            }

            _boundaryColliderStates.Clear();
        }

        private void SetUiHidden(bool hidden)
        {
            if (!hidden)
            {
                RestoreCanvases();
                _uiHidden = false;
                Debug.Log("[MV Capture] UI表示");
                return;
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                if (!_canvasStates.ContainsKey(canvas))
                {
                    _canvasStates.Add(canvas, canvas.enabled);
                }

                canvas.enabled = false;
            }

            _uiHidden = true;
            Debug.Log($"[MV Capture] UI非表示 ({_canvasStates.Count} Canvas)");
        }

        private void RestoreCanvases()
        {
            foreach (KeyValuePair<Canvas, bool> state in _canvasStates)
            {
                if (state.Key != null)
                {
                    state.Key.enabled = state.Value;
                }
            }

            _canvasStates.Clear();
        }

        private void SetPlayerHidden(bool hidden)
        {
            if (!hidden)
            {
                RestorePlayerRenderers();
                _playerHidden = false;
                Debug.Log("[MV Capture] プレイヤー表示");
                return;
            }

            _playerHidden = true;
            HidePlayerRenderers();
            Debug.Log($"[MV Capture] プレイヤー非表示 ({_playerRendererStates.Count} Renderer)");
        }

        private void HidePlayerRenderers()
        {
            PlayerFacade facade = Object.FindFirstObjectByType<PlayerFacade>(FindObjectsInactive.Exclude);
            if (facade == null)
            {
                return;
            }

            Renderer[] renderers = facade.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer targetRenderer in renderers)
            {
                if (!_playerRendererStates.ContainsKey(targetRenderer))
                {
                    _playerRendererStates.Add(targetRenderer, targetRenderer.enabled);
                }

                targetRenderer.enabled = false;
            }
        }

        private void RestorePlayerRenderers()
        {
            foreach (KeyValuePair<Renderer, bool> state in _playerRendererStates)
            {
                if (state.Key != null)
                {
                    state.Key.enabled = state.Value;
                }
            }

            _playerRendererStates.Clear();
        }

        private void SetCollectiblesHidden(bool hidden)
        {
            if (!hidden)
            {
                RestoreCollectibleRenderers();
                _collectiblesHidden = false;
                Debug.Log("[MV Capture] Collectible表示");
                return;
            }

            _collectiblesHidden = true;
            HideCollectibleRenderers();
            Debug.Log($"[MV Capture] Collectible非表示 ({_collectibleRendererStates.Count} Renderer)");
        }

        private void HideCollectibleRenderers()
        {
            if (!ResolveCollectibleRegistry(false))
            {
                return;
            }

            foreach (CollectibleObject collectible in _collectibleRegistry.ActiveCollectibles)
            {
                if (collectible == null)
                {
                    continue;
                }

                Renderer[] renderers = collectible.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer targetRenderer in renderers)
                {
                    if (!_collectibleRendererStates.ContainsKey(targetRenderer))
                    {
                        _collectibleRendererStates.Add(targetRenderer, targetRenderer.enabled);
                    }

                    targetRenderer.enabled = false;
                }
            }
        }

        private void RestoreCollectibleRenderers()
        {
            foreach (KeyValuePair<Renderer, bool> state in _collectibleRendererStates)
            {
                if (state.Key != null)
                {
                    state.Key.enabled = state.Value;
                }
            }

            _collectibleRendererStates.Clear();
        }

        private void DeleteCollectibles()
        {
            if (!ResolveCollectibleRegistry())
            {
                return;
            }

            if (_collectiblesHidden)
            {
                RestoreCollectibleRenderers();
            }

            List<CollectibleObject> collectibles = new List<CollectibleObject>(_collectibleRegistry.ActiveCollectibles);
            foreach (CollectibleObject collectible in collectibles)
            {
                if (collectible != null)
                {
                    collectible.Despawn();
                }
            }

            Debug.Log($"[MV Capture] Collectibleを{collectibles.Count}個削除");
        }

        private bool ResolveCollectibleRegistry(bool logWarning = true)
        {
            if (_collectibleRegistry == null)
            {
                _collectibleRegistry = Object.FindFirstObjectByType<CollectibleRegistry>(FindObjectsInactive.Exclude);
            }

            if (_collectibleRegistry != null)
            {
                return true;
            }

            if (logWarning)
            {
                Debug.LogWarning("[MV Capture] CollectibleRegistryが見つかりません。");
            }

            return false;
        }

        private void SetCollectibleSpawningPaused(bool paused)
        {
            _collectibleSpawningPaused = paused;
            if (_collectibleSpawner == null)
            {
                _collectibleSpawner = Object.FindFirstObjectByType<CollectibleSpawner>(FindObjectsInactive.Exclude);
            }

            if (_collectibleSpawner == null)
            {
                if (paused)
                {
                    Debug.LogWarning("[MV Capture] CollectibleSpawnerが見つかりません。");
                }
                return;
            }

            _collectibleSpawner.SetRuntimeSpawningEnabled(!paused);
            Debug.Log(paused ? "[MV Capture] Collectible生成停止" : "[MV Capture] Collectible生成再開");
        }

        private void SetHighlightDisabled(bool disabled)
        {
            if (!disabled)
            {
                RestoreHighlightFeatures();
                _highlightDisabled = false;
                Debug.Log("[MV Capture] プレイヤーハイライト ON");
                return;
            }

            if (_rendererData == null)
            {
                Debug.LogWarning("[MV Capture] RendererDataが設定されていません。");
                return;
            }

            foreach (ScriptableRendererFeature feature in _rendererData.rendererFeatures)
            {
                if (feature == null || !PlayerHighlightFeatureNames.Contains(feature.name))
                {
                    continue;
                }

                if (!_highlightFeatureStates.ContainsKey(feature))
                {
                    _highlightFeatureStates.Add(feature, feature.isActive);
                }

                feature.SetActive(false);
            }

            _highlightDisabled = true;
            Debug.Log($"[MV Capture] プレイヤーハイライト OFF ({_highlightFeatureStates.Count} Feature)");
        }

        private void RestoreHighlightFeatures()
        {
            foreach (KeyValuePair<ScriptableRendererFeature, bool> state in _highlightFeatureStates)
            {
                if (state.Key != null)
                {
                    state.Key.SetActive(state.Value);
                }
            }

            _highlightFeatureStates.Clear();
        }

        private void ToggleFocusTarget()
        {
            if (_enemyFocusActive)
            {
                _enemyFocusActive = false;
                _focusedEnemy = null;
                ResetFocusTransition();
                Debug.Log("[MV Capture] 注視対象: プレイヤー");
                return;
            }

            EnemyController[] enemies = GetFocusCandidates();
            if (enemies.Length == 0)
            {
                Debug.LogWarning("[MV Capture] 注視できる敵がいません。");
                return;
            }

            _focusedEnemy = enemies[0];
            _enemyFocusActive = true;
            ResetFocusTransition();
            Debug.Log($"[MV Capture] 注視対象: {_focusedEnemy.InstanceEnemyId}");
        }

        private void SwitchFocusedEnemy(int direction)
        {
            EnemyController[] enemies = GetFocusCandidates();
            if (enemies.Length <= 1)
            {
                return;
            }

            int currentIndex = System.Array.IndexOf(enemies, _focusedEnemy);
            int nextIndex = currentIndex < 0
                ? 0
                : (currentIndex + direction + enemies.Length) % enemies.Length;

            _focusedEnemy = enemies[nextIndex];
            ResetFocusTransition();
            Debug.Log($"[MV Capture] 注視対象: {_focusedEnemy.InstanceEnemyId} ({nextIndex + 1}/{enemies.Length})");
        }

        private Transform ResolveFocusTarget()
        {
            if (!_enemyFocusActive)
            {
                return _player;
            }

            if (IsValidFocusEnemy(_focusedEnemy))
            {
                return _focusedEnemy.transform;
            }

            EnemyController[] enemies = GetFocusCandidates();
            if (enemies.Length > 0)
            {
                _focusedEnemy = enemies[0];
                _focusSmoothVelocity = Vector3.zero;
                return _focusedEnemy.transform;
            }

            _enemyFocusActive = false;
            _focusedEnemy = null;
            _focusSmoothVelocity = Vector3.zero;
            Debug.Log("[MV Capture] 注視できる敵がいないため、プレイヤー注視へ戻しました。");
            return _player;
        }

        private static EnemyController[] GetFocusCandidates()
        {
            EnemyController[] foundEnemies = Object.FindObjectsByType<EnemyController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.InstanceID);
            List<EnemyController> validEnemies = new List<EnemyController>(foundEnemies.Length);

            foreach (EnemyController enemy in foundEnemies)
            {
                if (IsValidFocusEnemy(enemy))
                {
                    validEnemies.Add(enemy);
                }
            }

            return validEnemies.ToArray();
        }

        private static bool IsValidFocusEnemy(EnemyController enemy)
        {
            return enemy != null
                && enemy.gameObject.activeInHierarchy
                && enemy.CurrentState != EnemyState.Defeated;
        }

        private void ResetFocusTransition()
        {
            _focusSmoothVelocity = Vector3.zero;
            if (!_hasSmoothedFocusPosition)
            {
                InitializeThirdPersonFocus();
            }
        }

        private void SetGateEffectHidden(bool hidden)
        {
            if (!hidden)
            {
                RestoreGateEffects();
                _gateEffectHidden = false;
                Debug.Log("[MV Capture] 扉エフェクト表示");
                return;
            }

            _gateEffectHidden = true;
            GameOverGateAnchor[] gates = Object.FindObjectsByType<GameOverGateAnchor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (GameOverGateAnchor gate in gates)
            {
                GameObject effectRoot = gate.GateEffectRoot;
                if (effectRoot == null)
                {
                    continue;
                }

                if (!_gateEffectStates.ContainsKey(effectRoot))
                {
                    _gateEffectStates.Add(effectRoot, effectRoot.activeSelf);
                }

                effectRoot.SetActive(false);
            }

            Debug.Log($"[MV Capture] 扉エフェクト非表示 ({_gateEffectStates.Count}個)");
        }

        private void RestoreGateEffects()
        {
            foreach (KeyValuePair<GameObject, bool> state in _gateEffectStates)
            {
                if (state.Key != null)
                {
                    state.Key.SetActive(state.Value);
                }
            }

            _gateEffectStates.Clear();
        }

        private void SpawnSingleShowcaseEnemy(int direction)
        {
            if (!TryGetShowcaseSpawner(out EnemySpawner spawner))
            {
                return;
            }

            ClearShowcaseEnemies();
            _showcaseEnemyIndex = GetNextShowcaseEnemyIndex(_showcaseEnemyIndex, direction);
            if (_showcaseEnemyIndex < 0)
            {
                Debug.LogWarning("[MV Capture] 撮影用の敵データが設定されていません。");
                return;
            }

            EnemyDefinition definition = _showcaseEnemies[_showcaseEnemyIndex];
            if (spawner.TrySpawnEnemy(definition, 1f, 1f, 0.5f, out EnemyController enemy))
            {
                _showcaseEnemyInstances.Add(enemy);
                Debug.Log($"[MV Capture] 撮影用の敵を生成: {definition.EnemyId} ({_showcaseEnemyIndex + 1}/{_showcaseEnemies.Length})");
            }
        }

        private void SpawnShowcaseEnemyLineup()
        {
            if (_showcaseEnemies == null || _showcaseEnemies.Length == 0)
            {
                Debug.LogWarning("[MV Capture] 撮影用の敵データが設定されていません。");
                return;
            }

            if (!TryGetShowcaseSpawner(out EnemySpawner spawner))
            {
                return;
            }

            ClearShowcaseEnemies();

            int validEnemyCount = 0;
            foreach (EnemyDefinition definition in _showcaseEnemies)
            {
                if (definition != null)
                {
                    validEnemyCount++;
                }
            }

            if (validEnemyCount == 0)
            {
                Debug.LogWarning("[MV Capture] 撮影用の敵データが設定されていません。");
                return;
            }

            foreach (EnemyDefinition definition in _showcaseEnemies)
            {
                if (definition == null)
                {
                    continue;
                }

                if (spawner.TrySpawnEnemy(definition, 1f, 1f, 2.5f, out EnemyController enemy))
                {
                    _showcaseEnemyInstances.Add(enemy);
                }
            }

            Debug.Log($"[MV Capture] 撮影用の敵を{_showcaseEnemyInstances.Count}体生成");
        }

        private bool TryGetShowcaseSpawner(out EnemySpawner spawner)
        {
            if (_enemySpawner == null)
            {
                _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>(FindObjectsInactive.Exclude);
            }

            spawner = _enemySpawner;
            if (spawner == null)
            {
                Debug.LogWarning("[MV Capture] EnemySpawnerが見つかりません。");
                return false;
            }

            return true;
        }

        private int GetNextShowcaseEnemyIndex(int currentIndex, int direction)
        {
            if (_showcaseEnemies == null || _showcaseEnemies.Length == 0)
            {
                return -1;
            }

            if (currentIndex < 0 && direction < 0)
            {
                currentIndex = 0;
            }

            for (int i = 0; i < _showcaseEnemies.Length; i++)
            {
                currentIndex = (currentIndex + direction + _showcaseEnemies.Length) % _showcaseEnemies.Length;
                if (_showcaseEnemies[currentIndex] != null)
                {
                    return currentIndex;
                }
            }

            return -1;
        }

        private void ClearShowcaseEnemies()
        {
            foreach (EnemyController enemy in _showcaseEnemyInstances)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            _showcaseEnemyInstances.Clear();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _player = null;
            _cameraRig = null;
            _enemySpawner = null;
            _collectibleSpawner = null;
            _collectibleRegistry = null;
            _showcaseEnemyInstances.Clear();
            _hasSmoothedFocusPosition = false;
            _focusSmoothVelocity = Vector3.zero;

            if (_isCaptureActive && ResolveRuntimeReferences())
            {
                _cameraRig?.SetCinematicModeActive(true);
                InitializeThirdPersonFocus();
            }

            if (_boundariesDisabled)
            {
                SetBoundariesDisabled(true);
            }

            if (_uiHidden)
            {
                SetUiHidden(true);
            }

            if (_playerHidden)
            {
                HidePlayerRenderers();
            }

            if (_collectiblesHidden)
            {
                HideCollectibleRenderers();
            }

            if (_collectibleSpawningPaused)
            {
                _collectibleSpawner = Object.FindFirstObjectByType<CollectibleSpawner>(FindObjectsInactive.Exclude);
                _collectibleSpawner?.SetRuntimeSpawningEnabled(false);
            }

            if (_gateEffectHidden)
            {
                SetGateEffectHidden(true);
            }
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
