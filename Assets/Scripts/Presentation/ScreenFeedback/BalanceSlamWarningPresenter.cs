using UnityEngine;
using Game.Core.Events;

namespace Game.Presentation.ScreenFeedback
{
    public sealed class BalanceSlamWarningPresenter : MonoBehaviour
    {
        private void OnEnable() => EventBus.Subscribe<BalanceSlamWarningEvent>(HandleWarning);
        private void OnDisable() => EventBus.Unsubscribe<BalanceSlamWarningEvent>(HandleWarning);

        private void HandleWarning(BalanceSlamWarningEvent ev)
        {
            EventBus.Publish(new CameraShakeRequestedEvent());
        }
    }
}
