// ------------------------------------------------------------
// File		: BocchaGimmick_StoneCandy.cs
// Summary	: 石化キャンディを散布するギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 調整値（持続時間・サイズ・数）は Prefab側の Life Time と、このSOの Scale / Spawn Count で行ってくれ！！
// ------------------------------------------------------------
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 石化キャンディを生成するギミック
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaStoneCandy", menuName = "Boss/Gimmicks/Boccha/StoneCandy")]
    public sealed class BocchaGimmick_StoneCandy : BocchaHazardGimmickBase
    {
        protected override BocchaHazardType HazardType => BocchaHazardType.StoneCandy;
    }
}
