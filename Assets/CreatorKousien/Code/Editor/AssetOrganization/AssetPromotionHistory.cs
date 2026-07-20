using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class AssetPromotionHistory
    {
        private const string SessionKey = "CreatorKousien.AssetOrganization.LastPromotion";

        public static void Record(IReadOnlyList<AssetMovePlan> moves)
        {
            AssetPromotionReceipt receipt = new AssetPromotionReceipt
            {
                Moves = moves.Select(move => new AssetMovePlan
                {
                    SourcePath = move.SourcePath,
                    DestinationPath = move.DestinationPath,
                    Guid = move.Guid,
                }).ToList(),
            };
            SessionState.SetString(SessionKey, JsonUtility.ToJson(receipt));
        }

        public static bool CanUndo(out string reason)
        {
            AssetPromotionReceipt receipt = Load();
            if (receipt == null || receipt.Moves == null || receipt.Moves.Count == 0)
            {
                reason = "このUnityセッションには取り消せる配置履歴がありません。";
                return false;
            }

            foreach (AssetMovePlan move in receipt.Moves)
            {
                string currentGuid = AssetDatabase.AssetPathToGUID(move.DestinationPath);
                if (!string.Equals(currentGuid, move.Guid, StringComparison.OrdinalIgnoreCase))
                {
                    reason = $"配置後に変更されたAssetがあります: {move.DestinationPath}";
                    return false;
                }

                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(move.SourcePath)))
                {
                    reason = $"元の場所が既に使用されています: {move.SourcePath}";
                    return false;
                }
            }

            reason = null;
            return true;
        }

        public static AssetMoveResult UndoLast()
        {
            if (!CanUndo(out string reason))
            {
                AssetMoveResult unavailable = new AssetMoveResult();
                unavailable.Errors.Add(reason);
                return unavailable;
            }

            AssetPromotionReceipt receipt = Load();
            List<AssetMovePlan> reverse = receipt.Moves
                .AsEnumerable()
                .Reverse()
                .Select(move => new AssetMovePlan
                {
                    SourcePath = move.DestinationPath,
                    DestinationPath = move.SourcePath,
                    Guid = move.Guid,
                })
                .ToList();
            AssetMoveResult result = AssetMoveExecutor.Execute(reverse);
            if (result.Succeeded)
            {
                SessionState.EraseString(SessionKey);
            }

            return result;
        }

        private static AssetPromotionReceipt Load()
        {
            string json = SessionState.GetString(SessionKey, string.Empty);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<AssetPromotionReceipt>(json);
        }
    }
}
