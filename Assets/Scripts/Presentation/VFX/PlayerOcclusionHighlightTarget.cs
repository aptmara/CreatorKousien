using UnityEngine;

namespace Game.Presentation.VFX
{
    [DisallowMultipleComponent]
    public sealed class PlayerOcclusionHighlightTarget : MonoBehaviour
    {
        private const string PlayerLayerName = "Player";

        [SerializeField] private Transform _visualRoot;

        private void Awake()
        {
            int playerLayer = LayerMask.NameToLayer(PlayerLayerName);
            Renderer[] renderers = _visualRoot.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer targetRenderer in renderers)
            {
                targetRenderer.gameObject.layer = playerLayer;
            }
        }
    }
}
