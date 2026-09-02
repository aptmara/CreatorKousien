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
    public abstract class GameProgressionManagerBase : MonoBehaviour
    {
        public static GameProgressionManagerBase Instance { get; private set; }

        [Header("--- シーン名設定 ---")]
        [SerializeField] protected string _roguelikeSceneName = "Roguelike";
        [SerializeField] protected string _resultSceneName = "Result";


        [Header("--- Stage移行 ---")]
        [Tooltip("Stage移行時にローディング画面を最低限見せる時間(秒)")]
        [SerializeField] private float _minimumStageLoadingDuration = 3f;


        [Header("--- Waveシステム ---")]
        [SerializeField] protected WaveRunner _waveRunner;

        [Header("--- 参照 ---")]
        [SerializeField] protected EnemySpawner _enemySpawner;
        [SerializeField] protected GameUIController _gameUIController;

        [Header("--- ショップ演出関連の参照 ---")]
        [SerializeField] protected CameraRigController _cameraRigController;
        [SerializeField] protected ShopVehicleController _shopVehicleController;
        [SerializeField] protected ShopCinematicCameraController _shopCinematicCameraController;

        [Header("--- クリア演出・猶予設定 ---")]

        [Tooltip("クリアした瞬間の時間の進み方 (例 0.1f: 10%のスローモーション)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _slowMotionTimeScale = 0.2f;


        [Header("--- クリア演出関連の参照 ---")]
        [SerializeField] protected GameClearCinematicController _gameClearCinematicController;


        protected GameProgressionState _currentState = GameProgressionState.Setup;
        protected bool _isCameraWorkFinished = false;
        protected List<WaveDataSO> _waveSequence = new();
        protected bool _isFirstWavePrepared;
        protected bool _hasGameStarted;
        protected bool _preparationFailed;
        protected bool _isPreparingFirstWave;

        // --- Stage進行 ---
        protected StageDataSO _currentStageData;          // 現在プレイ中のStage
        protected int _baseSeed;                          // 現在のStageで使用する乱数シード
        protected int _stageIndex;                        // 現在のStage番号
        protected bool _isAdvancingStage;                 // Stage移行中かどうかのフラグ


        // カメラの固定画角を保持しておく変数
        private Vector3 _savedBattleCameraPosition;
        private Quaternion _savedBattleCameraRotation;

        public GameResultSummary ResultSummary { get; protected set; }
        public GameProgressionState CurrentState => _currentState;
        public int CurrentWaveIndex => _currentWaveIndex + 1;

        public int _currentWaveIndex;

        /// <summary>
        /// 現在プレイ中のStageDataSO
        /// </summary>
        public StageDataSO CurrentStageData => _currentStageData;

        public bool HasNextStage => _currentStageData != null && _currentStageData.HasNextStage;
        public bool IsFirstWavePrepared => _isFirstWavePrepared;
        public bool PreparationFailed => _preparationFailed;


        // 共通の初期化処理
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 自動参照取得のセーフティ
            // Enemy Spawner
            if (_enemySpawner == null)
            {
                _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
            }

            if (_gameUIController == null)
            {
                _gameUIController = Object.FindFirstObjectByType<GameUIController>();
            }

            // Camera Rig Controller
            if (_cameraRigController == null)
            {
                _cameraRigController = Object.FindFirstObjectByType<CameraRigController>();
            }

            // Shop Vehicle Controller
            if (_shopVehicleController == null)
            {
                var vehicles = Object.FindObjectsByType<ShopVehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (vehicles.Length > 0) _shopVehicleController = vehicles[0];
            }

            // Shop Cinematic Camera Controller
            if (_shopCinematicCameraController == null)
            {
                var cams = Object.FindObjectsByType<ShopCinematicCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (cams.Length > 0) _shopCinematicCameraController = cams[0];
            }

            // Game Clear Cinematic Controller
            if (_gameClearCinematicController == null)
            {
                _gameClearCinematicController = Object.FindFirstObjectByType<GameClearCinematicController>();
            }

            if (_waveRunner == null)
            {
                _waveRunner = GetComponent<WaveRunner>();
            }
        }

        private void Start()
        {
            Scene loadingScene = SceneManager.GetSceneByName("Loading");
            if (!loadingScene.IsValid() || !loadingScene.isLoaded)
            {
                StartCoroutine(PrepareAndBeginForStandaloneRoutine());
            }
        }

        private void OnEnable()
        {
            // エネミーの撃破イベントを購読
            EventBus.Subscribe<DefLineBreakReactionEvent>(OnDefenseLineBroken);

            // 演出カメラからの完了通知イベントを購読
            if (_shopCinematicCameraController != null)
            {
                _shopCinematicCameraController.OnCompleteCameraWork += HandleCameraWorkComplete;
            }
        }

        private void OnDisable()
        {
            // エネミーの撃破イベントの購読解除
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnDefenseLineBroken);

            // 演出カメラからの完了通知イベントを購読解除
            if (_shopCinematicCameraController != null)
            {
                _shopCinematicCameraController.OnCompleteCameraWork -= HandleCameraWorkComplete;
            }
        }

        private void Update()
        {
            // ESCでマウスカーソル解放
            if (Keyboard.current != null && Keyboard.current[Key.Escape].wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // バトル中にマウスクリックで再ロック
            if (_currentState == GameProgressionState.Battle)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }


        // ------- 共通処理 -------
        private void HandleCameraWorkComplete()
        {
            // カメラの回り込み完了合図を受け取ったらフラグをONに
            _isCameraWorkFinished = true;
        }


        /// <summary>
        /// WaveRunnerを使って1Waveを実行し、完了後の演出へ進みます。
        /// </summary>
        protected IEnumerator StartBattleWaveRoutine(int waveIndex)
        {
            if (_waveSequence == null || waveIndex < 0 || waveIndex >= _waveSequence.Count)
            {
                Debug.LogError($"[Progression] Wave Index {waveIndex}のデータが存在しません。");
                yield break;
            }

            if (_waveRunner == null || _enemySpawner == null)
            {
                Debug.LogError("[Progression] WaveRunnerまたはEnemySpawnerが見つかりません。");
                yield break;
            }

            if (_gameUIController == null)
            {
                Debug.LogError("[Progression] GameUIConterollerが見つかりません。");
                yield break;
            }
            _currentState = GameProgressionState.Battle;

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            _gameUIController.UIVisible(0.5f);
            _gameUIController.SetWave((waveIndex + 1).ToString() + " / " + _waveSequence.Count);

            WaveDataSO waveData = _waveSequence[waveIndex];

            Debug.Log($"[Progression] ===== Wave {waveIndex + 1} 開始：{waveData.WaveName} =====");

            yield return StartCoroutine(_waveRunner.PlayWave(waveData, _enemySpawner));

            if (!_waveRunner.LastRunSucceeded)
            {
                _currentState = GameProgressionState.Setup;
                Debug.LogError($"[Progression] Wave「{waveData.WaveName}」の実行に失敗しました。", waveData);
                yield break;
            }

            bool isFinalWave = waveIndex + 1 >= _waveSequence.Count;
            if (!isFinalWave)
            {

            }

            // 越智 TODO 終了後のロジックは不要なので外に出す
            yield return StartCoroutine(AnimateWaveClearRoutine(isFinalWave, waveData.CompleteDelay));
        }

   
        /// <summary>
        /// クリア演出コルーチン
        /// </summary>
        protected IEnumerator AnimateWaveClearRoutine(bool isFinalWave, float completeDelay)
        {
            float validCompleteDelay = Mathf.Max(0f, completeDelay);

            // 一時的に状態を逃がす
            _currentState = GameProgressionState.Setup;
            Debug.Log($"[Progression] 最後の敵の撃破を検知！ 弾の着弾猶予として {validCompleteDelay} 秒間スローモーション演出を行うぜよ。");


            // --- 最終ウェーブクリア時の処理 ---
            if (isFinalWave)
            {
                // 最終ウェーブクリア時は、ゲームクリア演出を再生する
                SoundManager.instance?.StopBGM();
                if (_gameClearCinematicController != null)
                {
                    yield return StartCoroutine(_gameClearCinematicController.PlayRoutine());
                }
                else
                {
                    // ゲームクリア演出が設定されていない場合は、スローモーション猶予だけ行う
                    Time.timeScale = _slowMotionTimeScale;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;

                    yield return new WaitForSecondsRealtime(validCompleteDelay);

                    Time.timeScale = 1f;
                    Time.fixedDeltaTime = 0.02f;
                }

                // 最終ウェーブクリア後は、ゲームクリア状態へ遷移
                HandleGameResult(isClear: true);
                yield break;
            }



            // --- 通常のウェーブクリア時の処理 ---

            // 1. 画面を一瞬スローモーション、同時にUIを透明化
            Time.timeScale = _slowMotionTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            _gameUIController.UIInvisible(0.5f);
            // 2. コンボが切れるまでの時間を実時間で待機
            yield return new WaitForSecondsRealtime(validCompleteDelay);

            // 3. 猶予が終了したら、進行処理へ
            // 屋台演出へ繋ぐ
            yield return StartCoroutine(ShopPresentationSequenceRoutine());
            SoundManager.instance?.SoundVolume(0.3f);
        }


        /// <summary>
        /// 等速復帰、カメラ乗っ取り、屋台爆走、カメラズーム完了を待機する演出
        /// </summary>
        protected IEnumerator ShopPresentationSequenceRoutine()
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

            // 越智 TODO こっちもロジックは切り出す
            // 5. 演出完了後にロード & ポーズ
            HandleWaveClear();
        }

        /// <summary>
        /// ローグライクで強化カードが選択され、シーケンスが完了した時に呼ぶよ。
        /// ローグライクシーンからこれ呼んでね^^
        /// </summary>
        public abstract void CompleteRoguelikeSequence();

        protected IEnumerator UnloadRoguelikeAndAdvanceRoutine()
        {
            Debug.Log("[Progression] ローグライク強化終了。屋台退出ぜよ！");

            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;

            // 通常のカメラの画角に戻す
            if (_shopCinematicCameraController != null)
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

            // プレイヤーの角度をフィールドに合わせる
            EventBus.Publish(new PlayerTiltEvent(0.0f));

            SoundManager.instance?.SoundVolume(1.0f);
            RoguelikeToComeback();
        }

        protected IEnumerator PrepareAndBeginForStandaloneRoutine()
        {
            yield return PrepareFirstWaveRoutine();
            BeginPreparedGame();
        }


        /// <summary>
        /// ローグライクシーンの加算ロード
        /// </summary>
        /// <returns></returns>
        protected IEnumerator LoadSceneAdditiveRoutine(string sceneName)
        {
            // 重複ロード防止
            Scene existingScene = SceneManager.GetSceneByName(sceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                yield break;
            }

            // Additiveロード
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Debug.LogError($"[Progression] シーン '{sceneName}' のロードに失敗。Build Profiles を確認してちょ。");
                Time.timeScale = 1f;
                _currentState = GameProgressionState.Battle;
                yield break;
            }

            while (!op.isDone)
            {
                yield return null;
            }

            Debug.Log("[Progression] ローグライクシーンの加算ロード完了ｩｩｳ！");
        }

        /// <summary>
        /// ゲームオーバー演出などが終了した後に、外部からリザルト画面へ遷移させるためのメソッド
        /// </summary>
        public void GoToResult(bool isClear)
        {
            HandleGameResult(isClear);
        }

        private void HandleGameResult(bool isClear)
        {
            ResultSummary = CreateSummry(isClear);
            StartCoroutine(LoadSceneAdditiveRoutine(_resultSceneName));
        }


        protected void RefreshStageSceneReferences()
        {
            // 古い演出カメラの購読を解除しておく
            if (_shopCinematicCameraController != null)
            {
                _shopCinematicCameraController.OnCompleteCameraWork -= HandleCameraWorkComplete;
            }

            _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();

            var vehicles = Object.FindObjectsByType<ShopVehicleController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _shopVehicleController = vehicles.Length > 0 ? vehicles[0] : null;

            var cams = Object.FindObjectsByType<ShopCinematicCameraController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            _shopCinematicCameraController = cams.Length > 0 ? cams[0] : null;

            // 新しい演出カメラうを購読する
            if (_shopCinematicCameraController != null)
            {
                _shopCinematicCameraController.OnCompleteCameraWork += HandleCameraWorkComplete;
            }

            if (_enemySpawner == null)
            {
                Debug.LogError("[Progression] 新しいStageシーンにEnemySpawnerが見つかりません。");
            }
        }

        protected void RefreshUISceneReferences()
        {
            _gameUIController = Object.FindFirstObjectByType<GameUIController>();
            if (_gameUIController == null)
            {
                Debug.LogError("[Progression] 新しいUIシーンにGameUIControllerが見つかりません。");
            }
        }


