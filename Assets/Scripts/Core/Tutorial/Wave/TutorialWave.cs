using UnityEngine;
using System.Collections.Generic;

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
    }
    [Header("クリア条件")]
    [SerializeField] public ClearConditions clearConditions;

    // Wave開始時に行う処理リクエストSO
    [SerializeField] public TutorialStartRequest waveStartRequest;

    // Wave終了時に行う処理リクエストSO
}
