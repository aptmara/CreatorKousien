// ================================================================================
// File         : ActionPhaseState.cs
// Author       : Iwai Shogo
//
// Description  : アクションフェーズの処理を行います 。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// アクションフェーズの処理を行います。
    /// </summary>
    public class ActionPhaseState : IBattlePhaseState
    {
        public PhaseType Type => PhaseType.Action;

        public void Enter()
        {
            Debug.Log("--- アクションフェーズ開始 ---");
        }

        public void Update()
        {
            // キューの消化を TurnManager に依頼する
        }

        public void Exit()
        {
            Debug.Log("--- アクションフェーズ終了 ---");
        }
    }
}
