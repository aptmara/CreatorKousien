//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_UpgradeRuntimeState.cs
// brief  : 現在取得済みの強化とレベルを保存するランタイムSO
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System.Collections.Generic;
using Game.Data.Player;
using Game.Gameplay.Roguelike.Effects;

[CreateAssetMenu(fileName = "SO_UpgradeRuntimeState", menuName = "Scriptable Objects/SO_UpgradeRuntimeState")]
public class SO_UpgradeRuntimeState : ScriptableObject
{
    [SerializeField]
    private List<UpgradeRuntimeEntry> _entries = new();

    public IReadOnlyList<UpgradeRuntimeEntry> Entries => _entries;

    public int AcquiredCount => _entries.Count;

    /// <summary>
    /// 取得済みリストに追加、既存ならレベルを1上げる
    /// ※実ステータスへの反映は行わない
    /// </summary>
    /// <param name="card"></param>
    public void AddOrLevelUp(UpgradeData card)
        => AddLevels(card, 1);

    public int AddLevels(UpgradeData card, int amount)
    {
        if (card == null || amount <= 0) return 0;

        UpgradeRuntimeEntry entry = _entries.Find(x => x.CardData == card);
        if(entry == null)
        {
            int added = Mathf.Min(amount, card.MaxLevel);
            _entries.Add(new UpgradeRuntimeEntry(card, added));
            return added;
        }

        int previousLevel = entry.Level;
        entry.Level = Mathf.Min(entry.Level + amount, card.MaxLevel);
        return entry.Level - previousLevel;
    }

    public int GetLevel(UpgradeData card)
    {
        UpgradeRuntimeEntry entry = _entries.Find(x => x.CardData == card);
        return entry != null ? entry.Level : 0;
    }

    public bool IsAcquired(UpgradeData card)
        => GetLevel(card) > 0;

    public void Clear()
    {
        _entries.Clear();
        RoguelikeEffectRuntime.Reset();
    }
}
