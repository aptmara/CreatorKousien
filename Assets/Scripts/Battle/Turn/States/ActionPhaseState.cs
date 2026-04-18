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
        private readonly TurnManager _owner;
        private bool _isActionProcessing = false;

        public ActionPhaseState(TurnManager owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("--- アクションフェーズ開始 ---");
            ProcessNext();
        }

        public void Update()
        {
            // TODO: EventBus 等からの演出終了通知を待機
        }

        private void ProcessNext()
        {
            _owner.ExecuteNextAction();
        }

        public void Exit()
        {
            Debug.Log("--- アクションフェーズ終了 ---");
        }

        /// <summary>
        /// View側のアニメーションが完了した時に、外部から呼ばれる想定
        /// </summary>
        public void NotifyActionComplete()
        {
            // 次の1手を実行
            ProcessNext();
        }
    }
}
