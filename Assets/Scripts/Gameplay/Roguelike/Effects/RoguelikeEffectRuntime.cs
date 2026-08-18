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

        public static float GetSpawnWeightMultiplier(CollectibleType type)
        {
            float weight = 1f;
            foreach ((RoguelikeEffectModule module, int level) in GetModules())
                weight = module.ModifySpawnWeight(type, _lastHitType, _sameTypeHitStreak, level, weight);

            return weight;
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
