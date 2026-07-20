using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class PackagePlacementPlanner
    {
        public static string BuildIncomingRoot(AssetDomain domain, string entity, string packageName)
        {
            string safePackage = PackagePathUtility.SanitizeSegment(packageName, "Package");
            string safeEntity = PackagePathUtility.SanitizeSegment(entity, "Shared");
            return $"Assets/_Incoming/{domain}/{safeEntity}/{safePackage}";
        }

        public static string BuildPackageIncomingRoot(string packageName, string uniqueSuffix)
        {
            string safePackage = PackagePathUtility.SanitizeSegment(packageName, "Package");
            string safeSuffix = PackagePathUtility.SanitizeSegment(uniqueSuffix, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            return $"Assets/_Incoming/Packages/{safePackage}_{safeSuffix}";
        }

        public static string GetFormalRoot(AssetDomain domain, string entity)
        {
            string safeEntity = PackagePathUtility.SanitizeSegment(entity, "Shared");
            switch (domain)
            {
                case AssetDomain.Enemies:
                    return $"Assets/CreatorKousien/Content/Features/Enemies/{safeEntity}";
                case AssetDomain.Bosses:
                    return $"Assets/CreatorKousien/Content/Features/Enemies/Bosses/{safeEntity}";
                case AssetDomain.Player:
                    return $"Assets/CreatorKousien/Content/Features/Player/{safeEntity}";
                case AssetDomain.Collectibles:
                    return $"Assets/CreatorKousien/Content/Features/Collectibles/{safeEntity}";
                case AssetDomain.Stage:
                    return $"Assets/CreatorKousien/Content/Features/Stage/{safeEntity}";
                case AssetDomain.Roguelike:
                    return "Assets/CreatorKousien/Content/Features/Roguelike";
                case AssetDomain.Shop:
                    return "Assets/CreatorKousien/Content/Features/Shop";
                case AssetDomain.UI:
                    return $"Assets/CreatorKousien/Content/Presentation/UI/{safeEntity}";
                case AssetDomain.Audio:
                    return "Assets/CreatorKousien/Content/Presentation/Audio";
                case AssetDomain.Camera:
                    return "Assets/CreatorKousien/Content/Presentation/Camera";
                case AssetDomain.VFX:
                    return "Assets/CreatorKousien/Content/Presentation/SharedVFX";
                case AssetDomain.Shared:
                    return "Assets/CreatorKousien/Content/Shared";
                case AssetDomain.Development:
                    return "Assets/CreatorKousien/Content/Development";
                case AssetDomain.ThirdParty:
                    return $"Assets/ThirdParty/{safeEntity}";
                default:
                    throw new ArgumentOutOfRangeException(nameof(domain), domain, null);
            }
        }

        public static AssetMovePlan PlanPromotion(string sourcePath, AssetDomain domain, string entity)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || !sourcePath.StartsWith("Assets/_Incoming/", StringComparison.Ordinal))
            {
                throw new ArgumentException("Only assets under Assets/_Incoming can be promoted.", nameof(sourcePath));
            }

            string root = GetFormalRoot(domain, entity);
            string category = GetCategory(sourcePath);
            string destination = $"{root}/{category}/{Path.GetFileName(sourcePath)}";
            return new AssetMovePlan
            {
                SourcePath = sourcePath,
                DestinationPath = destination,
                Guid = AssetDatabase.AssetPathToGUID(sourcePath),
            };
        }

        public static AssetMovePlan PlanPromotion(AssetClassificationSuggestion suggestion)
        {
            if (suggestion == null)
            {
                throw new ArgumentNullException(nameof(suggestion));
            }

            suggestion.DestinationPath = BuildDestination(
                suggestion.SourcePath,
                suggestion.Domain,
                suggestion.Entity,
                suggestion.Category);
            return new AssetMovePlan
            {
                SourcePath = suggestion.SourcePath,
                DestinationPath = suggestion.DestinationPath,
                Guid = string.IsNullOrWhiteSpace(suggestion.Guid)
                    ? AssetDatabase.AssetPathToGUID(suggestion.SourcePath)
                    : suggestion.Guid,
            };
        }

        public static string BuildDestination(string sourcePath, AssetDomain domain, string entity, string category)
        {
            string root = GetFormalRoot(domain, entity);
            string safeCategory = string.IsNullOrWhiteSpace(category) ? "Other" : category.Trim().Trim('/');
            return $"{root}/{safeCategory}/{Path.GetFileName(sourcePath)}";
        }

        public static string GetCategory(string assetPath)
        {
            Type type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            string extension = Path.GetExtension(assetPath).ToLowerInvariant();

            return GetCategory(assetPath, extension, type);
        }

        public static string GetCategoryFromPath(string sourcePath)
        {
            return GetCategory(sourcePath, Path.GetExtension(sourcePath).ToLowerInvariant(), null);
        }

        private static string GetCategory(string assetPath, string extension, Type type)
        {
            bool looksLikeVfx = assetPath.IndexOf("vfx", StringComparison.OrdinalIgnoreCase) >= 0
                || assetPath.IndexOf("effect", StringComparison.OrdinalIgnoreCase) >= 0
                || assetPath.IndexOf("particle", StringComparison.OrdinalIgnoreCase) >= 0;

            if (type == typeof(GameObject) && extension == ".prefab")
            {
                return looksLikeVfx ? "VFX/Prefabs" : "Prefabs";
            }

            if (extension == ".prefab")
            {
                return looksLikeVfx ? "VFX/Prefabs" : "Prefabs";
            }

            if (type == typeof(Material) || extension == ".mat" || extension == ".physicmaterial" || extension == ".physicsmaterial2d")
            {
                return looksLikeVfx ? "VFX/Materials" : "Materials";
            }

            if (type == typeof(Texture2D)
                || extension == ".png"
                || extension == ".jpg"
                || extension == ".jpeg"
                || extension == ".psd"
                || extension == ".tga"
                || extension == ".exr"
                || extension == ".hdr")
            {
                return looksLikeVfx ? "VFX/Textures" : "Textures";
            }

            if (type == typeof(AudioClip)
                || extension == ".wav"
                || extension == ".mp3"
                || extension == ".ogg"
                || extension == ".aif"
                || extension == ".aiff")
            {
                return "Audio";
            }

            if (extension == ".fbx" || extension == ".obj" || extension == ".blend")
            {
                return "Models";
            }

            if (extension == ".anim" || extension == ".controller" || extension == ".overridecontroller")
            {
                return "Animations";
            }

            if (extension == ".shader" || extension == ".shadergraph" || extension == ".hlsl" || extension == ".cginc")
            {
                return "Shaders";
            }

            if (extension == ".asset")
            {
                return "Data";
            }

            if (extension == ".unity")
            {
                return "Scenes";
            }

            return "Other";
        }
    }
}
