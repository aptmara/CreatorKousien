using System.Linq;
using NUnit.Framework;

namespace CreatorKousien.Editor.AssetOrganization.Tests
{
    public sealed class DirectImportDiffAnalyzerTests
    {
        [Test]
        public void Compare_SeparatesNewModifiedAndFolderPaths()
        {
            DirectImportSnapshot before = new DirectImportSnapshot();
            before.Assets.Add(State("Assets/Existing.mat", "a", 10, 100, false));
            before.Assets.Add(State("Assets/Unchanged.mat", "b", 20, 200, false));

            DirectImportSnapshot after = new DirectImportSnapshot();
            after.Assets.Add(State("Assets/Existing.mat", "a", 11, 300, false));
            after.Assets.Add(State("Assets/Unchanged.mat", "b", 20, 200, false));
            after.Assets.Add(State("Assets/Imported", "folder", 0, 0, true));
            after.Assets.Add(State("Assets/Imported/New.prefab", "c", 30, 400, false));

            DirectImportDiff result = DirectImportDiffAnalyzer.Compare(before, after);

            Assert.That(result.NewAssetPaths.Single(), Is.EqualTo("Assets/Imported/New.prefab"));
            Assert.That(result.NewFolderPaths.Single(), Is.EqualTo("Assets/Imported"));
            Assert.That(result.ModifiedExistingPaths.Single(), Is.EqualTo("Assets/Existing.mat"));
        }

        private static DirectImportAssetState State(string path, string guid, long length, long ticks, bool folder)
        {
            return new DirectImportAssetState
            {
                Path = path,
                Guid = guid,
                Length = length,
                WriteUtcTicks = ticks,
                MetaLength = length,
                MetaWriteUtcTicks = ticks,
                IsFolder = folder,
            };
        }
    }
}
