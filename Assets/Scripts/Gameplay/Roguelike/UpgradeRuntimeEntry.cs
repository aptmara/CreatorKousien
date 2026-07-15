//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : UpgradeRuntimeEntry.cs
// brief  : 取得済み強化1件分のデータ
//
// auther : Takitani Shohei
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using Game.Data.Player;
using System;
using UnityEngine;

[Serializable]
public class UpgradeRuntimeEntry
{
    [SerializeField] private UpgradeData _cardData;
    [SerializeField] private int _level;

    public UpgradeData CardData => _cardData;

    public int Level
    {
        get => _level;
        set => _level = Mathf.Max(0, value);
    }

    public UpgradeRuntimeEntry(UpgradeData cardData, int level)
    {
        _cardData = cardData;
        _level = level;
    }
    
}
