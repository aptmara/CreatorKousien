using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core.Roguelike
{
    /// <summary>
    /// 1ラン中だけ有効な、数値強化ではない抽選・生成ルールを保持する。
    /// </summary>
    public static class RoguelikeRunRuleRuntime
    {
        public const string EchoRelicId = "relic_echo";
        public const string MomentumRelicId = "relic_momentum";
        public const string CrossFeedRelicId = "relic_crossfeed";
        public const string GluttonyContractId = "contract_gluttony";
        public const string DelayContractId = "contract_delay";
        public const string CompressionContractId = "contract_compression";
        public const string ResonanceEvolutionId = "evolution_resonance";
        public const string CataclysmEvolutionId = "evolution_cataclysm";
        public const string EndlessEchoEvolutionId = "evolution_endless_echo";

        private static readonly HashSet<string> AcquiredRules = new HashSet<string>();

        private static int? _lastHitType;
        private static int _sameTypeHitStreak;

        public static event Action Changed;

        public static int? LastHitType => _lastHitType;
        public static int SameTypeHitStreak => _sameTypeHitStreak;
        public static bool HasEcho => Has(EchoRelicId) || Has(EndlessEchoEvolutionId);
        public static bool EchoEveryTrigger => Has(EndlessEchoEvolutionId);
        public static bool HasMomentum => Has(MomentumRelicId);
        public static bool HasCrossFeed => Has(CrossFeedRelicId) || Has(ResonanceEvolutionId);
        public static int CrossFeedAmount => Has(ResonanceEvolutionId) ? 2 : 1;
        public static bool HasGluttony => Has(GluttonyContractId);
        public static bool HasDelayedRelease => Has(DelayContractId);
        public static bool HasCompression => Has(CompressionContractId);
        public static bool HasCataclysm => Has(CataclysmEvolutionId);

        public static bool Apply(string ruleId)
        {
            if (string.IsNullOrWhiteSpace(ruleId) || !AcquiredRules.Add(ruleId))
                return false;

            Changed?.Invoke();
            return true;
        }

        public static bool Has(string ruleId)
            => !string.IsNullOrWhiteSpace(ruleId) && AcquiredRules.Contains(ruleId);

        public static void RecordCollectibleHit(int type)
        {
            if (_lastHitType == type)
                _sameTypeHitStreak++;
            else
            {
                _lastHitType = type;
                _sameTypeHitStreak = 1;
            }
        }

        public static float GetSpawnWeightMultiplier(int type)
        {
            if (!_lastHitType.HasValue)
                return 1f;

            if (HasGluttony)
                return type == _lastHitType.Value ? 3.5f : 0.45f;

            if (HasMomentum && type == _lastHitType.Value)
                return 1f + Mathf.Min(2f, _sameTypeHitStreak * 0.2f);

            return 1f;
        }

        public static void Reset()
        {
            AcquiredRules.Clear();
            _lastHitType = null;
            _sameTypeHitStreak = 0;
            Changed?.Invoke();
        }
    }
}
