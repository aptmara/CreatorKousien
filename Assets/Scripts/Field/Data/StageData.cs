using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Scriptable Objects/StageData")]
public class StageData : ScriptableObject
{
    [Header("フィールドのサイズ")]
    [Tooltip("フィールドの幅")]
    [SerializeField] private int _width = 10;// フィールドの幅

    [Tooltip("フィールドの奥行")]
    [SerializeField] private int _height = 10;// フィールドの奥行

    [Tooltip("フィールドの見た目のプレハブ")]
    [SerializeField] public GameObject FieldGroundPrefab;

    public int width => _width;

    public int height => _height;

    public List<SpawnZoneData> spawns;
}


[Serializable]
public class SpawnZoneData
{
    [SerializeField]
    public GameObject SpawnerPrefab;

    public Vector3 position;
}
