/**
 * File: BalanceTrayContent_BigDrop.cs
 * 
 * 皿の上の大ダメージの落ち物、開封した瞬間にテンビンの頭の上に大きな落ち物が降ってくる
 */
using System.Collections;
using UnityEngine;
using Game.Data.Collectibles;

namespace Game.Gameplay.Enemy.Boss
{
    [DisallowMultipleComponent]
    public class BalanceTrayContent_BigDrop : BalanceTrayContentBase
    {
        [Header("===== 落ち物 =====")]
        [SerializeField]
        private CollectibleData _bigDropData;
        [SerializeField, Min(1)]
        private int _dropCount = 3;
        [SerializeField, Min(0.1f)]
        private float _scaleMultiplier = 2.0f;
        [SerializeField, Min(0.0f)]
        private float _interval = 0.25f;

        [Header("===== 落とす位置 ======")]
        [SerializeField]
        private BossSocket _target = BossSocket.Head;
        [SerializeField, Min(0.0f)]
        private float _dropHeight = 10.0f;
        [SerializeField, Min(0.0f)]
        private float _spread = 1.5f;
        [SerializeField]
        private float _initialDownSpeed = 3.0f;

        protected override void OnRevealed()
        {
            StartCoroutine(DropRoutine());
        }

        private IEnumerator DropRoutine()
        {
            if (_bigDropData == null || Ctx.Collectibles == null) yield break;

            var wait = new WaitForSeconds(_interval);
            for(int i = 0;i < _dropCount;++i)
            {
                Vector3 center = Ctx.Boss.GetSocket(_target).position;
                Vector3 o = UnityEngine.Random.insideUnitSphere * _spread;
                Vector3 pos = center + new Vector3(o.x, _dropHeight, o.y);

                Ctx.Collectibles.SpawnWithVelocity(_bigDropData, pos, Vector3.down * _initialDownSpeed, _scaleMultiplier);
                yield return wait;
            }
        }

    }

}
