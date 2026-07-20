using System;
using System.Linq;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class IncomingAssetPromotion
    {
        [MenuItem("Tools/CreatorKousien/Promote Selected Incoming Assets", priority = 12)]
        private static void Open()
        {
            string[] paths = Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/_Incoming/", StringComparison.Ordinal))
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            PackageIntakeWindow.OpenIncomingSelection(paths);
        }

        [MenuItem("Tools/CreatorKousien/Promote Selected Incoming Assets", true)]
        private static bool ValidateOpen()
        {
            return Selection.assetGUIDs
                .Select(AssetDatabase.GUIDToAssetPath)
                .Any(path => path.StartsWith("Assets/_Incoming/", StringComparison.Ordinal)
                    && !AssetDatabase.IsValidFolder(path));
        }
    }
}