#if UNITY_EDITOR

        /// <summary>
        /// デバッグ用。実行中のWaveを中断して、指定したWaveを演出なしで開始しまっす！
        /// </summary>
        /// <param name="waveIndex">開始したいWaveの番号(0始まり)</param>
        public abstract void DebugStartWaveAt(int waveIndex);

        /// <summary>
        /// デバッグ用。演出を飛ばして次のWaveへ進みます
        /// </summary>
        public abstract void DebugSkipToNextWave();

        /// <summary>
        /// デバッグ用。演出を飛ばして最終Wave(Boss)へ飛びます
        /// </summary>
        public abstract void DebugJumpToFinalWave();
#endif

        /// <summary>
        /// 加算ロード後に良い感じに初期化処理する関数
        /// </summary>
        public abstract IEnumerator PrepareFirstWaveRoutine();

        public abstract void BeginPreparedGame();

        protected abstract GameResultSummary CreateSummry(bool isClear);

        /// <summary>
        /// リザルト画面の「つぎへ」ボタンが押された時に呼ばれるメソッド
        /// 8/16 Asano: Stage2移行のために新規追加
        /// </summary>
        public abstract void RequestNextStage();

        /// <summary>
        /// ウェーブクリア時の処理
        /// </summary>
        protected abstract void HandleWaveClear();

        protected abstract void OnDefenseLineBroken(DefLineBreakReactionEvent ev);


        protected abstract void RoguelikeToComeback();
    }

}

