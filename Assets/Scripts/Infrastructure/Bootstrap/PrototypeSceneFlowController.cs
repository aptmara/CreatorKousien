// ------------------------------------------------------------
// File     : PrototypeSceneFlowController.cs
// Summary  : 統合プロトタイプ用のシーン読み込みと初期接続を行う
//
// Author   : 山内陽
// Created  : 2026-05-06
//
// Notes    :
// - 5/6: Bootから設計通りのAdditiveシーンを読み込む統合用Bootstrapを追加
// ------------------------------------------------------------
using System.Collections;
using Game.Gameplay.Cameras;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Player;
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

        [Tooltip("統合ステージシーン名")]
        [SerializeField] private string _stageScene = "Stage_Prototype_01";

        [Tooltip("HUDシーン名")]
        [SerializeField] private string _uiScene = "UI_HUD";

        [Tooltip("DebugOverlayシーン名")]
        [SerializeField] private string _debugScene = "DebugOverlay";

        [Header("Initial Runtime")]
        [Tooltip("統合シーン開始時に生成する収集物数")]
        [SerializeField] private int _initialCollectibleCount = 80;

        [Tooltip("統合プロトタイプではプレイヤーの保持/収集モードを使わない")]
        [SerializeField] private bool _disablePlayerHoldMode = true;

        private bool _isBootstrapped;

        private void Start()
        {
            StartCoroutine(BootstrapRoutine());
        }

        /// <summary>
        /// シーン読み込み、参照接続、初期スポーンを順番に実行する。
        /// </summary>
        private IEnumerator BootstrapRoutine()
        {
            if (_isBootstrapped)
            {
                yield break;
            }

            _isBootstrapped = true;

            yield return LoadSceneIfNeeded(_gameplayShellScene);
            yield return LoadSceneIfNeeded(_stageScene);
            yield return LoadSceneIfNeeded(_uiScene);
            yield return LoadSceneIfNeeded(_debugScene);

            Scene gameplayScene = SceneManager.GetSceneByName(_gameplayShellScene);
            if (gameplayScene.IsValid())
            {
                SceneManager.SetActiveScene(gameplayScene);
            }

            yield return null;

            PlayerFacade player = SpawnPlayer();
            DisablePlayerHoldMode(player);
            BindCamera(player);
            SpawnCollectibles();
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
        /// カメラリグの追従対象を生成済みプレイヤーへ接続する。
        /// </summary>
        /// <param name="player">追従対象のプレイヤー</param>
        private static void BindCamera(PlayerFacade player)
        {
            if (player == null)
            {
                return;
            }

            CameraRigController cameraRig = Object.FindFirstObjectByType<CameraRigController>();
            if (cameraRig == null)
            {
                Debug.LogWarning("[PrototypeSceneFlowController] CameraRigControllerが見つかりません。");
                return;
            }

            cameraRig.SetTarget(player.transform);
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

            spawner.SpawnCollectibles(Mathf.Max(0, _initialCollectibleCount));
        }
    }
}
