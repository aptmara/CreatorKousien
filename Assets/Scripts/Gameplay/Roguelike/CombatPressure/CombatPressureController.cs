using System;
using System.Collections;
using System.Collections.Generic;
using Game.Core.Events;
using Game.Core.Roguelike;
using Game.Data.Collectibles;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Player;
using Game.Gameplay.Roguelike.Effects;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Gameplay.Roguelike.CombatPressure
{
    /// <summary>
    /// 全体コンボと状態異常の場の状態を入力に、取得したビルドの固有挙動を実行する。
    /// </summary>
    public sealed class CombatPressureController : MonoBehaviour, IRoguelikeEffectHost
    {
        private const string ComboRuleId = "combo-gummy";
        private const string PoisonRuleId = "poison-field";
        private const string IceRuleId = "ice-stack";
        private const string PoisonStatus = "Poison";
        private const string IceStatus = "Ice";

        private sealed class RuleRuntime
        {
            public bool WasActive;
            public float NextSpawnTime;
            public float PulseActiveUntil;
            public int AppliedLevel;
            public int ComboHitRemainder;
        }

        [Header("設定")]
        [SerializeField] private CombatPressureRuleSet _ruleSet;
        [SerializeField] private CollectibleSpawner _collectibleSpawner;
        [SerializeField] private Transform _spawnAnchor;

        [Header("開発用表示")]
        [SerializeField] private bool _showDebugOverlay;

        private readonly Dictionary<string, Dictionary<string, int>> _statusesByEnemy =
            new Dictionary<string, Dictionary<string, int>>();
        private readonly Dictionary<string, Vector3> _lastPositionByEnemy =
            new Dictionary<string, Vector3>();
        private readonly Dictionary<int, RuleRuntime> _ruleRuntime = new Dictionary<int, RuleRuntime>();
        private readonly Dictionary<string, int> _observedBuildLevels = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _cumulativeStatusProgress = new Dictionary<string, int>();
        private int _currentCombo;
        private bool _isCrossFeeding;
        private Vector3 _lastCombatPosition;
        private string _pendingPreviewRuleId;
        private int _pendingPreviewSpawnCount;

        public CombatPressureRuleSet RuleSet => _ruleSet;
        public int CurrentCombo => _currentCombo;
        public int GetAffectedEnemyCount(string statusType) => GetStatusTotals(statusType).AffectedEnemies;
        public int GetTotalStackCount(string statusType) => GetStatusTotals(statusType).TotalStacks;
        public int GetCumulativeStatusProgress(string statusType) =>
            _cumulativeStatusProgress.TryGetValue(statusType, out int progress) ? progress : 0;

        public void BindFocusedCollectible(string ruleId, CollectibleData collectible)
        {
            if (string.IsNullOrWhiteSpace(ruleId) || collectible == null)
                return;

            int level = Mathf.Max(1, RoguelikeBuildRuntime.GetCombatRuleLevel(ruleId));
            RoguelikeBuildRuntime.SetCombatRule(ruleId, level, (int)collectible.Type);
        }

        public CollectibleData GetFocusedCollectible(string ruleId)
        {
            int? focusedType = RoguelikeBuildRuntime.GetFocusedCollectibleType(ruleId);
            return focusedType.HasValue && _ruleSet != null
                ? _ruleSet.GetCollectible((CollectibleType)focusedType.Value)
                : null;
        }

        public void AcquireAllRulesForDebug()
        {
            if (_ruleSet == null) return;

            foreach (CombatPressureRule rule in _ruleSet.Rules)
            {
                if (rule != null)
                {
                    RoguelikeBuildRuntime.SetCombatRule(
                        rule.Id,
                        1,
                        rule.FocusedCollectible != null ? (int?)rule.FocusedCollectible.Type : null);
                }
            }
        }

        public void Initialize(CombatPressureRuleSet ruleSet)
        {
            _ruleSet = ruleSet;
            SynchronizeObservedBuildLevels();
            EvaluateRules();
        }

        private void Awake()
        {
            ResolveSceneReferences();
        }

        private void ResolveSceneReferences()
        {
            if (_collectibleSpawner == null)
                _collectibleSpawner = UnityEngine.Object.FindFirstObjectByType<CollectibleSpawner>();

            if (_spawnAnchor == null)
            {
                PlayerFacade player = UnityEngine.Object.FindFirstObjectByType<PlayerFacade>();
                if (player != null)
                    _spawnAnchor = player.transform;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Subscribe<EnemyStatusChangedEvent>(OnEnemyStatusChanged);
            EventBus.Subscribe<EnemyDefeatStatusSnapshotEvent>(OnEnemyDefeatStatusSnapshot);
            EventBus.Subscribe<EnemyFreezeBrokenEvent>(OnEnemyFreezeBroken);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Subscribe<BarrierHitBatchEvent>(OnBarrierHit);
            RoguelikeBuildRuntime.Changed += OnBuildChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ComboChangedEvent>(OnComboChanged);
            EventBus.Unsubscribe<EnemyStatusChangedEvent>(OnEnemyStatusChanged);
            EventBus.Unsubscribe<EnemyDefeatStatusSnapshotEvent>(OnEnemyDefeatStatusSnapshot);
            EventBus.Unsubscribe<EnemyFreezeBrokenEvent>(OnEnemyFreezeBroken);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Unsubscribe<BarrierHitBatchEvent>(OnBarrierHit);
            RoguelikeBuildRuntime.Changed -= OnBuildChanged;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            ClearAppliedEffects();
        }

        private void Update()
        {
            CombatPressurePlayerModifiers.Tick();
            EvaluateRules();
            TryRunAcquisitionPreview();
        }

        public void SetDebugCombo(int combo)
        {
            _currentCombo = Mathf.Max(0, combo);
            EvaluateRules();
        }

        public void SetDebugStatus(string statusType, int affectedEnemies, int totalStacks)
        {
            _ = affectedEnemies;
            _cumulativeStatusProgress[statusType] = 0;
            AddCumulativeStatusProgress(statusType, Mathf.Max(0, totalStacks), _lastCombatPosition);
        }

        public void ResetPressure()
        {
            _currentCombo = 0;
            _statusesByEnemy.Clear();
            _lastPositionByEnemy.Clear();
            _ruleRuntime.Clear();
            _cumulativeStatusProgress.Clear();
            _isCrossFeeding = false;
            _lastCombatPosition = Vector3.zero;
            ClearAppliedEffects();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _collectibleSpawner = null;
            _spawnAnchor = null;
            ResetPressure();
            ResolveSceneReferences();
        }

        private void OnComboChanged(ComboChangedEvent ev)
        {
            _currentCombo = ev.CurrentCombo;
            EvaluateRules();
        }

        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            RecordCollectibleHit(ev.ItemDataRaw);
            _lastCombatPosition = ev.HitPosition;
            if (!string.IsNullOrEmpty(ev.EnemyId))
                _lastPositionByEnemy[ev.EnemyId] = ev.HitPosition;
            HandleComboHit(ev.HitCount, ev.HitPosition);
        }

        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            RecordCollectibleHit(ev.ItemDataRaw);
            _lastCombatPosition = ev.HitPosition;
            if (!string.IsNullOrEmpty(ev.EnemyId))
                _lastPositionByEnemy[ev.EnemyId] = ev.HitPosition;
            HandleComboHit(ev.HitCount, ev.HitPosition);
        }

        private void HandleComboHit(int hitCount, Vector3 position)
        {
            CombatPressureRule rule = FindRule(ComboRuleId, out int ruleIndex);
            int level = rule != null ? RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id) : 0;
            if (rule == null || level < 2 || !IsConditionActive(rule, level))
                return;

            float recoverySeconds = CombatPressureProgression.GetComboRecoverySeconds(level, hitCount);
            if (recoverySeconds > 0f)
                EventBus.Publish(new ComboDurationRecoveryRequestedEvent(recoverySeconds));

            if (level < 3)
                return;

            RuleRuntime state = GetRuleRuntime(ruleIndex);
            int accumulatedHits = state.ComboHitRemainder + Mathf.Max(0, hitCount);
            int spawnCount = CombatPressureProgression.GetComboEchoSpawnCount(level, accumulatedHits);
            state.ComboHitRemainder = CombatPressureProgression.GetComboEchoRemainder(level, accumulatedHits);
            if (spawnCount <= 0)
                return;

            SpawnFocusedCollectibleAt(rule, position, spawnCount);
        }

        private void OnEnemyStatusChanged(EnemyStatusChangedEvent ev)
        {
            if (string.IsNullOrEmpty(ev.EnemyId) || string.IsNullOrEmpty(ev.StatusType))
                return;

            int previousStacks = GetEnemyStatusStacks(ev.EnemyId, ev.StatusType);
            if (!ev.IsActive || ev.StackCount <= 0)
            {
                if (_statusesByEnemy.TryGetValue(ev.EnemyId, out Dictionary<string, int> statuses))
                {
                    statuses.Remove(ev.StatusType);
                    if (statuses.Count == 0)
                        _statusesByEnemy.Remove(ev.EnemyId);
                }
            }
            else
            {
                if (!_statusesByEnemy.TryGetValue(ev.EnemyId, out Dictionary<string, int> statuses))
                {
                    statuses = new Dictionary<string, int>();
                    _statusesByEnemy[ev.EnemyId] = statuses;
                }

                statuses[ev.StatusType] = ev.StackCount;
            }

            if (ev.IsActive && ev.StackCount > 0)
            {
                Vector3 position = _lastPositionByEnemy.TryGetValue(ev.EnemyId, out Vector3 enemyPosition)
                    ? enemyPosition
                    : _lastCombatPosition;
                int gainedProgress = previousStacks <= 0
                    ? ev.StackCount
                    : Mathf.Max(1, ev.StackCount - previousStacks);
                AddCumulativeStatusProgress(ev.StatusType, gainedProgress, position);
            }

            EvaluateRules();
        }

        private void AddCumulativeStatusProgress(string statusType, int gainedProgress, Vector3 position)
        {
            if (gainedProgress <= 0 || string.IsNullOrEmpty(statusType))
                return;

            CombatPressureRule rule = FindStatusRule(statusType, out int ruleIndex);
            int level = rule != null ? RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id) : 0;
            if (rule == null || level <= 0)
                return;

            int previousProgress = GetCumulativeStatusProgress(statusType);
            int threshold = CombatPressureProgression.GetEffectiveThreshold(rule.Threshold, level);
            int completedCycles = CombatPressureProgression.GetCompletedCycles(
                previousProgress,
                gainedProgress,
                threshold);
            int remainingProgress = CombatPressureProgression.GetRemainingProgress(
                previousProgress,
                gainedProgress,
                threshold);
            _cumulativeStatusProgress[statusType] = remainingProgress;

            if (completedCycles > 0)
                TriggerStatusCycles(rule, ruleIndex, level, completedCycles, position);

        }

        private void TriggerStatusCycles(
            CombatPressureRule rule,
            int ruleIndex,
            int level,
            int completedCycles,
            Vector3 position)
        {
            RuleRuntime state = GetRuleRuntime(ruleIndex);
            state.PulseActiveUntil = Time.time + Mathf.Max(0.1f, rule.BuffDuration);
            ApplyRuleEffects(rule, ruleIndex, level);

            int spawnCount = completedCycles * (rule.SpawnCount + level - 1);
            SpawnFocusedCollectibleAt(rule, position, spawnCount);
        }

        private void OnEnemyDefeatStatusSnapshot(EnemyDefeatStatusSnapshotEvent ev)
        {
            CombatPressureRule rule = FindRule(PoisonRuleId, out _);
            int level = rule != null ? RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id) : 0;
            int spawnCount = CombatPressureProgression.GetPoisonDefeatSpawnCount(
                level,
                ContainsStatus(ev.ActiveStatusTypes, PoisonStatus));
            if (spawnCount <= 0)
                return;

            SpawnFocusedCollectibleAt(rule, ev.Position, spawnCount);
        }

        private void OnEnemyFreezeBroken(EnemyFreezeBrokenEvent ev)
        {
            CombatPressureRule rule = FindRule(IceRuleId, out _);
            int level = rule != null ? RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id) : 0;
            int spawnCount = CombatPressureProgression.GetIceBreakSpawnCount(level);
            if (spawnCount <= 0)
                return;

            SpawnFocusedCollectibleAt(rule, ev.Position, spawnCount);
        }

        private void OnEnemyDefeated(EnemyDefeatedEvent ev)
        {
            bool changed = _statusesByEnemy.Remove(ev.EnemyId);
            _lastPositionByEnemy.Remove(ev.EnemyId);
            if (changed)
                EvaluateRules();
        }

        private void OnBuildChanged()
        {
            if (_ruleSet == null)
                return;

            foreach (CombatPressureRule rule in _ruleSet.Rules)
            {
                if (rule == null)
                    continue;

                int currentLevel = RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id);
                _observedBuildLevels.TryGetValue(rule.Id, out int previousLevel);
                if (currentLevel > previousLevel)
                {
                    _pendingPreviewRuleId = rule.Id;
                    _pendingPreviewSpawnCount = CombatPressureProgression.GetAcquisitionPreviewSpawnCount(currentLevel);
                }

                _observedBuildLevels[rule.Id] = currentLevel;
            }

            EvaluateRules();
        }

        private void TryRunAcquisitionPreview()
        {
            if (string.IsNullOrEmpty(_pendingPreviewRuleId) || Time.timeScale <= 0f)
                return;

            CombatPressureRule rule = FindRule(_pendingPreviewRuleId, out _);
            CollectibleData focused = rule != null ? GetFocusedCollectible(rule.Id) : null;
            if (rule == null || focused == null)
                return;

            ResolveSceneReferences();
            if (_collectibleSpawner == null || _spawnAnchor == null)
                return;

            SpawnFocusedCollectibleAt(rule, _spawnAnchor.position, _pendingPreviewSpawnCount);
            _pendingPreviewRuleId = null;
            _pendingPreviewSpawnCount = 0;
        }

        private void SynchronizeObservedBuildLevels()
        {
            _observedBuildLevels.Clear();
            if (_ruleSet == null)
                return;

            foreach (CombatPressureRule rule in _ruleSet.Rules)
            {
                if (rule != null)
                    _observedBuildLevels[rule.Id] = RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id);
            }
        }

        private void EvaluateRules()
        {
            if (_ruleSet == null)
                return;

            IReadOnlyList<CombatPressureRule> rules = _ruleSet.Rules;
            for (int index = 0; index < rules.Count; index++)
            {
                CombatPressureRule rule = rules[index];
                if (rule == null)
                    continue;

                RuleRuntime state = GetRuleRuntime(index);
                string sourceId = GetSourceId(index);
                int ruleLevel = RoguelikeBuildRuntime.GetCombatRuleLevel(rule.Id);
                if (!rule.Enabled || ruleLevel <= 0)
                {
                    state.WasActive = false;
                    state.AppliedLevel = 0;
                    state.ComboHitRemainder = 0;
                    state.PulseActiveUntil = 0f;
                    CombatPressurePlayerModifiers.RemoveSource(sourceId);
                    CombatPressureSpawnWeights.RemoveSource(sourceId);
                    continue;
                }

                if (rule.Source == CombatPressureSource.Status)
                {
                    bool pulseActive = Time.time < state.PulseActiveUntil;
                    if (pulseActive)
                    {
                        if (!state.WasActive || state.AppliedLevel != ruleLevel)
                            ApplyRuleEffects(rule, index, ruleLevel);
                    }
                    else
                    {
                        CombatPressurePlayerModifiers.RemoveSource(sourceId);
                        CombatPressureSpawnWeights.RemoveSource(sourceId);
                        state.AppliedLevel = 0;
                    }

                    state.WasActive = pulseActive;
                    continue;
                }

                bool isActive = IsConditionActive(rule, ruleLevel);
                if (isActive)
                {
                    if (!state.WasActive || state.AppliedLevel != ruleLevel)
                        ApplyRuleEffects(rule, index, ruleLevel);

                    if (!state.WasActive || (rule.SpawnInterval > 0f && Time.time >= state.NextSpawnTime))
                    {
                        SpawnFocusedCollectible(rule, rule.SpawnCount + ruleLevel - 1);
                        state.NextSpawnTime = rule.SpawnInterval > 0f
                            ? Time.time + rule.SpawnInterval
                            : float.PositiveInfinity;
                    }
                }
                else
                {
                    if (rule.BuffDuration <= 0f)
                        CombatPressurePlayerModifiers.RemoveSource(sourceId);
                    CombatPressureSpawnWeights.RemoveSource(sourceId);
                    state.NextSpawnTime = 0f;
                    state.ComboHitRemainder = 0;
                }

                state.WasActive = isActive;
            }
        }

        private void ApplyRuleEffects(CombatPressureRule rule, int ruleIndex, int ruleLevel)
        {
            RuleRuntime state = GetRuleRuntime(ruleIndex);
            string sourceId = GetSourceId(ruleIndex);
            CombatPressurePlayerModifiers.SetSource(
                sourceId,
                ScaleMultiplier(rule.MoveSpeedMultiplier, ruleLevel),
                ScaleMultiplier(rule.AttachmentScaleMultiplier, ruleLevel),
                rule.BuffDuration);
            CollectibleData focus = GetFocusedCollectible(rule.Id);
            CollectibleType weightedType = focus != null
                ? focus.Type
                : rule.WeightedCollectibleType;
            CombatPressureSpawnWeights.SetSource(
                sourceId,
                weightedType,
                ScaleMultiplier(rule.NormalSpawnWeightMultiplier, ruleLevel));
            state.AppliedLevel = ruleLevel;
        }

        private bool IsConditionActive(CombatPressureRule rule, int level)
        {
            StatusTotals totals = rule.Source == CombatPressureSource.Status
                ? GetStatusTotals(rule.StatusType)
                : default;
            int metric = rule.GetMetricValue(_currentCombo, totals.AffectedEnemies, totals.TotalStacks);
            int threshold = CombatPressureProgression.GetEffectiveThreshold(rule.Threshold, level);
            return metric >= threshold;
        }

        private void SpawnFocusedCollectible(CombatPressureRule rule, int spawnCount)
        {
            Vector3 position = _lastCombatPosition;
            if (position == Vector3.zero && _spawnAnchor != null)
                position = _spawnAnchor.position;
            SpawnFocusedCollectibleAt(rule, position, spawnCount);
        }

        private void SpawnFocusedCollectibleAt(CombatPressureRule rule, Vector3 position, int spawnCount)
        {
            if (rule == null || spawnCount <= 0)
                return;

            if (_collectibleSpawner == null)
                ResolveSceneReferences();

            CollectibleData focusedCollectible = GetFocusedCollectible(rule.Id) ?? rule.FocusedCollectible;
            if (_collectibleSpawner == null || focusedCollectible == null)
                return;

            if (position == Vector3.zero && _spawnAnchor != null)
                position = _spawnAnchor.position;

            var context = new RoguelikePressureEffectContext(
                this,
                rule.Id,
                position,
                spawnCount,
                focusedCollectible,
                true);
            foreach ((RoguelikeEffectModule module, int level) in RoguelikeEffectRuntime.GetModules())
            {
                context.Level = level;
                module.ModifyPressureSpawnCount(context);
            }
            foreach ((RoguelikeEffectModule module, int level) in RoguelikeEffectRuntime.GetModules())
            {
                context.Level = level;
                module.OnPressureTriggered(context);
            }
            foreach ((RoguelikeEffectModule module, int level) in RoguelikeEffectRuntime.GetModules())
            {
                context.Level = level;
                module.InterceptPressureDrop(context);
            }
            if (!context.CancelDefault)
                EmitPressureDrop(rule.Id, position, context.SpawnCount, focusedCollectible, true);
        }

        private void EmitPressureDrop(
            string ruleId,
            Vector3 position,
            int spawnCount,
            CollectibleData collectible,
            bool allowEcho)
        {
            if (_collectibleSpawner == null || collectible == null || spawnCount <= 0)
                return;

            var context = new RoguelikePressureEffectContext(
                this,
                ruleId,
                position,
                spawnCount,
                collectible,
                allowEcho);
            foreach ((RoguelikeEffectModule module, int level) in RoguelikeEffectRuntime.GetModules())
            {
                context.Level = level;
                module.EmitPressureDrop(context);
            }
            if (!context.CancelDefault)
                _collectibleSpawner.SpawnCollectiblesFromAboveAt(position, context.SpawnCount, collectible);
            foreach ((RoguelikeEffectModule module, int level) in RoguelikeEffectRuntime.GetModules())
            {
                context.Level = level;
                module.AfterPressureDrop(context);
            }
        }

        private IEnumerator EchoDropRoutine(
            string ruleId,
            Vector3 position,
            int spawnCount,
            CollectibleData collectible,
            float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            EmitPressureDrop(ruleId, position, spawnCount, collectible, false);
        }

        private static void RecordCollectibleHit(ScriptableObject itemDataRaw)
        {
            if (itemDataRaw is CollectibleData collectible)
                RoguelikeEffectRuntime.RecordCollectibleHit(collectible.Type);
        }

        void IRoguelikeEffectHost.SpawnDefault(Vector3 position, int count, CollectibleData collectible)
        {
            _collectibleSpawner?.SpawnCollectiblesFromAboveAt(position, count, collectible);
        }

        void IRoguelikeEffectHost.SpawnCustom(
            Vector3 position,
            int count,
            CollectibleData collectible,
            float height,
            float scatter,
            float scale)
        {
            _collectibleSpawner?.SpawnCollectiblesFromAboveAt(position, count, collectible, height, scatter, scale);
        }

        void IRoguelikeEffectHost.EmitPressureDrop(
            string ruleId,
            Vector3 position,
            int count,
            CollectibleData collectible,
            bool allowEcho)
        {
            EmitPressureDrop(ruleId, position, count, collectible, allowEcho);
        }

        void IRoguelikeEffectHost.SchedulePressureDrop(
            string ruleId,
            Vector3 position,
            int count,
            CollectibleData collectible,
            float delaySeconds)
        {
            StartCoroutine(EchoDropRoutine(ruleId, position, count, collectible, delaySeconds));
        }

        bool IRoguelikeEffectHost.IsRuleAcquired(string ruleId)
            => RoguelikeBuildRuntime.IsCombatRuleAcquired(ruleId);

        void IRoguelikeEffectHost.FeedStatusProgress(string statusType, int amount, Vector3 position)
        {
            if (_isCrossFeeding)
                return;
            _isCrossFeeding = true;
            try
            {
                AddCumulativeStatusProgress(statusType, amount, position);
            }
            finally
            {
                _isCrossFeeding = false;
            }
        }

        private CombatPressureRule FindRule(string ruleId, out int ruleIndex)
        {
            ruleIndex = -1;
            if (_ruleSet == null)
                return null;

            for (int index = 0; index < _ruleSet.Rules.Count; index++)
            {
                CombatPressureRule rule = _ruleSet.Rules[index];
                if (rule != null && string.Equals(rule.Id, ruleId, StringComparison.Ordinal))
                {
                    ruleIndex = index;
                    return rule;
                }
            }

            return null;
        }

        private CombatPressureRule FindStatusRule(string statusType, out int ruleIndex)
        {
            ruleIndex = -1;
            if (_ruleSet == null)
                return null;

            for (int index = 0; index < _ruleSet.Rules.Count; index++)
            {
                CombatPressureRule rule = _ruleSet.Rules[index];
                if (rule != null &&
                    rule.Source == CombatPressureSource.Status &&
                    string.Equals(rule.StatusType, statusType, StringComparison.Ordinal))
                {
                    ruleIndex = index;
                    return rule;
                }
            }

            return null;
        }

        private RuleRuntime GetRuleRuntime(int index)
        {
            if (!_ruleRuntime.TryGetValue(index, out RuleRuntime state))
            {
                state = new RuleRuntime();
                _ruleRuntime[index] = state;
            }

            return state;
        }

        private int GetEnemyStatusStacks(string enemyId, string statusType)
        {
            return _statusesByEnemy.TryGetValue(enemyId, out Dictionary<string, int> statuses) &&
                   statuses.TryGetValue(statusType, out int stacks)
                ? stacks
                : 0;
        }

        private StatusTotals GetStatusTotals(string statusType)
        {
            int affectedEnemies = 0;
            int totalStacks = 0;
            foreach (Dictionary<string, int> statuses in _statusesByEnemy.Values)
            {
                if (statuses.TryGetValue(statusType, out int stacks) && stacks > 0)
                {
                    affectedEnemies++;
                    totalStacks += stacks;
                }
            }

            return new StatusTotals(affectedEnemies, totalStacks);
        }

        private void ClearAppliedEffects()
        {
            if (_ruleSet == null)
                return;

            for (int index = 0; index < _ruleSet.Rules.Count; index++)
            {
                string sourceId = GetSourceId(index);
                CombatPressurePlayerModifiers.RemoveSource(sourceId);
                CombatPressureSpawnWeights.RemoveSource(sourceId);
            }
        }

        private static bool ContainsStatus(IReadOnlyList<string> statuses, string target)
        {
            if (statuses == null)
                return false;

            for (int index = 0; index < statuses.Count; index++)
            {
                if (string.Equals(statuses[index], target, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static float ScaleMultiplier(float baseMultiplier, int level)
        {
            return 1f + Mathf.Max(0f, baseMultiplier - 1f) * Mathf.Max(1, level);
        }

        private string GetSourceId(int index) => $"{GetInstanceID()}:{index}";

        private void OnGUI()
        {
            if (!_showDebugOverlay || !Application.isPlaying)
                return;

            GUILayout.BeginArea(new Rect(12f, 140f, 290f, 160f), GUI.skin.box);
            GUILayout.Label("Combat Pressure Debug");
            GUILayout.Label($"Combo: {_currentCombo}");
            GUILayout.Label($"Poison累計: {GetCumulativeStatusProgress(PoisonStatus)}");
            GUILayout.Label($"Ice累計: {GetCumulativeStatusProgress(IceStatus)}");
            GUILayout.Label($"Speed x{CombatPressurePlayerModifiers.MoveSpeedMultiplier:0.00}");
            GUILayout.Label($"Hand x{CombatPressurePlayerModifiers.AttachmentScaleMultiplier:0.00}");
            GUILayout.EndArea();
        }

        private readonly struct StatusTotals
        {
            public readonly int AffectedEnemies;
            public readonly int TotalStacks;

            public StatusTotals(int affectedEnemies, int totalStacks)
            {
                AffectedEnemies = affectedEnemies;
                TotalStacks = totalStacks;
            }
        }
    }
}
