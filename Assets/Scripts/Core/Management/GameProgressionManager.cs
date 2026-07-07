// ================================================================================
// File         : GameProgressionManager.cs
// Author       : Iwai Shogo
//
// Description  : ゲーム全体の進行状態を管理するマネージャー
// Created      : 2026-07-02
// ================================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Game.Core.Events;
using Game.Core.Enemy;
using Game.Gameplay.Cameras;
using Game.Gameplay.Shop;

namespace Game.Core.Management
{
    /// <summary>
    /// ゲーム全体のループ進行を統括する司令塔マネージャー
    /// </summary>
    public sealed class GameProgressionManager : MonoBehaviour
    {
        public static GameProgressionManager Instance { get; private set; }

        [Header("--- シーン名設定 ---")]
        [SerializeField] private string _roguelikeSceneName = "Roguelike";
        [SerializeField] private string _resultSceneName = "Result";

        [Header("--- ウェーブ設定定義アセット ---")]
        [SerializeField] private EnemySpawnerDefinition _spawnerDefinition;

        [Header("--- 参照 ---")]
        [SerializeField] private EnemySpawner _enemySpawner;

        [Header("--- ショップ演出関連の参照 ---")]
        [SerializeField] private CameraRigController _cameraRigController;
        [SerializeField] private ShopVehicleController _shopVehicleController;
        [SerializeField] private ShopCinematicCameraController _shopCinematicCameraController;

        [Header("--- クリア演出・猶予設定 ---")]
        [Tooltip("最後の敵を倒してから、コンボや弾が当たり切るまでの猶予時間 (秒)")]
        [SerializeField] private float _clearDelayDuration = 2.0f;

        [Tooltip("クリアした瞬間の時間の進み方 (例 0.1f: 10%のスローモーション)")]
        [Range(0.01f, 1f)]
        [SerializeField] private float _slowMotionTimeScale = 0.2f;

        private GameProgressionState _currentState = GameProgressionState.Setup;
        private int _currentWaveIndex = 0;
        private int _totalEnemiesInCurrentWave = 0;
        private int _defeatedEnemiesInCurrentWave = 0;
        private bool _isCameraWorkFinished = false;

        public GameResultSummary ResultSummary { get; private set; }
        public GameProgressionState CurrentState => _currentState;
        public int CurrentWaveIndex => _currentWaveIndex + 1;

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

            // Camera Rig Controller
            if (_cameraRigController == null)
            {
                _cameraRigController = Object.FindFirstObjectByType<CameraRigController>();
            }

            // Shop Vehicle Controller
            if (_shopVehicleController == null)
            {
                _shopVehicleController = Object.FindFirstObjectByType<ShopVehicleController>();
            }

            // Shop Cinematic Camera Controller
            if (_shopCinematicCameraController == null)
            {

            }
        }

        private void Start()
        {
            StartCoroutine(WaitAndStartFirstWaveRoutine());
        }

        /// <summary>
        /// 加算ロードされた別シーンの初期化が完全に終わるのを待ってからウェーブを開始する
        /// </summary>
        private IEnumerator WaitAndStartFirstWaveRoutine()
        {
            _currentWaveIndex = 0;
            _currentState = GameProgressionState.Setup;

            Debug.Log("[Progression] フィールドシーンおよびEnemySpawnerのロード・接続を待機中...");

            // EnemySpawner が見つかるまで毎フレーム待機
            while (_enemySpawner == null)
            {
                _enemySpawner = UnityEngine.Object.FindFirstObjectByType<EnemySpawner>();
                yield return null;
            }

            Debug.Log("[Progression] EnemySpawner の動的接続に成功しました。バトルを開始します。");

            // スポーナーが見つかったら、最初のウェーブを開始
            StartBattleWave(_currentWaveIndex);
        }

        private void OnEnable()
        {
            // エネミーの撃破イベントを購読
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
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
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<DefLineBreakReactionEvent>(OnDefenseLineBroken);

            // 演出カメラからの完了通知イベントを購読解除
            if (_shopCinematicCameraController != null)
            {
                _shopCinematicCameraController.OnCompleteCameraWork -= HandleCameraWorkComplete;
            }
        }

        private void HandleCameraWorkComplete()
        {
            // カメラの回り込み完了合図を受け取ったらフラグをONに
            _isCameraWorkFinished = true;
        }

        /// <summary>
        /// バトルウェーブの開始処理
        /// </summary>
        /// <param name="waveIndex">開始するウェーブの番号</param>
        private void StartBattleWave(int waveIndex)
        {
            _currentState = GameProgressionState.Battle;
            Time.timeScale = 1f;    // ポーズ解除
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            if (_spawnerDefinition == null || waveIndex >= _spawnerDefinition.WaveDatas.Count)
            {
                Debug.LogError($"[Progression] ウェーブインデックス {waveIndex} のデータが存在しないぜよ。");
                return;
            }

            var waveData = _spawnerDefinition.WaveDatas[waveIndex];

            // カウンタの初期化
            _defeatedEnemiesInCurrentWave = 0;
            _totalEnemiesInCurrentWave = waveData.SpawnEnemies.Count;

            Debug.Log($"[Progression] ===== ウェーブ {waveIndex + 1} 開始。敵総数: {_totalEnemiesInCurrentWave} =====");

            // スポナーへ現在のウェーブデータを渡して実行
            if (_enemySpawner != null)
            {
                _enemySpawner.InjectAndStartWave(waveData, _spawnerDefinition.UndergroundOffset);
            }
        }

