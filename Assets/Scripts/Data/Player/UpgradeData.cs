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
// - 2026/09/07 - 常設ショップMENU化に伴い、Contract/Evolution/Relic/CombatPressureRule系を廃止 - 浅野
// ------------------------------------------------------------
using Game.Gameplay.Roguelike.Effects;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Player
{
    public enum UpgradeOfferType
    {
        Standard,
    }

    /// <summary>
    /// 1つの強化を表すマスターデータ。
    /// ショップ画面に常設表示し、選ばれたものをPlayerStatsServiceへ渡す。
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

        [SerializeReference, Tooltip("この強化が追加するゲームルール。新しい効果型は管理画面へ自動表示される")]
        public List<RoguelikeEffectModule> Effects = new List<RoguelikeEffectModule>();


        [Header("UI表示(ショップ画面用)")]
        [Tooltip("強化の最大回数")]
        public int MaxLevel = 5;

        [Tooltip("カード表示用アイコン")]
        public Sprite Icon;

        [Tooltip("強化の分類")]
        public UpgradeCategory Category;

        [Header("コスト(ショップでの購入用)")]
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

            return Description;
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
            string numericPreview = BuildNumericPreview(currentLevel, nextLevel);
            return string.IsNullOrEmpty(numericPreview)
                ? GetEffectText(nextLevel)
                : numericPreview;
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
                "20" => $"バリア耐久力  Lv.{currentLevel} → Lv.{nextLevel}（強度・修復・最大HP同時上昇）",
                _ => string.Empty,
            };
        }

        private string BuildMultiplierPreview(string label, int currentLevel, int nextLevel)
        {
            float multiplier = GameplayValue > 0f ? GameplayValue : 1f;
            return $"{label}  x{Mathf.Pow(multiplier, currentLevel):0.00} → x{Mathf.Pow(multiplier, nextLevel):0.00}";
        }

        private static string FormatPreviewValue(ModifierOperation operation, float value)
        {
            return operation == ModifierOperation.Multiply ? $"x{value:0.00}" : value.ToString("0.##");
        }
    }
}
