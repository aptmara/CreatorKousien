// ================================================================================
// File         : CollectibleSpawner.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の生成と初期化を制御する司令塔。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using Game.Data.Collectibles;
using TMPro;

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
            if (_pool == null || _registry == null || _spawnAreas.Length == 0 || _spawnableData.Length == 0)
            {
                Debug.LogError("[CollectibleSpawner] 必要な参照またはデータが設定されていません。");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                // 1. 場所とデータをランダムに選ぶ
                CollectibleSpawnArea area = _spawnAreas[Random.Range(0, _spawnAreas.Length)];
                CollectibleData data = _spawnableData[Random.Range(0, _spawnableData.Length)];

                // 2. Poolから取得
                CollectibleObject obj = _pool.Get();

                // 3. 位置と回転を設定
                obj.transform.position = area.GetRandomPosition();
                obj.transform.rotation = Random.rotation;

                // 4. 初期化と返却処理のバインド
                obj.Initialize(data, ReturnToPool);

                // 5. 台帳へ登録
                _registry.Register(obj);
            }
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