        /// <summary>
        /// エネミー撃破時のコールバック。全滅した瞬間にローグライクフェーズへ遷移。
        /// </summary>
        /// <param name="ev"></param>
        private void OnEnemyDefeated(EnemyDefeatedEvent ev)
        {
            if (_currentState != GameProgressionState.Battle) return;

            _defeatedEnemiesInCurrentWave++;
            Debug.Log($"[Progression] エネミー撃破検知: {_defeatedEnemiesInCurrentWave} / {_totalEnemiesInCurrentWave}");

            // 現在のウェーブの敵が全滅したかどうかチェック
            if (_defeatedEnemiesInCurrentWave >= _totalEnemiesInCurrentWave)
            {
                if (_currentWaveIndex + 1 >= _spawnerDefinition.WaveDatas.Count)
                {
                    // 最終ウェーブクリア -> ゲームクリア状態へ遷移
                    StartCoroutine(AnimateWaveClearRoutine(isFinalWave: true));
                }
                else
                {
                    // ウェーブクリア -> ローグライクフェーズへ遷移
                    StartCoroutine(AnimateWaveClearRoutine(isFinalWave: false));
                }
            }
        }

        private void OnDefenseLineBroken(DefLineBreakReactionEvent ev)
        {
            if (_currentState != GameProgressionState.Battle) return;
            Debug.Log("[Progression] 防衛ラインのバリア崩壊を検知。ゲームオーバー処理を開始するぜよ。");
            HandleGameResult(isClear: false);
        }

        /// <summary>
        /// ウェーブを全滅させたあとに弾が当たり切る猶予を作る最強の非同期演出ルーチン
        /// </summary>
        /// <param name="isFinalWave"></param>
        private IEnumerator AnimateWaveClearRoutine(bool isFinalWave)
        {
            // 一時的に状態を逃がす
            _currentState = GameProgressionState.Setup;

            Debug.Log($"[Progression] 最後の敵の撃破を検知！ 弾の着弾猶予として {_clearDelayDuration} 秒間スローモーション演出を行うぜよ。");

            // 1. 画面を一瞬スローモーション
            Time.timeScale = _slowMotionTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // 2. コンボが切れるまでの時間を実時間で待機
            yield return new WaitForSecondsRealtime(_clearDelayDuration);

            // 3. 猶予が終了したら、進行処理へ
            if (isFinalWave)
            {
                HandleGameResult(isClear: true);
            }
            else
            {
                // 屋台演出へ繋ぐ
                yield return StartCoroutine(ShopPresentationSequenceRoutine());
            }
        }

        /// <summary>
        /// 等速復帰、カメラ乗っ取り、屋台爆走、カメラズーム完了を待機する演出
        /// </summary>
        private IEnumerator ShopPresentationSequenceRoutine()
        {
            Debug.Log("[Progression] ショップ登場演出シーケンスを開始するぜよ！");

            // 1. スローモーションを解除
            Time.timeScale = 1f;
        }

        /// <summary>
        /// ウェーブクリア時の処理
        ///     (バトルポーズ -> ローグライク加算ロード)
        /// </summary>
        private void HandleWaveClear()
        {
            Debug.Log($"[Progression] よくやった。ウェーブ {_currentWaveIndex + 1} クリアしたぜよ！\nローグライクフェーズへ遷移するぜよ。");

            _currentState = GameProgressionState.Roguelike;

            // バトル側のポーズ処理
            Time.timeScale = 0f;

            // マウスカーソル表示
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // ローグライクシーンの加算ロード
            StartCoroutine(LoadSceneAdditiveRoutine(_roguelikeSceneName));
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
            ResultSummary = new GameResultSummary(isClear, _currentWaveIndex, currentHp);

            Debug.Log($"[Progression] 勝敗決定 - Clear: {isClear}, 最終ウェーブ: {_currentWaveIndex + 1}. リザルト画面をロードします。");
            StartCoroutine(LoadSceneAdditiveRoutine(_resultSceneName));
        }

        /// <summary>
        /// ローグライクシーンの加算ロード
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadSceneAdditiveRoutine(string sceneName)
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
        /// ローグライクで強化カードが選択され、シーケンスが完了した時に呼ぶよ。
        /// ローグライクシーンからこれ呼んでね^^
        /// </summary>
        public void CompleteRoguelikeSequence()
        {
            if (_currentState != GameProgressionState.Roguelike) return;

            StartCoroutine(UnloadRoguelikeAndAdvanceRoutine());
        }

        private IEnumerator UnloadRoguelikeAndAdvanceRoutine()
        {
            Debug.Log("[Progression] ローグライク強化。シーンをアンロードするぜよ。\nキミのおかげで次に進める気がする...！");

            AsyncOperation op = SceneManager.UnloadSceneAsync(_roguelikeSceneName);
            while (!op.isDone)
            {
                yield return null;
            }

            // ウェーブカウントをインクリメント
            _currentWaveIndex++;

            // 次のウェーブがあるかチェック
            if (_currentWaveIndex < _spawnerDefinition.WaveDatas.Count)
            {
                // マウスカーソルをロック
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // 次のウェーブを開始
                StartBattleWave(_currentWaveIndex);
            }
        }
    }
}
