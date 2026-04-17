// ================================================================================
// File         : EnemyIntent.cs
// Author       : Iwai Shogo
//
// Description  : 敵の予定をまとめる箱
// Created      : 2026-04-18
// ================================================================================

using UnityEngine;
using CreatorKousien.Data;
using System.Collections.Generic;

namespace CreatorKousien.Enemy
{
    /// <summary>
    /// 準備フェーズでEnemyAIが提出する「このターンに行う内容」
    /// </summary>
    public class EnemyIntent
    {
        public int SourceActorId;
        public AttackProperty AttackInfo;
        public List<Vector2Int> RawTargetCells;
        public int ChargeTurns;
        public bool IsInterruptible;
    }
}
