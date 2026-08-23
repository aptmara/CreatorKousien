// ================================================================================
// File         : CollectibleSpawner.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の生成と初期化を制御する司令塔。
// Created      : 2026-05-06
// Updated      : 2026-05-12 (維持管理機能をFieldMonitorへ分離)
// ================================================================================

using UnityEngine;
using UnityEngine.InputSystem;
using Game.Data.Collectibles;
using Game.Core.Roguelike;
using UnityEngine.Polybrush;

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

        [Header("Free Motion")]
        [Tooltip("生成時に水平方向へ与える速度の最小値")]
        [SerializeField] private float _minInitialSpeed = 1.5f;

        [Tooltip("生成時に水平方向へ与える速度の最大値")]
        [SerializeField] private float _maxInitialSpeed = 4.0f;

        [Tooltip("生成時に上方向へ与える速度")]
        [SerializeField] private float _initialUpwardSpeed = 0.5f;

        [Tooltip("プレイヤーが触れた時に保持/回収できるか")]
        [SerializeField] private bool _canBeCollectedByPlayer = false;

        [Header("高さ補正")]
        [Tooltip("指定位置から下方向に地面を探し、地面からこの高さに収集物を出す")]
        [SerializeField] private float _spawnHeightOffset = 0.5f;
        [SerializeField] private LayerMask _spawnGroundMask = ~0;

        [SerializeField, Tooltip("SubScene連携用SO")]
        private SceneEventChannel _eventChannel;


        [Header("Debug")]
        [SerializeField, Tooltip("デバック生成用のキー")]
        private Key _debugSpawnKey = Key.F5;


        // テスト用: Context Menuから実行可能
        [ContextMenu("Spawn Test Items (10)")]
        public void SpawnTestItems10() => SpawnCollectibles(10);

        [ContextMenu("Spawn Test Items (100)")]
        public void SpawnTestItems100() => SpawnCollectibles(100);


        private void OnEnable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.OnExecuteInt += SpawnCollectibles;
            }
        }

        private void OnDisable()
        {
            if (_eventChannel != null)
            {
                _eventChannel.OnExecuteInt -= SpawnCollectibles;
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (Keyboard.current != null && Keyboard.current[_debugSpawnKey].wasPressedThisFrame)
            {
                SpawnTestItems100();
            }
#endif
        }


        /// <summary>
        /// 指定された数のアイテムを生成して配置します。
        /// </summary>
        public void SpawnCollectibles(int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (!CanSpawnInArea())
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                SpawnOne();
            }
        }

        /// <summary>
        /// 指定位置を中心にアイテムを生成して、初期速度を与えます。
        /// </summary>
        public void SpawnCollectiblesAt(Vector3 position, int count)
        {
            if (count <= 0)
            {
                return;
            }

            if (!CanSpawnAtPosition())
            {
                return;
            }

            position = GetHeightAdjustedPosition(position);
            for (int i = 0; i < count; i++)
            {
                SpawnOne(position);
            }
        }

        private Vector3 GetHeightAdjustedPosition(Vector3 position)
        {
            Vector3 origin = position + Vector3.up * 50f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 100f,
                _spawnGroundMask, QueryTriggerInteraction.Ignore);
            bool found = false;
            float groundY = float.MinValue;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player"))
                {
                    continue;
                }

                if (hit.point.y <= position.y + 0.1f && (!found || hit.point.y > groundY))
                {
                    found = true;
                    groundY = hit.point.y;
                }
            }

            if (found)
            {
                position.y = groundY + _spawnHeightOffset;
            }

            return position;
        }

        /// <summary>
        /// 1個のアイテムを生成し、自由に動く初期速度を与える。
        /// </summary>
        private void SpawnOne()
        {
            CollectibleSpawnArea area = _spawnAreas[Random.Range(0, _spawnAreas.Length)];
            CollectibleData data = GetRandomUnlockedData();
            if (data == null) return;
            CollectibleObject obj = _pool.Get();

            obj.transform.position = area.GetRandomPosition();
            obj.transform.rotation = Random.rotation;
            obj.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            obj.SetInitialMotion(CreateInitialVelocity(), Random.insideUnitSphere * Random.Range(2f, 8f));

            _registry.Register(obj);
        }

        /// <summary>
        /// 指定位置に1個のアイテムを生成する。
        /// </summary>
        private void SpawnOne(Vector3 position)
        {
            CollectibleData data = GetRandomUnlockedData();
            if (data == null) return;
            CollectibleObject obj = _pool.Get();

            obj.transform.position = position;
            obj.transform.rotation = Random.rotation;
            obj.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            obj.SetInitialMotion(CreateInitialVelocity(), Random.insideUnitSphere * Random.Range(2f, 8f));

            _registry.Register(obj);
        }

        /// <summary>
        /// 指定したCollectibleDataを指定位置にランダムな選択なしで生成する。
        /// ボスギミック等種類を確定させたいケースで使用する
        /// </summary>
        /// <param name="data"></param>
        /// <param name="position"></param>
        public void SpawnSpecificAt(CollectibleData data, Vector3 position)
        {
            if(data == null) return;
            if (!CanSpawnAtPosition()) return;

            position = GetHeightAdjustedPosition(position);
            CollectibleObject obj = _pool.Get();

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.identity;
            obj.Initialize(data, ReturnToPool, _canBeCollectedByPlayer);
            obj.SetInitialMotion(CreateInitialVelocity(), Random.insideUnitSphere * Random.Range(2.0f, 8.0f));

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

        private CollectibleData GetRandomUnlockedData()
        {
            int unlockedCount = 0;
            foreach (CollectibleData data in _spawnableData)
            {
                if (data != null && RoguelikeUpgradeRuntime.IsCollectibleUnlocked((int)data.Type))
                {
                    unlockedCount++;
                }
            }

            if (unlockedCount == 0)
            {
                Debug.LogError("[CollectibleSpawner] 解禁済みのCollectibleDataがありません。", this);
                return null;
            }

            int selectedIndex = Random.Range(0, unlockedCount);
            foreach (CollectibleData data in _spawnableData)
            {
                if (data == null || !RoguelikeUpgradeRuntime.IsCollectibleUnlocked((int)data.Type)) continue;
                if (selectedIndex-- == 0) return data;
            }

            return null;
        }

        /// <summary>
        /// 通常生成に必要な参照が揃っているか確認します。
        /// </summary>
        /// <returns>通常生成が可能な場合はtrue</returns>
        private bool CanSpawnInArea()
        {
            if (!HasCoreSpawnDependencies())
            {
                return false;
            }

            if (_spawnAreas == null || _spawnAreas.Length == 0)
            {
                Debug.LogError("[CollectibleSpawner] 通常生成に必要なSpawnAreaが設定されていません。");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 位置指定生成に必要な参照が揃っているか確認します。
        /// </summary>
        /// <returns>位置指定生成が可能な場合はtrue</returns>
        private bool CanSpawnAtPosition()
        {
            return HasCoreSpawnDependencies();
        }

        /// <summary>
        /// 生成処理の共通必須参照が揃っているか確認します。
        /// </summary>
        /// <returns>共通必須参照が揃っている場合はtrue</returns>
        private bool HasCoreSpawnDependencies()
        {
            if (_pool == null)
            {
                Debug.LogError("[CollectibleSpawner] 生成に必要なCollectiblePoolが設定されていません。");
                return false;
            }

            if (_registry == null)
            {
                Debug.LogError("[CollectibleSpawner] 生成に必要なCollectibleRegistryが設定されていません。");
                return false;
            }

            if (_spawnableData == null || _spawnableData.Length == 0)
            {
                Debug.LogError("[CollectibleSpawner] 生成に必要なCollectibleDataが設定されていません。");
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
