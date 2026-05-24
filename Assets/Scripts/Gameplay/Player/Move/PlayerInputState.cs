// ------------------------------------------------------------
// File		: PlayerInputState.cs
// Summary	: プレイヤーの入力状態を管理する構造体定義
//
// Author	: [浅野勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの入力状態を保持する構造体
    /// </summary>
    public struct PlayerInputState
    {
        /// <summary>
        /// 移動入力ベクトル
        /// </summary>
        public Vector2 MoveDirection;

        /// <summary>
        /// 回転方向入力ベクトル
        /// </summary>
        public Vector2 LookDirection;

        /// <summary>
        /// アタッチメントのスケールの入力状態を示すフラグ
        /// </summary>
        public bool AttachmentScaleHeld;
    }
}
