using NUnit.Framework;
using UnityEngine;

using System.Collections.Generic;
using Game.Core.Enemy;
using System;

[CreateAssetMenu(fileName = "EnemySpawnerDefinition", menuName = "Scriptable Objects/EnemySpawnerDefinition")]
public class EnemySpawnerDefinition : ScriptableObject
{
    [Serializable]
    public struct WaveData
    {
        [Header("HP倍率補正")]
        [Tooltip("ウェーブ毎に異なるHPにかかる倍率補正、敵毎に保持する基準HPをかけることで実数値となる")]
        float HPRate;

        [Header("スポーン間隔")]
        [Tooltip("敵のスポーン間隔")]
        float SpawnInterval;

        [Header("出現する敵リスト")]
        [Tooltip("ウェーブで出現する全ての敵")]
        List<EnemyDefinition> SpawnEnemies;

        [Tooltip("同時に存在できる敵の最大数,0以下なら制限なし")]
        [SerializeField] int MaxAliveEnemies;
    }

    [Header("ウェーブ情報")]
    [Tooltip("このスポナーが保持する全ウェーブ情報")]
    List<WaveData> WaveDatas;

    [Header("ウェーブ情報")]
    [Tooltip("どのくらい下から出現させるか")]
    float _undergroundOffset = 10.0f;

    [Header("スポーン設定")]
    [Tooltip("既存の敵と最低限空ける距離")]
    [SerializeField, Min(0f)] float _minDistanceFromOtherEnemies = 3.0f;

    [Tooltip("自動スポーン開始までの待機時間")]
    [SerializeField, Min(0f)] private float _initialSpawnDelay = 2.0f;


}
