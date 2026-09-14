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
            Assert.That(RoguelikeUpgradeRuntime.Apply("20", 2, 0f), Is.True);

            Assert.That(RoguelikeUpgradeRuntime.CollectibleUnlockLevel, Is.EqualTo(2));
            Assert.That(RoguelikeUpgradeRuntime.CollectibleDamageMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.CollectibleScaleMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.AdditionalPumpkinDropCount, Is.EqualTo(20));
            Assert.That(RoguelikeUpgradeRuntime.CoinGainMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.RerollDiscountRate, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.ShopDiscountRate, Is.EqualTo(0.2f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.NormalEnemyDamageMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            // "20"(バリア耐久力アップ統合強化)が最後に上書きするため、強度/修復/最大HPは
            // BarrierAllUp系のLv2時点の値になる
            Assert.That(RoguelikeUpgradeRuntime.BarrierDefenseMultiplier, Is.EqualTo(1.44f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.PinchAttachmentMultiplier, Is.EqualTo(1.21f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond, Is.EqualTo(0.06f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierMaxHpMultiplier, Is.EqualTo(1.5625f).Within(0.0001f));
        }

        [Test]
        public void Apply_LevelTenKeepsExistingPerLevelGrowth()
        {
            RoguelikeUpgradeRuntime.Apply("4", 10, 1.2f);
            RoguelikeUpgradeRuntime.Apply("5", 10, 2f);
            RoguelikeUpgradeRuntime.Apply("6", 10, 1.25f);

            Assert.That(RoguelikeUpgradeRuntime.CollectibleDamageMultiplier, Is.EqualTo(6.191736f).Within(0.0001f));
            Assert.That(RoguelikeUpgradeRuntime.AdditionalPumpkinDropCount, Is.EqualTo(20));
            Assert.That(RoguelikeUpgradeRuntime.CoinGainMultiplier, Is.EqualTo(9.313226f).Within(0.0001f));
        }

        [Test]
        public void Apply_BarrierAllUp_GrowsAllThreeStatsTogether()
        {
            RoguelikeUpgradeRuntime.Apply("20", 10, 0f);

            Assert.That(RoguelikeUpgradeRuntime.BarrierDefenseMultiplier, Is.GreaterThan(1f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierRepairRatePerSecond, Is.GreaterThan(0f));
            Assert.That(RoguelikeUpgradeRuntime.BarrierMaxHpMultiplier, Is.GreaterThan(1f));
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
