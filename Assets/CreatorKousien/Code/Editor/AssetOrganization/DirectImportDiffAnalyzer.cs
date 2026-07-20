using System;
using System.Collections.Generic;
using System.Linq;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class DirectImportDiffAnalyzer
    {
        public static DirectImportDiff Compare(DirectImportSnapshot before, DirectImportSnapshot after)
        {
            if (before == null)
            {
                throw new ArgumentNullException(nameof(before));
            }

            if (after == null)
            {
                throw new ArgumentNullException(nameof(after));
            }

            Dictionary<string, DirectImportAssetState> beforeByPath = before.Assets
                .ToDictionary(asset => asset.Path, StringComparer.OrdinalIgnoreCase);
            DirectImportDiff diff = new DirectImportDiff();
            foreach (DirectImportAssetState current in after.Assets)
            {
                if (!beforeByPath.TryGetValue(current.Path, out DirectImportAssetState previous))
                {
                    if (current.IsFolder)
                    {
                        diff.NewFolderPaths.Add(current.Path);
                    }
                    else
                    {
                        diff.NewAssetPaths.Add(current.Path);
                    }

                    continue;
                }

                if (!current.IsFolder && HasChanged(previous, current))
                {
                    diff.ModifiedExistingPaths.Add(current.Path);
                }
            }

            diff.NewAssetPaths.Sort(StringComparer.Ordinal);
            diff.NewFolderPaths.Sort(StringComparer.Ordinal);
            diff.ModifiedExistingPaths.Sort(StringComparer.Ordinal);
            return diff;
        }

        private static bool HasChanged(DirectImportAssetState before, DirectImportAssetState after)
        {
            return !string.Equals(before.Guid, after.Guid, StringComparison.OrdinalIgnoreCase)
                || before.Length != after.Length
                || before.WriteUtcTicks != after.WriteUtcTicks
                || before.MetaLength != after.MetaLength
                || before.MetaWriteUtcTicks != after.MetaWriteUtcTicks;
        }
    }
}
