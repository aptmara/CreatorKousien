using UnityEngine;
using Game.Data.Collectibles;

namespace Game.Gameplay.Roguelike.CombatPressure
{
    /// <summary>
    /// Play Modeでコンボ・状態異常圧力を直接再現するためのテスト用コンポーネント。
    /// </summary>
    public sealed class CombatPressureTester : MonoBehaviour
    {
        [SerializeField] private CombatPressureController _controller;
        [SerializeField, Min(0)] private int _debugCombo = 50;
        [SerializeField] private string _statusType = "Poison";
        [SerializeField, Min(0)] private int _affectedEnemyCount = 4;
        [SerializeField, Min(0)] private int _totalStackCount = 12;
        [SerializeField] private string _bindingRuleId = "combo-gummy";
        [SerializeField] private CollectibleData _bindingTarget;

        public CombatPressureController Controller => _controller;
        public int DebugCombo => _debugCombo;
        public string StatusType => _statusType;
        public int AffectedEnemyCount => _affectedEnemyCount;
        public int TotalStackCount => _totalStackCount;
        public CollectibleData BindingTarget => _bindingTarget;

        private void Awake()
        {
            if (_controller == null)
                _controller = Object.FindFirstObjectByType<CombatPressureController>();
        }

        public void ApplyCombo()
        {
            _controller?.AcquireAllRulesForDebug();
            _controller?.SetDebugCombo(_debugCombo);
        }

        public void AddCombo(int amount)
        {
            if (_controller == null)
                return;

            _debugCombo = Mathf.Max(0, _controller.CurrentCombo + amount);
            _controller.SetDebugCombo(_debugCombo);
        }

        public void ApplyStatus()
        {
            _controller?.AcquireAllRulesForDebug();
            _controller?.SetDebugStatus(_statusType, _affectedEnemyCount, _totalStackCount);
        }

        public void BindFocusedCollectible()
        {
            _controller?.BindFocusedCollectible(_bindingRuleId, _bindingTarget);
        }

        public void ResetPressure()
        {
            _controller?.ResetPressure();
        }
    }
}
