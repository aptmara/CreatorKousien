// ------------------------------------------------------------
// File		: IBocchaHazard.cs
// Summary	: ボスの爆発で一掃されるお邪魔アイテムのインターフェース
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 固定物(BocchaHazardBase)と落し物(BocchaSpikeBall)を同じレジストリで扱うために用意する
// ------------------------------------------------------------
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスの爆発時に消えるお邪魔アイテム。
    /// </summary>
    public interface IBocchaHazard
    {
        /// <summary>お邪魔アイテムの種類</summary>
        BocchaHazardType HazardType { get; }

        /// <summary>現在位置。一掃時のVFX表示に使う。</summary>
        Vector3 Position { get; }

        /// <summary>強制的に消滅させる</summary>
        void ForceDespawn();
    }
}
