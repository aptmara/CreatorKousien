using UnityEngine;
using CreatorKousien.Command;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;

namespace CreatorKousien.Command
{
    public class UseCardCommand : ICommand
    {
        // 実行方向
        private SlotDirection _registerSlotDirection;
        public SlotDirection RegisterSlotDirection => _registerSlotDirection;

        public UseCardCommand(SlotDirection registerSlotDirection)
        {
            _registerSlotDirection = registerSlotDirection;
        }
    }
    

    public class PickCommand : ICommand
    {
        private int _poolID;
        public int PoolID => _poolID;

        //! カード抽選枚数
        private int _pickCount;
        public int PickCount => _pickCount;
        public PickCommand(int poolID, int pickCount)
        {
            _poolID = poolID;
            _pickCount = pickCount;
        }
    }


    public class SetSlotCardCommand : ICommand
    {
        private Dictionary<SlotDirection, int> _slotIDs;
        public Dictionary<SlotDirection, int> SlotIDs => _slotIDs;
        public SetSlotCardCommand(Dictionary<SlotDirection, int> slotIDs)
        {
            _slotIDs = slotIDs;
        }
    }
}
