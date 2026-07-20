using System;
using System.IO;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class PackagePathUtility
    {
        private static readonly string[] BlockedExtensions =
        {
            ".cs",
            ".asmdef",
            ".asmref",
            ".dll",
            ".rsp",
            ".aar",
            ".jar",
        };

        public static bool TryNormalizeAssetPath(string sourcePath, out string normalized, out string error)
        {
            normalized = null;
            error = null;

            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                error = "Package pathname is empty.";
                return false;
            }

            string path = sourcePath.Trim().Replace('\\', '/').TrimEnd('/');
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) && path != "Assets")
            {
                error = $"Package path must be under Assets: {sourcePath}";
                return false;
            }

            string[] segments = path.Split('/');
            foreach (string segment in segments)
            {
                if (segment == "." || segment == ".." || string.IsNullOrWhiteSpace(segment))
                {
                    error = $"Package path contains an invalid segment: {sourcePath}";
                    return false;
                }
            }

            normalized = path;
            return true;
        }

        public static bool IsBlockedExtension(string path)
        {
            string extension = Path.GetExtension(path ?? string.Empty);
            foreach (string blockedExtension in BlockedExtensions)
            {
                if (string.Equals(extension, blockedExtension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string SanitizeSegment(string value, string fallback)
        {
            string source = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                source = source.Replace(invalid, '_');
            }

            source = source.Replace('/', '_').Replace('\\', '_').Trim();
            return string.IsNullOrWhiteSpace(source) ? fallback : source;
        }
    }
}
