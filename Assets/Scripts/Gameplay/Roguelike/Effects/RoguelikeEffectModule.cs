using System;
using System.Collections.Generic;
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Roguelike.Effects
{
    public interface IRoguelikeEffectHost
    {
        void SpawnDefault(Vector3 position, int count, CollectibleData collectible);
        void SpawnCustom(Vector3 position, int count, CollectibleData collectible, float height, float scatter, float scale);
        void EmitPressureDrop(string ruleId, Vector3 position, int count, CollectibleData collectible, bool allowEcho);
        void SchedulePressureDrop(string ruleId, Vector3 position, int count, CollectibleData collectible, float delaySeconds);
        bool IsRuleAcquired(string ruleId);
        void FeedStatusProgress(string statusType, int amount, Vector3 position);
    }

    public sealed class RoguelikePressureEffectContext
    {
        public IRoguelikeEffectHost Host { get; }
        public string RuleId { get; }
        public Vector3 Position { get; }
        public CollectibleData Collectible { get; }
        public bool AllowEcho { get; }
        public int Level { get; internal set; }
        public int SpawnCount { get; set; }
        public bool CancelDefault { get; set; }

        public RoguelikePressureEffectContext(
            IRoguelikeEffectHost host,
            string ruleId,
            Vector3 position,
            int spawnCount,
            CollectibleData collectible,
            bool allowEcho)
        {
            Host = host;
            RuleId = ruleId;
            Position = position;
            SpawnCount = spawnCount;
            Collectible = collectible;
            AllowEcho = allowEcho;
        }
    }

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

        public virtual void ModifyPressureSpawnCount(RoguelikePressureEffectContext context) { }
        public virtual void OnPressureTriggered(RoguelikePressureEffectContext context) { }
        public virtual void InterceptPressureDrop(RoguelikePressureEffectContext context) { }
        public virtual void EmitPressureDrop(RoguelikePressureEffectContext context) { }
        public virtual void AfterPressureDrop(RoguelikePressureEffectContext context) { }
        public virtual void ResetRuntimeState() { }

        public virtual float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
            => currentWeight;
    }

    [Serializable, RoguelikeEffectMenu("出現確率 / 指定モデルを増やす")]
    public sealed class FocusWeightEffect : RoguelikeEffectModule
    {
        [SerializeField] private CollectibleType _collectibleType;
        [SerializeField, Min(0f)] private float _multiplier = 3.5f;
        [SerializeField, Min(0f)] private float _otherMultiplier = 0.45f;

        public override string Summary => $"{CollectibleTable.GetDisplayName(_collectibleType)}の通常出現を×{_multiplier:0.##}";
        public override string ExclusiveGroup => "spawn-focus-weight";

        public override float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
        {
            return currentWeight * (candidate == _collectibleType ? _multiplier : _otherMultiplier);
        }
    }

    [Serializable, RoguelikeEffectMenu("出現確率 / 直前に当てたモデルへ特化")]
    public sealed class LastHitFocusWeightEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(0f)] private float _focusedMultiplier = 3.5f;
        [SerializeField, Min(0f)] private float _otherMultiplier = 0.45f;

        public override string Summary => $"直前に当てたモデル×{_focusedMultiplier:0.##}、他×{_otherMultiplier:0.##}";
        public override string ExclusiveGroup => "spawn-focus-weight";

        public override float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
        {
            if (!lastHit.HasValue)
                return currentWeight;
            return currentWeight * (candidate == lastHit.Value ? _focusedMultiplier : _otherMultiplier);
        }
    }

    [Serializable, RoguelikeEffectMenu("出現確率 / 直前に当てたモデルを連鎖")]
    public sealed class MomentumWeightEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(0f)] private float _bonusPerHit = 0.2f;
        [SerializeField, Min(0f)] private float _maximumBonus = 2f;

        public override string Summary => $"同じモデルを当て続けるほど通常出現率+{_bonusPerHit:0.##}/Hit";
        public override string ExclusiveGroup => "spawn-focus-weight";

        public override float ModifySpawnWeight(
            CollectibleType candidate,
            CollectibleType? lastHit,
            int sameTypeHitStreak,
            int level,
            float currentWeight)
        {
            if (!lastHit.HasValue || candidate != lastHit.Value)
                return currentWeight;

            return currentWeight * (1f + Mathf.Min(_maximumBonus, sameTypeHitStreak * _bonusPerHit));
        }
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 反響")]
    public sealed class EchoDropEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(1)] private int _triggerInterval = 3;
        [SerializeField, Range(0.01f, 2f)] private float _countRatio = 0.5f;
        [SerializeField, Min(0f)] private float _delaySeconds = 0.65f;

        public int TriggerInterval => Mathf.Max(1, _triggerInterval);
        public float CountRatio => Mathf.Max(0.01f, _countRatio);
        public float DelaySeconds => Mathf.Max(0f, _delaySeconds);
        public override string Summary => $"{TriggerInterval}回ごとに{_countRatio:P0}を再降下";
        public override string ExclusiveGroup => "pressure-echo";

        [NonSerialized] private int _triggerCount;

        public override void AfterPressureDrop(RoguelikePressureEffectContext context)
        {
            if (!context.AllowEcho || ++_triggerCount % TriggerInterval != 0)
                return;
            context.Host.SchedulePressureDrop(
                context.RuleId,
                context.Position,
                Mathf.Max(1, Mathf.CeilToInt(context.SpawnCount * CountRatio)),
                context.Collectible,
                DelaySeconds);
        }

        public override void ResetRuntimeState() => _triggerCount = 0;
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 反響を毎回発生")]
    public sealed class EndlessEchoEffect : RoguelikeEffectModule
    {
        [SerializeField, Range(0.01f, 2f)] private float _countRatio = 0.5f;
        [SerializeField, Min(0f)] private float _delaySeconds = 0.65f;

        public float CountRatio => Mathf.Max(0.01f, _countRatio);
        public float DelaySeconds => Mathf.Max(0f, _delaySeconds);
        public override string Summary => $"毎回{_countRatio:P0}を再降下";
        public override string ExclusiveGroup => "pressure-echo";

        public override void AfterPressureDrop(RoguelikePressureEffectContext context)
        {
            if (!context.AllowEcho)
                return;
            context.Host.SchedulePressureDrop(
                context.RuleId,
                context.Position,
                Mathf.Max(1, Mathf.CeilToInt(context.SpawnCount * CountRatio)),
                context.Collectible,
                DelaySeconds);
        }
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 連続発動ボーナス")]
    public sealed class MomentumDropEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(0f)] private float _streakWindowSeconds = 4f;
        [SerializeField, Min(0)] private int _maximumBonusCount = 3;

        public float StreakWindowSeconds => Mathf.Max(0f, _streakWindowSeconds);
        public int MaximumBonusCount => Mathf.Max(0, _maximumBonusCount);
        public override string Summary => $"{_streakWindowSeconds:0.#}秒内の連続発動で最大+{_maximumBonusCount}個";

        [NonSerialized] private int _streak;
        [NonSerialized] private float _lastTriggerTime = float.NegativeInfinity;

        public override void ModifyPressureSpawnCount(RoguelikePressureEffectContext context)
        {
            _streak = Time.time - _lastTriggerTime <= StreakWindowSeconds
                ? Mathf.Min(MaximumBonusCount + 1, _streak + 1)
                : 1;
            _lastTriggerTime = Time.time;
            context.SpawnCount += _streak - 1;
        }

        public override void ResetRuntimeState()
        {
            _streak = 0;
            _lastTriggerTime = float.NegativeInfinity;
        }
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 遅延してまとめる")]
    public sealed class DelayedReleaseEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(1)] private int _triggerCount = 3;
        [SerializeField, Min(0f)] private float _releaseMultiplier = 1.5f;

        public int TriggerCount => Mathf.Max(1, _triggerCount);
        public float ReleaseMultiplier => Mathf.Max(0f, _releaseMultiplier);
        public override string Summary => $"{TriggerCount}回分を溜めて×{ReleaseMultiplier:0.##}で降下";

        [Serializable]
        private readonly struct PendingDrop
        {
            public readonly string RuleId;
            public readonly Vector3 Position;
            public readonly int Count;
            public readonly CollectibleData Collectible;

            public PendingDrop(RoguelikePressureEffectContext context)
            {
                RuleId = context.RuleId;
                Position = context.Position;
                Count = context.SpawnCount;
                Collectible = context.Collectible;
            }
        }

        [NonSerialized] private List<PendingDrop> _pending;

        public override void InterceptPressureDrop(RoguelikePressureEffectContext context)
        {
            _pending ??= new List<PendingDrop>();
            _pending.Add(new PendingDrop(context));
            context.CancelDefault = true;
            if (_pending.Count < TriggerCount)
                return;

            PendingDrop[] release = _pending.ToArray();
            _pending.Clear();
            foreach (PendingDrop pending in release)
            {
                context.Host.EmitPressureDrop(
                    pending.RuleId,
                    pending.Position,
                    Mathf.CeilToInt(pending.Count * ReleaseMultiplier),
                    pending.Collectible,
                    true);
            }
        }

        public override void ResetRuntimeState() => _pending?.Clear();
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 圧縮して巨大化")]
    public sealed class CompressionEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(1)] private int _itemsPerGiant = 6;
        [SerializeField, Min(1f)] private float _giantScale = 2.6f;
        [SerializeField, Min(0f)] private float _spawnHeight = 13f;

        public int ItemsPerGiant => Mathf.Max(1, _itemsPerGiant);
        public float GiantScale => Mathf.Max(1f, _giantScale);
        public float SpawnHeight => Mathf.Max(0f, _spawnHeight);
        public override string Summary => $"{ItemsPerGiant}個を大きさ×{GiantScale:0.##}の1個へ圧縮";

        public override void EmitPressureDrop(RoguelikePressureEffectContext context)
        {
            int giantCount = context.SpawnCount / ItemsPerGiant;
            int normalCount = context.SpawnCount % ItemsPerGiant;
            if (normalCount > 0)
                context.Host.SpawnDefault(context.Position, normalCount, context.Collectible);
            if (giantCount > 0)
                context.Host.SpawnCustom(context.Position, giantCount, context.Collectible, SpawnHeight, 2f, GiantScale);
            context.CancelDefault = true;
        }
    }

    [Serializable, RoguelikeEffectMenu("追加降下 / 一定回数ごとに巨大物")]
    public sealed class CataclysmEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(1)] private int _triggerInterval = 3;
        [SerializeField, Min(1)] private int _spawnCount = 1;
        [SerializeField, Min(1f)] private float _scale = 3.2f;
        [SerializeField, Min(0f)] private float _spawnHeight = 16f;

        public int TriggerInterval => Mathf.Max(1, _triggerInterval);
        public int SpawnCount => Mathf.Max(1, _spawnCount);
        public float Scale => Mathf.Max(1f, _scale);
        public float SpawnHeight => Mathf.Max(0f, _spawnHeight);
        public override string Summary => $"{TriggerInterval}回ごとに巨大物×{SpawnCount}";

        [NonSerialized] private int _triggerCount;

        public override void AfterPressureDrop(RoguelikePressureEffectContext context)
        {
            if (++_triggerCount % TriggerInterval == 0)
                context.Host.SpawnCustom(context.Position, SpawnCount, context.Collectible, SpawnHeight, 1f, Scale);
        }

        public override void ResetRuntimeState() => _triggerCount = 0;
    }

    [Serializable, RoguelikeEffectMenu("状態異常 / 別ビルドへ累計を渡す")]
    public sealed class CrossFeedEffect : RoguelikeEffectModule
    {
        [SerializeField, Min(1)] private int _progressAmount = 1;
        [SerializeField] private string _poisonRuleId = "poison-field";
        [SerializeField] private string _poisonStatusType = "Poison";
        [SerializeField] private string _iceRuleId = "ice-stack";
        [SerializeField] private string _iceStatusType = "Ice";
        public int ProgressAmount => Mathf.Max(1, _progressAmount);
        public override string Summary => $"別の状態異常ビルドへ累計+{ProgressAmount}";
        public override string ExclusiveGroup => "status-cross-feed";

        public override void OnPressureTriggered(RoguelikePressureEffectContext context)
        {
            if (!string.Equals(context.RuleId, _poisonRuleId, StringComparison.Ordinal) &&
                context.Host.IsRuleAcquired(_poisonRuleId))
            {
                context.Host.FeedStatusProgress(_poisonStatusType, ProgressAmount, context.Position);
            }

            if (!string.Equals(context.RuleId, _iceRuleId, StringComparison.Ordinal) &&
                context.Host.IsRuleAcquired(_iceRuleId))
            {
                context.Host.FeedStatusProgress(_iceStatusType, ProgressAmount, context.Position);
            }
        }
    }
}
