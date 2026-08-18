using System;
using System.Collections.Generic;

namespace Game.Core.Roguelike
{
    /// <summary>
    /// 1ラン中に取得したビルドエンジンと、プレイヤーが選んだ特化モデルを保持する。
    /// </summary>
    public static class RoguelikeBuildRuntime
    {
        private sealed class CombatRuleState
        {
            public int Level;
            public int FocusedCollectibleType = -1;
        }

        private static readonly Dictionary<string, CombatRuleState> CombatRules =
            new Dictionary<string, CombatRuleState>();

        public static event Action Changed;

        public static void SetCombatRule(string ruleId, int level, int? focusedCollectibleType = null)
        {
            if (string.IsNullOrWhiteSpace(ruleId) || level <= 0)
                return;

            if (!CombatRules.TryGetValue(ruleId, out CombatRuleState state))
            {
                state = new CombatRuleState();
                CombatRules[ruleId] = state;
            }

            state.Level = level;
            if (focusedCollectibleType.HasValue && focusedCollectibleType.Value >= 0)
                state.FocusedCollectibleType = focusedCollectibleType.Value;

            Changed?.Invoke();
        }

        public static bool IsCombatRuleAcquired(string ruleId)
            => GetCombatRuleLevel(ruleId) > 0;

        public static int GetCombatRuleLevel(string ruleId)
        {
            return !string.IsNullOrWhiteSpace(ruleId) &&
                   CombatRules.TryGetValue(ruleId, out CombatRuleState state)
                ? state.Level
                : 0;
        }

        public static int? GetFocusedCollectibleType(string ruleId)
        {
            return !string.IsNullOrWhiteSpace(ruleId) &&
                   CombatRules.TryGetValue(ruleId, out CombatRuleState state) &&
                   state.FocusedCollectibleType >= 0
                ? state.FocusedCollectibleType
                : (int?)null;
        }

        public static void Reset()
        {
            CombatRules.Clear();
            Changed?.Invoke();
        }
    }
}
