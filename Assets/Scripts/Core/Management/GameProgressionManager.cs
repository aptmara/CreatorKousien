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

        [Header("--- ウェーブ設定定義アセット ---")]
        [SerializeField] private EnemySpawnerDefinition _spawnerDefinition;

        [Header("--- 参照 ---")]
        [SerializeField] private EnemySpawner _enemySpawner;

        private GameProgressionState _currentState = GameProgressionState.Setup;
        private int _currentWaveIndex = 0;
        private int _totalEnemiesInCurrentWave = 0;
        private int _defeatedEnemiesInCurrentWave = 0;

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

            if (_enemySpawner == null)
            {
                _enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
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
        }

        private void OnDisable()
        {
            // エネミーの撃破イベントの購読解除
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        /// <summary>
        /// バトルウェーブの開始処理
        /// </summary>
        /// <param name="waveIndex">開始するウェーブの番号</param>
        private void StartBattleWave(int waveIndex)
        {
            _currentState = GameProgressionState.Battle;
            Time.timeScale = 1f;    // ポーズ解除

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
                    HandleGameClear();
                }
                else
                {
                    // ウェーブクリア -> ローグライクフェーズへ遷移
                    HandleWaveClear();
                }
            }
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
            StartCoroutine(LoadRoguelikeSceneRoutine());
        }

        private void HandleGameClear()
        {
            _currentState = GameProgressionState.Result;

            // バトル側のポーズ処理、マウスカーソル解放
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            Debug.Log("[Progression] ★★★ 全ウェーブ完全走破！ゲームクリアだぜよ！ ★★★");
        }

        /// <summary>
        /// ローグライクシーンの加算ロード
        /// </summary>
        /// <returns></returns>
        private IEnumerator LoadRoguelikeSceneRoutine()
        {
            // 重複ロード防止
            Scene existingScene = SceneManager.GetSceneByName(_roguelikeSceneName);
            if (existingScene.IsValid() && existingScene.isLoaded)
            {
                yield break;
            }

            // Additiveロード
            AsyncOperation op = SceneManager.LoadSceneAsync(_roguelikeSceneName, LoadSceneMode.Additive);
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
            else
            {
                // フォールバック
                _currentState = GameProgressionState.Result;
                Debug.Log("[Progression] すべてのウェーブをクリアしたぜよ！\nゲームクリア状態へ遷移するぜよ！");
            }
        }
    }
}
