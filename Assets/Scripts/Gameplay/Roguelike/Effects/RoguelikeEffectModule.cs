using System;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Roguelike.Effects
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RoguelikeEffectMenuAttribute : Attribute
    {
        public string Name { get; }

        public RoguelikeEffectMenuAttribute(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    public abstract class RoguelikeEffectModule
    {
        [SerializeField] private bool _enabled = true;
        public bool Enabled => _enabled;
        public abstract string Summary { get; }
        public virtual string ExclusiveGroup => null;

        public virtual void ResetRuntimeState() { }

        public virtual float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
            => currentWeight;
    }

    [Serializable, RoguelikeEffectMenu("出現確率 / 通常出現率アップ")]
    public sealed class ItemSpawnRateUpEffect : RoguelikeEffectModule
    {
        [SerializeField] private CollectibleType _collectibleType;
        [SerializeField, Min(0f)] private float _bonusPerLevel = 0.25f;

        public CollectibleType CollectibleType => _collectibleType;

        public override string Summary =>
            $"{CollectibleTable.GetDisplayName(_collectibleType)}はLv0では出現せず、Lv1以降で出現を開始し毎Lv+{_bonusPerLevel:P0}相対比重が増える";

        // 種別ごとに独立させ、複数種類のアイテム出現率アップを同時に所持できるようにする
        public override string ExclusiveGroup => $"spawn-rate-up:{_collectibleType}";

        public override float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
        {
            return candidate == _collectibleType
                ? currentWeight * (1f + Mathf.Max(0, level) * _bonusPerLevel)
                : currentWeight;
        }
    }
}
