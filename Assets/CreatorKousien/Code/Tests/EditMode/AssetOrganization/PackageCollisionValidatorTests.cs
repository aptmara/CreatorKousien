using NUnit.Framework;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class PackageCollisionValidatorTests
    {
        [Test]
        public void ClassifyExistingAsset_SamePathAndHashes_IsInstalled()
        {
            PackageAssetStatus status = PackageCollisionValidator.ClassifyExistingAsset(
                "Assets/Enemy/Bat.mat",
                "asset-hash",
                "meta-hash",
                "Assets/Enemy/Bat.mat",
                "asset-hash",
                "meta-hash");

            Assert.That(status, Is.EqualTo(PackageAssetStatus.Installed));
        }

        [TestCase("changed-asset", "meta-hash")]
        [TestCase("asset-hash", "changed-meta")]
        public void ClassifyExistingAsset_SamePathButChangedContent_IsUpdateCandidate(
            string existingAssetHash,
            string existingMetaHash)
        {
            PackageAssetStatus status = PackageCollisionValidator.ClassifyExistingAsset(
                "Assets/Enemy/Bat.mat",
                "asset-hash",
                "meta-hash",
                "Assets/Enemy/Bat.mat",
                existingAssetHash,
                existingMetaHash);

            Assert.That(status, Is.EqualTo(PackageAssetStatus.UpdateCandidate));
        }

        [Test]
        public void ClassifyExistingAsset_SameGuidAtDifferentPath_IsConflict()
        {
            PackageAssetStatus status = PackageCollisionValidator.ClassifyExistingAsset(
                "Assets/Enemy/Bat.mat",
                "asset-hash",
                "meta-hash",
                "Assets/Shared/Bat.mat",
                "asset-hash",
                "meta-hash");

            Assert.That(status, Is.EqualTo(PackageAssetStatus.Conflict));
        }
    }
}
