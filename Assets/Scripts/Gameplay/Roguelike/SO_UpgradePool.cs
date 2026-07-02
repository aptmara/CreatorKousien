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

[CreateAssetMenu(fileName = "SO_UpgradePool", menuName = "Scriptable Objects/SO_UpgradePool")]
public class SO_UpgradePool : ScriptableObject
{
    //______________________________
    // variables
    [SerializeField]
    private List<SO_UpgradeCardData> _upgrades = new();

    public IReadOnlyList<SO_UpgradeCardData> Upgrades => _upgrades;

    public int Count => _upgrades.Count;


    //______________________________
    // functions

    /// <summary>
    /// UpgradeIdから該当する強化データを取得
    /// </summary>
    /// <param name="upgradeId"></param>
    /// <returns></returns>
    public SO_UpgradeCardData GetById(string upgradeId)
    {
        return _upgrades.FirstOrDefault(
            x => x != null && x.UpgradeId == upgradeId);
    }

    /// <summary>
    /// 指定categoryの強化データ一覧を取得する関数 
    /// </summary>
    /// <param name="category">指定カテゴリー</param>
    /// <returns></returns>
    public List<SO_UpgradeCardData> GetByCategory(UpgradeCategory category)
    {
        //todo SO_UpgradeDataにCategoryフィールドを追加後、フィルタ処理に置き換え

        return _upgrades
            .Where(x => x != null && x.Category == category)
            .ToList();
    }

    /// <summary>
    /// 指定カテゴリーの強化データを取得する関数
    /// </summary>
    /// <param name="category">指定カテゴリー</param>
    /// <returns></returns>
    public int GetCountByCategory(UpgradeCategory category)
    {
        //todo SO_UpgradeDataにCategoryフィールドを追加後、カウント処理に置き換える

        return _upgrades.Count(x => x != null);
    }


    public List<SO_UpgradeCardData> GetAvailableUpgrades(SO_UpgradeRuntimeState runtimeState)
    {
        return _upgrades
            .Where(x => x != null)
            .Where(x => runtimeState.GetLevel(x) < x.MaxLevel)
            .ToList();
    }

}
