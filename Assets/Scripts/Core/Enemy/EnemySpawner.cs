/**
 * 作成：寺田晴
 *
 * 内容：敵を生成する(現在はデバッグキー込み)
 *
 * 更新履歴：
 * 5/30: 敵生成時、重なってスポーンしないように修正しました - 浅野
 *       自動で敵がスポーンするようにしました。
 *       
 *       
 * 6/24: データをSO化した！
 *       生成方法を空のゲームオブジェクトを出してコンポーネントつけて
 *       子オブジェクトに当たり判定用オブジェクトを生成する形に変更した！
 */
using System.Collections;
using Unity.VisualScripting;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using System.Collections.Generic;
using static Unity.VisualScripting.AnnotationUtility;
using Game.Presentation.UI;


namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵のスポーンを行う
    /// 生成と同時に上昇処理も行う
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("DebugInput設定")]
        [SerializeField] private InputAction _spawnAction;

        [Tooltip("生成する範囲(2D:横幅*奥行)")]
        [SerializeField] private Vector2 _rangeSize;

        [Header("出現位置の設定")]
        [Tooltip("最終目標地点")]
        [SerializeField] private Transform _spawnBasePoint;
        // どのくらい下から出現させるか
        float _undergroundOffset = 10.0f;

        
        // 既存の敵と最低限空ける距離
        [Min(0f)] private float _minDistanceFromOtherEnemies = 3.0f;

        // スポーン位置を探す最大試行回数"
        [Min(1)] private int _maxSpawnPositionAttempts = 20;


        // 一定時間ごとに自動でスポーンするか
        private bool _enableAutoSpawn = true;

        // 自動スポーン開始までの待機時間
        [Min(0f)] private float _initialSpawnDelay = 2.0f;

        // 自動スポーンの間隔
        [Min(0.1f)] private float _spawnInterval = 5.0f;

        // 1回の自動スポーンで出す敵の数
        [Min(1)] private int _enemiesPerSpawn = 1;

        // 同時に存在できる敵の最大数,0以下なら制限なし
        private int _maxAliveEnemies = 3;


        [Tooltip("スポナー情報")]
        [SerializeField]private EnemySpawnerDefinition _enemySpawnerDefinition = null;

        // ウェーブごとの敵のステータス補正
        private List<EnemyDefinition> _currentSpawnEnemies;
        private float _currentHpRate = 1.0f;
        private float _currentBarrierRate = 1.0f;

        private bool _isEndSpawn = false;

        private Coroutine _autoSpawnCoroutine;

        int _currentWaveCount = 0;


        private void Start()
        {
            _currentWaveCount = 0;
            if(_enemySpawnerDefinition.WaveDatas.Count <= 0)
            {
                Debug.Log("ウェーブが設定されていません！");
                return;
            }

            ApplySpawnDefinition();

            if (_enableAutoSpawn)
            {
                _autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnEnable()
        {
            // デバッグインプットを行うための登録
            if (_spawnAction != null)
            {
                _spawnAction.performed += OnSpawnPerformed;
                _spawnAction.Enable();
            }

        }

        private void OnDisable()
        {
            // デバッグインプットの登録解除
            if (_spawnAction != null)
            {
                _spawnAction.performed -= OnSpawnPerformed;
                _spawnAction.Disable();
            }

            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
                _autoSpawnCoroutine = null;
            }
        }

        /// <summary>
        /// デバッグインプットにより行う関数
        /// </summary>
        /// <param name="context"></param>
        private void OnSpawnPerformed(InputAction.CallbackContext context)
        {
            SpawnEnemy();
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
        private void SpawnEnemy()
        {
            if (_currentSpawnEnemies.Count <= 0 || _isEndSpawn) return;

            // 敵情報を取得しデータから削除
            EnemyDefinition definition = _currentSpawnEnemies[0];
            _currentSpawnEnemies.RemoveAt(0);

            // 目標位置計算
            if (!TryGetSpawnTargetPosition(out Vector3 targetPos))
            {
                Debug.LogWarning("[EnemySpawner] 敵のスポーンに失敗しました。既存の敵と十分な距離を取れる位置が見つかりませんでした。");
                return;
            }

            // Defから敵生成
            GameObject enemyGo = new GameObject("EmptyObject");
            enemyGo.transform.SetPositionAndRotation(targetPos, new Quaternion(0.0f, 0.0f, 0.0f, 0.0f));

            GameObject body = Instantiate(definition.EnemyBody, enemyGo.transform);
            GameObject barrier = null; ;

            if (definition.HasBarrier)
            {
                barrier = Instantiate(definition.BarrierBody, enemyGo.transform);
            }


            // コントローラーを追加
            enemyGo.AddComponent<EnemyController>();
            // 苦肉の策でRisingもモノビヘイビア
            enemyGo.AddComponent<EnemyRising>();

            enemyGo.AddComponent<EnemyWorldStatusView>();

            // 敵の初期化
            if (!enemyGo.TryGetComponent(out EnemyController controller))
            {
                Debug.LogWarning("[EnemySpawner] EnemyController が付与されていないため生成を中止します。", enemyGo);
                Destroy(enemyGo);
                return;
            }
            if (!body.TryGetComponent(out EnemyBodyController bodyController))
            {
                Debug.LogWarning("[EnemySpawner] EnemyHitReceiver が付与されていないため生成を中止します。", body);
                // Bodyが正しく生成されなかった場合親ごと削除
                Destroy(enemyGo);
                return;
            }
            // 生成した敵を初期化
            EnemyController.SpawnSummary spawnSummary = new EnemyController.SpawnSummary(targetPos, _undergroundOffset, _currentHpRate, _currentBarrierRate);
            string enemyId = controller.Initialize(definition, spawnSummary);
            // ボディを初期化
            bodyController.Initialize(enemyId);

            // バリアが存在する場合初期化
            if (definition.HasBarrier)
            {
                controller.BarrierInitialize(definition, spawnSummary, barrier);
            }


            if (_currentSpawnEnemies.Count <= 0)
            {
                if (!AddWave())
                {
                    _isEndSpawn = true;
                }

            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnBasePoint == null) return;
            // 生成位置と開始位置の可視化
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(_rangeSize.x, 0.1f, _rangeSize.y);
            Gizmos.DrawWireCube(_spawnBasePoint.position, size);

            Gizmos.color = Color.magenta;
            float gizmoUndergroundOffset = _enemySpawnerDefinition != null ? _enemySpawnerDefinition.UndergroundOffset : 0.0f;
            Gizmos.DrawWireCube(_spawnBasePoint.position + Vector3.down * gizmoUndergroundOffset, size);
        }


        /// <summary>
        /// スポーンターゲットの位置を試行的に取得する
        /// </summary>
        /// <param name="targetPos">ターゲット位置</param>
        /// <returns>見つかったらtrueを返す</returns>
        private bool TryGetSpawnTargetPosition(out Vector3 targetPos)
        {
            for (int i = 0; i < _maxSpawnPositionAttempts; i++)
            {
                targetPos = CreateRandomTargetPosition();

                if (IsFarEnoughFromExistingEnemies(targetPos))
                {
                    return true;
                }
            }

            targetPos = default;
            return false;
        }


        /// <summary>
        /// ランダムなターゲット位置を生成する
        /// </summary>
        /// <returns>ランダムなターゲット位置</returns>
        private Vector3 CreateRandomTargetPosition()
        {
            Vector3 spawnPos = _spawnBasePoint != null ? _spawnBasePoint.position : transform.position;

            float randomX = Random.Range(spawnPos.x - _rangeSize.x / 2f, spawnPos.x + _rangeSize.x / 2f);
            float randomZ = Random.Range(spawnPos.z - _rangeSize.y / 2f, spawnPos.z + _rangeSize.y / 2f);

            return new Vector3(randomX, spawnPos.y, randomZ);
        }


        /// <summary>
        /// 既存の敵と十分な距離があるか
        /// </summary>
        /// <param name="position">ターゲットのポジション</param>
        /// <returns>結果</returns>
        private bool IsFarEnoughFromExistingEnemies(Vector3 position)
        {
            float minDistanceSqr = _minDistanceFromOtherEnemies * _minDistanceFromOtherEnemies;
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                Vector3 enemyPos = enemy.transform.position;
                enemyPos.y = position.y; // Y軸は無視して距離を計算

                if ((enemyPos - position).sqrMagnitude < minDistanceSqr)
                {
                    return false; // 既存の敵と近すぎる
                }
            }

            return true; // 十分な距離がある
        }


        /// <summary>
        /// 自動スポーンのルーチン
        /// </summary>
        /// <returns>時間</returns>
        private IEnumerator AutoSpawnRoutine()
        {
            if (_initialSpawnDelay > 0f)
            {
                yield return new WaitForSeconds(_initialSpawnDelay);
            }

            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.1f, _spawnInterval));

            while (isActiveAndEnabled)
            {
                SpawnAutoWave();
                yield return wait;
            }
        }


        /// <summary>
        /// 自動スポーンの1回分の処理
        /// </summary>
        private void SpawnAutoWave()
        {
            int spawnCount = Mathf.Max(1, _enemiesPerSpawn);

            for (int i = 0; i < spawnCount; i++)
            {
                if (IsAliveEnemyLimitReached())
                {
                    return; // 同時に存在できる敵の最大数に達している場合はスポーンを中止
                }

                SpawnEnemy();
            }
        }

        /// <summary>
        /// 同時に存在できる敵の最大数に達しているか
        /// </summary>
        /// <returns>達していたらtrue</returns>
        private bool IsAliveEnemyLimitReached()
        {
            if (_maxAliveEnemies <= 0) return false; // 制限なし

            return CountAliveEnemies() >= _maxAliveEnemies;
        }


        /// <summary>
        /// 現在存在する生存中の敵の数を数える
        /// </summary>
        /// <returns>生存する敵の数</returns>
        private int CountAliveEnemies()
        {
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            int count = 0;

            foreach (EnemyController enemy in enemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Waveを進める
        /// </summary>
        /// <returns></returns>
        private bool AddWave()
        {
            _currentWaveCount++;
            // Waveが存在しない場合失敗を返す
            if(_enemySpawnerDefinition.WaveDatas.Count <= _currentWaveCount) return false;

            
            ApplyWaveData(_enemySpawnerDefinition.WaveDatas[_currentWaveCount]);
            return true;
        }

        void ApplyWaveData(EnemySpawnerDefinition.WaveData waveData)
        {
            // ウェーブごとのスポーン設定
            _maxAliveEnemies = waveData.MaxAliveEnemies;
            _minDistanceFromOtherEnemies = waveData.MinDistanceFromOtherEnemies;
            _spawnInterval = waveData.SpawnInterval;

            // ウェーブごとの敵情報設定
            _currentSpawnEnemies = waveData.SpawnEnemies;
            _currentHpRate = waveData.HPRate;
            _currentBarrierRate = waveData.BarrierRate;
        }

        private void ApplySpawnDefinition()
        {
            // ウェーブに関係なく一貫した情報を保持
            _undergroundOffset = _enemySpawnerDefinition.UndergroundOffset;
            _maxAliveEnemies = _enemySpawnerDefinition.MaxSpawnPositionAttempts;
            _initialSpawnDelay = _enemySpawnerDefinition.InitialSpawnDelay;
            _enemiesPerSpawn = _enemySpawnerDefinition.EnemyPerSpawn;
            _enableAutoSpawn = _enemySpawnerDefinition.EnableAutoSpawn;
           
            ApplyWaveData(_enemySpawnerDefinition.WaveDatas[_currentWaveCount]);
        }
    }
}
