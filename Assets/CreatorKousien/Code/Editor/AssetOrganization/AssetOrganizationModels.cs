using System;
using System.Collections.Generic;

namespace CreatorKousien.Editor.AssetOrganization
{
    public enum AssetDomain
    {
        Enemies,
        Bosses,
        Player,
        Collectibles,
        Stage,
        Roguelike,
        Shop,
        UI,
        Audio,
        Camera,
        VFX,
        Shared,
        Development,
        ThirdParty,
    }

    public enum ClassificationConfidence
    {
        Low,
        Medium,
        High,
    }

    public enum PackageIssueSeverity
    {
        Info,
        Warning,
        Error,
    }

    public enum PackageAssetStatus
    {
        New,
        Installed,
        UpdateCandidate,
        Conflict,
        Blocked,
    }

    [Serializable]
    public sealed class UnityPackageAssetRecord
    {
        public string ArchiveId;
        public string SourcePath;
        public string Guid;
        public long AssetSize;
        public string Sha256;
        public string MetaSha256;
        public bool HasAsset;
        public bool HasMeta;
        public PackageAssetStatus Status;
        public string ExistingPath;
        public string StatusMessage;

        public string Extension => System.IO.Path.GetExtension(SourcePath ?? string.Empty);
    }

    public sealed class PackageInspection
    {
        public string PackagePath;
        public IReadOnlyList<UnityPackageAssetRecord> Assets;
    }

    public sealed class PackageIssue
    {
        public PackageIssueSeverity Severity;
        public string SourcePath;
        public string Message;
    }

    [Serializable]
    public sealed class AssetMovePlan
    {
        public string SourcePath;
        public string DestinationPath;
        public string Guid;
    }

    public sealed class AssetMoveResult
    {
        public bool Succeeded;
        public List<string> Errors = new List<string>();
        public List<AssetMovePlan> CompletedMoves = new List<AssetMovePlan>();
    }

    [Serializable]
    public sealed class AssetClassificationSuggestion
    {
        public string SourcePath;
        public string OriginalPath;
        public string Guid;
        public AssetDomain Domain;
        public string Entity;
        public string Category;
        public ClassificationConfidence Confidence;
        public int Score;
        public string Reason;
        public string DestinationPath;
        public bool Selected = true;

        public bool RequiresReview => Confidence == ClassificationConfidence.Low
            || string.Equals(Category, "Other", StringComparison.Ordinal)
            || string.Equals(Category, "Scenes", StringComparison.Ordinal);
    }

    [Serializable]
    public sealed class DirectImportAssetState
    {
        public string Path;
        public string Guid;
        public long Length;
        public long WriteUtcTicks;
        public long MetaLength;
        public long MetaWriteUtcTicks;
        public bool IsFolder;
    }

    [Serializable]
    public sealed class DirectImportSnapshot
    {
        public string PackageName;
        public long StartedUtcTicks;
        public List<DirectImportAssetState> Assets = new List<DirectImportAssetState>();
    }

    public sealed class DirectImportDiff
    {
        public List<string> NewAssetPaths = new List<string>();
        public List<string> NewFolderPaths = new List<string>();
        public List<string> ModifiedExistingPaths = new List<string>();
    }

    [Serializable]
    public sealed class DirectImportReport
    {
        public string PackageName;
        public string IncomingRoot;
        public List<string> QuarantinedPaths = new List<string>();
        public List<string> ModifiedExistingPaths = new List<string>();
        public List<string> BlockedPaths = new List<string>();
        public List<string> Errors = new List<string>();
    }

    [Serializable]
    public sealed class AssetPromotionReceipt
    {
        public List<AssetMovePlan> Moves = new List<AssetMovePlan>();
    }

    public sealed class AssetLayoutIssue
    {
        public PackageIssueSeverity Severity;
        public string Path;
        public string Message;
    }
}
