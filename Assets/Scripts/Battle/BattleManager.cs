// ================================================================================
// File         : BattleManager.cs
// Author       : Iwai Shogo
//
// Description  : ダメージ計算、HP増減、生死判定のみを担当する純粋なバトルロジック
// Created      : 2026-04-17
// ================================================================================

using System.Collections.Generic;
using UnityEngine;
using CreatorKousien.Data;
using CreatorKousien.Enemy;
using CreatorKousien.Player;

namespace CreatorKousien.Battle
{
    public class BattleManager
    {
        private PlayerSystem _playerSystem;
        private EnemySystem _enemySystem;

        public BattleManager(PlayerSystem playerSystem, EnemySystem enemySystem)
        {
            _playerSystem = playerSystem;
            _enemySystem = enemySystem;
        }

        /// <summary>
        /// AttackUseCaseから呼ばれる、ダメージ計算と適用のメイン処理
        /// </summary>
        /// <param name="sourceActorId"></param>
        /// <param name="targetActorId"></param>
        /// <param name="property"></param>
        public void ResolveAttack(int sourceActorId, List<int> targetActorIds, AttackProperty property)
        {
            // 1. 攻撃者の基礎攻撃力を取得
            int baseAttack = GetBaseAttack(sourceActorId);

            // 2. 最終ダメージを計算 (一応実装しておく)
            int finalDamage = Mathf.RoundToInt(baseAttack * property.DamageMultiplier);

            Debug.Log($"[BattleManager] Actor:{sourceActorId} の攻撃！(基礎 {baseAttack} × 倍率 {property.DamageMultiplier} = 最終ダメージ {finalDamage})");

            // 3. 各ターゲットにダメージを適用
            foreach (int targetId in targetActorIds)
            {
                ApplyDamage(targetId, finalDamage);
            }
        }

        /// <summary>
        /// ActorIdから基礎攻撃力を取得する
        /// </summary>
        /// <param name="actorId"></param>
        /// <returns></returns>
        private int GetBaseAttack(int actorId)
        {
            if (actorId == 1) // ID = 1: プレイヤー
            {
                // TODO: PlayerRuntimeDataに実装され次第
                // return _playerSystem.RuntimeData.CurrentAttack;
                return 10;
            }
            else
            {
                var enemyData = _enemySystem.GetEnemyData(actorId);
                return enemyData != null ? enemyData.CurrentAttack : 0;
            }
        }

        /// <summary>
        /// 対象のHPを減らし、生死判定を行う
        /// </summary>
        /// <param name="targetId"></param>
        /// <param name="damage"></param>
        private void ApplyDamage(int targetId, int damage)
        {
            if (targetId == 1) // Player
            {
                // プレイヤーへのダメージ適用
                _playerSystem.ChangeHp(-damage);
                Debug.Log($"[BattleManager] プレイヤー(ID:1) に {damage} ダメージ！ 残りHP: {_playerSystem.RuntimeData.CurrentHp}");
            }
            else
            {
                // 敵へのダメージ適用
                var enemyData = _enemySystem.GetEnemyData(targetId);
                if (enemyData != null)
                {
                    enemyData.CurrentHp -= damage;
                    Debug.Log($"[BattleManager] 敵(ID:{targetId}) に {damage} ダメージ！ 残りHP: {enemyData.CurrentHp}");

                    // 死亡判定
                    if (enemyData.CurrentHp <= 0)
                    {
                        Debug.Log($"[BattleManager] 敵(ID:{targetId}) 撃破！！");
                        // TODO: EnemySystemの死亡処理、死亡通知を飛ばすEventBus?
                    }
                }
            }
        }
    }
}
