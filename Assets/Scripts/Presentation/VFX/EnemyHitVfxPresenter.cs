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
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using UnityEngine;

namespace Game.Presentation.VFX
{
    public class EnemyHitVfxPresenter : MonoBehaviour
    {
        [Header("VFX 設定")]
        [SerializeField] private Vector3 _hitVfxOffset = new Vector3(0.0f, 1f, 0f);
        [SerializeField] private GameObject _hitVfxPrefab;
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

        private int _currentCombo = 0;
        private Vector3 _prefabScale = Vector3.one;


        private readonly Queue<GameObject> _pool = new();
        private readonly Dictionary<string, float> _nextEffectTimes = new();

        private void Awake()
        {
            // プレハブの元スケールを基準として保持
            if (_hitVfxPrefab != null)
                _prefabScale = _hitVfxPrefab.transform.localScale;

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
            Play(ev.EnemyId, ev.HitPosition, ev.EnemyTransform);
        }

        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            Play(ev.EnemyId, ev.HitPosition, ev.BarrierTransform);
        }

        private void OnComboChanged(ComboChangedEvent ev)
        {
            _currentCombo = ev.CurrentCombo;
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


            // コンボ1を基準に、1コンボ増えるごとに _growthPerCombo ずつ倍率を増やす
            float mul = _baseScale + Mathf.Max(0, _currentCombo - 1) * _growthPerCombo;
            mul = Mathf.Min(mul, _maxScale);
            obj.transform.localScale = _prefabScale * mul;


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
