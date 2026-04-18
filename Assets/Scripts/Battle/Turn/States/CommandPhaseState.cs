// ================================================================================
// File         : CommandPhaseState.cs
// Author       : Iwai Shogo
//
// Description  : コマンドフェーズの処理を行います。
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// コマンドフェーズの処理を行います。
    /// </summary>
    public class CommandPhaseState : IBattlePhaseState
    {
        public PhaseType Type => PhaseType.Command;
        private readonly TurnManager _owner;

        public CommandPhaseState(TurnManager owner) => _owner = owner;

        public void Enter()
        {
            Debug.Log("--- コマンドフェーズ開始 ---");
            // TODO: UIに準備開始の演出を出す、時間ゲージのリセットなど
        }

        public void Update()
        {
            // 時間ゲージのカウントダウンロジック、敵の思考、カード選択等
        }

        /// <summary>
        /// 時間切れ、またはプレイヤーの決定ボタンで呼ばれる
        /// </summary>
        public void Exit()
        {
            // ここでプレイヤーの3手と敵の3手を合体させて交互に並べる
            _owner.TransitionTo(PhaseType.Action);
        }
    }
}
