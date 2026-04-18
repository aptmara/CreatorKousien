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

        public void Enter()
        {
            Debug.Log("--- コマンドフェーズ開始 ---");
        }

        public void Update()
        {
            // 時間ゲージのカウントダウンロジック、敵の思考、カード選択等
        }

        public void Exit()
        {
            Debug.Log("--- コマンドフェーズ終了 ---");
        }
    }
}
