// ================================================================================
// File         : PlayerActionCommand.cs
// Author       : Iwai Shogo
//
// Description  : プレイヤーがアクション(カード)を選択した際に発行されるコマンド。
// Created      : 2026-04-20
// ================================================================================

using CreatorKousien.Data;

namespace CreatorKousien.Command
{
    /// <summary>
    /// プレイヤーの行動を要求するコマンド
    /// </summary>
    public class PlayerActionCommand : ICommand
    {
        public SlotPosition Slot { get; }

        public PlayerActionCommand(SlotPosition slot)
        {
            Slot = slot;
        }
    }
}
