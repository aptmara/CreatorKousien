using Game.Core.Roguelike;
using NUnit.Framework;

namespace Game.Tests
{
    public sealed class RoguelikeUpgradeRuntimeTests
    {
        [SetUp]
        public void SetUp()
        {
            RoguelikeUpgradeRuntime.Reset();
        }

        [Test]
        public void Apply_ConfiguresImplementedUpgradeEffects()
        {
            Assert.That(RoguelikeUpgradeRuntime.Apply("3", 2, 1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("4", 2, 1.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("5", 2, 10f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("6", 2, 1.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("7", 2, 0.2f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("8", 2, 0.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("10", 2, 1.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("12", 2, 1.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("13", 2, 1.1f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("14", 2, 1.01f), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.Apply("15", 2, 1.1f), Is.True);

            Assert.That(RoguelikeUpgradeRuntime.CollectibleUnlockLevel, Is.EqualTo(2));
            Assert.That(RoguelikeUpgradeRuntime.CollectibleDamageMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.CollectibleScaleMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.AdditionalPumpkinDropCount, Is.EqualTo(20));
            Assert.That(RoguelikeUpgradeRuntime.CoinGainMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.RerollDiscountRate, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.ShopDiscountRate, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.NormalEnemyDamageMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierDefenseMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.PinchAttachmentMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond, Is.EqualTo(0.02f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierMaxHpMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
        }

        [Test]
        public void Apply_LevelTenKeepsExistingPerLevelGrowth()
        {
            RoguelikeUpgradeRuntime.Apply("4", 10, 1.2f);
            RoguelikeUpgradeRuntime.Apply("5", 10, 2f);
            RoguelikeUpgradeRuntime.Apply("6", 10, 1.25f);
            RoguelikeUpgradeRuntime.Apply("14", 10, 1.03f);
            RoguelikeUpgradeRuntime.Apply("15", 10, 1.25f);

            Assert.That(RoguelikeUpgradeRuntime.CollectibleDamageMultiplier, Is.EqualTo(6.191736f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.AdditionalPumpkinDropCount, Is.EqualTo(20));
            Assert.That(RoguelikeUpgradeRuntime.CoinGainMultiplier, Is.EqualTo(9.313226f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond, Is.EqualTo(0.3f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierMaxHpMultiplier, Is.EqualTo(9.313226f).Within(0.0001f));
        }

        [Test]
        public void RunRules_GluttonyTracksActualHitAndNarrowsWeights()
        {
            RoguelikeRunRuleRuntime.Apply(RoguelikeRunRuleRuntime.GluttonyContractId);
            RoguelikeRunRuleRuntime.RecordCollectibleHit(2);

            Assert.That(RoguelikeRunRuleRuntime.GetSpawnWeightMultiplier(2), Is.EqualTo(3.5f));
            Assert.That(RoguelikeRunRuleRuntime.GetSpawnWeightMultiplier(3), Is.EqualTo(0.45f));
        }

        [Test]
        public void RunRules_EvolutionChangesConnectionRulesAndResetClearsThem()
        {
            RoguelikeRunRuleRuntime.Apply(RoguelikeRunRuleRuntime.EndlessEchoEvolutionId);
            RoguelikeRunRuleRuntime.Apply(RoguelikeRunRuleRuntime.ResonanceEvolutionId);

            Assert.That(RoguelikeRunRuleRuntime.HasEcho, Is.True);
            Assert.That(RoguelikeRunRuleRuntime.EchoEveryTrigger, Is.True);
            Assert.That(RoguelikeRunRuleRuntime.CrossFeedAmount, Is.EqualTo(2));

            RoguelikeUpgradeRuntime.Reset();

            Assert.That(RoguelikeRunRuleRuntime.HasEcho, Is.False);
            Assert.That(RoguelikeRunRuleRuntime.HasCrossFeed, Is.False);
        }

        [Test]
        public void GetDiscountedCost_UsesPercentageAndRoundsUp()
        {
            RoguelikeUpgradeRuntime.Apply("8", 3, 0.1f);

            Assert.That(RoguelikeUpgradeRuntime.GetDiscountedCost(101), Is.EqualTo(71));
        }

        [Test]
        public void GetRerollCost_UsesRerollDiscountAndRoundsUp()
        {
            RoguelikeUpgradeRuntime.Apply("7", 2, 0.2f);

            Assert.That(RoguelikeUpgradeRuntime.GetRerollCost(51), Is.EqualTo(31));
        }

        [Test]
        public void BuildState_KeepsRuleLevelFocusAndExactUnlockUntilReset()
        {
            RoguelikeBuildRuntime.SetCombatRule("combo-gummy", 2, 5);
            RoguelikeUpgradeRuntime.UnlockCollectible(5);

            Assert.That(RoguelikeBuildRuntime.GetCombatRuleLevel("combo-gummy"), Is.EqualTo(2));
            Assert.That(RoguelikeBuildRuntime.GetFocusedCollectibleType("combo-gummy"), Is.EqualTo(5));
            Assert.That(RoguelikeUpgradeRuntime.IsCollectibleUnlocked(5), Is.True);

            RoguelikeUpgradeRuntime.Reset();

            Assert.That(RoguelikeBuildRuntime.IsCombatRuleAcquired("combo-gummy"), Is.False);
            Assert.That(RoguelikeUpgradeRuntime.IsCollectibleUnlocked(5), Is.False);
        }

        [Test]
        public void CombatPressureProgression_UnlocksQualitativeEffectsByLevel()
        {
            Assert.That(CombatPressureProgression.GetEffectiveThreshold(50, 1), Is.EqualTo(50));
            Assert.That(CombatPressureProgression.GetEffectiveThreshold(50, 2), Is.EqualTo(43));
            Assert.That(CombatPressureProgression.GetEffectiveThreshold(50, 3), Is.EqualTo(35));

            Assert.That(CombatPressureProgression.GetComboRecoverySeconds(1, 4), Is.Zero);
            Assert.That(CombatPressureProgression.GetComboRecoverySeconds(2, 4), Is.EqualTo(1f));
            Assert.That(CombatPressureProgression.GetComboEchoSpawnCount(2, 6), Is.Zero);
            Assert.That(CombatPressureProgression.GetComboEchoSpawnCount(3, 7), Is.EqualTo(2));
            Assert.That(CombatPressureProgression.GetComboEchoRemainder(3, 7), Is.EqualTo(1));

            Assert.That(CombatPressureProgression.GetCompletedCycles(2, 1, 3), Is.EqualTo(1));
            Assert.That(CombatPressureProgression.GetRemainingProgress(2, 1, 3), Is.Zero);
            Assert.That(CombatPressureProgression.GetCompletedCycles(2, 5, 3), Is.EqualTo(2));
            Assert.That(CombatPressureProgression.GetRemainingProgress(2, 5, 3), Is.EqualTo(1));
            Assert.That(CombatPressureProgression.GetPoisonDefeatSpawnCount(2, true), Is.Zero);
            Assert.That(CombatPressureProgression.GetPoisonDefeatSpawnCount(3, true), Is.EqualTo(4));

            Assert.That(CombatPressureProgression.GetIceBreakSpawnCount(1), Is.Zero);
            Assert.That(CombatPressureProgression.GetIceBreakSpawnCount(2), Is.EqualTo(3));
            Assert.That(CombatPressureProgression.GetIceBreakSpawnCount(3), Is.EqualTo(6));
        }

        [Test]
        public void CombatPressureProgression_UsesPlannerConfiguredValuesAndResetRestoresDefaults()
        {
            CombatPressureProgression.Configure(
                0.1f,
                3,
                0.5f,
                4,
                2,
                4,
                7,
                3,
                5,
                5,
                9,
                1,
                5);

            Assert.That(CombatPressureProgression.GetEffectiveThreshold(50, 3), Is.EqualTo(40));
            Assert.That(CombatPressureProgression.GetComboRecoverySeconds(2, 4), Is.Zero);
            Assert.That(CombatPressureProgression.GetComboRecoverySeconds(3, 4), Is.EqualTo(2f));
            Assert.That(CombatPressureProgression.GetComboEchoSpawnCount(4, 7), Is.EqualTo(3));
            Assert.That(CombatPressureProgression.GetPoisonDefeatSpawnCount(4, true), Is.EqualTo(7));
            Assert.That(CombatPressureProgression.GetIceBreakSpawnCount(3), Is.EqualTo(5));
            Assert.That(CombatPressureProgression.GetIceBreakSpawnCount(5), Is.EqualTo(9));
            Assert.That(CombatPressureProgression.GetAcquisitionPreviewSpawnCount(4), Is.EqualTo(5));

            RoguelikeUpgradeRuntime.Reset();

            Assert.That(CombatPressureProgression.GetEffectiveThreshold(50, 2), Is.EqualTo(43));
            Assert.That(CombatPressureProgression.GetPoisonDefeatSpawnCount(3, true), Is.EqualTo(4));
        }

        [Test]
        public void Reset_RestoresDefaultValuesAndRequestsStateClear()
        {
            RoguelikeUpgradeRuntime.Apply("4", 3, 1.1f);
            RoguelikeUpgradeRuntime.ConsumeRuntimeStateClearRequest();

            RoguelikeUpgradeRuntime.Reset();

            Assert.That(RoguelikeUpgradeRuntime.CollectibleDamageMultiplier, Is.EqualTo(1f));
            Assert.That(RoguelikeUpgradeRuntime.ShopDiscountRate, Is.EqualTo(0f));
            Assert.That(RoguelikeUpgradeRuntime.RerollDiscountRate, Is.EqualTo(0f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond, Is.EqualTo(0f));
            Assert.That(RoguelikeUpgradeRuntime.ConsumeRuntimeStateClearRequest(), Is.True);
            Assert.That(RoguelikeUpgradeRuntime.ConsumeRuntimeStateClearRequest(), Is.False);
        }

        [TestCase("9")]
        [TestCase("11")]
        public void Apply_DoesNotImplementExcludedUpgrades(string upgradeId)
        {
            Assert.That(RoguelikeUpgradeRuntime.Apply(upgradeId, 1, 1.1f), Is.False);
        }
    }
}
