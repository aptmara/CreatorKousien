using NUnit.Framework;
using UnityEngine;

using System.Collections.Generic;
using Game.Core.Enemy;
using System;
using System.Data;

namespace Game.Core.Enemy
{
    [CreateAssetMenu(fileName = "EnemySpawnerDefinition", menuName = "Game/Spawn/EnemySpawnerDefinition")]
    public class EnemySpawnerDefinition : ScriptableObject
    {
        [Serializable]
        public struct WaveData
        {
            [Header("HP倍率補正")]
            [Tooltip("ウェーブ毎に異なるHPにかかる倍率補正、敵毎に保持する基準HPをかけることで実数値となる")]
            [SerializeField] private float _hpRate;
            public float HPRate => _hpRate;

            [Header("バリア倍率補正")]
            [Tooltip("ウェーブ毎に異なるバリアにかかる倍率補正、敵毎に保持する基準バリアHPをかけることで実数値となる")]
            [SerializeField] private float _barrierRate;
            public float BarrierRate => _barrierRate;

            [Header("スポーン間隔")]
            [Tooltip("敵のスポーン間隔")]
            [SerializeField] private float _spawnInterval;
            public float SpawnInterval => _spawnInterval;

            [Header("出現する敵リスト")]
            [Tooltip("ウェーブで出現する全ての敵")]
            [SerializeField] private List<EnemyDefinition> _spawnEnemies;
            public List<EnemyDefinition> SpawnEnemies => new(_spawnEnemies); // Listの参照を渡さないために新たにコピーを作成し返す

            [Header("同時に存在できる敵の数")]
            [Tooltip("同時に存在できる敵の最大数,0以下なら制限なし")]
            [SerializeField]private int _maxAliveEnemies;
            public int MaxAliveEnemies => _maxAliveEnemies;

            [Header("スポーン設定")]
            [Tooltip("既存の敵と最低限空ける距離")]
            [SerializeField, Min(0f)] private float _minDistanceFromOtherEnemies;
            public float MinDistanceFromOtherEnemies => _minDistanceFromOtherEnemies;
        }

        [Header("ウェーブ情報")]
        [Tooltip("このスポナーが保持する全ウェーブ情報")]
        [SerializeField] private List<WaveData> _waveDatas;

        public List<WaveData> WaveDatas => new (_waveDatas);  // Listの参照を渡さないために新たにコピーを作成し返す

        [Header("自動スポーン設定")]
        [Tooltip("どのくらい下から出現させるか")]
        [SerializeField] private float _undergroundOffset = 10.0f;
        public float UndergroundOffset => _undergroundOffset;

        [Tooltip("自動スポーン開始までの待機時間")]
        [SerializeField, Min(0f)] private float _initialSpawnDelay = 2.0f;
        public float InitialSpawnDelay => _initialSpawnDelay;

        [Tooltip("スポーン位置を探す最大試行回数")]
        [SerializeField, Min(1)] int _maxSpawnPositionAttempts = 20;
        public int MaxSpawnPositionAttempts => _maxSpawnPositionAttempts;


        [Tooltip("1回の自動スポーンで出す敵の数")]
        [SerializeField, Min(1)] private int _enemiesPerSpawn = 1;
        public int EnemyPerSpawn => _enemiesPerSpawn;

        [Tooltip("一定時間ごとに自動でスポーンするか")]
        [SerializeField] private bool _enableAutoSpawn = true;
        public bool EnableAutoSpawn => _enableAutoSpawn;

    }

}
