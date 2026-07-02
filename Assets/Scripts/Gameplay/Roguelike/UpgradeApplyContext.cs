//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : UpgradeApplyContext.cs
// brief  : 強化効果の適応先をまとめる
//
// auther : Shohei Takitani
// date   : 2026/06/30 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using Game.Gameplay.Combo;
using Game.Gameplay.Player.Progression;
using UnityEngine;

public class UpgradeApplyContext
{
   public ComboManager ComboManager { get; }        // 一旦使用しない
    public PlayerRuntimeData RuntimeData { get; }

    public UpgradeApplyContext(
          PlayerRuntimeData runtimeData)
    {
        RuntimeData = runtimeData;
    }

}
