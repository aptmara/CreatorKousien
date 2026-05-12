// ------------------------------------------------------------
// File		: PlayerInputReader.cs
// Summary	: New Input Systemからプレイヤーの入力を読み取るクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの入力を読み取るクラス
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        private PlayerInputState _currentInput;     ///< 現在の入力状態

        /// <summary>
        /// 現在の入力状態を取得
        /// </summary>
        public PlayerInputState CurrentInput
        {
            get { return _currentInput; }
        }

        /// <summary>
        /// 移動入力時のコールバック
        /// </summary>
        /// <param name="context">入力コンテキスト</param>
        public void OnMove(InputAction.CallbackContext context)
        {
            _currentInput.MoveDirection = context.ReadValue<Vector2>();
        }

        /// <summary>
        /// 回転更新時のコールバック
        /// </summary>
        /// <param name="context">回転コンテキスト</param>
        public void OnRotate(InputAction.CallbackContext context)
        {
            _currentInput.LookDirection = context.ReadValue<Vector2>();
        }

    }
}
