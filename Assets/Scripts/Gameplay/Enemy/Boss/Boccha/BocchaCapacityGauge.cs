// ------------------------------------------------------------
// File		: BocchaCapacityGauge.cs
// Summary	: キング・ボッチャの「キャパ」を蓄積し、満タンで分身フェーズへ移行させる
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 蓄積要素はCapacitySourceで切り替える。既定は「何が出るかな？」の実行回数！！
// - _feverGimmickDataが未設定の間はログのみ出す（M4でやりまうそ）。
// - 変化のたびにBocchaCapacityChangedEventを発行するため、UIはやるなら後付けかな？
// - キング・ボッチャでやってるけど、ななみ様の意向でカボチャじゃなくなりそうだけど、名前はいいか？ｗｗ
// ------------------------------------------------------------
using System;
using Game.Core.Events;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// キャパを蓄積する要因。
    /// </summary>
    public enum BocchaCapacitySource
    {
        /// <summary>「何が出るかな？」の実行回数（既定）</summary>
        SpitCount = 0,

        /// <summary>ボスが受けた累計ダメージ</summary>
        DamageTaken = 1,

        /// <summary>戦闘中の経過時間</summary>
        ElapsedTime = 2,

        /// <summary>プレイヤーが落ち物をボスに当てた回数（既定）</summary>
        HitCount = 3,
    }

    /// <summary>
    /// キャパゲージを管理するコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaCapacityGauge : MonoBehaviour
    {
        [Header("==== 参照 ====")]

        [SerializeField]
        [Tooltip("対象のボス戦フロー制御。未設定なら同じGameObjectから取得する")]
        private BossBattleFlowController _flowController;


        [Header("==== 蓄積設定 ====")]

        [SerializeField]
        [Tooltip("キャパを蓄積する要因")]
        private BocchaCapacitySource _source = BocchaCapacitySource.HitCount;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("キャパオーバーになる値。HitCountなら「何個当てたら」の個数")]
        private float _capacityMax = 15.0f;


        [Header("==== キャパオーバー時 ====")]

        [SerializeField]
        [Tooltip("満タン時に割り込み実行するギミック。未設定ならログのみ")]
        private BossGimmickData _feverGimmickData;

        [SerializeField]
        [Tooltip("ギミック未設定時に自動でゲージをリセットするか。動作確認用")]
        private bool _autoResetWhenGimmickMissing = true;


        [Header("==== デバッグ ====")]

        [SerializeField]
        [Tooltip("蓄積のたびにログを出すかどうか")]
        private bool _logCapacity = true;


        // ランタイム状態
        // ------------------------------------------------------------

        private float _current;
        private bool _isOverflowing;
        private float _lastHp = -1.0f;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>現在のキャパ量。</summary>
        public float Current => _current;

        /// <summary>キャパオーバーになる値。</summary>
        public float CapacityMax => _capacityMax;

        /// <summary>0.0〜1.0 の正規化値（UI用）。</summary>
        public float Ratio => _capacityMax > 0.0f ? Mathf.Clamp01(_current / _capacityMax) : 0.0f;

        /// <summary>キャパオーバー中かどうか。</summary>
        public bool IsOverflowing => _isOverflowing;

        /// <summary>キャパ量が変化した際の通知。&lt;現在値, 最大値&gt;</summary>
        public event Action<float, float> OnCapacityChanged;


        private void Awake()
        {
            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }

            if (_flowController == null)
            {
                Debug.LogWarning($"[{nameof(BocchaCapacityGauge)}] BossBattleFlowController が未設定です。");
            }
        }


        private void OnEnable()
        {
            // ボス個体IDで絞るため、フロー制御の有無に関わらず購読する
            EventBus.Subscribe<EnemyHitBatchEvent>(HandleEnemyHit);

            if (_flowController == null)
            {
                return;
            }

            _flowController.OnHpChanged += HandleHpChanged;
        }


        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(HandleEnemyHit);

            if (_flowController == null)
            {
                return;
            }
            _flowController.OnHpChanged -= HandleHpChanged;
        }


        private void Update()
        {
            if (_source == BocchaCapacitySource.ElapsedTime)
            {
                return;
            }

            if (_flowController == null || _flowController.CurrentState != BossBattleFlowState.InBattle)
            {
                return;
            }

            Add(Time.deltaTime);
        }


        // 公開メソッド
        // ------------------------------------------------------------

        // <summary>
        /// 「何が出るかな？」の実行を1回ぶん蓄積する。SpitCount以外の設定では無視される。
        /// </summary>
        /// <param name="amount">蓄積量。既定は1.0f</param>
        public void AddFromSpit(float amount = 1.0f)
        {
            if (_source != BocchaCapacitySource.SpitCount)
            {
                return;
            }

            Add(amount);
        }


        /// <summary>
        /// キャパを蓄積する。満タンになったらギミックを割り込み実行する。
        /// </summary>
        /// <param name="amount">キャパの蓄積量</param>
        public void Add(float amount)
        {
            if (amount <= 0.0f || _isOverflowing)
            {
                return;
            }

            _current = Mathf.Min(_capacityMax, _capacityMax + amount);

            NotifyChanged();

            if (_logCapacity)
            {
                Debug.Log($"[CapacityGauge] キャパ {_current:0.##} / {_capacityMax:0.##}", this);
            }

            if (_current < _capacityMax) return;

            TriggerOverflow();
        }


        /// <summary>
        /// ゲージを空に戻す。分身フェーズ終了時などに呼び出す予定！
        /// </summary>
        public void ResetGauge()
        {
            _current = 0.0f;
            _isOverflowing = false;

            NotifyChanged();
        }


        // 内部処理
        // ------------------------------------------------------------

        /// <summary>
        /// 落ち物がボスへ当たったことを受けて蓄積する。HitCount以外の設定では無視される。
        /// </summary>
        /// <param name="hitEvent">ヒット通知</param>
        private void HandleEnemyHit(EnemyHitBatchEvent hitEvent)
        {
            if (_source != BocchaCapacitySource.HitCount) return;

            if (_flowController == null) return;

            // 雑魚敵や他のボス個体へのヒットを除外する
            if (hitEvent.EnemyId != _flowController.BossInstanceId) return;

            Add(Mathf.Max(1, hitEvent.HitCount));
        }

        /// <summary>
        /// HP変化を受けてダメージ量を蓄積する。DamageTaken以外の設定では無視される。
        /// </summary>
        /// <param name="current">現在のHP</param>
        /// <param name="max">最大HP</param>
        private void HandleHpChanged(float current, float max)
        {
            if (_source != BocchaCapacitySource.DamageTaken) return;

            if (_lastHp < 0.0f)
            {
                _lastHp = current;

                return;
            }

            float damage = _lastHp - current;

            _lastHp = current;

            if (damage <= 0.0f) return;

            Add(damage);
        }


        /// <summary>
        /// キャパオーバー時のギミックを割り込み実行する。
        /// </summary>
        private void TriggerOverflow()
        {
            _isOverflowing = true;

            if (_feverGimmickData == null)
            {
                Debug.Log("[CapacityGauge] <color=orange>キャパオーバー！</color>（Feverギミック未設定のため発動しません）", this);

                if (_autoResetWhenGimmickMissing) ResetGauge();

                return;
            }

            Debug.Log($"[CapacityGauge] <color=orange>キャパオーバー！</color>（Feverギミック: {_feverGimmickData.name} を発動）", this);

            // ギミックを割り込み実行する
            _flowController.EnqueueInterruptGimmick(_feverGimmickData);
        }


        /// <summary>
        /// キャパ量が変化したことを通知する。
        /// </summary>
        private void NotifyChanged()
        {
            OnCapacityChanged?.Invoke(_current, _capacityMax);

            EventBus.Publish(new BocchaCapacityChangedEvent(_current, _capacityMax));
        }


        // デバッグ用
        // ------------------------------------------------------------

        [ContextMenu("ぱちチート/キャパを即満タンにするぜよ")]
        private void DebugFillCapacity()
        {
            _isOverflowing = false;

            Add(_capacityMax);
        }


        /// <summary>
        /// キャパを空に戻す。動作確認用。
        /// </summary>
        [ContextMenu("ぱちチート/キャパをリセット")]
        private void DebugResetCapacity() => ResetGauge();
    }
}
