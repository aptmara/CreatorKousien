// ------------------------------------------------------------
// File		: StageTransitionController.cs
// Summary	: Stageシーンを入れ替えて次のStageへ移行する
//
// Author	: [浅野 勇生]
// Created	: 2026-08-16
//
// Notes	:
// - GameplayShellに1つだけ配置します
// - GameplayShell側にあるプレイヤーやカメラは作り直さず、位置と状態だけ戻します
// ------------------------------------------------------------
using UnityEngine;
using Game.Core.DefenceLine;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Stage;
using Game.WaveSystem;
using UnityEngine.SceneManagement;
using System.Collections;
using Game.Gameplay.Collectibles;


namespace Game.Infrastructure.Bootstrap
{
    [DisallowMultipleComponent]
    public sealed class StageTransitionController : MonoBehaviour
    {
        /// <summary>
        /// Stage移行中かどうか
        /// </summary>
        public bool IsTransitioning { get; private set; }

        public IEnumerator TransitionRoutine(StageDataSO currentStage, StageDataSO nextStage)
        {
            if (IsTransitioning)
            {
                yield break;
            }

            if (currentStage == null || nextStage == null)
            {
                Debug.LogError("[StageTransition] StageDataSOがnullです。Stageを移行できません。");
                yield break;
            }

            IsTransitioning = true;

            // 1. 防衛ラインの残量を、シーンを壊す前に控えておく
            DefenseLineGauge gauge = Object.FindAnyObjectByType<DefenseLineGauge>();
            float carriedRatio = gauge != null ? gauge.HealthRatio : 1.0f;

            // 2. フィールド情報を未設定に戻してから、現在のStageシーンをアンロードする
            FieldContext.Invalidate();

            yield return UnloadSceneIfLoaded(currentStage.StageSceneName);

            // 3. 次のStageシーンを読み込む
            //    CollectiblePoolがシングルトンなので、必ずアンロードを終えてから読み込むこと。
            //    先に読み込むと、新しいPoolが自分自身をDestroyしてしまう。
            yield return LoadSceneAdditive(nextStage.StageSceneName);

            if (!IsSceneLoaded(nextStage.StageSceneName))
            {
                Debug.LogError($"[StageTransition] Stageシーン「{nextStage.StageSceneName}」の読み込みに失敗しました。Build Profilesを確認してください。");
                IsTransitioning = false;
                yield break;
            }

            // 4. 新しいStageの初期化が終わるまで待つ
            yield return WaitForStageReady();

            // 5. プレイヤーを開始位置へ戻して、操作できる状態にする
            ResetPlayer();

            // 6. 防衛ラインへ残量を引き継ぐ
            ApplyDefenseLineCarryOver(carriedRatio, currentStage.ClearHealRatio);

            Debug.Log($"[StageTransition]「{currentStage.StageName}」から「{nextStage.StageName}」へ移行しました。");

            IsTransitioning = false;
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 読み込み済みのシーンだけアンロードする
        /// </summary>
        /// <param name="sceneName">アンロード対象のシーン名</param>
        private static IEnumerator UnloadSceneIfLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                yield break;
            }

            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                yield break;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
            while (operation != null && !operation.isDone)
            {
                yield return null;
            }
        }


        /// <summary>
        /// シーンをAdditiveで読み込む
        /// </summary>
        /// <param name="sceneName">読み込むシーン名</param>
        private static IEnumerator LoadSceneAdditive(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogError("[StageTransition] 空のシーン名は読み込めません。");
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
                Debug.LogError($"[StageTransition] シーン「{sceneName}」の読み込み開始に失敗しました。");
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }
        }


        /// <summary>
        /// 名前からシーンがロード済みかどうかを判定する
        /// </summary>
        /// <param name="sceneName">シーン名</param>
        /// <returns>ロード済みかどうか</returns>
        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }


        /// <summary>
        /// 新しいStageシーンの初期化が終わるまで待つ
        /// </summary>
        private static IEnumerator WaitForStageReady()
        {
            // 新しいFieldBuilderがフィールドの傾きを設定するまで待つ
            while (!FieldContext.IsReady)
            {
                yield return null;
            }

            // CollectiblePoolの事前生成が終わるまで待つ
            CollectiblePool pool = Object.FindFirstObjectByType<CollectiblePool>();
            while (pool != null && !pool.IsPrewarmed)
            {
                yield return null;
            }
        }


        /// <summary>
        /// プレイヤーを開始位置に戻して、クリア演出で止めた状態を解除する
        /// </summary>
        private static void ResetPlayer()
        {
            // クリア演出でカメラを乗っ取ったままなので、通常追従へ戻す
            CameraRigController cameraRig = Object.FindFirstObjectByType<CameraRigController>();
            if (cameraRig != null)
            {
                cameraRig.SetCinematicModeActive(false);

                // 固定カメラはLateUpdateでは戻らないので、設定を当て直す
                cameraRig.RestoreStaticCamera();
            }

            PlayerFacade player = Object.FindFirstObjectByType<PlayerFacade>();
            PlayerSpawner spawner = Object.FindFirstObjectByType<PlayerSpawner>();

            if (player == null)
            {
                Debug.LogWarning("[StageTransition] プレイヤーが見つかりませんでした。");
                return;
            }

            // クリア演出で止めた状態を解除する
            PlayerController controller = player.GetComponentInChildren<PlayerController>(true);
            controller?.UnfreezePhysics();

            // クリア専用の腕を片付けて、通常の腕に戻す
            controller?.RestoreAttachmentFromClear();

            // クリアポーズと表情を解除する
            player.GetComponentInChildren<PlayerAnimationController>(true)?.PlayIdle();
            player.GetComponentInChildren<PlayerFaceController>(true)?.ResetFace();

            // 位置が戻せなくても操作は必ず復帰させる
            controller?.SetCanMove(true);

            if (spawner == null || spawner.SpawnPoint == null)
            {
                Debug.LogWarning("[StageTransition] プレイヤーの開始位置が見つかりませんでした。");
                return;
            }

            controller?.WarpTo(spawner.SpawnPoint.position, spawner.SpawnPoint.eulerAngles.y);
        }


        /// <summary>
        /// 新しいStageの防衛ラインに、前のStageからの残量を引き継ぐ
        /// </summary>
        /// <param name="carriedRatio">前のStageのクリア時点の残量の割合</param>
        /// <param name="healRatio">回復量の割合</param>
        private static void ApplyDefenseLineCarryOver(float carriedRatio, float healRatio)
        {
            DefenseLineGauge gauge = Object.FindAnyObjectByType<DefenseLineGauge>();
            if (gauge == null)
            {
                Debug.LogWarning("[StageTransition] DefenseLineGaugeが見つかりませんでした。");
                return;
            }

            gauge.ApplyStageCarryOver(carriedRatio, healRatio);
        }
    }
}
