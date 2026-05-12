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

        public float DamageAmount => _data != null ? _data.DamageAmount : 0f;

        public bool CanBeCollectedByPlayer { get; private set; } = true;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        /// <summary>
        /// Poolから取得した際の初期化処理
        /// </summary>
        public void Initialize(CollectibleData data, Action<CollectibleObject> returnAction)
        {
            Initialize(data, returnAction, true);
        }

        /// <summary>
        /// Poolから取得した際の初期化処理
        /// </summary>
        /// <param name="data">収集物データ</param>
        /// <param name="returnAction">Pool返却処理</param>
        /// <param name="canBeCollectedByPlayer">プレイヤー収集を許可するか</param>
        public void Initialize(CollectibleData data, Action<CollectibleObject> returnAction, bool canBeCollectedByPlayer)
        {
            _data = data;
            _returnAction = returnAction;
            CanBeCollectedByPlayer = canBeCollectedByPlayer;
            UpdateVisual();
        }

        /// <summary>
        /// 外部システムから物理挙動を初期化する。
        /// </summary>
        /// <param name="velocity">初期速度</param>
        /// <param name="angularVelocity">初期角速度</param>
        public void SetInitialMotion(Vector3 velocity, Vector3 angularVelocity)
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = angularVelocity;
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
            if (_data == null || !CanBeCollectedByPlayer)
            {
                return null;
            }

            // 物理オブジェクトを保持せず、軽量データへ変換
            HeldItem heldItem = new HeldItem(_data, this);

            // 自身をPoolへ返却する
            _returnAction?.Invoke(this);

            return heldItem;
        }


        /// <summary>
        /// プレイヤーが物理的に拾い上げる際の処理。
        /// </summary>
        /// <returns>Itemの軽量データ</returns>
        public HeldItem TakeOwnership()
        {
            if (_data == null || !CanBeCollectedByPlayer)
            {
                return null;
            }

            // 物理演算と当たり判定を無効化（ブレード内での大爆発を防ぐため）
//            if (_rigidbody != null) _rigidbody.isKinematic = true;
//            Collider col = GetComponent<Collider>();
//            if (col != null) col.enabled = false;

            // 自分自身（this）を渡しつつデータ化
            return new HeldItem(_data, this);
        }


        /// <summary>
        /// Pool返却時に状態をリセットします
        /// </summary>
        public void ResetState()
        {
            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            CanBeCollectedByPlayer = true;
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
        /// 収集以外の理由でPoolへ返却する。
        /// </summary>
        public void Despawn()
        {
            if (_returnAction != null)
            {
                _returnAction.Invoke(this);
                return;
            }

            ResetState();
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
