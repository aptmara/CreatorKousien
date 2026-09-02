// ------------------------------------------------------------
// File     : PrototypeSceneFlowController.cs
// Summary  : 統合プロトタイプ用のシーン読み込みと初期接続を行う
//
// Author   : 山内陽
// Created  : 2026-05-06
// Updated  : 2026-06-02 (カメラモード分岐ロジック)
//
// Notes    :
// - 5/6: Bootから設計通りのAdditiveシーンを読み込む統合用Bootstrapを追加
// - 8/14: Stage2対応 - 浅野
// - 8/25: なんかイントロでプレイヤーが移動するバグ直しますお - 浅野
// ------------------------------------------------------------
using System.Collections;
using Game.Core.Management;
using Game.Core.Roguelike;
using Game.Gameplay.Cameras;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Player;
using Game.WaveSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Infrastructure.Bootstrap
{
    /// <summary>
    /// Bootシーンから統合プロトタイプに必要なシーンをAdditiveロードし、最低限の実行参照を接続する。
    /// </summary>
    public sealed class PrototypeSceneFlowController : MonoBehaviour
    {
        [Header("Scene Flow")]
        [Tooltip("統合の器になるGameplayShellシーン名")]
        [SerializeField] private string _gameplayShellScene = "GameplayShell";

        [Tooltip("StageDataSOからScene名を取得できなかったときに読み込むStageシーン名(フォールバック)")]
        [SerializeField] private string _stageScene = "Stage_Prototype_01";

        [Tooltip("HUDシーン名")]
        [SerializeField] private string _uiScene = "UI_HUD";

        [Tooltip("DebugOverlayシーン名")]
        [SerializeField] private string _debugScene = "DebugOverlay";

        [Header("カメラシステム設定群")]
        [Tooltip("カメラの位置・角度パラメータを保持するScriptableObjectアセット")]
        [SerializeField] private StaticCameraConfig _cameraConfig;

        [Tooltip("このプロトタイプ起動時に適用するカメラの投影モード")]
        [SerializeField] private CameraRigController.ProjectionMode _cameraProjectionMode = CameraRigController.ProjectionMode.Perspective;

        [Header("Initial Runtime")]
        [Tooltip("統合シーン開始時に生成する収集物数")]
        [SerializeField] private int _initialCollectibleCount = 80;

        [Tooltip("統合プロトタイプではプレイヤーの保持/収集モードを使わない")]
        [SerializeField] private bool _disablePlayerHoldMode = true;

        private bool _isBootstrapped;
        private PlayerFacade _preparedPlayer;
        private string _resolvedStageScene;         // 実際に読み込んだStageシーンの名前

        public bool IsPrepared { get; private set; }
        public bool PreparationFailed { get; private set; }

        private void Start()
        {
            Scene loadingScene = SceneManager.GetSceneByName("Loading");
            if (!loadingScene.IsValid() || !loadingScene.isLoaded)
            {
                StartCoroutine(PrepareAndStartStandaloneRoutine());
            }
        }

        /// <summary>
        /// シーン読み込み、参照接続、初期スポーンを順番に実行する。
        /// StageDataSOが渡されない場合は、StageSceneContextの設定をそのまま使う。
        /// </summary>
        public IEnumerator PrepareGameRoutine()
        {
            yield return PrepareGameRoutine(null);
        }


        /// <summary>
        /// シーン読み込み、参照接続、初期スポーンを順番に実行する。
        /// </summary>
        /// <param name="stageData">StageDataSOの参照。nullの場合はStageSceneContextの設定をそのまま使う。</param>
        public IEnumerator PrepareGameRoutine(StageDataSO stageData)
        {
            if (_isBootstrapped)
            {
                yield break;
            }

            _isBootstrapped = true;
            RoguelikeUpgradeRuntime.Reset();

            // StageSceneCotextからStageシーンを取得
            StageSceneContext stageContext = Object.FindFirstObjectByType<StageSceneContext>();

            if (stageContext == null)
            {
                Debug.LogError("[PrototypeSceneFlowController] StageSceneContextが見つかりません。");
                PreparationFailed = true;
                yield break;
            }

            // StageSelectから渡されたStageDataSOがあれば、そちらを優先する
            if (stageData != null)
            {
                stageContext.RegisterStageData(stageData);
            }

            // どのStageを読み込むかStageDataSOから決める
            _resolvedStageScene = ResolveStageSceneName(stageContext.StageData);

            yield return LoadSceneIfNeeded(_gameplayShellScene);
            yield return LoadSceneIfNeeded(_resolvedStageScene);
            yield return LoadSceneIfNeeded(_uiScene);
            yield return LoadSceneIfNeeded(_debugScene);

            if (!IsSceneLoaded(_gameplayShellScene) ||
                !IsSceneLoaded(_resolvedStageScene) ||
                !IsSceneLoaded(_uiScene) ||
                !IsSceneLoaded(_debugScene))
            {
                Debug.LogError("[PrototypeSceneFlowController] Sceneがすべてロードされていません");
                PreparationFailed = true;
                yield break;
            }

            Scene gameplayScene = SceneManager.GetSceneByName(_gameplayShellScene);
            if (gameplayScene.IsValid())
            {
                SceneManager.SetActiveScene(gameplayScene);
            }

            yield return null;

            _preparedPlayer = SpawnPlayer();
            if (_preparedPlayer == null)
            {
                PreparationFailed = true;
                yield break;
            }

            SetPlayerMovement(_preparedPlayer, false);
            DisablePlayerHoldMode(_preparedPlayer);
            ApplyCameraConfiguration();
            SpawnCollectibles();

            yield return WaitForRuntimeInitialization();

            GameProgressionManagerBase progression = Object.FindFirstObjectByType<GameProgressionManagerBase>();
            if (progression == null)
            {
                Debug.LogError("[PrototypeSceneFlowController] GameProgressionManagerが見つかりません。");
                PreparationFailed = true;
                yield break;
            }

            yield return progression.PrepareFirstWaveRoutine();
            if (progression.PreparationFailed || !progression.IsFirstWavePrepared)
            {
                PreparationFailed = true;
                yield break;
            }

            IsPrepared = true;
        }


        /// <summary>
        /// StageDataSOからStageSceneNameを取得する。nullや空文字の場合はフォールバックのStageシーン名を返す。
        /// </summary>
        /// <param name="stageData"></param>
        /// <returns>StageSceneの名前</returns>
        private string ResolveStageSceneName(StageDataSO stageData)
        {
            if (stageData != null && !string.IsNullOrWhiteSpace(stageData.StageSceneName))
            {
                return stageData.StageSceneName;
            }

            Debug.LogWarning("[PrototypeSceneFlowController] StageDataSOからStageSceneNameを取得できませんでした。フォールバックのStageシーンを使用します。");
            return _stageScene;
        }


        /// <summary>
        /// 未ロードのシーンだけAdditiveロードする。
        /// </summary>
        /// <param name="sceneName">ロード対象シーン名</param>
        private static IEnumerator LoadSceneIfNeeded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning("[PrototypeSceneFlowController] 空のシーン名はロードできません。");
                yield break;
            }

            Scene existingScene = SceneManager.GetSceneByName(sceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[PrototypeSceneFlowController] シーンロード開始に失敗しました: {sceneName}");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private static IEnumerator WaitForRuntimeInitialization()
        {
            while (!Game.Gameplay.Stage.FieldContext.IsReady)
            {
                yield return null;
            }

            CollectiblePool pool = Object.FindFirstObjectByType<CollectiblePool>();
            while (pool != null && !pool.IsPrewarmed)
            {
                yield return null;
            }
        }

        public void StartPreparedGame()
        {
            if (!IsPrepared || PreparationFailed)
            {
                return;
            }

            SetPlayerMovement(_preparedPlayer, true);

            GameProgressionManagerBase progression = Object.FindFirstObjectByType<GameProgressionManagerBase>();
            progression?.BeginPreparedGame();
            SoundManager.instance?.PlayBGM("InGame");
        }

        private static void SetPlayerMovement(PlayerFacade player, bool canMove)
        {
            if (player == null)
            {
                return;
            }

            PlayerController controller = player.GetComponentInChildren<PlayerController>(true);

            if (canMove)
            {
                controller?.UnfreezePhysics();
            }
            else
            {
                controller?.FreezePhysics();
            }
        }

        private IEnumerator PrepareAndStartStandaloneRoutine()
        {
            yield return PrepareGameRoutine();
            StartPreparedGame();
        }

        /// <summary>
        /// GameplayShell上のPlayerSpawnerからプレイヤーを生成する。
        /// </summary>
        /// <returns>生成されたPlayerFacade。失敗時はnull</returns>
        private static PlayerFacade SpawnPlayer()
        {
            PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[PrototypeSceneFlowController] PlayerSpawnerが見つかりません。");
                return null;
            }

            return spawner.Spawn();
        }

        /// <summary>
        /// 統合シーンではアイテムを保持せず、自由移動する物体として扱う。
        /// </summary>
        /// <param name="player">生成されたプレイヤー</param>
        private void DisablePlayerHoldMode(PlayerFacade player)
        {
            if (!_disablePlayerHoldMode || player == null)
            {
                return;
            }

            foreach (PlayerCollector collector in player.GetComponentsInChildren<PlayerCollector>(true))
            {
                collector.enabled = false;

                if (collector.TryGetComponent(out Collider collectCollider))
                {
                    collectCollider.enabled = false;
                }

                collector.gameObject.SetActive(false);
            }

            foreach (HeldItemViewController heldItemView in player.GetComponentsInChildren<HeldItemViewController>(true))
            {
                heldItemView.enabled = false;
            }

            foreach (PlayerHolder holder in player.GetComponentsInChildren<PlayerHolder>(true))
            {
                holder.enabled = false;
            }
        }

        /// <summary>
        /// 指定されたConfigと投影モードをCameraRigControllerに流し込んで固定配置する
        /// </summary>
        private void ApplyCameraConfiguration()
        {
            CameraRigController cameraRig = Object.FindFirstObjectByType<CameraRigController>();
            if (cameraRig == null)
            {
                Debug.LogWarning("[PrototypeSceneFlowController] 設定適用対象のCameraRigControllerが見つかりません。");
                return;
            }

            // configとモードを流し込むよん
            cameraRig.SetupStaticCamera(_cameraConfig, _cameraProjectionMode);
        }

        /// <summary>
        /// カメラリグの追従対象を生成済みプレイヤーへ接続する。
        /// </summary>
        /// <param name="player">追従対象のプレイヤー</param>
        private static void BindCameraLegacy(PlayerFacade player)
        {
            if (player == null) return;

            CameraRigController cameraRig = Object.FindFirstObjectByType<CameraRigController>();

            if (cameraRig != null) cameraRig.SetTarget(player.transform);
        }

        /// <summary>
        /// Stage側のCollectibleSpawnerから初期収集物を生成する。
        /// </summary>
        private void SpawnCollectibles()
        {
            CollectibleSpawner spawner = Object.FindFirstObjectByType<CollectibleSpawner>();
            if (spawner == null)
            {
                Debug.LogWarning("[PrototypeSceneFlowController] CollectibleSpawnerが見つかりません。");
                return;
            }

            // spawner.SpawnCollectibles(Mathf.Max(0, _initialCollectibleCount));
        }
    }
}
