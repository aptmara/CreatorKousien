using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System;
using Game.Core.Enemy;
using Game.WaveSystem;

[Serializable]
[CreateAssetMenu(fileName = "TutorialStartRequest", menuName = "Game/Tutorial/TutorialStartRequest")]
public class TutorialStartRequest : ScriptableObject
{
    [SerializeField] bool spawnEnemy;
    public bool UseEnemySpawn => spawnEnemy;
    [SerializeField] List<EnemyDefinition> enemies;
    public List<EnemyDefinition> Enemies => enemies;

    [SerializeField] bool startWave;
    public bool UseStartWave => startWave;
    [SerializeField]
    WaveDataSO waveData;
    public WaveDataSO WaveData => waveData;
}
