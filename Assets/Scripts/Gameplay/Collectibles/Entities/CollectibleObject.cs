// ================================================================================
// File         : CollectibleData.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using Game.Data.Collectibles;
using System;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// 物理実体としての収集物。
    /// プレイヤーに拾われるとデータに変換され、オブジェクト自体はPoolへ返却されます。
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class CollectibleObject : MonoBehaviour
    {
        [Tooltip("このオブジェクトのマスターデータ")]
        [SerializeField] private CollectibleData _data;

        private Rigidbody _rigidbody;
        private Action<CollectibleObject> _returnAction;

        private Dictionary<int, GameObject> _visualCache = new Dictionary<int, GameObject>();
        private GameObject _currentVisual;

        public string Id => _data != null ? _data.Id : string.Empty;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Poolから取得した際の初期化処理
        /// </summary>
        public void Initialize(CollectibleData data, Action<CollectibleObject> returnAction)
        {
            _data = data;
            _returnAction = returnAction;
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            // 現在表示されている見た目があれば非表示にする
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
            }

            if (_data == null || _data.ViewPrefab == null) return;

            int prefabId = _data.ViewPrefab.GetInstanceID();

            // キャッシュに存在しない場合は新規生成
            if (!_visualCache.TryGetValue(prefabId, out GameObject visualInstance))
            {
                visualInstance = Instantiate(_data.ViewPrefab, transform);
                visualInstance.transform.localPosition = Vector3.zero;
                visualInstance.transform.localRotation = Quaternion.identity;

                // 物理演算の干渉を防ぐ
                Collider visualCollider = visualInstance.GetComponent<Collider>();
                if (visualCollider != null) Destroy(visualCollider);

                _visualCache[prefabId] = visualInstance;
            }

            // 該当する見た目を有効化
            visualInstance.SetActive(true);
            _currentVisual = visualInstance;
        }

        /// <summary>
        /// プレイヤーのCollectorに収集された際に呼ばれる処理
        /// </summary>
        /// <returns>軽量データ化された HeldItem</returns>
        public HeldItem OnCollected()
        {
            if (_data == null) return null;

            // 物理オブジェクトを保持せず、軽量データへ変換
            HeldItem heldItem = new HeldItem(_data);

            // 自身をPoolへ返却する
            _returnAction?.Invoke(this);

            return heldItem;
        }

        /// <summary>
        /// Pool返却時に状態をリセットします
        /// </summary>
        public void ResetState()
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;

            // プール返却時に見た目も非表示にリセット
            if (_currentVisual != null)
            {
                _currentVisual.SetActive(false);
                _currentVisual = null;
            }

            gameObject.SetActive(false);
        }

        /// <summary>
        /// E担当AvalancheControllerから呼ばれ、物理的な力を受けます。
        /// </summary>
        public void ApplyAvalancheForce(Vector3 force)
        {
            _rigidbody.AddForce(force, ForceMode.Impulse);
        }
    }
}
