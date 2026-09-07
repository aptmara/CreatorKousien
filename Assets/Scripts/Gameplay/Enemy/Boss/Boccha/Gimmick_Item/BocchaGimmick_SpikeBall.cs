// ------------------------------------------------------------
// File		: BocchaGimmick_SpikeBall.cs
// Summary	: トゲ玉を散布するギミック
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 調整値（サイズ・数）はこのSOのScale Multiplier / Spawn Countで行う!
// - バリアダメージ量はPrefab側のBocchaSpikeBallで設定する。
// ------------------------------------------------------------
using Game.Core.Events;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// トゲ玉を生成するギミック
    /// </summary>
    [UnityEngine.CreateAssetMenu(
        fileName = "Gimmick_BocchaSpikeBall", menuName = "Boss/Gimmicks/Boccha/SpikeBall")]
    public sealed class BocchaGimmick_SpikeBall : BocchaHazardGimmickBase
    {
        protected override BocchaHazardType HazardType => BocchaHazardType.SpikeBall;
    }
}
