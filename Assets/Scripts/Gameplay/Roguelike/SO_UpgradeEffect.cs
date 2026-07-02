//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : SO_UpgradeEffect.cs
// brief  : 
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using UnityEngine;

public abstract class SO_UpgradeEffect : ScriptableObject
{
    public abstract void Apply(UpgradeApplyContext context, int level);
}
