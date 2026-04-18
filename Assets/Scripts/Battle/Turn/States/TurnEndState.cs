// ================================================================================
// File         : TurnEndState.cs
// Author       : Iwai Shogo
//
// Description  : ターン終了の処理を行います。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// ターン終了フェーズの処理を行います。
    /// </summary>
    public class TurnEndState : IBattlePhaseState
    {
        public PhaseType Type => PhaseType.TurnEnd;
        private readonly TurnManager _owner;

        public TurnEndState(TurnManager owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("--- ターン終了処理開始 ---");
        }

        public void Update()
        {
            // バフなどの計算が終わったら自動で次へ行く予定
        }

        public void Exit()
        {
            Debug.Log("--- ターン終了処理終了 ---");
        }
    }
}
