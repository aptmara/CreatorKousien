// ================================================================================
// File         : EnemySystem.cs
// Author       : Iwai Shogo
//
// Description  : 敵キャラクター全体を管理するシステム。
// Created      : 2026-04-17
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Battle;

namespace CreatorKousien.Enemy
{
    public class EnemySystem
    {
        // ActorIDをキーにして、敵のデータとAIを管理する
        private Dictionary<int, EnemyRuntimeData> _enemyDataMap = new Dictionary<int, EnemyRuntimeData>();
        private Dictionary<int, EnemyAI> _enemyAIMap = new Dictionary<int, EnemyAI>();

        private ActionTelegraphSystem _telegraphSystem;

        public EnemySystem(ActionTelegraphSystem telegraphSystem)
        {
            _telegraphSystem = telegraphSystem;
        }

        /// <summary>
        /// バトル開始時に呼ばれ、敵を生成・登録する
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="data"></param>
        /// <param name="spawnPos"></param>
        public void SpawnEnemy(int actorId, EnemyData data, Vector2Int spawnPos)
        {
            var runtimeData = new EnemyRuntimeData
            {
                ActorId = actorId,
                EnemyId = data.EnemyId,
                Position = spawnPos,
                CurrentHp = data.MaxHp,
                CurrentAttack = data.Attack
            };

            var ai = new EnemyAI(runtimeData, data);

            _enemyDataMap[actorId] = runtimeData;
            _enemyAIMap[actorId] = ai;
        }

        /// <summary>
        /// 対象の敵にダメージを与え、死亡した場合は true を返す
        /// </summary>
        /// <param name="actorId"></param>
        /// <param name="damage"></param>
        /// <returns></returns>
        public bool TakeDamage(int actorId, int damage)
        {
            var data = GetEnemyData(actorId);
            if (data == null) return false;

            data.CurrentHp -= damage;
            Debug.Log($"[EnemySystem] 敵(ID:{actorId}) に {damage} ダメージ！ 残りHP: {data.CurrentHp}");

            if (data.CurrentHp <= 0)
            {
                OnDeath(actorId);
                return true;
            }

            return false;
        }

        private void OnDeath(int actorId)
        {
            Debug.Log($"<color=red>[EnemySystem] 敵(ID:{actorId}) 撃破！！</color>");

            // この敵が予約していた攻撃をすべて消去
            _telegraphSystem.CancelByActorId(actorId);

            // 辞書からデータを削除
            _enemyDataMap.Remove(actorId);
            _enemyAIMap.Remove(actorId);
        }

        /// <summary>
        /// 対象の敵AIを取得する
        /// </summary>
        /// <param name="actorId"></param>
        /// <returns></returns>
        public EnemyAI GetEnemyAI(int actorId)
        {
            return _enemyAIMap.TryGetValue(actorId, out var ai) ? ai : null;
        }

        /// <summary>
        /// 対象の敵データを取得する
        /// </summary>
        /// <param name="actorId"></param>
        /// <returns></returns>
        public EnemyRuntimeData GetEnemyData(int actorId)
        {
            return _enemyDataMap.TryGetValue(actorId, out var data) ? data : null;
        }
    }
}
