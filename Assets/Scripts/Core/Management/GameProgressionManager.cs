// ================================================================================
// File         : GameProgressionManager.cs
// Author       : Iwai Shogo
//
// Description  : ゲーム全体の進行状態を管理するマネージャー
// Created      : 2026-07-02
//
// Notes        : クリア演出追加しました! - Asano 2026-07-09
//              : WaveSystemの実装を追加しました! - Asano 2026-07-15
//              : Stage2移行のために移行メソッドを追加しました! - Asano 2026-08-16
// ================================================================================

using Game.Core.Enemy;
using Game.Core.Events;
using Game.Gameplay.Cameras;
using Game.Gameplay.Player;
using Game.Gameplay.Shop;
using Game.Gameplay.Stage;
using Game.Presentation.GameClearCinematic;
using Game.WaveSystem;
using Game.Infrastructure.Bootstrap;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Game.Presentation.UI.Loading;

namespace Game.Core.Management
{
    /// <summary>
    /// ゲーム全体のループ進行を統括する司令塔マネージャー
    /// </summary>
    public sealed class GameProgressionManager : GameProgressionManagerBase
    {

        [Header("--- Stage移行 ---")]
        [Tooltip("Stage移行時にローディング画面を最低限見せる時間(秒)")]
        [SerializeField] private float _minimumStageLoadingDuration = 3f;


        [Header("--- クリア演出・猶予設定 ---")]

