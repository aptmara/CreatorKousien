// ================================================================================
// File         : IBattlePhaseState.cs
// Author       : Iwai Shogo
//
// Description  : 全てのフェーズが共通で持つ台本のインターフェース。
// Created      : 2026-04-18
// ================================================================================

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 全てのフェーズが共通で持つ台本のインターフェース
    /// </summary>
    public interface IBattlePhaseState
    {
        PhaseType Type { get; }
        void Enter();   // フェーズ開始時に1回呼ばれる
        void Update();  // 毎フレーム呼ばれる
        void Exit();    // フェーズ終了時に1回呼ばれる
    }
}
