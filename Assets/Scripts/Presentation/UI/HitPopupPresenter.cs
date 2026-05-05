// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;
using UnityEngine.UI;

namespace Game.Presentation.UI
{
    /// <summary>
    /// 大量ヒットやダメージのポップアップ表示を制御する。
    /// （プロトタイプでは簡易的にCanvas上のTextとして生成）
    /// </summary>
    public class HitPopupPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _popupPrefab;
        [SerializeField] private Transform _popupContainer;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnHitBatch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnHitBatch);
        }

        private void OnHitBatch(EnemyHitBatchEvent ev)
        {
            if (_popupPrefab == null || _popupContainer == null) return;

            // TODO: 本来はPoolServiceを使う
            var popupObj = Instantiate(_popupPrefab, _popupContainer);
            
            // 適当なオフセット位置
            popupObj.transform.position = ev.HitPosition + Vector3.up * Random.Range(1.0f, 2.0f);
            
            var textComp = popupObj.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                textComp.text = $"{ev.HitCount} Hits!\n{ev.BodyDamage:F0} Dmg";
            }

            Destroy(popupObj, 1.5f); // 仮の寿命
        }
    }
}
