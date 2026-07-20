using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class AssetLayoutValidator
    {
        private static readonly HashSet<string> AllowedRootDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CreatorKousien",
            "_Incoming",
            "ThirdParty",
            "AddressableAssetsData",
            "TextMesh Pro",
        };

        private static readonly HashSet<string> ForbiddenDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ma",
            "tex",
            "pre",
            "sharder",
            "before",
        };

        public static List<AssetLayoutIssue> Validate(bool includeLegacyRoots)
        {
            List<AssetLayoutIssue> issues = new List<AssetLayoutIssue>();
            string assetsPath = UnityEngine.Application.dataPath;
            foreach (string directory in Directory.EnumerateDirectories(assetsPath, "*", SearchOption.AllDirectories))
            {
                string name = Path.GetFileName(directory);
                string assetPath = "Assets" + directory.Substring(assetsPath.Length).Replace('\\', '/');
                if (ForbiddenDirectoryNames.Contains(name))
                {
                    issues.Add(Error(assetPath, $"Forbidden ambiguous folder name: {name}"));
                }

                if (Regex.IsMatch(name, @"\s+\d+$", RegexOptions.CultureInvariant))
                {
                    issues.Add(Error(assetPath, "Number-suffixed duplicate folder name is not allowed."));
                }
            }

            if (!includeLegacyRoots)
            {
                foreach (string root in Directory.EnumerateDirectories(assetsPath, "*", SearchOption.TopDirectoryOnly))
                {
                    string name = Path.GetFileName(root);
                    if (!AllowedRootDirectories.Contains(name))
                    {
                        issues.Add(Error($"Assets/{name}", "Asset root is outside the approved layout."));
                    }
                }
            }

            string incomingPath = Path.Combine(assetsPath, "_Incoming");
            if (Directory.Exists(incomingPath) && Directory.EnumerateFileSystemEntries(incomingPath, "*", SearchOption.AllDirectories).Any())
            {
                issues.Add(new AssetLayoutIssue
                {
                    Severity = PackageIssueSeverity.Warning,
                    Path = "Assets/_Incoming",
                    Message = "Incoming assets remain unpromoted.",
                });
            }

            return issues.OrderBy(issue => issue.Path, StringComparer.Ordinal).ToList();
        }

        [MenuItem("Tools/CreatorKousien/Validate Asset Layout")]
        private static void ValidateFromMenu()
        {
            List<AssetLayoutIssue> issues = Validate(true);
            if (issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Asset Layout", "No layout issues were found.", "OK");
                return;
            }

            foreach (AssetLayoutIssue issue in issues)
            {
                UnityEngine.Debug.LogWarning($"[Asset Layout] {issue.Path}: {issue.Message}");
            }

            EditorUtility.DisplayDialog("Asset Layout", $"Found {issues.Count} issue(s). See Console.", "OK");
        }

        private static AssetLayoutIssue Error(string path, string message)
        {
            return new AssetLayoutIssue
            {
                Severity = PackageIssueSeverity.Error,
                Path = path,
                Message = message,
            };
        }
    }
}
