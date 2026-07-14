//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_UpgradePool.cs
// brief  : 全てのSO_UpgradeDataの一覧を保持するデータベースSO.
//
// auther : Shohei Takitani
// date   : 2026/07/01 - begin
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Game.Data.Player;

[CreateAssetMenu(fileName = "SO_UpgradePool", menuName = "Scriptable Objects/SO_UpgradePool")]
public class SO_UpgradePool : ScriptableObject
{
    //______________________________
    // variables
    [SerializeField]
    private List<UpgradeData> _upgrades = new();

    public IReadOnlyList<UpgradeData> Upgrades => _upgrades;

    public int Count => _upgrades.Count;




    //______________________________
    // functions

    /// <summary>
    /// UpgradeIdから該当する強化データを取得
    /// </summary>
    /// <param name="upgradeId"></param>
    /// <returns></returns>
    public UpgradeData GetById(string upgradId)
        => _upgrades.FirstOrDefault(x => x != null && x.Id == upgradId);

    /// <summary>
    /// 指定categoryの強化データ一覧を取得する関数 
    /// </summary>
    /// <param name="category">指定カテゴリー</param>
    /// <returns></returns>
    public List<UpgradeData> GetByCategory(UpgradeCategory category)
        => _upgrades.Where(x => x != null && x.Category == category).ToList();

    /// <summary>
    /// 指定カテゴリーの強化データを取得する関数
    /// </summary>
    /// <param name="category">指定カテゴリー</param>
    /// <returns></returns>
    public int GetCountByCategory(UpgradeCategory category)
    => _upgrades.Count(x => x != null && x.Category == category);


    public List<UpgradeData> GetAvailableUpgrades(SO_UpgradeRuntimeState runtimeState)
        => _upgrades.Where(x => x != null)
            .Where(x => runtimeState.GetLevel(x) < x.MaxLevel)
            .ToList();
}
