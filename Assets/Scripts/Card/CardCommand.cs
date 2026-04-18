using UnityEngine;
using CreatorKousien.Command;
using UnityEngine.UI;
using NUnit.Framework;
using System.Collections.Generic;
public class RegisterSlotCardCommand : ICommand
{
    // 実行ID
    private SlotDirection _registerSlotDirection;
    public SlotDirection RegisterSlotDirection => _registerSlotDirection;

    public RegisterSlotCardCommand(SlotDirection registerSlotDirection)
    {
        _registerSlotDirection = registerSlotDirection;
    }
}

public class ApplyEffectCommand : ICommand
{
    public ApplyEffectCommand()
    {
    }
}

public class SetPoolCommand : ICommand
{
    private int _poolID;
    public int PoolID => _poolID;

    public SetPoolCommand(int poolID)
    {
        _poolID = poolID;
    }
}

public class PickCommand : ICommand
{
    //! カード抽選枚数
    private int _pickCount;
    public int PickCount => _pickCount;
    public PickCommand(int pickCount)
    {
        _pickCount = pickCount;
    }
}

public class AdvanceTurnCommand : ICommand
{
    private int _turnToAdvance;
    public int TurnToAdvance => _turnToAdvance;

    public AdvanceTurnCommand(int turnToAdvance)
    {
        _turnToAdvance = turnToAdvance;
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
