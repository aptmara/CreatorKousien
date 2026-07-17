//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : ModifierMath
// brief  : StatModifierの値を計算
// auther : Takitani Shohei
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/


using System;

namespace Game.Data.Player
{
    public static class ModifierMath
    {
        public static float Calculate(
            float current, ModifierOperation operation, float value)
        {
            float result;

            switch(operation)
            {
                case ModifierOperation.Add:
                    result = current + value;
                    break;
                case ModifierOperation.Multiply:
                    result = current * value;
                    break;
                case ModifierOperation.SubTract:
                    result = current - value;
                    break;

                default:
                    result = current;
                    break;
            }

            return MathF.Max(0.0f, result);
        }
    }
}
