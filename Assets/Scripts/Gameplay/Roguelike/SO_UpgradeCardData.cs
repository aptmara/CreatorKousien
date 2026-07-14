//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_UpgradeData.cs
// brief  : ローグライク選択画面のカード表示データ.
//          SourceUpgradeをPlayerFacade.ApplyUpgrade()に渡すことで反映される
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;
using Game.Data.Player;

[CreateAssetMenu(fileName = "SO_UpgradeCardData", menuName = "Scriptable Objects/SO_UpgradeCardData")]
public class SO_UpgradeCardData : ScriptableObject
{
    //____________________________________
    // variables


    [Header("適応する実データ")]
    [Tooltip("選択時にPlayerFacade.ApplyUpgrade()へ渡す実際の効果データ(あさーののSO)")]
    [SerializeField] private Game.Data.Player.UpgradeData _sourceUpgrade;

    [Header("レベル")]
    [Tooltip("同じ強化を選べる最大回数(表示上のLv上限)")]
    [SerializeField] private int _maxLevel = 5;

    [Header("アイコン")]
    [SerializeField] private Sprite _icon;

    [Header("分類")]
    [SerializeField] private UpgradeCategory _category;

    [Header("コスト")]
    [SerializeField] private int _cost = 10;
    [SerializeField] private float _costMagni = 1.2f;


    // Id/DisplayName/DiscriptionをUpgradeDataから取得
    public string UpgradeId => _sourceUpgrade != null ? _sourceUpgrade.Id : string.Empty;
    public string DisplayName => _sourceUpgrade != null ? _sourceUpgrade.DisplayName : string.Empty;
    public string Description => _sourceUpgrade != null ? _sourceUpgrade.Description : string.Empty;


    public int MaxLevel => _maxLevel;
    public Sprite Icon => _icon;
    public UpgradeCategory Category => _category;
    public Game.Data.Player.UpgradeData SourceUpgrade => _sourceUpgrade;
    public int Cost => _cost;
    public float CostMagni => _costMagni;



    //____________________________________
    // public funtion

    /// <summary>
    /// 指定レベルでの効果説明テキストを組み立てる
    /// Add計はlevel倍、Multiply計はlevel回乗算した想定値を表示する
    /// </summary>
    /// <param name="level"></param>
    /// <returns></returns>
    public string GetEffectText(int level)
    {
        string result = Description;

        if(_sourceUpgrade == null || _sourceUpgrade.Modifiers == null)
        {
            return result;
        }

        //foreach(var modifier in _sourceUpgrade.Modifiers)
        //{
        //    string line = BuildModifierLine(modifier, level);
        //    if (string.IsNullOrEmpty(line)) continue;
        //    result += "\n" + line;
        //}

        return result;
    }



    //____________________________________
    // private funtion

    private string BuildModifierLine(StatModifier modifier, int level)
    {
        string statName = Description;
        float totalValue = CalclateTotalValue(modifier, level);
        string valueText = FormatValue(modifier.Operation, totalValue);

        return $"{statName} {valueText}";
    }

    /// <summary>
    /// レベル分を反映した想定値を計算する
    /// Add：1回分の値 * level
    /// Multiply：1回分の値をLevel回乗算した結果
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="level"></param>
    /// <returns></returns>
    private float CalclateTotalValue(StatModifier modifier, int level)
    {
        switch (modifier.Operation)
        {
            case ModifierOperation.Add:
                return modifier.Value * level;
            case ModifierOperation.Multiply:
                float result = 1.0f;
                for(int i = 0; i < level; ++i)
                {
                    result *= modifier.Value;
                }
                return result;

            default:
                return modifier.Value;
        }
    }

    private string FormatValue(ModifierOperation operation, float value)
    {
        switch (operation)
        {
            case ModifierOperation.Add:
                return value >= 0 ? $"+{value}" : value.ToString();
            case ModifierOperation.Multiply:
                return $"x{value:F2}";

            default:
                return value.ToString();
        }
    }

    private string GetStatDisplayName(PlayerStatType statType)
    {
        switch (statType)
        {
            case PlayerStatType.MaxHp:
                return "最大HP";
            case PlayerStatType.MoveSpeed:
                return "移動速度";
            case PlayerStatType.AttachmentScale:
                return "アタッチメント倍率";

            default:
                return statType.ToString();
        }
    }

}
