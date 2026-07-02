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
 * 
 * 
 * 7/02: ゲームループ (バトル -> ローグライク) 対応の為、単一ウェーブ管理型へリファクタリング。
 *       ウェーブ進行管理を GameProgressionManager へ分離。 - Iwai a.k.a. ZEUS
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Presentation.UI;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 命令された単一ウェーブの敵を管理・スポーンするスポナー
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

        // 自動スポーンの間隔
        [Min(0.1f)] private float _spawnInterval = 5.0f;

        // 同時に存在できる敵の最大数,0以下なら制限なし
        private int _maxAliveEnemies = 3;

        // 現在のウェーブの敵情報
        private List<EnemyDefinition> _currentSpawnEnemies = new List<EnemyDefinition>();
        private float _currentHpRate = 1.0f;
        private float _currentBarrierRate = 1.0f;

        private Coroutine _autoSpawnCoroutine;


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

            StopSpawnRoutine();
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
        /// マネージャーからウェーブ情報を受け取り、このウェーブの生成フローを最初から回す。
        /// </summary>
        /// <param name="waveData"></param>
        /// <param name="undergroundOffset"></param>
        public void InjectAndStartWave(EnemySpawnerDefinition.WaveData waveData, float undergroundOffset)
        {
            StopSpawnRoutine();

            // 進行マネージャーから渡されたウェーブ固有のパラメータを同期
            _undergroundOffset = undergroundOffset;
            _maxAliveEnemies = waveData.MaxAliveEnemies;
            _minDistanceFromOtherEnemies = waveData.MinDistanceFromOtherEnemies;
            _spawnInterval = waveData.SpawnInterval;

            // 敵リストのコピーとステータス倍率の適用
            _currentSpawnEnemies = waveData.SpawnEnemies;
            _currentHpRate = waveData.HPRate;
            _currentBarrierRate = waveData.BarrierRate;

            // スポーンの開始
            _autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
        }

        private void StopSpawnRoutine()
        {
            if (_autoSpawnCoroutine != null)
            {
                StopCoroutine(_autoSpawnCoroutine);
                _autoSpawnCoroutine = null;
            }
        }

        /// <summary>
        /// 敵の生成
        /// </summary>
        private void SpawnEnemy()
        {
            if (_currentSpawnEnemies.Count <= 0 || _currentSpawnEnemies.Count == 0) return;

            // 敵情報を取得しデータから削除
            EnemyDefinition definition = _currentSpawnEnemies[0];
            _currentSpawnEnemies.RemoveAt(0);

            // 目標位置計算
            if (!TryGetSpawnTargetPosition(out Vector3 targetPos))
            {
                Debug.LogWarning("[EnemySpawner] 敵のスポーンに失敗しました。既存の敵と十分な距離を取れる位置が見つかりませんでした。");
                return;
            }

            // 空オブジェクトを生成し、子に敵のボディとバリアを生成する
            GameObject enemyGo = new GameObject($"Enemy_{definition.EnemyId}");
            enemyGo.transform.SetPositionAndRotation(targetPos, Quaternion.identity);

            GameObject body = Instantiate(definition.EnemyBody, enemyGo.transform);
            GameObject barrier = null;

            if (definition.HasBarrier && definition.BarrierBody != null)
            {
                barrier = Instantiate(definition.BarrierBody, enemyGo.transform);
            }


            // コンポーネントをアタッチ
            EnemyController controller = enemyGo.AddComponent<EnemyController>();
            enemyGo.AddComponent<EnemyRising>();
            enemyGo.AddComponent<EnemyWorldStatusView>();

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
            if (definition.HasBarrier && barrier != null)
            {
                controller.BarrierInitialize(definition, spawnSummary, barrier);
            }
        }

        private IEnumerator AutoSpawnRoutine()
        {
            // インジェクト直後は少しだけ待機
            yield return new WaitForSecondsRealtime(0.5f);

            while (_currentSpawnEnemies.Count > 0)
            {
                // 同時に存在出来るエネミー上限に達していない場合のみスポーンする
                if (!IsAliveEnemyLimitReached())
                {
                    SpawnEnemy();
                }

                // ローグライク中はループ自体がポーズ
                yield return new WaitForSeconds(_spawnInterval);
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

            foreach (var enemy in enemies)
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

        private void OnDrawGizmosSelected()
        {
            if (_spawnBasePoint == null) return;

            Vector3 size = new Vector3(_rangeSize.x, 0.1f, _rangeSize.y);

            // 1. 生成目標位置 (地上・水色) の可視化
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_spawnBasePoint.position, size);

            // 2. スポーン開始位置（地下・マゼンタ）の可視化
            Gizmos.color = Color.magenta;
            Vector3 undergroundPosition = _spawnBasePoint.position + Vector3.down * _undergroundOffset;
            Gizmos.DrawWireCube(undergroundPosition, size);

            // 地上と地下を繋ぐガイドラインを描画
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawLine(_spawnBasePoint.position, undergroundPosition);
        }
    }
}
