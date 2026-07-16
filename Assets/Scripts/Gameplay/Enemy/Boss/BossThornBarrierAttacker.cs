// ------------------------------------------------------------
// File     : BossThornBarrierAttacker.cs
// Summary  : 有効なボスの棘が防衛バリアへ接触した際の攻撃を管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 棘1本につき1つアタッチして使用する。
// - 現在の攻撃段階でバリア攻撃が許可されている場合のみ攻撃する。
// - 有効かつ未破壊の棘だけがバリアへダメージを与える。
// - 1回のイバラタックル中、同じ棘によるダメージは1回だけ発生する。
// ------------------------------------------------------------
using System;
using Game.Core.DefenceLine;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスの棘と防衛バリアの接触を検出し、
    /// 防衛バリアへダメージイベントを送るコンポーネント。
    ///
    /// このクラスが担当するもの:
    /// ・防衛バリアとの接触検出
    /// ・現在の攻撃段階でバリア攻撃が可能かどうかの管理
    /// ・棘が有効かつ未破壊かどうかの確認
    /// ・同じ攻撃中における多重ダメージの防止
    /// ・RuleBarrierAttackEventの発行
    /// ・今後のヒット演出へ接触情報を通知
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BossThorn))]
    [RequireComponent(typeof(Collider))]
    public sealed class BossThornBarrierAttacker : MonoBehaviour
    {
        [Header("--- 参照 ---")]

        [SerializeField]
        [Tooltip("この攻撃判定が所属する棘")]
        private BossThorn _thorn;

        // ランタイム状態
        // ------------------------------------------------------------

        /// <summary>
        /// この攻撃段階で、棘一本が防衛バリアへ与えるダメージ量
        /// </summary>
        private float _barrierDamage;

        /// <summary>
        /// この棘が防衛バリアへの攻撃判定を受け付けるかどうか
        /// </summary>
        private bool _isAttackWindowOpen;

        /// <summary>
        /// この棘が防衛バリアへダメージを与えたかどうか
        /// </summary>
        private bool _hasDamagedBarrier;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// この攻撃段階で、棘一本が防衛バリアへ与えるダメージ量
        /// </summary>
        public bool IsAttackWindowOpen => _isAttackWindowOpen;

        /// <summary>
        /// この棘が防衛バリアへダメージを与えたかどうか
        /// </summary>
        public bool HasDamagedBarrier => _hasDamagedBarrier;


        // イベント
        // ------------------------------------------------------------

        /// <summary>
        /// 棘が防衛バリアへダメージを与えたときに発行されるイベント
        ///
        /// 引数:
        /// 1. ダメージを与えた棘のBossThornコンポーネント
        /// 2. 与えたダメージ量
        /// 3. 棘が防衛バリアへ接触した位置
        /// </summary>
        public event Action<BossThorn, float, Vector3> BarrierDamaged;


        // Unityイベント
        // ------------------------------------------------------------

        /// <summary>
        /// コンポーネント追加時の初期化処理
        /// </summary>
        private void Reset()
        {
            _thorn = GetComponent<BossThorn>();
        }

        /// <summary>
        /// Inspector上での値変更時の検証処理
        /// </summary>
        private void OnValidate()
        {
            if (_thorn == null)
            {
                _thorn = GetComponent<BossThorn>();
            }
        }

        /// <summary>
        /// Awake時の初期化処理
        /// </summary>
        private void Awake()
        {
            if (_thorn == null)
            {
                _thorn = GetComponent<BossThorn>();
            }

            if (_thorn == null)
            {
                Debug.LogError($"[{nameof(BossThornBarrierAttacker)}] BossThornがアタッチされてないぜよ…", this);

                enabled = false;
            }
        }

        /// <summary>
        /// コンポーネントが無効化されたときに、攻撃段階を終了する
        /// </summary>
        private void OnDisable()
        {
            EndAttackStep();
        }


        // 攻撃段階の制御
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のイバラタックルの攻撃段階を開始する
        /// </summary>
        /// <param name="barrierDamage">棘が防衛バリアへ与えるダメージ量</param>
        /// <param name="canDamageBarrier">防衛バリアへのダメージが可能かどうか</param>
        public void BeginAttackStep(float barrierDamage, bool canDamageBarrier)
        {
            _barrierDamage = Mathf.Max(0f, barrierDamage);

            // 新しい攻撃段階なので、前回の命中状態をリセットする
            _hasDamagedBarrier = false;

            // ダメージが0の場合も攻撃判定を開始しない！
            _isAttackWindowOpen = canDamageBarrier && _barrierDamage > 0f;
        }


        /// <summary>
        /// 現在のイバラタックルの攻撃段階を終了する
        /// </summary>
        public void EndAttackStep()
        {
            _isAttackWindowOpen = false;
            _hasDamagedBarrier = false;
        }


        // 接触判定
        // ------------------------------------------------------------

        /// <summary>
        /// 棘が防衛バリアへ接触したときに呼ばれる
        /// </summary>
        /// <param name="collision">接触情報</param>
        private void OnCollisionEnter(Collision collision)
        {
            Vector3 hitPosition = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);

            TryAttackBarrier(collision.collider, hitPosition);
        }

        /// <summary>
        /// 棘が防衛バリアへ接触したときに呼ばれる
        /// </summary>
        /// <param name="other">接触情報</param>
        private void OnTriggerEnter(Collider other)
        {
            Vector3 hitPosition = other.ClosestPoint(_thorn.transform.position);

            TryAttackBarrier(other, hitPosition);
        }


        // バリア攻撃
        // ------------------------------------------------------------

        /// <summary>
        /// 接触相手が防衛バリアであれば、ダメージイベントを発行する
        /// </summary>
        /// <param name="other">接触相手のコライダー</param>
        /// <param name="hitPosition">当たった場所</param>
        /// <returns>ダメージを与えたらtrue</returns>
        private bool TryAttackBarrier(Collider other, Vector3 hitPosition)
        {
            if (other == null || _thorn == null)
            {
                return false;
            }

            // 攻撃段階が許可されていない場合は攻撃しない
            if (!_isAttackWindowOpen)
            {
                return false;
            }


            // 同じ攻撃段階で既にダメージを与えていた場合は攻撃しない
            if (_hasDamagedBarrier)
            {
                return false;
            }

            // 棘が無効 or 破壊済みの場合は攻撃しない
            if (!_thorn.IsDamageable)
            {
                return false;
            }

            // 接触相手または、親にDefenseLineReactionがアタッチされていたら、防衛バリアへのダメージを発生させる
            DefenseLineReaction defenseLine = other.GetComponentInParent<DefenseLineReaction>();

            if (defenseLine == null)
            {
                return false;
            }

            // イベント発効前に無効にしておく
            _hasDamagedBarrier = true;

            EventBus.Publish(new RuleBarrierAttackEvent(_barrierDamage, hitPosition));

            BarrierDamaged?.Invoke(_thorn, _barrierDamage, hitPosition);

            return true;
        }
    }
}
