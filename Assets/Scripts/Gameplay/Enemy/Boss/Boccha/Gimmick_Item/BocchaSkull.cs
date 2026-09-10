// ------------------------------------------------------------
// File		: BocchaSkull.cs
// Summary	: ドクロ。キャンディと同じ落し物で、一定時間後に爆発してオカシを飛び散らせる
//
// Author	: [浅野 勇生]
// Created	: 2026-09-08
//
// Notes	:
// - プレイヤーが持ち運べるので、カウントダウンは生成直後から回す。爆発前に落とすゲーム！
// - トゲ玉と違いボスへのダメージは通す。BossHitReceiverはBocchaSpikeBallだけ弾いている。
// - 落し物はプールの共通Prefabを使い回すので、ギミック側から実行時に付与する。
// - 消滅時はBakuBurstSlicerでメッシュ分割する。破片はシーンルートに出るのでプール返却しても残る。
// ------------------------------------------------------------
using Game.Core.Events;
using Game.Gameplay.Collectibles;
using Game.Gameplay.Enemy.Baku;
using Game.Gameplay.Stage;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 落し物としてのドクロ。時限爆発でフィールド上のオカシを吹き飛ばす。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaSkull : MonoBehaviour, IBocchaHazard
    {
        // 設定はギミックからInitializeで受け取る
        // ------------------------------------------------------------

        private float _countdownDuration;
        private float _explosionRadius;
        private float _explosionSpeed;
        private float _upwardRatio;
        private GameObject _explosionVfxPrefab;
        private float _vfxLifeTime;


        // ランタイム状態
        // ------------------------------------------------------------

        private CollectibleObject _collectible;
        private CollectibleRegistry _collectibleRegistry;
        private float _countdownTimer;
        private bool _isActive;
        private bool _hasExploded;


        /// <summary>お邪魔アイテムの種類。</summary>
        public BocchaHazardType HazardType => BocchaHazardType.Skull;

        /// <summary>現在位置。</summary>
        public Vector3 Position => transform.position;

        /// <summary>爆発までの残り時間。UIの後付け用。</summary>
        public float RemainingTime => Mathf.Max(0.0f, _countdownTimer);


        /// <summary>
        /// ギミックから設定を受け取り、カウントダウンを開始する
        /// </summary>
        /// <param name="countdownDuration">爆発するまでの時間</param>
        /// <param name="explosionRadius">爆発が届く範囲</param>
        /// <param name="explosionSpeed">オカシを吹き飛ばす初速(m/s)</param>
        /// <param name="upwardRatio">吹き飛ばしを上方向へ寄せる割合</param>
        /// <param name="explosionVfxPrefab">爆発時のVFX</param>
        /// <param name="vfxLifeTime">VFXを破棄するまでの時間</param>
        public void Initialize(float countdownDuration, float explosionRadius, float explosionSpeed, float upwardRatio, GameObject explosionVfxPrefab, float vfxLifeTime)
        {
            // 設定を受け取る
            _countdownDuration = countdownDuration;
            _explosionRadius = explosionRadius;
            _explosionSpeed = explosionSpeed;
            _upwardRatio = upwardRatio;
            _explosionVfxPrefab = explosionVfxPrefab;
            _vfxLifeTime = vfxLifeTime;

            // 持っている間もカウントが進むので、生成した瞬間から回す
            _countdownTimer = _countdownDuration;
            _hasExploded = false;
            _isActive = true;

            if (_collectible == null)
            {
                _collectible = GetComponent<CollectibleObject>();
            }

            BocchaHazardRegistry.Register(this);
        }


        // プールへ戻された場合など、無効化されたら判定を止める
        private void OnDisable()
        {
            _isActive = false;

            BocchaHazardRegistry.Unregister(this);
        }


        private void Update()
        {
            if (!_isActive || _hasExploded)
            {
                return;
            }

            _countdownTimer -= Time.deltaTime;

            if (_countdownTimer > 0.0f)
            {
                return;
            }

            Explode();
        }


        /// <summary>
        /// 外部から強制的に消滅させる。分身フェーズの一掃などから呼ばれる
        /// </summary>
        public void ForceDespawn()
        {
            _isActive = false;

            // 爆発でも一掃でも、同じようにメッシュ分割して退場する
            PlayShatter();

            if (_collectible != null)
            {
                _collectible.Despawn();
            }
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 爆発してオカシを吹き飛ばす
        /// </summary>
        private void Explode()
        {
            _hasExploded = true;

            PlayVfx();

            int affectedCount = BlowAwayCollectibles();

            EventBus.Publish(new BocchaSkullExplodedEvent(transform.position, _explosionRadius, affectedCount));

            Debug.Log($"[Skull] <color=orange>爆発！ オカシ {affectedCount} 個を吹き飛ばしました</color>", this);

            ForceDespawn();
        }


        /// <summary>
        /// 範囲内のオカシをぶっ飛ばす！
        /// </summary>
        /// <returns>吹き飛ばした数</returns>
        private int BlowAwayCollectibles()
        {
            if (_collectibleRegistry == null)
            {
                _collectibleRegistry = FindFirstObjectByType<CollectibleRegistry>();
            }

            if (_collectibleRegistry == null)
            {
                return 0;
            }

            // フィールドの上方向を基準にぶっ飛ばす。フィールドが傾いている場合はそっちに合わせる
            Vector3 up = FieldContext.IsReady ? FieldContext.Up : Vector3.up;
            Vector3 center = transform.position;
            float sqrRadius = _explosionRadius * _explosionRadius;
            int affectedCount = 0;

            // ぶっ飛ばす対象は、落し物のCollectibleObjectに限定する
            foreach (CollectibleObject collectible in _collectibleRegistry.ActiveCollectibles)
            {
                if (collectible == null || collectible == _collectible ||
                    !collectible.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector3 diff = collectible.transform.position - center;

                if (diff.sqrMagnitude > sqrRadius)
                {
                    continue;
                }

                // 中心に近いものほど強く吹き飛ばす
                float distance = Mathf.Sqrt(diff.sqrMagnitude);
                float falloff = 1.0f - distance / Mathf.Max(0.01f, _explosionRadius);

                Vector3 direction = (diff.normalized + up * _upwardRatio).normalized;

                // ApplyAvalancheForceはForceMode.Impulseなので、質量を掛けないと速度にならないからmassを掛ける!
                float mass = collectible.TryGetComponent(out Rigidbody body) ? body.mass : 1.0f;

                collectible.ApplyAvalancheForce(direction * (_explosionSpeed * falloff * mass));

                affectedCount++;
            }

            return affectedCount;
        }


        /// <summary>
        /// 見た目をメッシュ分割して吹き飛ばす
        /// </summary>
        private void PlayShatter()
        {
            // 落し物は見た目を差し替える方式なので、その都度取り直す
            BakuBurstSlicer slicer = GetComponentInChildren<BakuBurstSlicer>(true);

            if (slicer == null)
            {
                return;
            }

            Renderer sourceRenderer = slicer.GetComponentInChildren<Renderer>(true);

            slicer.Shatter();

            // Shatterは元のRendererを消すが、プールで使い回すため表示を戻しておく
            // これないと透明になるらしい！＼(^o^)／
            if (sourceRenderer != null)
            {
                sourceRenderer.enabled = true;
            }
        }


        /// <summary>
        /// 爆発VFXを再生する。nullなら何もしない
        /// </summary>
        private void PlayVfx()
        {
            if (_explosionVfxPrefab == null)
            {
                return;
            }

            GameObject vfx = Instantiate(_explosionVfxPrefab, transform.position, Quaternion.identity);

            Destroy(vfx, _vfxLifeTime);
        }
    }
}
