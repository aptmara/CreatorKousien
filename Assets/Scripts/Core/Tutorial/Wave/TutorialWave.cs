using Game.Core.Enemy;
using Game.WaveSystem;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialWave", menuName = "Game/Tutorial/TutorialWave")]
public class TutorialWave : ScriptableObject
{
    [Header("壁を使うかどうか")]
    [SerializeField] public bool useCollectibleStopWall = false;


    [Header("存在している敵を動かすかどうか")]
    [SerializeField] public bool useEnemy = false;



    public enum ClearConditions
    {
        EnemyKill,
        WaveClear,
        GetCollectible,
        ShopEnd
    }
    [Header("クリア条件")]
    [SerializeField] public ClearConditions clearConditions;


    [Header("開始時リクエスト")]
    [SerializeField] bool spawnEnemy;
    public bool UseEnemySpawn => spawnEnemy;
    [SerializeField] List<EnemyDefinition> enemies;
    public List<EnemyDefinition> Enemies => enemies;

    [SerializeField] bool useStartText;
    public bool UseStartText => useStartText;
    [SerializeField] string startText;
    public string StartText => startText;

    [SerializeField] bool startWave;
    public bool UseStartWave => startWave;
    [SerializeField]
    WaveDataSO waveData;
    public WaveDataSO WaveData => waveData;
    [SerializeField]
    bool startRoguelike;
    public bool StartRoguelike => startRoguelike;

    [Header("終了時リクエスト")]
    [SerializeField] bool useEndText;
    public bool UseEndText => useEndText;
    [SerializeField] string endText;
    public string EndText => endText;

    

}