        [Tooltip("クリアした瞬間の時間の進み方 (例 0.1f: 10%のスローモーション)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _slowMotionTimeScale = 0.2f;


        private bool _isCameraWorkFinished = false;
        private List<WaveDataSO> _waveSequence = new();
        private bool _isFirstWavePrepared;
        private bool _hasGameStarted;
        private bool _preparationFailed;
        private bool _isPreparingFirstWave;

        /// <summary>
        /// Stageごとに違うWave順になるように、SeedへStage番号をかけて足す値
        /// </summary>
        private const int StageSeedStride = 7919;

        // --- Stage進行 ---
        private StageDataSO _currentStageData;          // 現在プレイ中のStage
        private int _baseSeed;                          // 現在のStageで使用する乱数シード
        private int _stageIndex;                        // 現在のStage番号
        private bool _isAdvancingStage;                 // Stage移行中かどうかのフラグ


        // カメラの固定画角を保持しておく変数
        private Vector3 _savedBattleCameraPosition;
        private Quaternion _savedBattleCameraRotation;

        /// <summary>
        /// 指定された番号のWaveを開始します。
        /// </summary>
        private void StartBattleWave(int waveIndex)
        {
            StartCoroutine(StartBattleWaveRoutine(waveIndex));
        }

        protected override void RoguelikeToComeback()
        {
            // ウェーブカウントをインクリメント
            _currentWaveIndex++;

            // 次のウェーブがあるかチェック
            if (_currentWaveIndex < _waveSequence.Count)
            {
                // マウスカーソルをロック
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 次のウェーブを開始
                StartBattleWave(_currentWaveIndex);
            }
        }

        protected override void OnDefenseLineBroken(DefLineBreakReactionEvent ev)
        {
            if (_currentState != GameProgressionState.Battle) return;
            Debug.Log("[Progression] 防衛ラインのバリア崩壊を検知。ゲームオーバー処理を開始するぜよ。");
            HandleGameResult(isClear: false);
        }


        protected override GameResultSummary CreateSummry(bool isClear)
        {
            _currentState = GameProgressionState.Result;

            // バトル側のポーズ処理、マウスカーソル解放
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 現状の防衛ラインの残りHPをシーンから取得
            var gauge = Object.FindFirstObjectByType<Core.DefenceLine.DefenseLineGauge>();
            float currentHp = gauge != null ? gauge.CurrentHP : 0f;

            // リザルト画面に渡す全データをパッキング
            // リザルト画面に渡す全データをパッキング
            // クリアかつ次のStageがあるときだけ「つぎへ」ボタンを出せるようにする
            return new GameResultSummary(isClear, _currentWaveIndex, currentHp, isClear && HasNextStage);
        }

        /// <summary>
        /// 等速復帰、カメラ乗っ取り、屋台爆走、カメラズーム完了を待機する演出
        /// </summary>
        private IEnumerator ShopPresentationSequenceRoutine()
        {
            Debug.Log("[Progression] ショップ登場演出シーケンスを開始するぜよ！");

            // プレイヤーの角度をフィールドに合わせる
            EventBus.Publish(new PlayerTiltEvent(25f));

            // バトル固定画角を退避
            if (Camera.main != null)
            {
                _savedBattleCameraPosition = Camera.main.transform.position;
                _savedBattleCameraRotation = Camera.main.transform.rotation;
            }

            // 1. スローモーションを解除
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            if (_shopVehicleController == null)
            {
                var vehicles = Object.FindObjectsByType<ShopVehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (vehicles.Length > 0) _shopVehicleController = vehicles[0];
            }
            if (_shopCinematicCameraController == null)
            {
                var cams = Object.FindObjectsByType<ShopCinematicCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cams.Length > 0) _shopCinematicCameraController = cams[0];
            }

            // シーン上のプレイヤーキャラクターを動的に検索
            var playerFacade = Object.FindFirstObjectByType<Gameplay.Player.PlayerFacade>();
            Transform playerTransform = playerFacade != null ? playerFacade.transform : null;

            if (playerTransform == null || _shopVehicleController == null || _cameraRigController == null || _shopCinematicCameraController == null)
            {
                Debug.LogError("[Progression] 演出に必要なコンポーネントやプレイヤーが見つからないぜよ。演出をスキップするぜよ。");
                HandleWaveClear();
                yield break;
            }

            // プレイヤーの操作入力を禁止
            var playerInput = playerTransform.GetComponentInChildren<MonoBehaviour>();
            if (playerInput != null) playerInput.enabled = false;

            // 2. 既存カメラの通常追従をOFFにして制御権を奪う
            _cameraRigController.SetCinematicModeActive(true);

            // 3. 演出用カメラと屋台スクリプトを同時に起動
            _isCameraWorkFinished = false;
            _shopCinematicCameraController.StartCinematic(playerTransform, _shopVehicleController);
            _shopVehicleController.LaunchShopSequence(playerTransform);

            // 4. 屋台のブレーキ振動 & カメラワークが完了するまで待機
            while (!_shopCinematicCameraController.IsCameraWorkFinished ||
                   _shopVehicleController.CurrentState != ShopVehicleController.VehicleState.Stationary)
            {
                // プレイヤーを屋台の方向に向かせるロジック
                Vector3 rawToShop = _shopVehicleController.transform.position - playerTransform.position;
                Vector3 fieldUp = FieldContext.Rotation * Vector3.up;
                Vector3 flatToShop = Vector3.ProjectOnPlane(rawToShop, fieldUp).normalized;

                if (flatToShop.sqrMagnitude > 0.001f)
                {
                    Quaternion targetLookRot = Quaternion.LookRotation(flatToShop, fieldUp);
                    playerTransform.rotation = Quaternion.Slerp(playerTransform.rotation, targetLookRot, Time.deltaTime * 5f);
                }

                yield return null;
            }

            Debug.Log("[Progression] 全ての演出とカメラワークが完了！ローグライクフェーズへ移行するぜよ。");

            if (playerInput != null) playerInput.enabled = true;

            // 5. 演出完了後にロード & ポーズ
            HandleWaveClear();
        }


        public override void BeginPreparedGame()
        {
            if (!_isFirstWavePrepared ||
                _preparationFailed ||
                _hasGameStarted ||
                _waveSequence == null ||
                _waveSequence.Count == 0)
            {
                return;
            }

            _hasGameStarted = true;
            StartBattleWave(_currentWaveIndex);
        }

        private IEnumerator UnloadRoguelikeAndAdvanceRoutine()
        {
            Debug.Log("[Progression] ローグライク強化終了。屋台退出ぜよ！");

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // 通常のカメラの画角に戻す
            if (_shopCinematicCameraController != null )
            {
                _shopCinematicCameraController.StopCinematicAndReturn(_savedBattleCameraPosition, _savedBattleCameraRotation);
            }

            // 屋台を右の畦道へ走らせる
            if (_shopVehicleController != null)
            {
                _shopVehicleController.DismissShopSequence();
            }

            // UIシーンのアンロード
            AsyncOperation op = SceneManager.UnloadSceneAsync(_roguelikeSceneName);
            while (!op.isDone) yield return null;

            // 屋台が画面外にはけるまで、ゲームの再開を少し待機する
            if (_shopVehicleController != null)
            {
                yield return new WaitUntil(() => _shopVehicleController.CurrentState == ShopVehicleController.VehicleState.Inactive);
            }

            // 演出が終わったら通常カメラを再始動
            if (_cameraRigController != null) _cameraRigController.SetCinematicModeActive(false);

            // 屋台が完全にはけたらプレイヤーの操作禁止を解除
            var playerFacade = Object.FindFirstObjectByType<Gameplay.Player.PlayerFacade>();
            Transform playerTransform = playerFacade != null ? playerFacade.transform : null;
            if (playerTransform != null)
            {
                var playerInput = playerTransform.GetComponentInChildren<MonoBehaviour>();
                if (playerInput != null) playerInput.enabled = true;
            }

            // ウェーブカウントをインクリメント
            _currentWaveIndex++;

            // 次のウェーブがあるかチェック
            if (_currentWaveIndex < _waveSequence.Count)
            {
                // マウスカーソルをロック
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 次のウェーブを開始
                StartBattleWave(_currentWaveIndex);
            }
            SoundManager.instance?.SoundVolume(1.0f);
        }


        private void HandleGameResult(bool isClear)
        {
            _currentState = GameProgressionState.Result;

            // バトル側のポーズ処理、マウスカーソル解放
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // 現状の防衛ラインの残りHPをシーンから取得
            var gauge = Object.FindFirstObjectByType<Core.DefenceLine.DefenseLineGauge>();
            float currentHp = gauge != null ? gauge.CurrentHP : 0f;

            // リザルト画面に渡す全データをパッキング
            // リザルト画面に渡す全データをパッキング
            // クリアかつ次のStageがあるときだけ「つぎへ」ボタンを出せるようにする
            ResultSummary = new GameResultSummary(isClear, _currentWaveIndex, currentHp, isClear && HasNextStage);
            StartCoroutine(LoadSceneAdditiveRoutine(_resultSceneName));
        }


        /// <summary>
        /// リザルト画面の「つぎへ」ボタンが押された時に呼ばれるメソッド
        /// 8/16 Asano: Stage2移行のために新規追加
        /// </summary>
        public override void RequestNextStage()
        {
            if (_isAdvancingStage)
            {
                return;
            }

            if (!HasNextStage)
            {
                Debug.LogWarning("[Progression] 次のStageが存在しないため、Stage移行をスキップします。");
                return;
            }

            if (_currentState != GameProgressionState.Result)
            {
                Debug.LogWarning("[Progression] 現在の状態がResultではないため、Stage移行をスキップします。");
                return;
            }

            _isAdvancingStage = true;

            StartCoroutine(AdvanceToNextStageRoutine());
        }


        /// <summary>
        /// リザルトを閉じてStageシーンを入れ換え、次のStageの1Wave目を開始するコルーチン
        /// 8/16 Asano: Stage2移行のために新規追加
        /// </summary>
        /// <returns></returns>
        private IEnumerator AdvanceToNextStageRoutine()
        {
            StageDataSO currentStage = _currentStageData;
            StageDataSO nextStage = currentStage.NextStage;

            _currentState = GameProgressionState.Setup;

            // リザルト表示でポーズしているので、時間を戻す
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // Stage1の起動時と同じローディング画面を、その場に作る
            GameObject loadingHost = new GameObject("StageTransitionLoading");
            loadingHost.transform.SetParent(transform, false);

            LoadingView loadingView = loadingHost.AddComponent<LoadingView>();
            loadingView.Initialize();

            // Initialize直後は表示状態なので、1フレームも出さないよう即座に隠す
            loadingView.SetLoadingVisible(false);

            // 暗転してローディング画面へ切り替える
            yield return loadingView.PlayEnterRoutine();

            // ローディング画面を最低限表示するために開始時間控えておく
            float loadingStartedAt = Time.realtimeSinceStartup;


            // ----- ローディング画面の裏で行う処理 -----

            // リザルトシーンを閉じる
            Scene resultScene = SceneManager.GetSceneByName(_resultSceneName);
            if (resultScene.IsValid() && resultScene.isLoaded)
            {
                AsyncOperation unloadResult = SceneManager.UnloadSceneAsync(resultScene);
                while (unloadResult != null && !unloadResult.isDone)
                {
                    yield return null;
                }
            }

            // Stageシーンを入れ替える
            StageTransitionController transition = Object.FindFirstObjectByType<StageTransitionController>();

            if (transition == null)
            {
                Debug.LogError("[Progression] StageTransitionControllerが見つかりません。Stage移行を中止します。");
                Destroy(loadingHost);
                _isAdvancingStage = false;
                yield break;
            }

            yield return transition.TransitionRoutine(currentStage, nextStage);

            // 新しいStageシーン側にある参照を取り直す
            RefreshStageSceneReferences();

            // 次のStage用にWave順を作り直す
            _stageIndex++;
            int seed = CreateStageSeed(_stageIndex);

            if (!StageWaveSequenceBuilder.TryBuild(nextStage, seed, out List<WaveDataSO> waveSequence, out string errorMessage))
            {
                Debug.LogError($"[Progression] 次のStageのWave順の生成に失敗しました。\n{errorMessage}", nextStage);
                Destroy(loadingHost);
                _isAdvancingStage = false;
                yield break;
            }

            _waveSequence = waveSequence;
            _currentWaveIndex = 0;
            _currentStageData = nextStage;
            ResultSummary = null;

            string nextStageName = HasNextStage ? _currentStageData.NextStage.StageName : "なし";
            Debug.Log($"[Progression] Stage移行完了: {_currentStageData.StageName} / Seed : {seed} / Wave数 : {_waveSequence.Count} / 次のStage：{nextStageName}");

            // 戦闘へ戻すのでカーソルをロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            SoundManager.instance?.PlayBGM("InGame");
            SoundManager.instance?.SoundVolume(1.0f);

            // シーンの入れ替えが早く終わっても、ローディング画面を一定時間は見せる
            float remainingDuration = _minimumStageLoadingDuration - (Time.realtimeSinceStartup - loadingStartedAt);
            if (remainingDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingDuration);
            }

            // 「START！」が出ている間は時間を止める
            Time.timeScale = 0f;

            yield return loadingView.PlayGameStartRoutine();

            Destroy(loadingHost);

            _isAdvancingStage = false;

            // StartBattleWaveRoutineの中で timeScale が 1 に戻る
            StartBattleWave(_currentWaveIndex);
        }


