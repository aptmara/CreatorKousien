/**
 * 作成：寺田晴
 * 
 * 内容：敵を生成する(現在はデバッグキー込み)
 * 
 */
using Game.Core.Enemy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;


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
            _spawnAction.Enable();
            _spawnAction.performed += OnSpawnPerformed;
        }

        private void OnDisable()
        {
            // デバッグインプットの登録解除
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
            if (_enemyPrefab == null) return;

            // 目標位置計算
            Vector3 spawnPos = _spawnBasePoint.position;
            float randomX = Random.Range(spawnPos.x - _rangeSize.x / 2f, spawnPos.x + _rangeSize.x / 2f);
            float randomZ = Random.Range(spawnPos.z - _rangeSize.y / 2f, spawnPos.z + _rangeSize.y / 2f);
            Vector3 TargetPos = new Vector3(randomX,spawnPos.y,randomZ);

            // プレハブから敵生成
            GameObject enemyGo = Instantiate(_enemyPrefab);

            // 敵の初期化
            var controller = enemyGo.GetComponent<EnemyController>();
            var rising = enemyGo.GetComponent<EnemyRising>();// 上昇
            if (rising != null) rising = enemyGo.AddComponent<EnemyRising>();

            controller.Initialize(_definition);
            // 上昇処理の開始
            rising.StartRise(controller, TargetPos, _undergroundOffset);
        }

        private void OnDrawGizmosSelected()
        {
            // 生成位置と開始位置の可視化
            Gizmos.color = Color.cyan;
            Vector3 size = new Vector3(_rangeSize.x, 0.1f, _rangeSize.y);
            Gizmos.DrawWireCube(_spawnBasePoint.position, size);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(_spawnBasePoint.position + Vector3.down * _undergroundOffset, size);
        }


    }

}
