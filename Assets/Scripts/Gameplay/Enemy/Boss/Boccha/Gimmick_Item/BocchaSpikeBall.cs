// ------------------------------------------------------------
// File		: BocchaSpikeBall.cs
// Summary	: トゲ玉。キャンディと同じ落し物で、防衛バリアに当たるとバリアへダメージを与える
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 「誤って落としてしまうと、バリアにダメージが入ってしまう」を防衛バリアとの接触で判定する。
// - 接触で判定するため、ボスが投げても、プレイヤーが動かしても同じルールで成立するようになる！！
// - ボスへは当たってもダメージを与えない。あくまでバリアを削る危険物！
// - 落し物はプールの共通Prefabを使い回すので、ギミック側から実行時に付与する。
// - そのためプールへ戻された個体にも残り続ける。Initializeされるまでは何もしない！
// - ボスの爆発時はBocchaHazardRegistry経由で消えるのでやるけど、なんか退場演出あったほうがいいよねぇ
//   → とりま煙出すことにしてみた！！ by 9/9のおれ
// ------------------------------------------------------------
using Game.Core.DefenceLine;
using Game.Core.Events;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 落し物としてのトゲ玉。プレイヤーが持ち運べ、防衛バリアへ当てるとバリアを削る。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaSpikeBall : MonoBehaviour, IBocchaHazard
    {
        // 設定はギミックからInitializeで受け取る
        // ------------------------------------------------------------

        private float _barrierDamage;
        private GameObject _barrierHitVfxPrefab;
        private float _vfxLifeTime;


        // ランタイム状態
        // ------------------------------------------------------------

        private CollectibleObject _collectible;
        private BocchaThrownItemMover _mover;
        private bool _isActive;
        private bool _hasDamaged;


        /// <summary>お邪魔アイテムの種類。</summary>
        public BocchaHazardType HazardType => BocchaHazardType.SpikeBall;

        /// <summary>現在位置。</summary>
        public Vector3 Position => transform.position;

        /// <summary>
        /// ギミックから設定を受け取り、バリアとの接触判定を開始する
        /// </summary>
        /// <param name="barrierDamage">バリアへ当たった時に与えるダメージ</param>
        /// <param name="barrierHitVfxPrefab">バリアへ当たった時のVFX</param>
        /// <param name="vfxLifeTime">VFXを破棄するまでの時間</param>
        public void Initialize(float barrierDamage, GameObject barrierHitVfxPrefab, float vfxLifeTime)
        {
            _barrierDamage = barrierDamage;
            _barrierHitVfxPrefab = barrierHitVfxPrefab;
            _vfxLifeTime = vfxLifeTime;

            _hasDamaged = false;
            _isActive = true;

            CacheComponents();

            BocchaHazardRegistry.Register(this);
        }


        // プールへ戻された場合など、無効化されたら判定を止める
        private void OnDisable()
        {
            _isActive = false;

            BocchaHazardRegistry.Unregister(this);
        }


        // 防衛バリアのコライダーはトリガーなのでこちらで拾える
        private void OnTriggerEnter(Collider other) => TryDamageBarrier(other, transform.position);

        // 念のため通常の衝突でも判定しておく
        private void OnCollisionEnter(Collision collision)
            => TryDamageBarrier(collision.collider, collision.GetContact(0).point);


        /// <summary>
        /// 外部から強制的に消滅させる
        /// </summary>
        public void ForceDespawn()
        {
            _isActive = false;

            if (_collectible != null)
            {
                _collectible.Despawn();
            }
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 接触相手が防衛バリアならダメージを与えて消滅する。1個につき1回だけ
        /// </summary>
        /// <param name="other">接触した相手</param>
        /// <param name="hitPosition">接触位置</param>
        private void TryDamageBarrier(Collider other, Vector3 hitPosition)
        {
            if (!_isActive || _hasDamaged || other == null)
            {
                return;
            }

            // ボスが投げている最中はバリアの上空を通過するため判定しない
            if (_mover != null && _mover.IsFlying)
            {
                return;
            }

            if (!IsDefenceLine(other))
            {
                return;
            }

            _hasDamaged = true;

            PlayVfx(hitPosition);

            EventBus.Publish(new RuleBarrierAttackEvent(_barrierDamage, hitPosition));
            EventBus.Publish(new BocchaSpikeBallDroppedEvent(hitPosition, _barrierDamage));

            Debug.Log($"[SpikeBall] <color=red>バリアに接触！ バリアへ {_barrierDamage:0.#} ダメージ</color>", this);

            ForceDespawn();
        }


        /// <summary>
        /// 接触相手が防衛バリアかどうか
        /// </summary>
        /// <param name="other">接触した相手</param>
        private static bool IsDefenceLine(Collider other)
        {
            return other.GetComponentInParent<DefenseLineFracture>() != null ||
                   other.GetComponentInParent<DefenseLineReaction>() != null;
        }


        /// <summary>
        /// VFXを再生する。nullなら何もしない。
        /// </summary>
        /// <param name="position">再生位置</param>
        private void PlayVfx(Vector3 position)
        {
            if (_barrierHitVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(_barrierHitVfxPrefab, position, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }


        private void CacheComponents()
        {
            if (_collectible == null)
            {
                _collectible = GetComponent<CollectibleObject>();
            }

            if (_mover == null)
            {
                _mover = GetComponent<BocchaThrownItemMover>();
            }
        }
    }
}
