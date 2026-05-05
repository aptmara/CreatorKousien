// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 大量ヒットや撃破のポップアップ表示を制御する。
    /// （プロトタイプでは簡易的にCanvas上のTextとして生成）
    /// </summary>
    public class HitPopupPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popupPrefab;
        [SerializeField] private Transform _popupContainer;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnHitBatch);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnDefeated);
        }

        private void OnHitBatch(EnemyHitBatchEvent ev)
        {
            if (_popupPrefab == null || _popupContainer == null) return;

            // TODO: 本来はPoolServiceを使う
            var popupObj = Instantiate(_popupPrefab, _popupContainer);
            popupObj.transform.position = ev.HitPosition + Vector3.up * Random.Range(1.0f, 2.0f);

            var textComp = popupObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = $"{ev.HitCount} Hits!\n{ev.BodyDamage:F0} Dmg";
            }

            Destroy(popupObj, 1.5f);
        }

        private void OnDefeated(EnemyDefeatedEvent ev)
        {
            if (_popupPrefab == null || _popupContainer == null) return;

            var popupObj = Instantiate(_popupPrefab, _popupContainer);

            // TODO: 撃破した敵のWorldPosition取得にはEnemyDefeatedEventへVector3を追加するか
            //       EnemyController側でWorldPositionを含めた発行に変更する（Phase2で対応）
            popupObj.transform.position = Vector3.zero + Vector3.up * 2f;

            var textComp = popupObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = $"DEFEATED!\n{ev.EnemyId}";
            }

            Destroy(popupObj, 2.5f);
        }
    }
}

