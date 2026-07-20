using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class AssetMoveExecutor
    {
        public static AssetMoveResult Execute(IReadOnlyList<AssetMovePlan> plans)
        {
            AssetMoveResult result = new AssetMoveResult();
            if (plans == null || plans.Count == 0)
            {
                result.Errors.Add("No assets were selected for movement.");
                return result;
            }

            Dictionary<string, HashSet<string>> dependencies = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            HashSet<string> destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AssetMovePlan plan in plans)
            {
                if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(plan.SourcePath)))
                {
                    result.Errors.Add($"Source asset does not exist: {plan.SourcePath}");
                }

                if (!destinations.Add(plan.DestinationPath))
                {
                    result.Errors.Add($"Multiple assets target the same destination: {plan.DestinationPath}");
                }

                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(plan.DestinationPath)))
                {
                    result.Errors.Add($"Destination already exists: {plan.DestinationPath}");
                }

                string actualGuid = AssetDatabase.AssetPathToGUID(plan.SourcePath);
                if (!string.IsNullOrWhiteSpace(plan.Guid)
                    && !string.Equals(actualGuid, plan.Guid, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"GUID changed before movement: {plan.SourcePath}");
                }

                dependencies[plan.SourcePath] = AssetDependencySnapshot.CaptureGuids(plan.SourcePath);
            }

            if (result.Errors.Count > 0)
            {
                return result;
            }

            foreach (AssetMovePlan plan in plans)
            {
                EnsureAssetFolder(Path.GetDirectoryName(plan.DestinationPath)?.Replace('\\', '/'));
            }

            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (AssetMovePlan plan in plans)
                {
                    string error = AssetDatabase.MoveAsset(plan.SourcePath, plan.DestinationPath);
                    if (!string.IsNullOrEmpty(error))
                    {
                        throw new InvalidOperationException($"Failed to move {plan.SourcePath}: {error}");
                    }

                    result.CompletedMoves.Add(plan);
                }
            }
            catch (Exception exception)
            {
                result.Errors.Add(exception.Message);
                RollBack(result.CompletedMoves, result.Errors);
                result.CompletedMoves.Clear();
                return result;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            foreach (AssetMovePlan plan in plans)
            {
                string actualGuid = AssetDatabase.AssetPathToGUID(plan.DestinationPath);
                if (!string.Equals(actualGuid, plan.Guid, StringComparison.OrdinalIgnoreCase))
                {
                    result.Errors.Add($"GUID was not preserved: {plan.DestinationPath}");
                    continue;
                }

                HashSet<string> after = AssetDependencySnapshot.CaptureGuids(plan.DestinationPath);
                if (!AssetDependencySnapshot.AreEqual(dependencies[plan.SourcePath], after))
                {
                    result.Errors.Add($"Dependency GUIDs changed: {plan.DestinationPath}");
                }
            }

            if (result.Errors.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    RollBack(result.CompletedMoves, result.Errors);
                    result.CompletedMoves.Clear();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return result;
            }

            result.Succeeded = true;
            return result;
        }

        private static void RollBack(IReadOnlyList<AssetMovePlan> completedMoves, List<string> errors)
        {
            for (int i = completedMoves.Count - 1; i >= 0; i--)
            {
                AssetMovePlan plan = completedMoves[i];
                string rollbackError = AssetDatabase.MoveAsset(plan.DestinationPath, plan.SourcePath);
                if (!string.IsNullOrEmpty(rollbackError))
                {
                    errors.Add($"Rollback failed for {plan.DestinationPath}: {rollbackError}");
                }
            }
        }

        private static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrWhiteSpace(folder) || folder == "Assets" || AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            string name = Path.GetFileName(folder);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
