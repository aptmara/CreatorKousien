// ================================================================================
// File         : CollectiblePool.cs
// Author       : Iwai Shogo
//
// Description  : CollectibleObjectの事前生成と貸出・返却を管理するPool。
// Created      : 2026-05-06
// Updated      : 2026-07-02 (全てのアイテムの強制回収を実装)
// ================================================================================

using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// CollectibleObjectの生成負荷を下げるためのオブジェクトプール。
    /// 大量発生と短寿命によるGCとフレーム落ちを防ぎます。
    /// </summary>
    public class CollectiblePool : MonoBehaviour
    {
        public static CollectiblePool Instance { get; private set; }

        [Header("Pool Settings")]
        [Tooltip("プールするアイテムの実体Prefab")]
        [SerializeField] private CollectibleObject _prefab;

        [Tooltip("ゲーム開始時に事前生成する数")]
        [SerializeField] private int _initialPoolSize = 300;

        private Queue<CollectibleObject> _pool = new Queue<CollectibleObject>();

        private List<CollectibleObject> _activeInField = new List<CollectibleObject>();

        public bool IsPrewarmed { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            Prewarm();
        }

        /// <summary>
        /// 初期生成処理
        /// </summary>
        private void Prewarm()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                CreateNewObject();
            }

            IsPrewarmed = true;
        }

        private CollectibleObject CreateNewObject()
        {
            CollectibleObject newObj = Instantiate(_prefab, transform);
            newObj.gameObject.SetActive(false);
            _pool.Enqueue(newObj);
            return newObj;
        }

        /// <summary>
        /// Poolからオブジェクトを取得します。
        /// </summary>
        public CollectibleObject Get()
        {
            if (_pool.Count == 0)
            {
                Debug.LogWarning("[CollectiblePool] Poolが不足したため追加生成します。初期Prewarmサイズを見直してください。");
                CreateNewObject();
            }

            CollectibleObject obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);

            _uiActiveItems.Add(obj);
            return obj;
        }

        // 管理用
        private HashSet<CollectibleObject> _uiActiveItems = new HashSet<CollectibleObject>();

        /// <summary>
        /// オブジェクトをPoolへ返却します。
        /// </summary>
        public void Return(CollectibleObject obj)
        {
            if (obj == null) return;
            if (!_uiActiveItems.Remove(obj)) return;

            obj.ResetState();
            _pool.Enqueue(obj);
        }

        /// <summary>
        /// 現在フィールド上に散らばっている全てのアイテムを強制回収する
        /// </summary>
        public void ClearAllActiveItemsInField()
        {
            Debug.Log($"[CollectiblePool] フィールド上の自由移動アイテムを一括クリーンアップします。対象数: {_uiActiveItems.Count}");

            CollectibleObject[] targets = new CollectibleObject[_uiActiveItems.Count];
            _uiActiveItems.CopyTo(targets);
            for (int i = targets.Length - 1; i >= 0; i--)
            {
                if (targets[i] != null && targets[i].gameObject.activeInHierarchy)
                {
                    targets[i].Despawn();
                }
            }
            _uiActiveItems.Clear();
        }
    }
}
