using UnityEngine;
using Game.Gameplay.Roguelike.Effects;
using Game.Core.Roguelike;

namespace Game.Gameplay.Roguelike.CombatPressure
{
    /// <summary>
    /// シーンに手作業でコンポーネントを置かなくても、ゲームプレイ開始時にCombat Pressureを起動する。
    /// </summary>
    public static class CombatPressureBootstrap
    {
        private const string DefaultRuleSetPath = "CombatPressure/SO_CombatPressure_Default";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            CombatPressurePlayerModifiers.Reset();
            CombatPressureSpawnWeights.Reset();
            RoguelikeEffectRuntime.Reset();
            CombatPressureProgression.ResetDefaults();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForGameplayScene()
        {
            if (Object.FindFirstObjectByType<CombatPressureController>() != null)
                return;

            SO_RoguelikeBalanceConfig config = SO_RoguelikeBalanceConfig.LoadDefault();
            config?.CombatPressureProgression.Apply();
            CombatPressureRuleSet ruleSet = config != null
                ? config.CombatPressureRuleSet
                : Resources.Load<CombatPressureRuleSet>(DefaultRuleSetPath);
            if (ruleSet == null)
                return;

            var root = new GameObject("CombatPressureRuntime");
            Object.DontDestroyOnLoad(root);
            CombatPressureController controller = root.AddComponent<CombatPressureController>();
            controller.Initialize(ruleSet);
        }
    }
}
