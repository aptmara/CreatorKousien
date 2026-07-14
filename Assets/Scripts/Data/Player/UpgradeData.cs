// ------------------------------------------------------------
// File		: UpgradeData.cs
// Summary	: プレイヤーのアップグレードデータを管理するSO
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - アップグレードデータの作成
// - 2026/07/14 - SO_UpgradeCardDataと統合、UI/コスト等の情報を集約 - 滝谷
// ------------------------------------------------------------
using System.Transactions;
using UnityEngine;

namespace Game.Data.Player
{
    /// <summary>
    /// 1つの強化を表すマスターデータ。
    /// ローグライク担当が候補として提示し、選ばれたものをPlayerStatsServiceへ渡す。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Upgrade_New", menuName = "Game/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("アップグレードデータ")]
        [Tooltip("アップグレードの一意なID")]
        public string Id;

        [Tooltip("表示名（UI用）")]
        public string DisplayName;

        [Tooltip("説明文（UI用）")]
        [TextArea]
        public string Description;

        [Header("Effects")]
        [Tooltip("この強化で適用するステータス変化（複数可）")]
        public StatModifier[] Modifiers;

        [Header("UI表示(ローグライク選択画面用)")]
        [Tooltip("強化の最大回数")]
        public int MaxLevel = 5;

        [Tooltip("カード表示用アイコン")]
        public Sprite Icon;

        [Tooltip("強化の分類")]
        public UpgradeCategory Category;

        [Header("コスト(ショップでの選択画面用)")]
        public int Cost = 10;
        [Tooltip("コスト倍率")]
        public float CostMagni = 1.2f;



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

            if (Modifiers == null)  return result;

            //foreach(var modifier in _sourceUpgrade.Modifiers)
            //{
            //    string line = BuildModifierLine(modifier, level);
            //    if (string.IsNullOrEmpty(line)) continue;
            //    result += "\n" + line;
            //}

            return result;
        }

        public int GetCost(int currentLevel)
        {
            float magni = 0.0f;
            // レベルが初期値なら倍率を計算しない
            if(currentLevel == 0)
            {
                magni = 1.0f;
            }
            else
            {
                magni = CostMagni * currentLevel;
            }
            return (int)(Cost * magni);
        }

        //____________________________________
        // private funtion

        private string BuildModifierLine(StatModifier modifier, int level)
        {
            string statName = GetStatDisplayName(modifier.TargetStat);
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
                    for (int i = 0; i < level; ++i)
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
}
