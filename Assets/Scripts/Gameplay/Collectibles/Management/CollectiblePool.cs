// ================================================================================
// File         : CollectiblePool.cs
// Author       : Iwai Shogo
//
// Description  : CollectibleObjectの事前生成と貸出・返却を管理するPool。
// Created      : 2026-05-06
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
        [Header("Pool Settings")]
        [Tooltip("プールするアイテムの実体Prefab")]
        [SerializeField] private CollectibleObject _prefab;

        [Tooltip("ゲーム開始時に事前生成する数")]
        [SerializeField] private int _initialPoolSize = 50;

        private Queue<CollectibleObject> _pool = new Queue<CollectibleObject>();

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
            return obj;
        }

        /// <summary>
        /// オブジェクトをPoolへ返却します。
        /// </summary>
        public void Return(CollectibleObject obj)
        {
            obj.ResetState();

            if (!_pool.Contains(obj))
            {
                _pool.Enqueue(obj);
            }
        }
    }
}
