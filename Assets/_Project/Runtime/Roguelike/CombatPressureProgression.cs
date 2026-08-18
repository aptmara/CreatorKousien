using UnityEngine;

namespace Game.Core.Roguelike
{
    /// <summary>
    /// Combat Pressureの各レベルで解禁される固有挙動の値をまとめる。
    /// </summary>
    public static class CombatPressureProgression
    {
        private static float _thresholdReductionPerLevel = 0.15f;
        private static int _comboRecoveryUnlockLevel = 2;
        private static float _comboRecoverySecondsPerHit = 0.25f;
        private static int _comboEchoUnlockLevel = 3;
        private static int _comboEchoHitInterval = 3;
        private static int _poisonDefeatUnlockLevel = 3;
        private static int _poisonDefeatSpawnCount = 4;
        private static int _iceBreakFirstLevel = 2;
        private static int _iceBreakFirstCount = 3;
        private static int _iceBreakSecondLevel = 3;
        private static int _iceBreakSecondCount = 6;
        private static int _previewBaseCount = 2;
        private static int _previewLevelCap = 3;

        public static void Configure(
            float thresholdReductionPerLevel,
            int comboRecoveryUnlockLevel,
            float comboRecoverySecondsPerHit,
            int comboEchoUnlockLevel,
            int comboEchoHitInterval,
            int poisonDefeatUnlockLevel,
            int poisonDefeatSpawnCount,
            int iceBreakFirstLevel,
            int iceBreakFirstCount,
            int iceBreakSecondLevel,
            int iceBreakSecondCount,
            int previewBaseCount,
            int previewLevelCap)
        {
            _thresholdReductionPerLevel = Mathf.Max(0f, thresholdReductionPerLevel);
            _comboRecoveryUnlockLevel = Mathf.Max(1, comboRecoveryUnlockLevel);
            _comboRecoverySecondsPerHit = Mathf.Max(0f, comboRecoverySecondsPerHit);
            _comboEchoUnlockLevel = Mathf.Max(1, comboEchoUnlockLevel);
            _comboEchoHitInterval = Mathf.Max(1, comboEchoHitInterval);
            _poisonDefeatUnlockLevel = Mathf.Max(1, poisonDefeatUnlockLevel);
            _poisonDefeatSpawnCount = Mathf.Max(0, poisonDefeatSpawnCount);
            _iceBreakFirstLevel = Mathf.Max(1, iceBreakFirstLevel);
            _iceBreakFirstCount = Mathf.Max(0, iceBreakFirstCount);
            _iceBreakSecondLevel = Mathf.Max(_iceBreakFirstLevel, iceBreakSecondLevel);
            _iceBreakSecondCount = Mathf.Max(0, iceBreakSecondCount);
            _previewBaseCount = Mathf.Max(0, previewBaseCount);
            _previewLevelCap = Mathf.Max(0, previewLevelCap);
        }

        public static void ResetDefaults()
        {
            Configure(0.15f, 2, 0.25f, 3, 3, 3, 4, 2, 3, 3, 6, 2, 3);
        }

        public static int GetEffectiveThreshold(int baseThreshold, int level)
        {
            float reduction = Mathf.Clamp01(_thresholdReductionPerLevel * (Mathf.Max(1, level) - 1));
            return Mathf.Max(1, Mathf.CeilToInt(baseThreshold * (1f - reduction)));
        }

        public static float GetComboRecoverySeconds(int level, int hitCount)
        {
            return level >= _comboRecoveryUnlockLevel
                ? Mathf.Max(0, hitCount) * _comboRecoverySecondsPerHit
                : 0f;
        }

        public static int GetComboEchoSpawnCount(int level, int accumulatedHits)
        {
            return level >= _comboEchoUnlockLevel
                ? Mathf.Max(0, accumulatedHits) / _comboEchoHitInterval
                : 0;
        }

        public static int GetComboEchoRemainder(int level, int accumulatedHits)
        {
            return level >= _comboEchoUnlockLevel
                ? Mathf.Max(0, accumulatedHits) % _comboEchoHitInterval
                : 0;
        }

        public static int GetCompletedCycles(int progress, int gainedProgress, int threshold)
        {
            int total = Mathf.Max(0, progress) + Mathf.Max(0, gainedProgress);
            return total / Mathf.Max(1, threshold);
        }

        public static int GetRemainingProgress(int progress, int gainedProgress, int threshold)
        {
            int total = Mathf.Max(0, progress) + Mathf.Max(0, gainedProgress);
            return total % Mathf.Max(1, threshold);
        }

        public static int GetPoisonDefeatSpawnCount(int level, bool wasPoisoned)
        {
            return level >= _poisonDefeatUnlockLevel && wasPoisoned ? _poisonDefeatSpawnCount : 0;
        }

        public static int GetIceBreakSpawnCount(int level)
        {
            if (level >= _iceBreakSecondLevel) return _iceBreakSecondCount;
            return level >= _iceBreakFirstLevel ? _iceBreakFirstCount : 0;
        }

        public static int GetAcquisitionPreviewSpawnCount(int level)
        {
            return _previewBaseCount + Mathf.Clamp(level, 1, _previewLevelCap);
        }
    }
}