        protected override void HandleWaveClear()
        {
            
        }

#if UNITY_EDITOR
        /// <summary>
        /// デバッグ用。実行中のWaveを中断して、指定したWaveを演出なしで開始しまっす！
        /// </summary>
        /// <param name="waveIndex">開始したいWaveの番号(0始まり)</param>
        public override void DebugStartWaveAt(int waveIndex)
        {
            if (_waveSequence == null || _waveSequence.Count == 0)
            {
                Debug.LogWarning("[Progression] Wave列がまだ作られていないため、Waveを切り替えられません。");
                return;
            }

            if (_currentState != GameProgressionState.Battle)
            {
                Debug.LogWarning($"[Progression] バトル中ではないため、Waveの切り替えを無視しました。(現在の状態: {_currentState})");
                return;
            }

            if (waveIndex < 0 || waveIndex >= _waveSequence.Count)
            {
                Debug.LogWarning($"[Progression] Wave {waveIndex + 1} は存在しません。(全{_waveSequence.Count}Wave)");
                return;
            }

            // 実行中のWaveと、その完了待ちコルーチンを止める
            StopAllCoroutines();
            _waveRunner?.AbortAndReset();

            // 生き残っている敵を片付ける
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy.gameObject);
                }
            }

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            _currentWaveIndex = waveIndex;

            Debug.Log($"[Progression] [DEBUG] 敵{enemies.Length}体を片付けて、Wave {_currentWaveIndex + 1}/{_waveSequence.Count} を開始します。");

            StartBattleWave(_currentWaveIndex);
        }


        /// <summary>
        /// デバッグ用。演出を飛ばして次のWaveへ進みます
        /// </summary>
        public override void DebugSkipToNextWave()
        {
            DebugStartWaveAt(_currentWaveIndex + 1);
        }


        /// <summary>
        /// デバッグ用。演出を飛ばして最終Wave(Boss)へ飛びます
        /// </summary>
        public override void DebugJumpToFinalWave()
        {
            DebugStartWaveAt(_waveSequence.Count - 1);
        }
