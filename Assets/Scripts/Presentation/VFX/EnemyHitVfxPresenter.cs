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
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Presentation.VFX
{
    public class EnemyHitVfxPresenter : MonoBehaviour
    {
        [SerializeField] private Vector3 _hitVfxOffset = new Vector3(0.0f, 1f, 0f);
        [SerializeField] private GameObject _hitVfxPrefab;
        [SerializeField] private int _poolSize = 20;
        [SerializeField] private float _effectIntervalPerEnemy = 0.08f;
        [SerializeField] private float _lifeTime = 1.0f;

        private readonly Queue<GameObject> _pool = new();
        private readonly Dictionary<string, float> _nextEffectTimes = new();

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                var obj = Instantiate(_hitVfxPrefab, transform);
                obj.SetActive(false);
                _pool.Enqueue(obj);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Subscribe<BarrierHitBatchEvent>(OnBarrierHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Unsubscribe<BarrierHitBatchEvent>(OnBarrierHit);
        }

        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            Play(ev.EnemyId, ev.HitPosition, ev.EnemyTransform);
        }

        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            Play(ev.EnemyId, ev.HitPosition, ev.BarrierTransform);
        }

        private void Play(string enemyId, Vector3 position, Transform targetTransform)
        {
            if (_hitVfxPrefab == null || string.IsNullOrEmpty(enemyId)) return;

            if (_nextEffectTimes.TryGetValue(enemyId, out float nextTime) && Time.time < nextTime)
                return;

            _nextEffectTimes[enemyId] = Time.time + _effectIntervalPerEnemy;

            var obj = _pool.Count > 0
                ? _pool.Dequeue()
                : Instantiate(_hitVfxPrefab, transform);

            obj.transform.position = position + _hitVfxOffset;

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

            StartCoroutine(ReturnAfter(obj, _lifeTime));
        }

        private IEnumerator ReturnAfter(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);

            foreach (var ps in obj.GetComponentsInChildren<ParticleSystem>())
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            obj.SetActive(false);
            _pool.Enqueue(obj);
        }
    }
}
