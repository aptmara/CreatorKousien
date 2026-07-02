// ================================================================================
// File         : GameProgressionState.cs
// Author       : Iwai Shogo
//
// Description  : ゲーム全体の進行状態を表すステート
// Created      : 2026-07-02
// ================================================================================

namespace Game.Core.Management
{
    /// <summary>
    /// ゲーム全体の進行状態を表すステート
    /// </summary>
    public enum GameProgressionState
    {
        Setup,      // ゲーム開始時の初期化・シーン読み込み中
        Battle,     // 通常のウェーブ迎撃バトル中
        Roguelike,  // ローグライクモード中
        Result,     // ゲームクリア or ゲームオーバー状態
    }
}
