using System.Collections.Generic;
using Game.Core.Roguelike;
using Game.Gameplay.Roguelike.Effects;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Roguelike.CombatPressure
{
    public static class CombatPressurePlayerModifiers
    {
        private struct Modifier
        {
            public float MoveSpeedMultiplier;
            public float AttachmentScaleMultiplier;
            public float ExpiresAt;
        }

        private static readonly Dictionary<string, Modifier> Sources = new Dictionary<string, Modifier>();

        public static float MoveSpeedMultiplier { get; private set; } = 1f;
        public static float AttachmentScaleMultiplier { get; private set; } = 1f;

        public static void SetSource(string sourceId, float moveSpeedMultiplier, float attachmentScaleMultiplier, float duration = 0f)
        {
            Sources[sourceId] = new Modifier
            {
                MoveSpeedMultiplier = Mathf.Max(1f, moveSpeedMultiplier),
                AttachmentScaleMultiplier = Mathf.Max(1f, attachmentScaleMultiplier),
                ExpiresAt = duration > 0f ? Time.time + duration : 0f,
            };
            Recalculate();
        }

        public static void Tick()
        {
            bool removed = false;
            var expired = new List<string>();
            foreach (var pair in Sources)
            {
                if (pair.Value.ExpiresAt > 0f && Time.time >= pair.Value.ExpiresAt)
                    expired.Add(pair.Key);
            }

            foreach (string sourceId in expired)
            {
                Sources.Remove(sourceId);
                removed = true;
            }

            if (removed)
                Recalculate();
        }

        public static void RemoveSource(string sourceId)
        {
            if (Sources.Remove(sourceId))
                Recalculate();
        }

        public static void Reset()
        {
            Sources.Clear();
            MoveSpeedMultiplier = 1f;
            AttachmentScaleMultiplier = 1f;
        }

        private static void Recalculate()
        {
            MoveSpeedMultiplier = 1f;
            AttachmentScaleMultiplier = 1f;
            foreach (Modifier modifier in Sources.Values)
            {
                MoveSpeedMultiplier *= modifier.MoveSpeedMultiplier;
                AttachmentScaleMultiplier *= modifier.AttachmentScaleMultiplier;
            }
        }
    }

    public static class CombatPressureSpawnWeights
    {
        private static readonly Dictionary<string, Dictionary<CollectibleType, float>> Sources =
            new Dictionary<string, Dictionary<CollectibleType, float>>();

        public static void SetSource(string sourceId, CollectibleType type, float multiplier)
        {
            if (!Sources.TryGetValue(sourceId, out Dictionary<CollectibleType, float> source))
            {
                source = new Dictionary<CollectibleType, float>();
                Sources[sourceId] = source;
            }

            source[type] = Mathf.Max(0.05f, multiplier);
        }

        public static void RemoveSource(string sourceId)
        {
            Sources.Remove(sourceId);
        }

        public static float GetWeight(CollectibleType type)
        {
            float weight = 1f;
            foreach (Dictionary<CollectibleType, float> source in Sources.Values)
            {
                if (source.TryGetValue(type, out float multiplier))
                    weight *= multiplier;
            }

            return weight * RoguelikeEffectRuntime.GetSpawnWeightMultiplier(type);
        }

        public static void Reset()
        {
            Sources.Clear();
        }
    }
}
