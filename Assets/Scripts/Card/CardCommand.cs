using UnityEngine;
using CreatorKousien.Command;
using UnityEngine.UI;
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
