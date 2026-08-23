// ------------------------------------------------------------
// File		: BakuBurstResolver.cs
// Summary	: バクの爆発判定を管理する純粋なクラス
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using Game.Core.Enemy;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// 破裂の範囲ダメージ解決
    /// TODO: 対象は「他の敵」のみだけど、かわるかも？
    /// </summary>
    public static class BakuBurstResolver
    {
        /// <summary>
        /// 指定位置を中心に範囲ダメージを与える
        /// </summary>
        /// <param name="self">破裂したバク自身</param>
        /// <param name="center">破裂の中心位置</param>
        /// <param name="radius">破裂の半径</param>
        /// <param name="damage">与えるダメージ量</param>
        /// <returns>実際にダメージを与えた敵の数</returns>
        public static int ApplyBurst(EnemyController self, Vector3 center, float radius, float damage)
        {
            if (radius <= 0f || damage <= 0f) return 0;

            float squaredRadius = radius * radius;
            int hitCount = 0;

            // 敵専用レイヤーが未定義のため、既存のEnemySpawner同様、全敵を操作
            // TODO: 流石に共通レイヤー作ったほうがいい説はあるけど、一旦単発イベントだから…((
            EnemyController[] enemies = Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

            foreach (EnemyController enemy in enemies)
            {
                if (enemy == null) continue;
                // 自分自身は除外
                if (enemy == self) continue;
                if (!enemy.gameObject.activeInHierarchy) continue;
                // ボスは巻き込まない
                // ここ変えたらボスにもダメージはいるぜ
                if (enemy.IsBoss) continue;
                if (enemy.CurrentState == EnemyState.Defeated) continue;
                if (enemy.CurrentState == EnemyState.Drop) continue;

                // 敵は地下から上昇してくるためYがバラつくので、高さ差は無視して水平距離で判定する
                Vector3 offset = enemy.transform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > squaredRadius) continue;

                enemy.OnBodyHit(damage);

                // OnBodyHitを直接呼ぶだけではEnemyHitReceiverを通らず、ヒットアニメ・スカッシュ演出が一切鳴らないので手動で発光させる
                EnemyHitReceiver receiver = enemy.GetComponentInChildren<EnemyHitReceiver>(true);

                if (receiver != null)
                {
                    receiver.OnHitAction?.Invoke();
                }

                hitCount++;
            }

            return hitCount;
        }
    }
}
