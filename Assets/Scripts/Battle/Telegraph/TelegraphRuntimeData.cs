// ================================================================================
// File         : TelegraphRuntimeData.cs
// Author       : Iwai Shogo
//
// Description  : 現在盤面上に出ている攻撃の予約表のデータ。
// Created      : 2026-04-13
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;

namespace CreatorKousien.Battle
{
    /// <summary>
    /// 盤面に表示されている1つ分の予告データ。
    /// エネミーかプレイヤーかは ActorId で判別する。
    /// </summary>
    public class TelegraphRuntimeData
    {
        public int TelegraphId;                 // この予告自体の固有ID
        public int SourceActorId;               // 誰が出した予告か
        public List<Vector2Int> TargetCells;    // 赤く光るマスの座標リスト
        public int RemainingTurn;               // 発動までの残りターン
        public ActionProperty Property;         // 攻撃の種類
        public bool IsInterruptible;            // プレイヤーの攻撃でキャンセル可能か
    }
}
