using CreatorKousien.Command;
using CreatorKousien.Effect;
using System.Collections.Generic;
using UnityEditor.Rendering.Universal;
using UnityEditor.Toolbars;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


public class EffectCommandFactory
{
    public List<ICommand> EffectToCommand(int userActorID ,EffectData effectData)
    {
        List<ICommand> commands = new List<ICommand>();

        // 各値を読み込みコマンドを作成
        // TODO データ形式を変更する、そもそも複数コマンド作成するか要検討
        foreach (var value in effectData.Value)
        {
            ICommand addEffectCommand = CreateEffectCommand(userActorID, value, effectData.TargetCells);
            if (addEffectCommand != null)
            {
                commands.Add(addEffectCommand);
            }
        }
        // 移動している場合コマンドを作成
        if(IsMove(effectData))
        {
            ICommand addMoveCommand = CreateMoveCommand(userActorID, effectData);
            commands.Add(addMoveCommand);
        }

        return commands;
    }

    ICommand CreateEffectCommand(int userActorID, EffectValue effectValue, List<Vector2Int> targetCells)
    {
        ICommand command = null;
        // Value
        switch(effectValue.ValueType)
        {
            case EffectValueType.Attack:
                {
                    CreatorKousien.Data.AttackProperty attackProperty = new CreatorKousien.Data.AttackProperty();
                    attackProperty.Type = CreatorKousien.Data.AttackPatternType.Normal;
                    attackProperty.DamageMultiplier = effectValue.Value;
                    attackProperty.HitCount = 1;
                    command = new AttackCommand(userActorID, attackProperty, targetCells);
                }
                break;

        }

        return command;
    }


    // TODO データを改善し移動処理を追加
    ICommand CreateMoveCommand(int userActorID, EffectData effectData)
    {
        ICommand command = null;

        return command;
    }
    // 移動可能か確認
    bool IsMove(EffectData effectData)
    {
        return effectData.MoveValue.x != 0 || effectData.MoveValue.y != 0; 
    }
}
