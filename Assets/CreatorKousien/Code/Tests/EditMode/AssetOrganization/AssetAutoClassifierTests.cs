using NUnit.Framework;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class AssetAutoClassifierTests
    {
        [Test]
        public void ClassifyPackageRecord_DetectsNamedBoss()
        {
            AssetClassificationSuggestion result = Classify("Assets/Boss/JackFlower/PF_Boss_JackFlower.prefab");

            Assert.That(result.Domain, Is.EqualTo(AssetDomain.Bosses));
            Assert.That(result.Entity, Is.EqualTo("JackFlower"));
            Assert.That(result.Category, Is.EqualTo("Prefabs"));
            Assert.That(result.Confidence, Is.EqualTo(ClassificationConfidence.High));
        }

        [Test]
        public void ClassifyPackageRecord_KeepsEnemyVfxUnderEnemy()
        {
            AssetClassificationSuggestion result = Classify("Assets/Enemy/Bat/VFX/TEX_Bat_Hit.png");

            Assert.That(result.Domain, Is.EqualTo(AssetDomain.Enemies));
            Assert.That(result.Entity, Is.EqualTo("Bat"));
            Assert.That(result.Category, Is.EqualTo("VFX/Textures"));
        }

        [TestCase("Assets/UI/Result/UI_Result_Button.png", AssetDomain.UI, "Result", "Textures")]
        [TestCase("Assets/Sound/BGM/BGM_Battle.wav", AssetDomain.Audio, "Shared", "Audio")]
        [TestCase("Assets/Stage/Stage02/Models/Stage02.fbx", AssetDomain.Stage, "Stage02", "Models")]
        public void ClassifyPackageRecord_DetectsCommonDomains(
            string path,
            AssetDomain expectedDomain,
            string expectedEntity,
            string expectedCategory)
        {
            AssetClassificationSuggestion result = Classify(path);

            Assert.That(result.Domain, Is.EqualTo(expectedDomain));
            Assert.That(result.Entity, Is.EqualTo(expectedEntity));
            Assert.That(result.Category, Is.EqualTo(expectedCategory));
        }

        [Test]
        public void ClassifyPackageRecord_LeavesAmbiguousAssetForReview()
        {
            AssetClassificationSuggestion result = Classify("Assets/Misc/blob.bytes");

            Assert.That(result.Domain, Is.EqualTo(AssetDomain.Shared));
            Assert.That(result.Confidence, Is.EqualTo(ClassificationConfidence.Low));
            Assert.That(result.Selected, Is.False);
            Assert.That(result.RequiresReview, Is.True);
        }

        private static AssetClassificationSuggestion Classify(string path)
        {
            return AssetAutoClassifier.ClassifyPackageRecord(new UnityPackageAssetRecord
            {
                SourcePath = path,
                Guid = "1234567890abcdef1234567890abcdef",
                HasAsset = true,
                HasMeta = true,
            });
        }
    }
}
