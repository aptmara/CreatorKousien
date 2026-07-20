using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace CreatorKousien.Editor.AssetOrganization
{
    [InitializeOnLoad]
    public static class DirectUnityPackageImportMonitor
    {
        private const string EnabledKey = "CreatorKousien.AssetOrganization.MonitorDirectPackageImport";
        private static readonly string SnapshotPath = Path.Combine(
            Directory.GetParent(Application.dataPath)?.FullName ?? string.Empty,
            "Library",
            "CreatorKousien",
            "DirectImportSnapshot.json");

        static DirectUnityPackageImportMonitor()
        {
            AssetDatabase.importPackageStarted -= OnImportStarted;
            AssetDatabase.importPackageStarted += OnImportStarted;
            AssetDatabase.importPackageCompleted -= OnImportCompleted;
            AssetDatabase.importPackageCompleted += OnImportCompleted;
            AssetDatabase.importPackageCancelled -= OnImportCancelled;
            AssetDatabase.importPackageCancelled += OnImportCancelled;
            AssetDatabase.importPackageFailed -= OnImportFailed;
            AssetDatabase.importPackageFailed += OnImportFailed;
        }

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(EnabledKey, true);
            set => EditorPrefs.SetBool(EnabledKey, value);
        }

        public static DirectImportSnapshot CaptureSnapshot(string packageName)
        {
            DirectImportSnapshot snapshot = new DirectImportSnapshot
            {
                PackageName = packageName,
                StartedUtcTicks = DateTime.UtcNow.Ticks,
            };

            foreach (string path in AssetDatabase.GetAllAssetPaths()
                         .Where(path => path.StartsWith("Assets/", StringComparison.Ordinal))
                         .OrderBy(path => path, StringComparer.Ordinal))
            {
                string absolute = ToAbsolutePath(path);
                bool isFolder = Directory.Exists(absolute);
                if (!isFolder && !File.Exists(absolute))
                {
                    continue;
                }

                FileInfo file = isFolder ? null : new FileInfo(absolute);
                FileInfo meta = File.Exists(absolute + ".meta") ? new FileInfo(absolute + ".meta") : null;
                snapshot.Assets.Add(new DirectImportAssetState
                {
                    Path = path,
                    Guid = AssetDatabase.AssetPathToGUID(path),
                    IsFolder = isFolder,
                    Length = file?.Length ?? 0,
                    WriteUtcTicks = file?.LastWriteTimeUtc.Ticks ?? 0,
                    MetaLength = meta?.Length ?? 0,
                    MetaWriteUtcTicks = meta?.LastWriteTimeUtc.Ticks ?? 0,
                });
            }

            return snapshot;
        }

        private static void OnImportStarted(string packageName)
        {
            if (!Enabled)
            {
                return;
            }

            try
            {
                SaveSnapshot(CaptureSnapshot(packageName));
                Debug.Log($"[Asset Intake] UnityPackage '{packageName}' の直接Importを監視しています。");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private static void OnImportCompleted(string packageName)
        {
            if (!Enabled)
            {
                return;
            }

            EditorApplication.delayCall += () => CompleteDirectImport(packageName);
        }

        private static void OnImportCancelled(string packageName)
        {
            DeleteSnapshot();
            Debug.Log($"[Asset Intake] UnityPackage '{packageName}' のImportはキャンセルされました。");
        }

        private static void OnImportFailed(string packageName, string errorMessage)
        {
            DeleteSnapshot();
            Debug.LogError($"[Asset Intake] UnityPackage '{packageName}' のImportに失敗しました: {errorMessage}");
        }

        private static void CompleteDirectImport(string packageName)
        {
            DirectImportSnapshot before = LoadSnapshot();
            if (before == null)
            {
                Debug.LogWarning("[Asset Intake] Import開始時のSnapshotが見つからないため、自動隔離できませんでした。");
                return;
            }

            DirectImportReport report = new DirectImportReport
            {
                PackageName = packageName,
                ModifiedExistingPaths = new List<string>(),
            };

            try
            {
                DirectImportSnapshot after = CaptureSnapshot(packageName);
                DirectImportDiff diff = DirectImportDiffAnalyzer.Compare(before, after);
                report.ModifiedExistingPaths.AddRange(diff.ModifiedExistingPaths);
                string suffix = new DateTime(before.StartedUtcTicks, DateTimeKind.Utc).ToString("yyyyMMdd_HHmmss");
                report.IncomingRoot = $"Assets/_Incoming/DirectImport/{PackagePathUtility.SanitizeSegment(packageName, "Package")}_{suffix}";

                List<AssetMovePlan> plans = new List<AssetMovePlan>();
                foreach (string sourcePath in diff.NewAssetPaths)
                {
                    if (PackagePathUtility.IsBlockedExtension(sourcePath)
                        || string.Equals(Path.GetExtension(sourcePath), ".unitypackage", StringComparison.OrdinalIgnoreCase))
                    {
                        report.BlockedPaths.Add(sourcePath);
                        continue;
                    }

                    string guid = AssetDatabase.AssetPathToGUID(sourcePath);
                    if (string.IsNullOrWhiteSpace(guid))
                    {
                        report.Errors.Add($"GUIDを取得できませんでした: {sourcePath}");
                        continue;
                    }

                    string relative = sourcePath.Substring("Assets/".Length);
                    plans.Add(new AssetMovePlan
                    {
                        SourcePath = sourcePath,
                        DestinationPath = $"{report.IncomingRoot}/{relative}",
                        Guid = guid,
                    });
                }

                if (plans.Count > 0)
                {
                    AssetMoveResult moveResult = AssetMoveExecutor.Execute(plans);
                    if (moveResult.Succeeded)
                    {
                        report.QuarantinedPaths.AddRange(moveResult.CompletedMoves.Select(move => move.DestinationPath));
                        RemoveEmptyImportedFolders(diff.NewFolderPaths);
                    }
                    else
                    {
                        report.Errors.AddRange(moveResult.Errors);
                    }
                }
            }
            catch (Exception exception)
            {
                report.Errors.Add(exception.Message);
                Debug.LogException(exception);
            }
            finally
            {
                DeleteSnapshot();
            }

            PackageIntakeWindow.OpenDirectImportReport(report);
        }

        private static void RemoveEmptyImportedFolders(IEnumerable<string> folderPaths)
        {
            foreach (string folder in folderPaths
                         .Where(path => !path.StartsWith("Assets/_Incoming", StringComparison.Ordinal))
                         .OrderByDescending(path => path.Count(character => character == '/')))
            {
                string absolute = ToAbsolutePath(folder);
                if (!Directory.Exists(absolute)
                    || Directory.EnumerateFileSystemEntries(absolute).Any()
                    || !AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                AssetDatabase.DeleteAsset(folder);
            }
        }

        private static void SaveSnapshot(DirectImportSnapshot snapshot)
        {
            string directory = Path.GetDirectoryName(SnapshotPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot));
        }

        private static DirectImportSnapshot LoadSnapshot()
        {
            return File.Exists(SnapshotPath)
                ? JsonUtility.FromJson<DirectImportSnapshot>(File.ReadAllText(SnapshotPath))
                : null;
        }

        private static void DeleteSnapshot()
        {
            if (File.Exists(SnapshotPath))
            {
                File.Delete(SnapshotPath);
            }
        }

        private static string ToAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Project root could not be resolved.");
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }
    }

    public static class UnityPackageOpenInterceptor
    {
        [OnOpenAsset(0)]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceId);
            if (!string.Equals(Path.GetExtension(assetPath), ".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return false;
            }

            PackageIntakeWindow.OpenWithPackage(Path.GetFullPath(Path.Combine(projectRoot, assetPath)));
            return true;
        }
    }
}
