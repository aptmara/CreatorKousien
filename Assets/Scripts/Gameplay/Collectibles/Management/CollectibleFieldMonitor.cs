// ================================================================================
// File         : CollectibleFieldMonitor.cs
// Author       : Iwai Shogo
//
// Description  : 特定のエリア内にある収集物を物理的にん視し、不足時にSpawnerへ補充を要求するクラス。
// Created      : 2026-05-12
// ================================================================================

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// エリア内の物理的な出入りを監視し、アイテム密度を保ちます。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CollectibleFieldMonitor : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("補充を実行するSpawner")]
        [SerializeField] private CollectibleSpawner _spawner;

        [Header("Monitor Settings")]
        [Tooltip("このエリア内に維持するアイテムの目標数")]
        [SerializeField] private int _targetActiveCount = 80;

        [Tooltip("1回の補充で要求する最大数")]
        [SerializeField] private int _spawnBatchSize = 12;

        [Tooltip("アイテム数の監視と補充判定の間隔")]
        [SerializeField] private float _checkInterval = 1.0f;

        private Collider _monitorCollider;
        private HashSet<CollectibleObject> _itemsInArea = new HashSet<CollectibleObject>();
        private Coroutine _monitorCoroutine;

        private void Awake()
        {
            _monitorCollider = GetComponent<Collider>();
            _monitorCollider.isTrigger = true;
        }

        private void OnEnable()
        {
            _monitorCoroutine = StartCoroutine(MonitorRoutine());
        }

        private void OnDisable()
        {
            if (_monitorCoroutine != null)
            {
                StopCoroutine(_monitorCoroutine);
                _monitorCoroutine = null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out CollectibleObject collectible))
            {
                _itemsInArea.Add(collectible);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out CollectibleObject collectible))
            {
                _itemsInArea.Remove(collectible);
            }
        }

        /// <summary>
        /// 定期的にエリア内の数をチェックし、不足していればSpawnerへ命令を出す
        /// </summary>
        private IEnumerator MonitorRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(Mathf.Max(0.1f, _checkInterval));

            while (isActiveAndEnabled)
            {
                // Pool返却等で非アクティブになったオブジェクトを除外（バグ予防）
                CleanUpInactiveItems();

                if (_spawner != null)
                {
                    int currentCount = _itemsInArea.Count;
                    if (currentCount < _targetActiveCount)
                    {
                        int shortage = _targetActiveCount - currentCount;
                        int spawnCount = Mathf.Min(shortage, Mathf.Max(1, _spawnBatchSize));

                        _spawner.SpawnCollectibles(spawnCount);
                    }
                }

                yield return wait;
            }
        }

        /// <summary>
        /// Poolへ戻されたことでOnTriggerExitが呼ばれなかったアイテムの参照をクリーンアップします。
        /// </summary>
        private void CleanUpInactiveItems()
        {
            _itemsInArea.RemoveWhere(item => item == null || !item.gameObject.activeInHierarchy);
        }
    }
}
