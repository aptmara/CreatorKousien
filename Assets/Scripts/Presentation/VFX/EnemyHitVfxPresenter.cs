// ------------------------------------------------------------
// File		: EnemyHitVfxPresenter.cs
// Summary	: 敵に攻撃が当たったときのVFXを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-02
//
// Notes	:
// - 6/2 ベース作成
// - 6/2 VFX再生処理の実装 - 浅野
// - 7/4 コンボ倍率に応じてVFXのスケールを変化させる処理を追加 - 浅野
// - 7/16 ヒットエフェクト複数種類配列化及びランダムプール駆動を実装 - Iwai
// - 8/10 タイプごとのヒットエフェクトに対応 - 越智
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Presentation.VFX
{
    public class EnemyHitVfxPresenter : MonoBehaviour
    {
        [Header("VFX 設定")]
        [SerializeField] private Vector3 _hitVfxOffset = new Vector3(0.0f, 1f, 0f);
        [SerializeField] private int _poolSize = 20;
        [SerializeField] private float _effectIntervalPerEnemy = 0.08f;
        [SerializeField] private float _lifeTime = 1.0f;

        [Header("コンボ連動スケール")]
        [Tooltip("コンボ1のときの基準倍率")]
        [SerializeField] private float _baseScale = 1.0f;

        [Tooltip("1コンボごとに増える倍率")]
        [SerializeField] private float _growthPerCombo = 0.08f;

        [Tooltip("これ以上大きくならない上限倍率")]
        [SerializeField] private float _maxScale = 3.0f;

        [Tooltip("事前登録する生成パターン")]
        [SerializeField] CollectibleHitVfxPattern[] _preregisteredPatterns;

        private int _currentCombo = 0;

        private struct PooledVfx
        {
            public GameObject Instance;
            public Vector3 OriginalScale;
        }

        private readonly Dictionary<string, GameObject[]> _prefabsDic = new();
        private readonly Dictionary<string, Queue<PooledVfx>> _vfxPoolDic = new();
        private readonly Dictionary<string, float> _nextEffectTimes = new();

        private void Awake()
        {
            // 事前にパターンを登録し、追加する
            if (_preregisteredPatterns != null)
            {
                foreach (var pattarn in _preregisteredPatterns)
                {
                    RegisterPrefabs(pattarn);
                }
            }
            
        }

        /// <summary>
        /// 指定されたインデックスのプレハブからインスタンスを生成してプールに保存する
        /// </summary>
        private PooledVfx CreateNewPoolInstance(int prefabIndex, GameObject[] prefabs, Queue<PooledVfx> queue)
        {
            GameObject prefab = prefabs[prefabIndex];
            if (prefab == null) return default;

            GameObject obj = Instantiate(prefab, transform);
            obj.SetActive(false);

            PooledVfx pooledVfx = new PooledVfx
            {
                Instance = obj,
                OriginalScale = prefab.transform.localScale
            };

            queue.Enqueue(pooledVfx);
            return pooledVfx;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Subscribe<BarrierHitBatchEvent>(OnBarrierHit);
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Unsubscribe<BarrierHitBatchEvent>(OnBarrierHit);
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
        }

        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            if (ev.ItemDataRaw is CollectibleData collectibleData)
            {
                RegisterPrefabs(collectibleData.HitPattern);
                Play(ev.EnemyId, ev.HitPosition, ev.EnemyTransform, collectibleData.HitPattern.ID);
            }
            
        }

        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            if (ev.ItemDataRaw is CollectibleData collectibleData)
            {
                Play(ev.EnemyId, ev.HitPosition, ev.BarrierTransform, collectibleData.HitPattern.ID);
            }
        }

        private void OnComboChanged(ComboChangedEvent ev)
        {
            _currentCombo = ev.CurrentCombo;
        }

        private void RegisterPrefabs(CollectibleHitVfxPattern collectibleHitVfxPattern)
        {
            if (collectibleHitVfxPattern == null) return;
            string id = collectibleHitVfxPattern.ID;
            // 既に両方値が存在している場合抜ける
            if (_vfxPoolDic.ContainsKey(id) && _prefabsDic.ContainsKey(id)) return;


            if (collectibleHitVfxPattern.HitPattern == null || collectibleHitVfxPattern.HitPattern.Length == 0)
            {
                Debug.LogWarning("[EnemyHitVfxPresenter] 渡されたプレハブ配列が空です。");
                return;
            }


            // 生成パターンを登録
            _prefabsDic[id] = collectibleHitVfxPattern.HitPattern;

            // プールを生成
            _vfxPoolDic[id] = new();
            for (int i = 0; i < _poolSize; i++)
            {
                CreateNewPoolInstance(i % _prefabsDic[id].Length, _prefabsDic[id], _vfxPoolDic[id]);
            }

        }

        private void Play(string enemyId, Vector3 position, Transform targetTransform, string key)
        {
            GameObject[] prefabs;
            if (!_prefabsDic.TryGetValue(key, out prefabs)) return;
            Queue<PooledVfx> pool;
            if (!_vfxPoolDic.TryGetValue(key, out pool)) return;

            if (prefabs == null || prefabs.Length == 0 || string.IsNullOrEmpty(enemyId)) return;

            if (_nextEffectTimes.TryGetValue(enemyId, out float nextTime) && Time.time < nextTime) return;

            _nextEffectTimes[enemyId] = Time.time + _effectIntervalPerEnemy;

            PooledVfx targetVfx;

            // プールにストックがある場合はそれを取り出す。
            if (pool.Count > 0)
            {
                targetVfx = pool.Dequeue();
            }
            else
            {
                int randomPrefabIndex = Random.Range(0, prefabs.Length);
                targetVfx = CreateNewPoolInstance(randomPrefabIndex, prefabs, pool);
                pool.Dequeue();
            }

            GameObject obj = targetVfx.Instance;
            if (obj == null) return;

            obj.transform.position = position + _hitVfxOffset;

            // コンボ1を基準に、コンボ数に応じて対応する元プレハブのスケールを等倍・巨大化補間
            float mul = _baseScale + Mathf.Max(0, _currentCombo - 1) * _growthPerCombo;
            mul = Mathf.Min(mul, _maxScale);
            obj.transform.localScale = targetVfx.OriginalScale * mul;

            if (targetTransform != null)
            {
                Vector3 dir = position - targetTransform.position;
                if (dir.sqrMagnitude > 0.001f)
                    obj.transform.rotation = Quaternion.LookRotation(dir.normalized);
            }

            obj.SetActive(true);

            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Clear(true);
                ps.Play(true);
            }

            StartCoroutine(ReturnAfter(targetVfx, _lifeTime, pool));
        }

        private IEnumerator ReturnAfter(PooledVfx vfx, float delay, Queue<PooledVfx> pool)
        {
            yield return new WaitForSeconds(delay);

            if (vfx.Instance != null)
            {
                foreach (var ps in vfx.Instance.GetComponentsInChildren<ParticleSystem>())
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                vfx.Instance.SetActive(false);
                pool.Enqueue(vfx);
            }
        }
    }
}
