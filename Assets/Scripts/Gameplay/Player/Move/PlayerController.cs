// ------------------------------------------------------------
// File		: PlayerController.cs
// Summary	: プレイヤーの移動を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6 ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの移動を管理するクラス
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("コンポーネント設定")]
        [Tooltip("入力読み取りコンポーネント")]
        [SerializeField] private PlayerInputReader _inputReader;

        [Tooltip("移動制御コンポーネント")]
        [SerializeField] private PlayerMotor _motor;

        private bool _canMove = true;    ///< プレイヤーが移動可能かどうかを示すフラグ



        // 関数処理
        // ------------------------------------------------------------

        ///<summary>
        /// 物理演算の更新処理
        /// </summary>
        private void FixedUpdate()
        {
            if (!_canMove)
            {
                return;
            }

            // Facadeとして入力を取得し、Motorに移動処理を渡す
            PlayerInputState input = _inputReader.CurrentInput;
            _motor.Move(input.MoveDirection);
        }

        /// <summary>
        /// プレイヤーの移動可否を設定する関数
        /// </summary>
        /// <param name="canMove">移動可能かどうか</param>
        public void SetCanMove(bool canMove)
        {
            _canMove = canMove;

            if (!_canMove)
            {
                _motor.Move(Vector2.zero); // 移動を停止させる
            }
        }
    }
}
