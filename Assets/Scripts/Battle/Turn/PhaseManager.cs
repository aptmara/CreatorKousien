// ================================================================================
// File         : PhaseManager.cs
// Author       : Iwai Shogo
//
// Description  : フェーズの切り替えと実行を管理するステートマシン。
// Created      : 2026-04-18
// ================================================================================

using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// フェーズの切り替えと実行を管理するステートマシン
    /// </summary>
    public class PhaseManager
    {
        private readonly Dictionary<PhaseType, IBattlePhaseState> _states = new Dictionary<PhaseType, IBattlePhaseState>();
        private IBattlePhaseState _currentState;

        /// <summary>
        /// フェーズを登録
        /// </summary>
        /// <param name="state"></param>
        public void RegisterState(IBattlePhaseState state)
        {
            _states[state.Type] = state;
        }

        /// <summary>
        /// フェーズを強制的に切り替える
        /// </summary>
        /// <param name="nextPhase"></param>
        public void TransitionTo(PhaseType nextPhase)
        {
            if (!_states.ContainsKey(nextPhase))
            {
                Debug.LogError($"[PhaseManager] {nextPhase} ステートが登録されていません。");
                return;
            }

            Debug.Log($"<color=cyan>[Phase] Exit: {CurrentPhaseType} -> Enter: {nextPhase}</color>");

            _currentState?.Exit();
            _currentState = _states[nextPhase];
            _currentState.Enter();
        }

        /// <summary>
        /// 現在のフェーズのUpdateを回す
        /// </summary>
        public void Update()
        {
            _currentState?.Update();
        }

        public PhaseType CurrentPhaseType => _currentState?.Type ?? PhaseType.Init;
    }
}
