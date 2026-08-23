using Game.Gameplay.Collectibles;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{
    public interface IBossHittable
    {
        bool IsHittable { get; }
        void OnHit(float damage, Vector3 hitPosition, CollectibleObject collectible);
    }

}
