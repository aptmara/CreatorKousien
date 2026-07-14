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
    {
        if (card == null) return;

        UpgradeRuntimeEntry entry = _entries.Find(x => x.CardData == card);
        if(entry == null)
        {
            _entries.Add(new UpgradeRuntimeEntry(card, 1));
            return;
        }
        entry.Level = Mathf.Min(entry.Level + 1, card.MaxLevel);
    }

    public int GetLevel(UpgradeData card)
    {
        UpgradeRuntimeEntry entry = _entries.Find(x => x.CardData == card);
        return entry != null ? entry.Level : 0;
    }

    public bool IsAcquired(UpgradeData card)
        => GetLevel(card) > 0;

    public void Clear() => _entries.Clear();
}
