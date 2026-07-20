using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class AssetDependencySnapshot
    {
        public static HashSet<string> CaptureGuids(string assetPath)
        {
            string[] dependencies = AssetDatabase.GetDependencies(assetPath, true);
            return dependencies
                .Select(AssetDatabase.AssetPathToGUID)
                .Where(guid => !string.IsNullOrWhiteSpace(guid))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        public static bool AreEqual(HashSet<string> before, HashSet<string> after)
        {
            return before != null && after != null && before.SetEquals(after);
        }
    }
}
