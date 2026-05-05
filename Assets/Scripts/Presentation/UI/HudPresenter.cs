// 制作者: 山内陽
using UnityEngine;
using UnityEngine.UI;
using Game.Core.Events;

namespace Game.Presentation.UI
{
    /// <summary>
    /// HUD表示を制御するプレゼンター。
    /// EventBusからのイベントを受け取り、UI部品を更新する。
    /// </summary>
    public class HudPresenter : MonoBehaviour
    {
        [SerializeField] private Text _collectionCountText;
        [SerializeField] private Slider _collectionCapacitySlider;

        private void OnEnable()
        {
            EventBus.Subscribe<CollectionChangedEvent>(OnCollectionChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CollectionChangedEvent>(OnCollectionChanged);
        }

        private void OnCollectionChanged(CollectionChangedEvent ev)
        {
            if (_collectionCountText != null)
            {
                _collectionCountText.text = $"{ev.CurrentCount} / {ev.Capacity}";
            }

            if (_collectionCapacitySlider != null && ev.Capacity > 0)
            {
                _collectionCapacitySlider.value = (float)ev.CurrentCount / ev.Capacity;
            }
        }
    }
}
