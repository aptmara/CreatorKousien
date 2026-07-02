//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_MoveSpeedEffect.cs
// brief  : 移動速度Up.
//
// auther : Shohei Takitani
// date   : 2026/07/01 - begin
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

[CreateAssetMenu(fileName = "SO_MoveSpeedEffect", menuName = "Scriptable Objects/SO_MoveSpeedEffect")]
public class SO_MoveSpeedEffect : SO_UpgradeEffect
{
    [SerializeField] private float addSpeedPerLevel = 0.5f;

    public override void Apply(UpgradeApplyContext context, int level)
    {
        if (context.RuntimeData == null) return;

        float addValue = addSpeedPerLevel * level;
        context.RuntimeData.MoveSpeedMultiplier += addValue;
    }
}
