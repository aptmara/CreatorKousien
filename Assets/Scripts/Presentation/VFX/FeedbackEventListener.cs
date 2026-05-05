// 制作者: 山内陽
using UnityEngine;
using Game.Core.Events;

namespace Game.Presentation.VFX
{
    /// <summary>
    /// イベントを受けてVFXやSEを再生するリスナー。
    /// （プロトタイプではConsole出力や簡易オブジェクト生成で代用）
    /// </summary>
    public class FeedbackEventListener : MonoBehaviour
    {
        [SerializeField] private GameObject _releaseVfxPrefab;

        private void OnEnable()
        {
            EventBus.Subscribe<PayloadReleasedEvent>(OnPayloadReleased);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PayloadReleasedEvent>(OnPayloadReleased);
        }

        private void OnPayloadReleased(PayloadReleasedEvent ev)
        {
            Debug.Log($"[Feedback] Played SE for Release. Total Power: {ev.TotalPower}");

            if (_releaseVfxPrefab != null)
            {
                var vfx = Instantiate(_releaseVfxPrefab, ev.ReleasePosition, Quaternion.LookRotation(ev.ReleaseDirection));
                Destroy(vfx, 2.0f); // 寿命
            }
        }
    }
}
