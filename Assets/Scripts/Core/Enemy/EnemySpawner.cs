/**
 * 作成：寺田晴
 * 
 * 内容：敵を生成する(現在はデバッグキー込み)
 * 
 */
using UnityEngine;
using UnityEngine.InputSystem;


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

        [Header("生成する敵の設定")]
        [Tooltip("生成する敵のプレハブ")]
        [SerializeField] private GameObject _enemyPrefab;
        [Tooltip("生成する敵のデータ")]
        [SerializeField] private EnemyDefinition _definition;
        [Tooltip("生成する範囲(2D:横幅*奥行)")]
        [SerializeField] private Vector2 _rangeSize;

        [Header("出現位置の設定")]
        [Tooltip("最終目標地点")]
        [SerializeField] private Transform _spawnBasePoint;
        [Tooltip("どのくらい下から出現させるか")]
        [SerializeField] private float _undergroundOffset = 10.0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnEnable()
        {
            // デバッグインプットを行うための登録
            if (_spawnAction == null) return;
            _spawnAction.Enable();
            _spawnAction.performed += OnSpawnPerformed;
        }

        private void OnDisable()
        {
            // デバッグインプットの登録解除
            if (_spawnAction == null) return;
            _spawnAction.performed -= OnSpawnPerformed;
            _spawnAction.Disable();
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
            if (_enemyPrefab == null || _definition == null) return;

            // 目標位置計算
            Vector3 spawnPos = _spawnBasePoint != null ? _spawnBasePoint.position : transform.position;
            float randomX = Random.Range(spawnPos.x - _rangeSize.x / 2f, spawnPos.x + _rangeSize.x / 2f);
            float randomZ = Random.Range(spawnPos.z - _rangeSize.y / 2f, spawnPos.z + _rangeSize.y / 2f);
            Vector3 targetPos = new Vector3(randomX, spawnPos.y, randomZ);

            // プレハブから敵生成
            GameObject enemyGo = Instantiate(_enemyPrefab);

            // 敵の初期化
            if (!enemyGo.TryGetComponent(out EnemyController controller))
            {
                Debug.LogWarning("[EnemySpawner] EnemyController が付与されていないため生成を中止します。", enemyGo);
                Destroy(enemyGo);
                return;
            }
            var rising = enemyGo.GetComponent<EnemyRising>(); // 上昇
            if (rising == null) rising = enemyGo.AddComponent<EnemyRising>();
            controller.Initialize(_definition);
            // 上昇処理の開始
            rising.StartRise(targetPos, _undergroundOffset);
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnBasePoint == null) return;
            // 生成位置と開始位置の可視化
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(_rangeSize.x, 0.1f, _rangeSize.y);
            Gizmos.DrawWireCube(_spawnBasePoint.position, size);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_spawnBasePoint.position + Vector3.down * _undergroundOffset, size);
        }


    }

}
