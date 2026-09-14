using System.Collections.Generic;
using Game.Data.Collectibles;
using Game.Data.Player;

namespace Game.Gameplay.Roguelike.Effects
{
    public static class RoguelikeEffectRuntime
    {
        private sealed class ActiveUpgrade
        {
            public UpgradeData Data;
            public int Level;
        }

        private static readonly List<ActiveUpgrade> Active = new List<ActiveUpgrade>();
        private static CollectibleType? _lastHitType;
        private static int _sameTypeHitStreak;

        public static void Register(UpgradeData data, int level)
        {
            if (data == null)
                return;

            ActiveUpgrade entry = Active.Find(item => item.Data == data);
            if (entry == null)
            {
                entry = new ActiveUpgrade { Data = data };
                Active.Add(entry);
            }

            entry.Level = level;
        }

        public static IEnumerable<(T Module, int Level)> GetModules<T>() where T : RoguelikeEffectModule
        {
            foreach ((RoguelikeEffectModule module, int level) in GetModules())
                if (module is T typed)
                    yield return (typed, level);
        }

        public static IEnumerable<(RoguelikeEffectModule Module, int Level)> GetModules()
        {
            var latestByGroup = new Dictionary<string, RoguelikeEffectModule>();
            foreach (ActiveUpgrade active in Active)
            {
                if (active.Data == null || active.Data.Effects == null)
                    continue;
                foreach (RoguelikeEffectModule module in active.Data.Effects)
                {
                    if (module != null && module.Enabled && !string.IsNullOrEmpty(module.ExclusiveGroup))
                        latestByGroup[module.ExclusiveGroup] = module;
                }
            }

            foreach (ActiveUpgrade active in Active)
            {
                if (active.Data == null || active.Data.Effects == null)
                    continue;
                foreach (RoguelikeEffectModule module in active.Data.Effects)
                {
                    if (module == null || !module.Enabled)
                        continue;
                    if (!string.IsNullOrEmpty(module.ExclusiveGroup) && latestByGroup[module.ExclusiveGroup] != module)
                        continue;
                    yield return (module, active.Level);
                }
            }
        }

        // Candyは購入不要の基本アイテムとして常時出現させる（出現率アップLv1相当の扱い）。
        private static readonly CollectibleType FreeSpawnType = CollectibleType.Candy;

        public static float GetSpawnWeightMultiplier(CollectibleType type)
        {
            if (type != FreeSpawnType && !HasOwnedSpawnRateUp(type))
                return 0f;

            float weight = 1f;
            foreach ((RoguelikeEffectModule module, int level) in GetModules())
                weight = module.ModifySpawnWeight(type, _lastHitType, _sameTypeHitStreak, level, weight);

            return weight;
        }

        /// <summary>
        /// 指定種別の「出現率アップ」がLv1以上で取得済みかどうか。
        /// Candy以外の種別は、対応する出現率アップを一度も取得していない限り出現ウェイトが0になる
        /// （＝Lv0→1にした瞬間から出現し始める）。
        /// </summary>
        private static bool HasOwnedSpawnRateUp(CollectibleType type)
        {
            foreach ((RoguelikeEffectModule module, int level) in GetModules())
            {
                if (module is ItemSpawnRateUpEffect spawnRateUp && spawnRateUp.CollectibleType == type && level >= 1)
                    return true;
            }
            return false;
        }

        public static void RecordCollectibleHit(CollectibleType type)
        {
            if (_lastHitType.HasValue && _lastHitType.Value == type)
                _sameTypeHitStreak++;
            else
                _sameTypeHitStreak = 1;

            _lastHitType = type;
        }

        public static void Reset()
        {
            foreach ((RoguelikeEffectModule module, int _) in GetModules())
                module.ResetRuntimeState();
            Active.Clear();
            _lastHitType = null;
            _sameTypeHitStreak = 0;
        }
    }
}
