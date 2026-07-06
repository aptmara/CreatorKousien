// ================================================================================
// File         : CollectibleObject.cs
// Author       : Iwai Shogo
//
// Description  : 収集物の各種パラメータを定義するScriptableObject。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using Game.Data.Collectibles;
using Game.Core.Events;
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
        [Header("--- データ ---")]
        [Tooltip("このオブジェクトのマスターデータ")]
        [SerializeField] private CollectibleData _data;

        [Header("--- 落下自動回収設定 ---")]
        [Tooltip("このY座標を下回ったら、自動でプールへ戻すデッドライン")]
        [SerializeField] private float _fallDeadLineY = -20f;


        [Header("--- 生成直後のすり抜け(対プレイヤー) ---")]
        [Tooltip("スポーン時のレイヤー")]
        [SerializeField] private string _spawnLayer = "CollectibleSpawning";
        [Tooltip("通常のレイヤー")]
        [SerializeField] private string _normalLayer = "Collectible";
        [Tooltip("生成直後にプレイヤーのコライダーをすり抜ける時間(秒)")]
        [SerializeField, Min(0f)] private float _passThroughDuration = 0.4f;


        private Rigidbody _rigidbody;
        private Action<CollectibleObject> _returnAction;
        private GameObject _currentVisual;

        private Dictionary<int, GameObject> _visualCache = new Dictionary<int, GameObject>();
        private Vector3 _initialScale;

        // --- 特殊効果用のランタイム変数 ---
        private int _currentBounceCount = 0;

        public string Id => _data != null ? _data.Id : string.Empty;
        public float DamageAmount => _data != null ? _data.DamageAmount : 0f;

        public float SameItemCooldown => _data != null ? _data.SameItemCooldown : 0.25f;

        public bool CanBeCollectedByPlayer { get; private set; } = true;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _initialScale = transform.localScale;
        }

        private void FixedUpdate()
        {
            // アイテムがステージ外へ落下した場合は自動クリーンアップ
            if (transform.position.y < _fallDeadLineY)
            {
                Despawn();
            }
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
            _currentBounceCount = 0;

            UpdateVisual();
            ApplySpecialPhysics();
        }

        /// <summary>
        /// アイテムのタイプに応じて物理特性を適用
        /// </summary>
        private void ApplySpecialPhysics()
        {
            if (_data == null) return;

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                // グミ且つPhysicsMaterialが設定されていれば適用
                if (_data.Type == CollectibleType.Gummy && _data.GummyPhysicsMaterial != null)
                {
                    col.material = _data.GummyPhysicsMaterial;
                }
                else
                {
                    col.material = null;
                }
            }
        }

        /// <summary>
        /// エネミーに衝突した際、このアイテム固有の技を発生させる。
        /// EnemyHitReceiverから呼び出されます。
        /// </summary>
        public bool ExecuteHitImpact(string enemyId, float bodyDamage, Vector3 hitPosition, Transform enemyTransform)
        {
            if (_data == null) return false;

            // 1. グミの最大連鎖数チェック
            if (_data.Type == CollectibleType.Gummy)
            {
                if (_currentBounceCount >= _data.MaxBounceChainCount)
                {
                    Despawn();
                    return false;
                }
                _currentBounceCount++;
            }

            // 2. イベントを発行
            EventBus.Publish(new EnemyHitBatchEvent(enemyId, 1, bodyDamage, hitPosition, enemyTransform, _data));

            // 3. 十字架専用ロジック
            if (_data.Type == CollectibleType.Cross)
            {
                SpawnCrossLaser(hitPosition);
            }

            return true;
        }

        /// <summary>
        /// 十字架レーザーを画面右側の同じ高さに生成する。
        /// </summary>
        private void SpawnCrossLaser(Vector3 hitPosition)
        {
            if (_data.LaserPrefab == null) return;

            Vector3 spawnPosition = new Vector3(15f, hitPosition.y, hitPosition.z);
            GameObject laserObj = Instantiate(_data.LaserPrefab, spawnPosition, Quaternion.LookRotation(Vector3.left));

            // レーザーの生存期間は任意で設定（例: 1.5秒）
            Destroy(laserObj, 1.5f);
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

            BeginSpawnPassThrough();
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

            // 自分自身（this）を渡しつつデータ化
            return new HeldItem(_data, this);
        }


        /// <summary>
        /// Pool返却時に状態をリセットします
        /// </summary>
        public void ResetState()
        {
            CancelInvoke(nameof(EndSpawnPassThrough));

            if (_rigidbody == null)
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            CanBeCollectedByPlayer = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            transform.localScale = _initialScale;

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

        public CollectibleData GetCollectableData()
        {
            return _data;
        }


        /// <summary>
        /// 生成直後のすり抜け時間が経過したら、通常のレイヤーに戻す
        /// </summary>
        private void EndSpawnPassThrough()
        {
            int normal = LayerMask.NameToLayer(_normalLayer);
            if (normal >= 0)
            {
                SetLayerRecursively(gameObject, normal);
            }
        }


        /// <summary>
        /// 指定したGameObjectとその子オブジェクトすべてのレイヤーを再帰的に設定する
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="newLayer"></param>
        private static void SetLayerRecursively(GameObject obj, int newLayer)
        {
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }


        /// <summary>
        /// 生成直後にプレイヤーのコライダーをすり抜けるためのレイヤー設定を行う
        /// </summary>
        private void BeginSpawnPassThrough()
        {
            int spawn = LayerMask.NameToLayer(_spawnLayer);
            if (spawn < 0)
            {
                return;
            }

            SetLayerRecursively(gameObject, spawn);
            CancelInvoke(nameof(EndSpawnPassThrough));
            Invoke(nameof(EndSpawnPassThrough), _passThroughDuration);
        }
    }
}
