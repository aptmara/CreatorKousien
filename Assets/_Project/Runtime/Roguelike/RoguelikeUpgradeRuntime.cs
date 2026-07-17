using System;
using UnityEngine;

namespace Game.Core.Roguelike
{
    /// <summary>
    /// 1ラン中に有効なローグライク強化値を保持する。
    /// </summary>
    public static class RoguelikeUpgradeRuntime
    {
        private const string AddDropItemId = "3";
        private const string DamageUpId = "4";
        private const string ItemSpawnUpId = "5";
        private const string GetCoinUpId = "6";
        private const string ShopCostDownId = "8";
        private const string NormalEnemyDamageUpId = "10";
        private const string BarrierHardUpId = "12";
        private const string PinchHandId = "13";
        private const string BarrierRepairId = "14";
        private const string BarrierLifeUpId = "15";

        private static bool _runtimeStateNeedsClear = true;

        public static event Action Changed;

        public static int CollectibleUnlockLevel { get; private set; }
        public static int AdditionalPumpkinDropCount { get; private set; }
        public static float CollectibleDamageMultiplier { get; private set; } = 1f;
        public static float CollectibleScaleMultiplier { get; private set; } = 1f;
        public static float CoinGainMultiplier { get; private set; } = 1f;
        public static float ShopDiscountRate { get; private set; }
        public static float NormalEnemyDamageMultiplier { get; private set; } = 1f;
        public static float BarrierDefenseMultiplier { get; private set; } = 1f;
        public static float PinchAttachmentMultiplier { get; private set; } = 1f;
        public static float BarrierRepairRatePerSecond { get; private set; }
        public static float BarrierMaxHpMultiplier { get; private set; } = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void InitializeOnLoad()
        {
            Changed = null;
            Reset();
        }

        public static void Reset()
        {
            CollectibleUnlockLevel = 0;
            AdditionalPumpkinDropCount = 0;
            CollectibleDamageMultiplier = 1f;
            CollectibleScaleMultiplier = 1f;
            CoinGainMultiplier = 1f;
            ShopDiscountRate = 0f;
            NormalEnemyDamageMultiplier = 1f;
            BarrierDefenseMultiplier = 1f;
            PinchAttachmentMultiplier = 1f;
            BarrierRepairRatePerSecond = 0f;
            BarrierMaxHpMultiplier = 1f;
            _runtimeStateNeedsClear = true;
            Changed?.Invoke();
        }

        public static bool ConsumeRuntimeStateClearRequest()
        {
            if (!_runtimeStateNeedsClear)
            {
                return false;
            }

            _runtimeStateNeedsClear = false;
            return true;
        }

        public static bool Apply(string upgradeId, int level, float value)
        {
            int validLevel = Mathf.Max(0, level);

            switch (upgradeId)
            {
                case AddDropItemId:
                    CollectibleUnlockLevel = validLevel;
                    break;
                case DamageUpId:
                    CollectibleDamageMultiplier = PowMultiplier(value, validLevel);
                    CollectibleScaleMultiplier = PowMultiplier(value, validLevel);
                    break;
                case ItemSpawnUpId:
                    AdditionalPumpkinDropCount = Mathf.Max(0, Mathf.RoundToInt(value * validLevel));
                    break;
                case GetCoinUpId:
                    CoinGainMultiplier = PowMultiplier(value, validLevel);
                    break;
                case ShopCostDownId:
                    ShopDiscountRate = Mathf.Clamp01(Mathf.Abs(value) * validLevel);
                    break;
                case NormalEnemyDamageUpId:
                    NormalEnemyDamageMultiplier = PowMultiplier(value, validLevel);
                    break;
                case BarrierHardUpId:
                    BarrierDefenseMultiplier = PowMultiplier(value, validLevel);
                    break;
                case PinchHandId:
                    PinchAttachmentMultiplier = PowMultiplier(value, validLevel);
                    break;
                case BarrierRepairId:
                    BarrierRepairRatePerSecond = Mathf.Max(0f, value - 1f) * validLevel;
                    break;
                case BarrierLifeUpId:
                    BarrierMaxHpMultiplier = PowMultiplier(value, validLevel);
                    break;
                default:
                    return false;
            }

            Changed?.Invoke();
            return true;
        }

        public static int GetDiscountedCost(int originalCost)
        {
            float multiplier = 1f - Mathf.Clamp01(ShopDiscountRate);
            return Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, originalCost) * multiplier));
        }

        public static bool IsCollectibleUnlocked(int collectibleTypeValue)
        {
            return collectibleTypeValue <= CollectibleUnlockLevel;
        }

        private static float PowMultiplier(float value, int level)
        {
            float multiplier = value > 0f ? value : 1f;
            return Mathf.Pow(multiplier, level);
        }
    }
}
