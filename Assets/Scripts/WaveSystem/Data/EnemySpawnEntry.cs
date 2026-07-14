// ------------------------------------------------------------
// File		: EnemySpawnEntry.cs
// Summary	: 敵の出現情報を管理するエントリ
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 敵の性能やPrefabは既存のEnemyDefinitionが管理します。
// - このクラスは「何体・いつ出すか」だけを管理します。
// - 出現位置は既存のEnemySpawnerが決定します。
// ------------------------------------------------------------
using Game.Core.Enemy;
using System;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// 1種類の敵を出現させるための設定
    /// 出現位置はWave側で設定せず、EnemySpawnerで設定する
    /// </summary>
    [Serializable]
    public class EnemySpawnEntry
    {
        [SerializeField]
        [Tooltip("出現させる敵データ。Game/Enemy/EnemyDefinitionから作成したSOを指定してください。")]
        private EnemyDefinition enemyDefinition;

        [SerializeField]
        [Min(1)]
        [Tooltip("この設定から出現させる敵の数")]
        private int spawnCount = 1;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Group開始後、この敵のスポーンを始めるまでの待ち時間")]
        private float startDelay = 0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("敵を1体出現させてから、次の1体を出現させるまでの時間")]
        private float spawnInterval = 0.5f;

        public EnemyDefinition EnemyDefinition => enemyDefinition;
        public int SpawnCount => spawnCount;
        public float StartDelay => startDelay;
        public float SpawnInterval => spawnInterval;
    }
}
