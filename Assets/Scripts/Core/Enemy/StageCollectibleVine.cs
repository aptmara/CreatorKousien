using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// ステージ上で殴ると消え、収集物を排出する蔦。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class StageCollectibleVine : MonoBehaviour, ICrystalBreakable
    {
        private StageCollectibleVineSpawner _owner;
        private bool _broken;

        public void Initialize(StageCollectibleVineSpawner owner)
        {
            _owner = owner;
        }

        public void Break(Vector3 hitPoint, Vector3 hitDirection)
        {
            if (_broken)
            {
                return;
            }

            _broken = true;
            _owner?.HandleVineBroken(this, transform.position);
            Destroy(gameObject);
        }
    }
}
