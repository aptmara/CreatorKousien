using NUnit.Framework;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class PackagePathUtilityTests
    {
        [Test]
        public void TryNormalizeAssetPath_RejectsTraversal()
        {
            bool result = PackagePathUtility.TryNormalizeAssetPath("Assets/Enemy/../Scripts/Bad.cs", out _, out _);

            Assert.That(result, Is.False);
        }

        [TestCase("Assets/Test.cs")]
        [TestCase("Assets/Test.asmdef")]
        [TestCase("Assets/Plugins/Test.dll")]
        public void IsBlockedExtension_BlocksCodeAndPlugins(string path)
        {
            Assert.That(PackagePathUtility.IsBlockedExtension(path), Is.True);
        }

        [Test]
        public void BuildIncomingRoot_UsesDomainAndEntity()
        {
            string result = PackagePlacementPlanner.BuildIncomingRoot(AssetDomain.Enemies, "Bat", "Enemy_Bat_v2");

            Assert.That(result, Is.EqualTo("Assets/_Incoming/Enemies/Bat/Enemy_Bat_v2"));
        }

        [Test]
        public void GetFormalRoot_PlacesBossUnderBosses()
        {
            string result = PackagePlacementPlanner.GetFormalRoot(AssetDomain.Bosses, "Witch");

            Assert.That(result, Is.EqualTo("Assets/CreatorKousien/Content/Features/Enemies/Bosses/Witch"));
        }

        [TestCase(AssetDomain.Audio, "Assets/CreatorKousien/Content/Presentation/Audio")]
        [TestCase(AssetDomain.Camera, "Assets/CreatorKousien/Content/Presentation/Camera")]
        [TestCase(AssetDomain.VFX, "Assets/CreatorKousien/Content/Presentation/SharedVFX")]
        [TestCase(AssetDomain.Development, "Assets/CreatorKousien/Content/Development")]
        public void GetFormalRoot_MapsPresentationAndDevelopmentDomains(AssetDomain domain, string expected)
        {
            Assert.That(PackagePlacementPlanner.GetFormalRoot(domain, "Shared"), Is.EqualTo(expected));
        }
    }
}
