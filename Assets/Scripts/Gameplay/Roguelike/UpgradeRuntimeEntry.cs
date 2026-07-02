//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : UpgradeRuntimeEntry.cs
// brief  : 取得済み強化1件分のデータ
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System;
using UnityEngine;

[Serializable]
public class UpgradeRuntimeEntry
{
    [SerializeField] private SO_UpgradeCardData _cardData;
    [SerializeField] private int _level;

    public SO_UpgradeCardData CardData => _cardData;

    public int Level
    {
        get => _level;
        set => _level = Mathf.Max(0, value);
    }

    public UpgradeRuntimeEntry(SO_UpgradeCardData cardData, int level)
    {
        _cardData = cardData;
        _level = level;
    }
    
}
