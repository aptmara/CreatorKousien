using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;

namespace CreatorKousien.Editor.AssetOrganization
{
    public static class AssetAutoClassifier
    {
        private static readonly HashSet<string> GenericSegments = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "assets", "resources", "game", "content", "features", "presentation", "shared",
            "enemy", "enemies", "boss", "bosses", "player", "collectible", "collectibles",
            "collectable", "collectables", "stage", "field", "ui", "audio", "sound", "vfx",
            "effect", "effects", "model", "models", "material", "materials", "texture", "textures",
            "prefab", "prefabs", "animation", "animations", "anim", "data", "scripts", "editor",
            "definition", "definitions", "runtime", "test", "tests", "proto", "prototype", "so", "pf",
            "m", "tex", "anim", "obj", "new", "folder",
        };

        public static AssetClassificationSuggestion ClassifyPackageRecord(UnityPackageAssetRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException(nameof(record));
            }

            return Classify(null, record.SourcePath, record.Guid);
        }

        public static AssetClassificationSuggestion ClassifyAsset(string assetPath, string originalPath = null)
        {
            return Classify(assetPath, string.IsNullOrWhiteSpace(originalPath) ? assetPath : originalPath, AssetDatabase.AssetPathToGUID(assetPath));
        }

        public static void RefreshDestination(AssetClassificationSuggestion suggestion)
        {
            if (suggestion == null)
            {
                return;
            }

            string category = NormalizeCategory(suggestion.Domain, suggestion.Category);
            suggestion.Category = category;
            suggestion.DestinationPath = PackagePlacementPlanner.BuildDestination(
                suggestion.SourcePath,
                suggestion.Domain,
                suggestion.Entity,
                category);
        }

        private static AssetClassificationSuggestion Classify(string assetPath, string evidencePath, string guid)
        {
            string normalized = (evidencePath ?? string.Empty).Replace('\\', '/');
            string lower = normalized.ToLowerInvariant();
            string extension = Path.GetExtension(normalized).ToLowerInvariant();
            Dictionary<AssetDomain, int> scores = Enum.GetValues(typeof(AssetDomain))
                .Cast<AssetDomain>()
                .ToDictionary(domain => domain, _ => 0);
            Dictionary<AssetDomain, List<string>> reasons = Enum.GetValues(typeof(AssetDomain))
                .Cast<AssetDomain>()
                .ToDictionary(domain => domain, _ => new List<string>());

            AddKeyword(scores, reasons, lower, AssetDomain.Bosses, 160, "Boss", "boss", "jackflower");
            AddKeyword(scores, reasons, lower, AssetDomain.Enemies, 135, "Enemy", "enemy", "enemies", "monster", "zombie", "ghost", "bat");
            AddKeyword(scores, reasons, lower, AssetDomain.Player, 135, "Player", "player", "attachment", "hand", "arm");
            AddKeyword(scores, reasons, lower, AssetDomain.Collectibles, 135, "Collectible", "collectible", "collectables", "collectable", "candy", "crystal", "dropitem");
            AddKeyword(scores, reasons, lower, AssetDomain.Stage, 135, "Stage / Field", "stage", "field", "terrain", "defenceline", "waveplaytest");
            AddKeyword(scores, reasons, lower, AssetDomain.Roguelike, 145, "Roguelike", "roguelike", "upgrade", "reroll");
            AddKeyword(scores, reasons, lower, AssetDomain.Shop, 145, "Shop", "shop", "clerk");
            AddKeyword(scores, reasons, lower, AssetDomain.UI, 135, "UI", "/ui/", "hud", "title", "loading", "result", "gameover", "gameclear", "gauge", "cursor", "button");
            AddKeyword(scores, reasons, lower, AssetDomain.Audio, 125, "Audio", "audio", "sound", "bgm", "_se", "/se/", "/bgm/");
            AddKeyword(scores, reasons, lower, AssetDomain.Camera, 130, "Camera", "camera", "cinemachine");
            AddKeyword(scores, reasons, lower, AssetDomain.VFX, 100, "VFX", "vfx", "effect", "particle", "shader", "smoke", "glow");
            AddKeyword(scores, reasons, lower, AssetDomain.Development, 80, "Development", "debug", "prototype", "playtest");
            AddKeyword(scores, reasons, lower, AssetDomain.ThirdParty, 170, "Third-party package", "thirdparty", "tutorialinfo", "fantasy skybox", "license", "releasenotes");

            if (IsAudioExtension(extension))
            {
                Add(scores, reasons, AssetDomain.Audio, 120, "Audio file extension");
            }

            if (IsShaderExtension(extension) && scores[AssetDomain.Enemies] == 0 && scores[AssetDomain.Stage] == 0)
            {
                Add(scores, reasons, AssetDomain.VFX, 80, "Shader file extension");
            }

            AssetDomain domain = scores
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .First().Key;
            int score = scores[domain];
            if (score == 0)
            {
                domain = AssetDomain.Shared;
                score = 20;
                reasons[domain].Add("No domain-specific keyword was found");
            }

            string entity = InferEntity(domain, normalized);
            string category = string.IsNullOrWhiteSpace(assetPath)
                ? PackagePlacementPlanner.GetCategoryFromPath(normalized)
                : PackagePlacementPlanner.GetCategory(assetPath);
            category = NormalizeCategory(domain, category);
            ClassificationConfidence confidence = score >= 120
                ? ClassificationConfidence.High
                : score >= 70
                    ? ClassificationConfidence.Medium
                    : ClassificationConfidence.Low;

            AssetClassificationSuggestion suggestion = new AssetClassificationSuggestion
            {
                SourcePath = assetPath ?? normalized,
                OriginalPath = normalized,
                Guid = guid,
                Domain = domain,
                Entity = entity,
                Category = category,
                Confidence = confidence,
                Score = score,
                Reason = string.Join(" / ", reasons[domain].Distinct(StringComparer.OrdinalIgnoreCase)),
            };
            RefreshDestination(suggestion);
            suggestion.Selected = !suggestion.RequiresReview;
            return suggestion;
        }

        private static string NormalizeCategory(AssetDomain domain, string category)
        {
            string value = string.IsNullOrWhiteSpace(category) ? "Other" : category;
            if (domain == AssetDomain.VFX && value.StartsWith("VFX/", StringComparison.Ordinal))
            {
                return value.Substring("VFX/".Length);
            }

            return value;
        }

        private static string InferEntity(AssetDomain domain, string path)
        {
            switch (domain)
            {
                case AssetDomain.UI:
                    return InferUiArea(path);
                case AssetDomain.Player:
                    return Contains(path, "attachment") ? "Attachments" : "Shared";
                case AssetDomain.Stage:
                    Match stage = Regex.Match(path, @"stage[_\- ]?(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    return stage.Success ? "Stage" + stage.Groups[1].Value : "Shared";
                case AssetDomain.Enemies:
                    return InferAfterDomain(path, "enemy", "enemies");
                case AssetDomain.Bosses:
                    return InferAfterDomain(path, "boss", "bosses");
                case AssetDomain.Collectibles:
                    return InferAfterDomain(path, "collectible", "collectibles", "collectable", "collectables");
                case AssetDomain.ThirdParty:
                    return InferFirstSpecificSegment(path, "Package");
                default:
                    return "Shared";
            }
        }

        private static string InferUiArea(string path)
        {
            if (Contains(path, "title")) return "Title";
            if (Contains(path, "loading")) return "Loading";
            if (Contains(path, "hud")) return "HUD";
            if (Contains(path, "result") || Contains(path, "gameover") || Contains(path, "gameclear")) return "Result";
            if (Contains(path, "roguelike") || Contains(path, "upgrade")) return "Roguelike";
            if (Contains(path, "shop")) return "Shop";
            return "Shared";
        }

        private static string InferAfterDomain(string path, params string[] markers)
        {
            string[] segments = path.Replace('\\', '/').Split('/');
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (!markers.Any(marker => string.Equals(segments[i], marker, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                string candidate = SanitizeEntityCandidate(segments[i + 1]);
                if (!string.IsNullOrEmpty(candidate))
                {
                    return candidate;
                }
            }

            return InferFromFileName(path, "Shared");
        }

        private static string InferFirstSpecificSegment(string path, string fallback)
        {
            foreach (string segment in path.Replace('\\', '/').Split('/'))
            {
                string candidate = SanitizeEntityCandidate(segment);
                if (!string.IsNullOrEmpty(candidate))
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private static string InferFromFileName(string path, string fallback)
        {
            string fileName = Path.GetFileNameWithoutExtension(path) ?? string.Empty;
            foreach (string token in Regex.Split(fileName, @"[_\-\s]+"))
            {
                string candidate = SanitizeEntityCandidate(token);
                if (!string.IsNullOrEmpty(candidate))
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private static string SanitizeEntityCandidate(string value)
        {
            string withoutExtension = Path.GetFileNameWithoutExtension(value ?? string.Empty);
            if (string.IsNullOrWhiteSpace(withoutExtension)
                || GenericSegments.Contains(withoutExtension)
                || withoutExtension.All(char.IsDigit))
            {
                return null;
            }

            return PackagePathUtility.SanitizeSegment(withoutExtension, "Shared");
        }

        private static void AddKeyword(
            IDictionary<AssetDomain, int> scores,
            IDictionary<AssetDomain, List<string>> reasons,
            string path,
            AssetDomain domain,
            int points,
            string reason,
            params string[] keywords)
        {
            if (keywords.Any(keyword => path.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                Add(scores, reasons, domain, points, reason);
            }
        }

        private static void Add(
            IDictionary<AssetDomain, int> scores,
            IDictionary<AssetDomain, List<string>> reasons,
            AssetDomain domain,
            int points,
            string reason)
        {
            scores[domain] += points;
            reasons[domain].Add(reason);
        }

        private static bool Contains(string value, string token)
        {
            return value.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsAudioExtension(string extension)
        {
            return extension == ".wav" || extension == ".mp3" || extension == ".ogg" || extension == ".aif" || extension == ".aiff";
        }

        private static bool IsShaderExtension(string extension)
        {
            return extension == ".shader" || extension == ".shadergraph" || extension == ".hlsl" || extension == ".cginc";
        }
    }
}
