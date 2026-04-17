// ================================================================================
// File         : EnemyRuntimeData.cs
// Author       : Iwai Shogo
//
// Description  : 盤面に存在する敵の個体の実行時状態を保持するクラス。
// Created      : 2026-04-13
// ================================================================================

using UnityEngine;

namespace CreatorKousien.Enemy
{
    public class EnemyRuntimeData
    {
        public int ActorId;         // 盤面にいる個体の管理番号
        public int EnemyId;         // 大元の設計図のID
        public Vector2Int Position; // 現在の座標
        public int CurrentHp;       // 今のHP
        public int CurrentAttack;   // 今の基礎攻撃力
    }
}
