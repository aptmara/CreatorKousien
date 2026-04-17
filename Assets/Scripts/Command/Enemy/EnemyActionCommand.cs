// ================================================================================
// File         : EnemyActionCommand.cs
// Author       : Iwai Shogo
//
// Description  : 敵に行動を促すコマンド。
// Created      : 2026-04-17
// ================================================================================

namespace CreatorKousien.Command
{
    /// <summary>
    /// 指定した敵キャラクターに行動を要求するコマンド
    /// </summary>
    public sealed class EnemyActionCommand : ICommand
    {
        /// <summary>
        /// 行動させたい敵のActorID
        /// </summary>
        public int EnemyActorId { get; }

        public EnemyActionCommand(int enemyActorId)
        {
            EnemyActorId = enemyActorId;
        }
    }
}
