using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class PackageCollisionValidator
    {
        public static List<PackageIssue> Validate(PackageInspection inspection, string incomingRoot)
        {
            if (inspection == null)
            {
                throw new ArgumentNullException(nameof(inspection));
            }

            List<PackageIssue> issues = new List<PackageIssue>();
            HashSet<string> packageGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> packagePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (UnityPackageAssetRecord record in inspection.Assets)
            {
                ResetAssessment(record);
                if (!PackagePathUtility.TryNormalizeAssetPath(record.SourcePath, out string normalizedPath, out string pathError))
                {
                    Block(record, issues, pathError);
                    continue;
                }

                record.SourcePath = normalizedPath;
                if (!record.HasMeta || string.IsNullOrWhiteSpace(record.Guid))
                {
                    Block(record, issues, "metaまたはGUIDがありません。UnityからPackageを書き出し直してください。");
                    continue;
                }

                if (!packageGuids.Add(record.Guid))
                {
                    Conflict(record, issues, $"Package内でGUID {record.Guid} が重複しています。Package作成元で修正してください。");
                    continue;
                }

                if (!packagePaths.Add(normalizedPath))
                {
                    Conflict(record, issues, "Package内に同じPathが複数あります。Package作成元で修正してください。");
                    continue;
                }

                if (PackagePathUtility.IsBlockedExtension(normalizedPath))
                {
                    Block(record, issues, "Script／asmdef／Pluginはアート素材として受け入れません。コード導入として別途レビューしてください。");
                    continue;
                }

                string existingGuidPath = AssetDatabase.GUIDToAssetPath(record.Guid);
                if (!string.IsNullOrEmpty(existingGuidPath))
                {
                    AssessMatchingGuid(record, normalizedPath, existingGuidPath, issues);
                    continue;
                }

                string guidAtSourcePath = AssetDatabase.AssetPathToGUID(normalizedPath);
                if (!string.IsNullOrWhiteSpace(guidAtSourcePath))
                {
                    Conflict(
                        record,
                        issues,
                        $"同じPathに別GUIDのAssetがあります（既存 {guidAtSourcePath} / Package {record.Guid}）。自動置換できません。");
                    record.ExistingPath = normalizedPath;
                    continue;
                }

                string relativePath = normalizedPath == "Assets"
                    ? string.Empty
                    : normalizedPath.Substring("Assets/".Length);
                string destinationPath = $"{incomingRoot.TrimEnd('/')}/{relativePath}";
                if (File.Exists(ToAbsolutePath(destinationPath)) || Directory.Exists(ToAbsolutePath(destinationPath)))
                {
                    Conflict(record, issues, $"一時展開先が既に存在します: {destinationPath}。前回の受け入れを確認してください。");
                    continue;
                }

                record.Status = PackageAssetStatus.New;
                record.StatusMessage = "新規Assetです。安全確認後に一時領域へ展開できます。";
                if (string.Equals(Path.GetExtension(normalizedPath), ".unity", StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new PackageIssue
                    {
                        Severity = PackageIssueSeverity.Warning,
                        SourcePath = normalizedPath,
                        Message = "Sceneが含まれています。自動選択せず、内容を確認してから配置してください。",
                    });
                }
            }

            return issues
                .OrderByDescending(issue => issue.Severity)
                .ThenBy(issue => issue.SourcePath, StringComparer.Ordinal)
                .ToList();
        }

        public static PackageAssetStatus ClassifyExistingAsset(
            string packagePath,
            string packageAssetSha256,
            string packageMetaSha256,
            string existingPath,
            string existingAssetSha256,
            string existingMetaSha256)
        {
            if (!string.Equals(packagePath, existingPath, StringComparison.OrdinalIgnoreCase))
            {
                return PackageAssetStatus.Conflict;
            }

            bool sameAsset = string.Equals(packageAssetSha256, existingAssetSha256, StringComparison.OrdinalIgnoreCase);
            bool sameMeta = string.Equals(packageMetaSha256, existingMetaSha256, StringComparison.OrdinalIgnoreCase);
            return sameAsset && sameMeta ? PackageAssetStatus.Installed : PackageAssetStatus.UpdateCandidate;
        }

        public static bool HasErrors(IEnumerable<PackageIssue> issues)
        {
            return issues.Any(issue => issue.Severity == PackageIssueSeverity.Error);
        }

        private static void AssessMatchingGuid(
            UnityPackageAssetRecord record,
            string normalizedPath,
            string existingPath,
            ICollection<PackageIssue> issues)
        {
            record.ExistingPath = existingPath;
            string existingAssetSha = ComputeFileSha256(ToAbsolutePath(existingPath));
            string existingMetaSha = ComputeFileSha256(ToAbsolutePath(existingPath) + ".meta");
            record.Status = ClassifyExistingAsset(
                normalizedPath,
                record.Sha256,
                record.MetaSha256,
                existingPath,
                existingAssetSha,
                existingMetaSha);

            if (record.Status == PackageAssetStatus.Installed)
            {
                record.StatusMessage = "同じGUID・Path・内容のAssetが導入済みです。今回は自動スキップします。";
                issues.Add(Info(record.SourcePath, record.StatusMessage));
                return;
            }

            if (record.Status == PackageAssetStatus.UpdateCandidate)
            {
                record.StatusMessage = "同じGUIDとPathですが内容が異なります。既存Assetを維持し、必要なら比較用フォルダへ書き出してください。";
                issues.Add(Warning(record.SourcePath, record.StatusMessage));
                return;
            }

            Conflict(
                record,
                issues,
                $"同じGUIDが別Pathで使用されています（Package: {normalizedPath} / 既存: {existingPath}）。Package作成元で修正してください。");
        }

        private static string ComputeFileSha256(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            using (FileStream stream = File.OpenRead(path))
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(stream);
                StringBuilder builder = new StringBuilder(hash.Length * 2);
                foreach (byte value in hash)
                {
                    builder.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                }

                return builder.ToString();
            }
        }

        private static void ResetAssessment(UnityPackageAssetRecord record)
        {
            record.Status = PackageAssetStatus.New;
            record.ExistingPath = null;
            record.StatusMessage = null;
        }

        private static void Block(UnityPackageAssetRecord record, ICollection<PackageIssue> issues, string message)
        {
            record.Status = PackageAssetStatus.Blocked;
            record.StatusMessage = message;
            issues.Add(Error(record.SourcePath, message));
        }

        private static void Conflict(UnityPackageAssetRecord record, ICollection<PackageIssue> issues, string message)
        {
            record.Status = PackageAssetStatus.Conflict;
            record.StatusMessage = message;
            issues.Add(Error(record.SourcePath, message));
        }

        private static PackageIssue Info(string path, string message)
        {
            return Issue(PackageIssueSeverity.Info, path, message);
        }

        private static PackageIssue Warning(string path, string message)
        {
            return Issue(PackageIssueSeverity.Warning, path, message);
        }

        private static PackageIssue Error(string path, string message)
        {
            return Issue(PackageIssueSeverity.Error, path, message);
        }

        private static PackageIssue Issue(PackageIssueSeverity severity, string path, string message)
        {
            return new PackageIssue
            {
                Severity = severity,
                SourcePath = path,
                Message = message,
            };
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Project root could not be resolved.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }
}
