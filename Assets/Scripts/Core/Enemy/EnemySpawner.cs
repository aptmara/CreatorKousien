/**
 * 作成：寺田晴
 *
 * 内容：敵を生成する
 *
 * 更新履歴：
 * 5/30: 敵生成時、重なってスポーンしないように修正しました - 浅野
 *       自動で敵がスポーンするようにしました。
 *
 *
 * 6/24: データをSO化した！
 *       生成方法を空のゲームオブジェクトを出してコンポーネントつけて
 *       子オブジェクトに当たり判定用オブジェクトを生成する形に変更した！
 *
 *
 * 7/02: ゲームループ (バトル -> ローグライク) 対応の為、単一ウェーブ管理型へリファクタリング。
 *       ウェーブ進行管理を GameProgressionManager へ分離。 - Iwai a.k.a. ZEUS
 *
 * 7/11: ボスの追加 - Y_Akira
 *
 * 7/15: EnemySpawnerほぼ全部修正。新たにつくったWave管理のデータから敵をスポーンするように変更。 - Asano a.k.a. Gachi
 */
using UnityEngine;
using Game.Presentation.UI;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 命令された単一ウェーブの敵を管理・スポーンするスポナー
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Tooltip("生成する範囲(2D:横幅*奥行)")]
        [SerializeField] private Vector2 _rangeSize;

        [Header("状態異常VFX設定")]
        [Tooltip("敵の状態異常ごとのVFX・マテリアル設定。EnemyStatusAilmentVFX.StatusAilmentVFXEntry と対応。")]
        [SerializeField] private EnemyStatusAilmentVFX.StatusAilmentVFXEntry[] _statusAilmentVFXEntries = System.Array.Empty<EnemyStatusAilmentVFX.StatusAilmentVFXEntry>();

        [Tooltip("状態異常VFXをアタッチする基準Transform。null の場合は敵ルート自身を使用。")]
        [SerializeField] private Transform _statusAilmentVFXRoot;

        [Tooltip("状態異常VFXのローカルオフセット（敵の上に表示したい場合は Y を正に）")]
        [SerializeField] private Vector3 _statusAilmentVFXOffset = new Vector3(0.24f, 1.31f, 0f);

        [Header("出現位置の設定")]
        [Tooltip("最終目標地点")]
        [SerializeField] private Transform _spawnBasePoint;

        [Tooltip("敵が地面の下から出現する際の地下方向オフセットです。")]
        [SerializeField]
        [Min(0f)]
        private float _undergroundOffset = 10f;


        // スポーン位置を探す最大試行回数"
        [Min(1)] private int _maxSpawnPositionAttempts = 20;




        public bool TrySpawnEnemy(EnemyDefinition definition, float hpRate, float barrierRate, float minDistanceFromOtherEnemies, out EnemyController spawnedEnemy)
        {
            spawnedEnemy = null;

            if (definition == null)
            {
                Debug.LogError("[EnemySpawner] スポーン対象の EnemyDefinition が null です。");
                return false;
            }

            if (definition.EnemyBody == null)
            {
                Debug.LogError("[EnemySpawner] EnemyDefinition の EnemyBody が null です。");
                return false;
            }

            // EnemyBodyController が付与されているか確認
            if (!definition.EnemyBody.TryGetComponent(out EnemyBodyController _))
            {
                Debug.LogError(
                    $"[EnemySpawner] 敵「{definition.EnemyId}」のEnemyBodyに" +
                    "EnemyBodyControllerが設定されていません。",
                    definition.EnemyBody);

                return false;
            }

            // 目標位置計算
            float validMinDistance = Mathf.Max(0f, minDistanceFromOtherEnemies);

            if (!TryGetSpawnTargetPosition(definition.IsBoss,validMinDistance,out Vector3 targetPosition))
            {
                return false;
            }

            // 敵の生成
            GameObject enemyObject = new GameObject($"Enemy_{definition.EnemyId}");

            enemyObject.transform.SetPositionAndRotation(targetPosition, Quaternion.identity);

            GameObject body = Instantiate(definition.EnemyBody, enemyObject.transform);
            GameObject barrier = null;

            if (definition.HasBarrier && definition.BarrierBody != null)
            {
                barrier = Instantiate(definition.BarrierBody, enemyObject.transform);
            }

            EnemyController enemyController = enemyObject.AddComponent<EnemyController>();

            enemyObject.AddComponent<EnemyRising>();
            enemyObject.AddComponent<EnemyWorldStatusView>();

            // 状態異常VFXの設定
            EnemyStatusAilmentVFX statusVFX = enemyObject.AddComponent<EnemyStatusAilmentVFX>();

            statusVFX.SetupEntries(_statusAilmentVFXEntries, _statusAilmentVFXRoot, _statusAilmentVFXOffset, !definition.IsBoss);

            if (!body.TryGetComponent(out EnemyBodyController bodyController))
            {
                Debug.LogWarning("[EnemySpawner] EnemyBodyController が付与されていないため生成を中止します。", body);
                Destroy(enemyObject);
                return false;
            }

            // WaveData から受け取ったステータス倍率を適用して敵を初期化
            float validHpRate = Mathf.Max(0.01f, hpRate);
            float validBarrierRate = Mathf.Max(0.01f, barrierRate);

            EnemyController.SpawnSummary spawnSummary = new EnemyController.SpawnSummary(targetPosition, _undergroundOffset, validHpRate, validBarrierRate);

            string enemyId = enemyController.Initialize(definition, spawnSummary);

            bodyController.Initialize(enemyId);

            if (definition.HasBarrier && barrier != null)
            {
                enemyController.BarrierInitialize(definition, spawnSummary, barrier);
            }

            spawnedEnemy = enemyController;
            return true;
        }

        /// <summary>
        /// スポーンターゲットの位置を試行的に取得する
        /// </summary>
        /// <param name="isBoss">ボスかどうか</param>
        /// <param name="minDistanceFromOtherEnemies">既存の敵と最低限空ける距離</param>
        /// <param name="targetPos">ターゲット位置</param>
        /// <returns>見つかったらtrueを返す</returns>
        private bool TryGetSpawnTargetPosition(bool isBoss, float minDistanceFromOtherEnemies, out Vector3 targetPos)
        {
            if (isBoss && TryGetCenterAnchorPosition(out targetPos))
            {
                return true;
            }

            // ランダムな位置を試行的に取得する
            for (int i = 0; i < _maxSpawnPositionAttempts; i++)
            {
                targetPos = CreateRandomTargetPosition();

                if (IsFarEnoughFromExistingEnemies(targetPos, minDistanceFromOtherEnemies))
                {
                    return true;
                }
            }

            targetPos = default;
            return false;
        }

        private bool TryGetCenterAnchorPosition(out Vector3 targetPos)
        {
            try
            {
                GameObject center = GameObject.FindGameObjectWithTag("Field_Center");
                if (center != null)
                {
                    Vector3 centerPosition = center.transform.position;
                    Vector3 spawnBasePosition = _spawnBasePoint != null
                        ? _spawnBasePoint.position
                        : centerPosition;
                    targetPos = new Vector3(centerPosition.x, spawnBasePosition.y, spawnBasePosition.z + 5.0f);
                    return true;
                }
            }
            catch (UnityException)
            {
                // タグが存在しない旧シーンでは通常のスポーンへフォールバックする。
            }

            targetPos = default;
            return false;
        }

        /// <summary>
        /// ランダムなターゲット位置を生成する
        /// </summary>
        /// <returns>ランダムなターゲット位置</returns>
        private Vector3 CreateRandomTargetPosition()
        {
            Vector3 spawnPos = _spawnBasePoint != null ? _spawnBasePoint.position : transform.position;

            float randomX = Random.Range(spawnPos.x - _rangeSize.x / 2f, spawnPos.x + _rangeSize.x / 2f);
            float randomZ = Random.Range(spawnPos.z - _rangeSize.y / 2f, spawnPos.z + _rangeSize.y / 2f);

            return new Vector3(randomX, spawnPos.y, randomZ);
        }

        /// <summary>
        /// 既存の敵と十分な距離があるか
        /// </summary>
        /// <param name="position">ターゲットのポジション</param>
        /// <param name="minDistanceFromOtherEnemies">既存の敵と最低限空ける距離</param>
        /// <returns>結果</returns>
        private bool IsFarEnoughFromExistingEnemies(Vector3 position, float minDistanceFromOtherEnemies)
        {
            float validMinDistance = Mathf.Max(0f, minDistanceFromOtherEnemies);
            float minDistanceSqr = validMinDistance * validMinDistance;
            EnemyController[] enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);

            foreach (var enemy in enemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

                Vector3 enemyPos = enemy.transform.position;
                enemyPos.y = position.y; // Y軸は無視して距離を計算

                if ((enemyPos - position).sqrMagnitude < minDistanceSqr)
                {
                    return false; // 既存の敵と近すぎる
                }
            }

            return true; // 十分な距離がある
        }

        private void OnDrawGizmosSelected()
        {
            if (_spawnBasePoint == null) return;

            Vector3 size = new Vector3(_rangeSize.x, 0.1f, _rangeSize.y);

            // 1. 生成目標位置 (地上・水色) の可視化
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_spawnBasePoint.position, size);

            // 2. スポーン開始位置（地下・マゼンタ）の可視化
            Gizmos.color = Color.magenta;
            Vector3 undergroundPosition = _spawnBasePoint.position + Vector3.down * _undergroundOffset;
            Gizmos.DrawWireCube(undergroundPosition, size);

            // 地上と地下を繋ぐガイドラインを描画
            Gizmos.color = new Color(1f, 0f, 1f, 0.3f);
            Gizmos.DrawLine(_spawnBasePoint.position, undergroundPosition);
        }
    }
}
