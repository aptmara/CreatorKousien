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
using Game.Data.Collectibles;
using Game.Core.Roguelike;
using Game.Gameplay.Roguelike.Effects;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Player
{
    public enum UpgradeOfferType
    {
        Standard,
        CombatPressureRule,
        Relic,
        Contract,
        Evolution,
    }

    [System.Flags]
    public enum UpgradeSynergyTag
    {
        None = 0,
        Player = 1 << 0,
        Drop = 1 << 1,
        Combo = 1 << 2,
        Poison = 1 << 3,
        Ice = 1 << 4,
        AutoDrop = 1 << 5,
        Weight = 1 << 6,
        Giant = 1 << 7,
        Echo = 1 << 8,
        Economy = 1 << 9,
        Barrier = 1 << 10,
    }

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

        [Tooltip("取得レベルごとの説明。1番目がLv.1。未設定時はDescriptionを使用する")]
        [TextArea]
        public string[] LevelDescriptions;

        [Tooltip("カード枠内に表示するレベル別の短い説明。未設定時はLevelDescriptionsを使用する")]
        [TextArea]
        public string[] LevelCardDescriptions;

        [Header("Effects")]
        [Tooltip("この強化で適用するステータス変化（複数可）")]
        public StatModifier[] Modifiers;

        [Tooltip("プレイヤー以外のゲームシステムへ適用する強化値")]
        public float GameplayValue;

        [Header("Roguelike Build")]
        [Tooltip("ショップでの強化の扱い")]
        public UpgradeOfferType OfferType;

        [Tooltip("Combat Pressure Rule Set内のルールID")]
        public string CombatPressureRuleId;

        [Tooltip("Combat Pressureで上空から降らせる固定モデル")]
        public CollectibleType CombatPressureOutputType;

        [Tooltip("初回取得時に生成対象のモデルを選択する")]
        public bool RequiresCollectibleFocus;

        [Tooltip("取得済み強化との相性抽選に使用するタグ")]
        public UpgradeSynergyTag SynergyTags;

        [Tooltip("契約取得後に候補へ出にくくするタグ")]
        public UpgradeSynergyTag SuppressedTags;

        [Tooltip("抽選時の基礎ウェイト。1が標準")]
        [Min(0.001f)] public float DraftWeight = 1f;

        [SerializeReference, Tooltip("この強化が追加するゲームルール。新しい効果型は管理画面へ自動表示される")]
        public List<RoguelikeEffectModule> Effects = new List<RoguelikeEffectModule>();


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
            int index = Mathf.Max(1, level) - 1;
            if (LevelDescriptions != null &&
                index < LevelDescriptions.Length &&
                !string.IsNullOrWhiteSpace(LevelDescriptions[index]))
            {
                return LevelDescriptions[index];
            }

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

        public string GetCardText(int level)
        {
            int index = Mathf.Max(1, level) - 1;
            if (LevelCardDescriptions != null &&
                index < LevelCardDescriptions.Length &&
                !string.IsNullOrWhiteSpace(LevelCardDescriptions[index]))
            {
                return LevelCardDescriptions[index];
            }

            return GetEffectText(level);
        }

        public string GetTransitionText(int currentLevel, int levelGain = 1)
        {
            int nextLevel = Mathf.Clamp(currentLevel + Mathf.Max(1, levelGain), 1, MaxLevel);
            if (OfferType == UpgradeOfferType.Relic ||
                OfferType == UpgradeOfferType.Contract ||
                OfferType == UpgradeOfferType.Evolution)
            {
                return GetEffectText(nextLevel);
            }

            string numericPreview = BuildNumericPreview(currentLevel, nextLevel);
            return string.IsNullOrEmpty(numericPreview)
                ? GetEffectText(nextLevel)
                : numericPreview;
        }

        public UpgradeSynergyTag GetEffectiveTags()
        {
            if (SynergyTags != UpgradeSynergyTag.None)
                return SynergyTags;

            if (OfferType == UpgradeOfferType.CombatPressureRule)
            {
                return CombatPressureRuleId switch
                {
                    "combo-gummy" => UpgradeSynergyTag.Combo | UpgradeSynergyTag.AutoDrop,
                    "poison-field" => UpgradeSynergyTag.Poison | UpgradeSynergyTag.AutoDrop | UpgradeSynergyTag.Weight,
                    "ice-stack" => UpgradeSynergyTag.Ice | UpgradeSynergyTag.AutoDrop,
                    _ => UpgradeSynergyTag.AutoDrop,
                };
            }

            return Id switch
            {
                "5" => UpgradeSynergyTag.Drop | UpgradeSynergyTag.AutoDrop,
                "6" or "7" => UpgradeSynergyTag.Economy,
                "12" or "13" or "14" or "15" => UpgradeSynergyTag.Barrier,
                _ => Category == UpgradeCategory.Player
                    ? UpgradeSynergyTag.Player
                    : UpgradeSynergyTag.Drop,
            };
        }

        public string GetOfferLabel(bool deepening = false)
        {
            if (deepening) return "深化";
            return OfferType switch
            {
                UpgradeOfferType.Relic => "遺物",
                UpgradeOfferType.Contract => "契約",
                UpgradeOfferType.Evolution => "進化",
                UpgradeOfferType.CombatPressureRule => "ビルド",
                _ => "成長",
            };
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

        private string BuildNumericPreview(int currentLevel, int nextLevel)
        {
            if (OfferType == UpgradeOfferType.CombatPressureRule)
                return BuildCombatPressurePreview(currentLevel, nextLevel);

            if (Modifiers != null && Modifiers.Length > 0)
            {
                StatModifier modifier = Modifiers[0];
                float before = currentLevel <= 0 ? 1f : CalclateTotalValue(modifier, currentLevel);
                float after = CalclateTotalValue(modifier, nextLevel);
                return $"{GetStatDisplayName(modifier.TargetStat)}  {FormatPreviewValue(modifier.Operation, before)} → {FormatPreviewValue(modifier.Operation, after)}";
            }

            return Id switch
            {
                "4" => BuildMultiplierPreview("威力・サイズ", currentLevel, nextLevel),
                "5" => $"追加生成数  +{Mathf.RoundToInt(GameplayValue * currentLevel)} → +{Mathf.RoundToInt(GameplayValue * nextLevel)}",
                "6" => BuildMultiplierPreview("コイン獲得", currentLevel, nextLevel),
                "7" => $"再抽選割引  {Mathf.RoundToInt(Mathf.Clamp01(GameplayValue * currentLevel) * 100f)}% → {Mathf.RoundToInt(Mathf.Clamp01(GameplayValue * nextLevel) * 100f)}%",
                "10" => BuildMultiplierPreview("雑魚への威力", currentLevel, nextLevel),
                "12" => BuildMultiplierPreview("バリア強度", currentLevel, nextLevel),
                "13" => BuildMultiplierPreview("低耐久時の腕", currentLevel, nextLevel),
                "14" => $"毎秒修復  {Mathf.Max(0f, GameplayValue - 1f) * currentLevel * 100f:0.#}% → {Mathf.Max(0f, GameplayValue - 1f) * nextLevel * 100f:0.#}%",
                "15" => BuildMultiplierPreview("最大バリア", currentLevel, nextLevel),
                _ => string.Empty,
            };
        }

        private string BuildMultiplierPreview(string label, int currentLevel, int nextLevel)
        {
            float multiplier = GameplayValue > 0f ? GameplayValue : 1f;
            return $"{label}  x{Mathf.Pow(multiplier, currentLevel):0.00} → x{Mathf.Pow(multiplier, nextLevel):0.00}";
        }

        private string BuildCombatPressurePreview(int currentLevel, int nextLevel)
        {
            int beforeLevel = Mathf.Max(1, currentLevel);
            int baseThreshold;
            string condition;
            switch (CombatPressureRuleId)
            {
                case "combo-gummy":
                    baseThreshold = 50;
                    condition = "全体コンボ";
                    break;
                case "poison-field":
                    baseThreshold = 3;
                    condition = "毒付与累計";
                    break;
                case "ice-stack":
                    baseThreshold = 10;
                    condition = "凍結付与累計";
                    break;
                default:
                    return GetEffectText(nextLevel);
            }

            int beforeThreshold = GetPressureThreshold(baseThreshold, beforeLevel);
            int afterThreshold = GetPressureThreshold(baseThreshold, nextLevel);
            int beforeCount = currentLevel <= 0 ? 0 : currentLevel + 1;
            int afterCount = nextLevel + 1;
            string output = CollectibleTable.GetDisplayName(CombatPressureOutputType);
            return $"{condition} {beforeThreshold} → {afterThreshold}\n{output}降下 ×{beforeCount} → ×{afterCount}";
        }

        private static int GetPressureThreshold(int baseThreshold, int level)
        {
            return CombatPressureProgression.GetEffectiveThreshold(baseThreshold, level);
        }

        private static string FormatPreviewValue(ModifierOperation operation, float value)
        {
            return operation == ModifierOperation.Multiply ? $"x{value:0.00}" : value.ToString("0.##");
        }
    }
}
