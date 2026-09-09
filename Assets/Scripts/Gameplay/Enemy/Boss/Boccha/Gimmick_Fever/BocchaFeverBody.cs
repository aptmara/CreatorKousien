// ------------------------------------------------------------
// File		: BocchaFeverBody.cs
// Summary	: 分身フェーズ中のボス本体の被弾を受け持つ
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - BossHitReceiverはIBossHittableが同じGameObjectにあるとそちらへ被弾を流す。それを利用する。
// - フィーバー中以外は今までどおりBossBattleFlowControllerへ流すので、既存挙動は変わらない。
// - つまりFlowControllerの設定漏れがあると通常時のダメージが消えるから要注意～！
// ------------------------------------------------------------
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 分身フェーズ中のボス本体。制限時間内に削り切られるとダウンする。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaFeverBody : MonoBehaviour, IBossHittable
    {
        [Header("==== 参照 ====")]

        [SerializeField]
        [Tooltip("通常時のダメージを流す先。未設定なら同じGameObjectから取得する")]
        private BossBattleFlowController _flowController;


        private float _maxHp;
        private float _currentHp;
        private bool _isFeverActive;

        // 無敵時間の残り
        private float _invincibleTimer;


        /// <summary>無敵中は当たらない。分岐はOnHit側で行う。</summary>
        public bool IsHittable => _invincibleTimer <= 0.0f;

        /// <summary>無敵中かどうか。演出の後付け用。</summary>
        public bool IsInvincible => _invincibleTimer > 0.0f;

        /// <summary>分身フェーズ中かどうか。</summary>
        public bool IsFeverActive => _isFeverActive;

        /// <summary>分身フェーズ中に本体HPを削り切ったかどうか。</summary>
        public bool IsDefeated => _isFeverActive && _currentHp <= 0.0f;

        /// <summary>本体HPの割合。ゲージ表示の後付け用。</summary>
        public float HpRatio => _maxHp > 0.0f ? Mathf.Clamp01(_currentHp / _maxHp) : 0.0f;


        /// <summary>
        /// Awake時にFlowControllerが設定されていなければ同じGameObjectから取得する
        /// </summary>
        private void Awake()
        {
            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }

            if (_flowController == null)
            {
                Debug.LogError("[FeverBody] BossBattleFlowControllerが設定されていないぜよ。通常時のダメージが入らないぜ。", this);
            }
        }


        /// <summary>
        /// 分身フェーズを開始する
        /// </summary>
        /// <param name="maxHp">分身後本体体力</param>
        public void BeginFever(float maxHp)
        {
            _maxHp = Mathf.Max(1.0f, maxHp);
            _currentHp = _maxHp;
            _isFeverActive = true;
        }


        /// <summary>
        /// 分身フェーズを終了する
        /// </summary>
        public void EndFever() => _isFeverActive = false;


        private void Update()
        {
            if (_invincibleTimer <= 0.0f)
                return;

            _invincibleTimer -= Time.deltaTime;
        }


        /// <summary>
        /// 無敵時間を開始する。分身フェーズ終了直後の連続被弾を防ぐ
        /// </summary>
        /// <param name="duration">無敵にする秒数</param>
        public void BeginInvincible(float duration)
        {
            _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
        }


        /// <summary>
        /// ボス本体が被弾したときに呼ばれる。分身フェーズ中は本体HPを削る！！
        /// </summary>
        /// <param name="damage">ダメージ</param>
        /// <param name="hitPosition">ヒット位置</param>
        /// <param name="collectible">ヒットしたコレクタブル</param>
        public void OnHit(float damage, Vector3 hitPosition, CollectibleObject collectible)
        {
            // フィーバー中でなければ、今まで通りボスのHPを削る
            if (!_isFeverActive)
            {
                _flowController?.TakeDamage(damage);

                return;
            }

            _currentHp = Mathf.Max(0.0f, _currentHp - damage);
        }
    }
}

