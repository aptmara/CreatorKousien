// ------------------------------------------------------------
// File		: BocchaGimmick_Melee.cs
// Summary	: キング・ボッチャの殴り攻撃。防衛ラインへ直接ダメージを与える
//
// Author	: [浅野 勇生]
// Created	: 2026-09-21
//
// Notes	:
// - 「何が出るかな？」を規定回数こなすとItemSpit側から割り込みで呼ばれる
// - ダメージの出し方は分身ギミックの時間切れ攻撃と同じRuleBarrierAttackEvent
// ------------------------------------------------------------
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// キング・ボッチャの殴り攻撃ギミック
    /// </summary>
    [CreateAssetMenu(fileName = "Gimmick_BocchaMelee", menuName = "Boss/Gimmicks/Boccha/Melee")]
    public class BocchaGimmick_Melee : BossGimmickSO
    {
        /// <summary>
        /// 攻撃の進行段階
        /// </summary>
        private enum MeleePhase
        {
            /// <summary>
            /// 予兆
            /// </summary>
            Windup,

            /// <summary>
            /// 硬直,殴った後の余韻
            /// </summary>
            Recover,
        }


        [Header("--- 攻撃 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("防衛ラインへ与えるダメージ量")]
        private float _barrierDamage = 25.0f;

        [SerializeField]
        [Tooltip("ONならフェーズのダメージ倍率を掛ける")]
        private bool _useDamageMultiplier = true;


        [Header("--- タイミング ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("振りかぶってから当たるまでの時間")]
        private float _windupDuration = 0.8f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("殴った後の硬直時間")]
        private float _recoverDuration = 0.6f;


        [Header("--- 演出 ---")]

        [SerializeField]
        [Tooltip("殴りモーションのTrigger名")]
        private string _animTrigger = "Melee";

        [SerializeField]
        [Tooltip("振りかぶり中に出すエフェクト")]
        private GameObject _windupVfxPrefab;

        [SerializeField]
        [Tooltip("命中の瞬間に出すエフェクト")]
        private GameObject _hitVfxPrefab;

        [SerializeField]
        [Tooltip("VFXを出す基準ソケット")]
        private BossSocket _vfxSocket = BossSocket.Muzzle;

        [SerializeField]
        [Tooltip("VFXの位置オフセット")]
        private Vector3 _vfxOffset = Vector3.zero;

        [SerializeField]
        [Min(0f)]
        [Tooltip("VFXの寿命[秒]")]
        private float _vfxLifetime = 3.0f;


        [Header("--- 移動 ---")]

        [SerializeField]
        [Tooltip("ONなら攻撃中は移動しない")]
        private bool _stopSwayDuringAttack = true;


        [Header("--- デバッグ ---")]

        [SerializeField]
        [Tooltip("攻撃のたびにログを出すかどうか")]
        private bool _logAttack = true;


        // ランタイム状態
        // ------------------------------------------------------------

        private BocchaSwayMover _swayMover;

        private MeleePhase _phase;
        private float _timer;
        private bool _isComplete = true;
        private bool _hasStoppedSway;


        public override bool IsComplete => _isComplete;

        public override bool IsTick => true;


        /// <summary>
        /// ギミック初期化
        /// </summary>
        /// <param name="context">ボスの参照コンテキスト</param>
        public override void Initialize(BossContext context)
        {
            base.Initialize(context);

            _swayMover = context.Transform.GetComponentInChildren<BocchaSwayMover>(true);

            if (_swayMover == null && _stopSwayDuringAttack)
            {
                Debug.LogWarning($"[{nameof(BocchaGimmick_Melee)}] BocchaSwayMoverが見つからないため、移動停止はスキップされます。");
            }
        }


        /// <summary>
        /// 攻撃開始、振りかぶりモーションの再生
        /// </summary>
        public override void Execute()
        {
            _isComplete = false;
            _phase = MeleePhase.Windup;
            _timer = 0f;

            if (!string.IsNullOrEmpty(_animTrigger) && Context.Animator != null)
            {
                Context.Animator.SetTrigger(_animTrigger);
            }

            StopSway();

            // 振りかぶり中のエフェクトを再生
            PlayVfx(_windupVfxPrefab);
        }


        /// <summary>
        /// 攻撃の進行
        /// </summary>
        /// <param name="dt">経過時間</param>
        public override void Tick(float dt)
        {
            if (_isComplete) return;

            _timer += dt;

            if (_phase == MeleePhase.Windup)
            {
                if (_timer < _windupDuration ) return;

                ApplyHit();

                _phase = MeleePhase.Recover;
                _timer = 0f;

                return;
            }

            if (_timer < _recoverDuration) return;

            Finish();
        }


        /// <summary>
        /// 中断された場合の後始末
        /// </summary>
        public override void Cancel()
        {
            ResumeSway();

            _isComplete = true;
        }


        // 内部処理
        // ------------------------------------------------------------

        private void ApplyHit()
        {
            float damage = _barrierDamage;

            if (_useDamageMultiplier)
            {
                damage *= Mathf.Max(0.0f, Context.PhaseMultipliers.DamageMultiplier);
            }

            EventBus.Publish(new RuleBarrierAttackEvent(damage, Context.Transform.position));

            PlayVfx(_hitVfxPrefab);

            if (_logAttack)
            {
                Debug.Log($"[BocchaMelee] <color=orange>殴り！</color> 防衛ラインへ {damage} ダメージ");
            }
        }


        /// <summary>
        /// 攻撃を終了する
        /// </summary>
        private void Finish()
        {
            ResumeSway();

            _isComplete = true;
        }



        /// <summary>
        /// VFXを再生する
        /// </summary>
        /// <param name="prefab">生成するVFXのプレハブ</param>
        private void PlayVfx(GameObject prefab)
        {
            if (prefab == null) return;

            Transform socket = Context.GetSocket(_vfxSocket);

            GameObject vfx = UnityEngine.Object.Instantiate(prefab, socket.position + _vfxOffset, socket.rotation);

            if (_vfxLifetime > 0f)
            {
                UnityEngine.Object.Destroy(vfx, _vfxLifetime);
            }
        }


        private void StopSway()
        {
            if (!_stopSwayDuringAttack || _swayMover == null || _hasStoppedSway)
            {
                return;
            }

            // 左右移動を無効にする
            _swayMover.SetMoveEnable(false);
            _hasStoppedSway = true;
        }


        private void ResumeSway()
        {
            if (!_hasStoppedSway) return;

            _hasStoppedSway= false;

            if (_swayMover == null) return;

            // 左右移動を有効にする
            _swayMover.SetMoveEnable(true);
        }
    }
}
