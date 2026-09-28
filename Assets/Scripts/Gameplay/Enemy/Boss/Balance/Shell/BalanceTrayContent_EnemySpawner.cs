/**
 * File:BalanceTrayContent_EnemySpawner.cs
 * 
 * 皿の上に乗る敵のスポナー。
 * 
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Core.Enemy;


namespace Game.Gameplay.Enemy.Boss
{
    [DisallowMultipleComponent]
    public sealed class BalanceTrayContent_EnemySpawner : BalanceTrayContentBase
    {
        [Header("===== 出す敵 ======")]
        [SerializeField]
        private List<EnemyDefinition> _enemies = new List<EnemyDefinition>();
        [SerializeField, Min(0.01f)]
        private float _hpRate = 1.0f;
        [SerializeField, Min(0.01f)]
        private float _barrierRate = 1.0f;
        [SerializeField, Min(0.0f)]
        private float _minDistanceFromOtherEnemies = 1.5f;
        [SerializeField, Tooltip("このスポナーが出した敵の同時生存数の上限")]
        [Min(1)] private int _maxAlive = 6;

        [Header("====== 皿の高さ =======")]
        [SerializeField, Tooltip("この傾きを超えると出現開始")]
        [Range(0.0f, 1.0f)] private float _minTiltToSpawn = 0.1f;
        [SerializeField, Min(0.1f)]
        private float _intervalAtMinTilt = 4.0f;
        [SerializeField, Min(0.1f)]
        private float _intervalAtMaxTilt = 1.0f;

        [Header("===== 演出フック ======")]
        [SerializeField]
        private UnityEvent _onEnemySpawned;

        private readonly List<EnemyController> _alive = new List<EnemyController>();
        private EnemySpawner _spawner;
        private float _timer;

        protected override void OnBound()
        {
            _spawner = FindFirstObjectByType<EnemySpawner>();
        }

        protected override void OnRevealed()
        {
            _timer = 0.0f;
        }

        private void Update()
        {
            if (!IsRevealed || _spawner == null || _enemies.Count == 0 || Ctx.Beam == null) return;

            float tilt = Ctx.Beam.GetTiltRatio(Side);
            if (tilt < _minTiltToSpawn) return;

            float k = Mathf.InverseLerp(_minTiltToSpawn, 1.0f, tilt);
            float interval = Mathf.Lerp(_intervalAtMinTilt, _intervalAtMaxTilt, k);

            _timer += Time.deltaTime;
            if (_timer < interval) return;

            _timer = 0.0f;
            TrySpawn();
        }

        private void TrySpawn()
        {
            _alive.RemoveAll(e => e == null);
            if (_alive.Count >= _maxAlive) return;

            EnemyDefinition definition = _enemies[UnityEngine.Random.Range(0, _enemies.Count)];
            if(definition == null) return;

            if(_spawner.TrySpawnEnemyAtLine(definition,transform.position.x,_hpRate,_barrierRate,
                    _minDistanceFromOtherEnemies,out EnemyController spawned))
            {
                _alive.Add(spawned);
                _onEnemySpawned?.Invoke();
            }
        }
    }

}