#endif


        /// <summary>
        /// 加算ロード完了後、StageDataSOからWave順を生成して開始します。
        /// </summary>
        public override IEnumerator PrepareFirstWaveRoutine()
        {
            if (_isFirstWavePrepared || _preparationFailed)
            {
                yield break;
            }

            if (_isPreparingFirstWave)
            {
                yield return new WaitUntil(() => _isFirstWavePrepared || _preparationFailed);
                yield break;
            }

            _isPreparingFirstWave = true;

            // 初期化
            _currentWaveIndex = 0;
            _currentState = GameProgressionState.Setup;

            // 参照取得
            StageSceneContext stageContext = null;
            PlayerFacade player = null;

            // 参照が揃うまで待機
            while (_enemySpawner == null || stageContext == null || player == null || _gameUIController == null)
            {
                if (_enemySpawner == null) _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
                if (stageContext == null) stageContext = Object.FindFirstObjectByType<StageSceneContext>();
                if (player == null) player = Object.FindFirstObjectByType<PlayerFacade>();
                if (_gameUIController == null) _gameUIController = Object.FindFirstObjectByType<GameUIController>();
                

                yield return null;
            }

            // WaveRunnerの参照がまだなら取得
            if (_waveRunner == null) _waveRunner = GetComponent<WaveRunner>();

            if (_waveRunner == null)
            {
                Debug.LogError("[Progression] WaveRunnerがGameProgressionManagerと同じGameObjectにありません。");
                _preparationFailed = true;
                _isPreparingFirstWave = false;
                yield break;
            }


            if (stageContext.ManualTestMode)
            {
                Debug.Log("[Progression] 手動テストモードのため、自動Wave進行をスキップします");
                _isFirstWavePrepared = true;
                _isPreparingFirstWave = false;
                yield break;
            }

            // Bootシーンはこの後アンロードされるので、Stage進行に必要な情報を控えておく
            _currentStageData = stageContext.StageData;
            _baseSeed = stageContext.CreateSeed();
            _stageIndex = 0;

            // StageDataSOからWave順を生成
            int seed = CreateStageSeed(_stageIndex);

            // Wave順の生成に失敗した場合はエラーをログに出して終了
            if (!StageWaveSequenceBuilder.TryBuild(stageContext.StageData, seed, out List<WaveDataSO> waveSequence, out string errorMessage))
            {
                Debug.LogError($"[Progression] Wave順の生成に失敗しました。\n{errorMessage}", stageContext);
                _preparationFailed = true;
                _isPreparingFirstWave = false;
                yield break;
            }

            // Wave順の生成に成功した場合は、生成されたWave順を保持
            _waveSequence = waveSequence;

            string nextStageName = HasNextStage ? _currentStageData.NextStage.StageName : "なし";
            Debug.Log($"[Progression] Stage開始：{_currentStageData.StageName} / Seed：{seed} / Wave数：{_waveSequence.Count} / 次のStage：{nextStageName}");

            _isFirstWavePrepared = true;
            _isPreparingFirstWave = false;
        }


        /// <summary>
        /// 指定した番号のStage用のSeedを生成します
        /// </summary>
        /// <param name="stageIndex">何番目のStageか</param>
        /// <returns>Wave抽選に使うSeed</returns>
        private int CreateStageSeed(int stageIndex)
        {
            return unchecked(_baseSeed + (stageIndex * StageSeedStride));
        }

    }
}
