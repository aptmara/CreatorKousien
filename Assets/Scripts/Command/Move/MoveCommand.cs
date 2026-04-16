// ------------------------------------------------------------
// File		: MoveCommand.cs
// Summary	: 移動を表すコマンド
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - 設計書に基づいて、移動を表すコマンドを定義しています。
// - MoveTypeなどは必要に応じて追加する予定です。
// ------------------------------------------------------------
using UnityEngine;

namespace CreatorKousien.Command
{
    /// <summary>
    /// 移動を表すコマンド
    /// </summary>
    public sealed class MoveCommand : ICommand
    {
        /// <summary>
        /// 移動させるオブジェクトのID
        /// </summary>
        public int MoverId { get; }


        /// <summary>
        /// 移動方向
        /// </summary>
        public GridDirection Direction { get; }


        /// <summary>
        /// 移動量
        /// </summary>
        public int Distance { get; }


        /// <summary>
        /// 移動を表すコマンドのコンストラクタ
        /// </summary>
        /// <param name="moveId">移動させるオブジェクトのID</param>
        /// <param name="direction">移動方向</param>
        /// <param name="distance">移動量</param>
        public MoveCommand(int moveId, GridDirection direction, int distance)
        {
            MoverId = moveId;
            Direction = direction;
            Distance = distance;
        }
    }
}
