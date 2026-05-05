// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;

namespace Game.DebugTools
{
    /// <summary>
    /// UIの単体検証を行うため、Inspectorから各種イベントを手動発行するデバッグ用スクリプト。
    /// </summary>
    public class DebugEventEmitter : MonoBehaviour
    {
        [Header("Collection Changed Event")]
        public int currentCount = 50;
        public int capacity = 100;

        [Header("Enemy Hit Batch Event")]
        public string enemyId = "Enemy_01";
        public int hitCount = 10;
        public float gaugeDamage = 5.0f;
        public float bodyDamage = 100.0f;

        [Header("Payload Released Event")]
        public int releasePayloadCount = 20;
        public float totalPower = 50.0f;

        [ContextMenu("Fire Collection Changed Event")]
        public void FireCollectionChanged()
        {
            EventBus.Publish(new CollectionChangedEvent(currentCount, capacity));
            Debug.Log("[Debug] Fire CollectionChangedEvent");
        }

        [ContextMenu("Fire Enemy Hit Batch Event")]
        public void FireEnemyHitBatch()
        {
            EventBus.Publish(new EnemyHitBatchEvent(enemyId, hitCount, gaugeDamage, bodyDamage, transform.position));
            Debug.Log("[Debug] Fire EnemyHitBatchEvent");
        }

        [ContextMenu("Fire Enemy Gauge Broken Event")]
        public void FireEnemyGaugeBroken()
        {
            EventBus.Publish(new EnemyGaugeBrokenEvent(enemyId));
            Debug.Log("[Debug] Fire EnemyGaugeBrokenEvent");
        }

        [ContextMenu("Fire Enemy Down Started Event")]
        public void FireEnemyDownStarted()
        {
            EventBus.Publish(new EnemyDownStartedEvent(enemyId, 3.0f));
            Debug.Log("[Debug] Fire EnemyDownStartedEvent");
        }

        [ContextMenu("Fire Payload Released Event")]
        public void FirePayloadReleased()
        {
            EventBus.Publish(new PayloadReleasedEvent(releasePayloadCount, totalPower, transform.position, Vector3.forward));
            Debug.Log("[Debug] Fire PayloadReleasedEvent");
        }

        [ContextMenu("Fire Stage Tilt Started Event")]
        public void FireStageTiltStarted()
        {
            EventBus.Publish(new StageTiltStartedEvent(Vector3.right));
            Debug.Log("[Debug] Fire StageTiltStartedEvent");
        }
    }
}
