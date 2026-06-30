//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_UpgradeData.cs
// brief  : 強化ID、表示名、最大レベル、効果SOを所持
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SO_UpgradeData", menuName = "Scriptable Objects/SO_UpgradeData")]
public class SO_UpgradeData : ScriptableObject
{
    [Header("基本の情報")]
    [SerializeField] private string _upgradeId;
    [SerializeField] private string _displayName;

    [Header("レベル")]
    [SerializeField] private int _maxLevel = 5;

    [Header("効果")]
    [SerializeField] private List<SO_UpgradeEffect> _effects = new();

    public string UpgradeId => _upgradeId;
    public string DisplayName => _displayName;
    public int MaxLevel => _maxLevel;

    public void Apply()
    {

    }

}
