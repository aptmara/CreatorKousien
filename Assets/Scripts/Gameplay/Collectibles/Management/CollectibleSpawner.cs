// ================================================================================
// File         : CollectibleSpawner.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の生成と初期化を制御する司令塔。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using Game.Data.Collectibles;
using System.Collections;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// Pool, Registry, SpawnAreaを統合し、アイテムの出現を管理します。
    /// </summary>
    public class CollectibleSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CollectiblePool _pool;
        [SerializeField] private CollectibleRegistry _registry;
        [SerializeField] private CollectibleSpawnArea[] _spawnAreas;

        [Header("Spawn Settings")]
        [Tooltip("出現させるアイテムのデータリスト")]
        [SerializeField] private CollectibleData[] _spawnableData;

        [Tooltip("ステージ上のアイテム数を自動で維持するか")]
        [SerializeField] private bool _maintainActiveCount = true;

        [Tooltip("ステージ上に維持するアイテム数")]
        [SerializeField] private int _targetActiveCount = 80;

        [Tooltip("1回の補充で生成する最大数")]
        [SerializeField] private int _spawnBatchSize = 12;

        [Tooltip("補充判定の間隔")]
        [SerializeField] private float _spawnInterval = 1.0f;

        [Header("Free Motion")]
        [Tooltip("生成時に水平方向へ与える速度の最小値")]
        [SerializeField] private float _minInitialSpeed = 1.5f;

        [Tooltip("生成時に水平方向へ与える速度の最大値")]
        [SerializeField] private float _maxInitialSpeed = 4.0f;

        [Tooltip("生成時に上方向へ与える速度")]
        [SerializeField] private float _initialUpwardSpeed = 0.5f;

        [Tooltip("プレイヤーが触れた時に保持/回収できるか")]
        [SerializeField] private bool _canBeCollectedByPlayer = false;

        private Coroutine _maintainCoroutine;

        private void OnEnable()
        {
            if (_maintainActiveCount)
            {
                _maintainCoroutine = StartCoroutine(MaintainSpawnRoutine());
            }
        }

        private void OnDisable()
        {
            if (_maintainCoroutine != null)
            {
                StopCoroutine(_maintainCoroutine);
                _maintainCoroutine = null;
            }
        }

        /// <summary>
        /// テスト用: コンテキストメニューから実行可能
        /// </summary>
        [ContextMenu("Spawn Test Items (10)")]
        public void SpawnTestItems10()
        {
            SpawnCollectibles(10);
        }
        [ContextMenu("Spawn Test Items (100)")]
        public void SpawnTestItems100()
        {
            SpawnCollectibles(100);
        }

        /// <summary>
        /// 指定された数のアイテムを生成して配置します。
        /// </summary>
        public void SpawnCollectibles(int count)
        {
            if (!CanSpawn())
            {
                return;
            }

            int spawnCount = Mathf.Max(0, count);
            if (_maintainActiveCount)
            {
                int shortage = Mathf.Max(0, _targetActiveCount - _registry.ActiveCount);
                spawnCount = Mathf.Min(spawnCount, shortage);
            }

            for (int i = 0; i < spawnCount; i++)
            {
                SpawnOne();
            }
        }

        /// <summary>
        /// 不足分を定期補充し、ステージ上のアイテム密度を保つ。
        /// </summary>
        private IEnumerator MaintainSpawnRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.1f, _spawnInterval));

            while (isActiveAndEnabled)
            {
                if (CanSpawn())
                {
                    int shortage = Mathf.Max(0, _targetActiveCount - _registry.ActiveCount);
                    int spawnCount = Mathf.Min(shortage, Mathf.Max(1, _spawnBatchSize));
                    SpawnCollectibles(spawnCount);
                }

                yield return wait;
            }
        }

        /// <summary>
        /// 1個のアイテムを生成し、自由に動く初期速度を与える。
        /// </summary>
        private void SpawnOne()
        {
            CollectibleSpawnArea area = _spawnAreas[Random.Range(0, _spawnAreas.Length)];
            CollectibleData data = _spawnableData[Random.Range(0, _spawnableData.Length)];
            CollectibleObject obj = _pool.Get();

            obj.transform.position = area.GetRandomPosition();
            obj.transform.rotation = Random.rotation;
            obj.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            obj.SetInitialMotion(CreateInitialVelocity(), Random.insideUnitSphere * Random.Range(2f, 8f));

            _registry.Register(obj);
        }

        /// <summary>
        /// 生成時のランダムな初期速度を作る。
        /// </summary>
        /// <returns>初期速度</returns>
        private Vector3 CreateInitialVelocity()
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.right;
            }

            float speed = Random.Range(_minInitialSpeed, _maxInitialSpeed);
            return new Vector3(direction.x * speed, _initialUpwardSpeed, direction.y * speed);
        }

        /// <summary>
        /// Spawnに必要な参照と設定が揃っているか確認する。
        /// </summary>
        /// <returns>Spawn可能ならtrue</returns>
        private bool CanSpawn()
        {
            if (_pool == null || _registry == null || _spawnAreas == null || _spawnAreas.Length == 0 || _spawnableData == null || _spawnableData.Length == 0)
            {
                Debug.LogError("[CollectibleSpawner] 必要な参照またはデータが設定されていません。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// アイテムが収集された際に呼ばれ、Poolへ返し台帳から消すコールバック
        /// </summary>
        private void ReturnToPool(CollectibleObject obj)
        {
            _registry.Unregister(obj);
            _pool.Return(obj);
        }
    }
}
