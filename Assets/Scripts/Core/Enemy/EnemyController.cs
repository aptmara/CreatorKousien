// 制作者: 山内陽
using UnityEngine;
using System.Collections;
using Game.Core.Events;

namespace Game.Core.Enemy
{
    /// <summary>
    /// 敵の全コンポーネントを統括するコントローラ。
    /// 責務:
    ///   入力: EventBusの EnemyHitBatchEvent を受信し、EnemyIdで自分宛かフィルタリング
    ///   変更: 現在の状態（EnemyStateManager）に応じてダメージをゲージ/HPへルーティング
    ///   出力: EventBus へ各種イベントを発行（EnemyGaugeChangedEvent, EnemyHealthChangedEvent,
    ///         EnemyGaugeBrokenEvent, EnemyDownStartedEvent, EnemyDefeatedEvent, EnemyAttackFiredEvent）
    /// </summary>
    [RequireComponent(typeof(EnemyAttackGauge))]
    [RequireComponent(typeof(BarrierController))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("この敵に適用するEnemyDefinition。実行時にInitialize(def)で差し替えも可能。")]
        private EnemyDefinition _definition;

        /// <summary>
        /// 実行時に割り当てられる一意の敵ID。複数敵がいる場合のイベントのルーティングに使用する。
        /// </summary>
        public string InstanceEnemyId { get; private set; }

        private EnemyHealth _health;
        private EnemyStateManager _stateManager;
        private EnemyBarrierGauge _barrierGauge;
        private EnemyRising _rising;
        private EnemyAttack _enemyAttack;
        private Coroutine _downTimerCoroutine;

        [Header("演出設定")]
        [Tooltip("撃破後、敵オブジェクトを消すまでの遅延時間（秒）。0の場合は即座に消す。")]
        [SerializeField, Min(0f)] private float _destroyDelay = 0f;

        private void Awake()
        {
            // RequireComponentで必ず存在するためnullチェック不要
            _barrierGauge = GetComponent<EnemyBarrierGauge>();
            _rising = GetComponent<EnemyRising>();
            _enemyAttack = GetComponent<EnemyAttack>();

            if (_definition != null)
            {
                Initialize(_definition);
            }
        }

        /// <summary>
        /// EnemyDefinitionで全コンポーネントを初期化する。
        /// Awake後の動的生成（スポーン）でも呼び出せる。
        /// </summary>
        /// <param name="def">適用するEnemyDefinition</param>
        public void Initialize(EnemyDefinition def)
        {
            _definition = def;
            InstanceEnemyId = $"{def.EnemyId}_{GetInstanceID()}";

            // 状態管理初期化
            _stateManager = new EnemyStateManager();

            // HP管理初期化
            _health = new EnemyHealth();
            _health.Initialize(InstanceEnemyId, def.MaxHp);
            _health.OnHealthChanged = (current, max) =>
                EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, current, max));
            _health.OnDefeated = HandleDefeated;

            // ゲージ管理初期化
            // TODO越智 EnemyDefinitionを現在の形に改変
            _barrierGauge.Initialize(InstanceEnemyId, def.MaxGauge, def.HealRegenWaitTime, def.HealPower);
            _barrierGauge.OnGaugeChanged = (current, max) =>
                EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max));
            _barrierGauge.OnGaugeBroken = HandleGaugeBroken;

            // 上昇初期化
            _rising.Initialize(def.RiseDuration);
            _rising.OnEnemyReachedGoal = HandleRose;
            _rising.OnLeftReachedGoal = HandleRoseLeft;

            // 敵攻撃処理初期化
            _enemyAttack.Initialize(def.AttackPower, def.Attackinterval, false);


            // 初期HP・ゲージをUIに通知
            EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, def.MaxHp, def.MaxHp));
            EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, 0f, def.MaxGauge));

            Debug.Log($"[EnemyController] {InstanceEnemyId} 初期化完了。HP={def.MaxHp}, MaxGauge={def.MaxGauge}, BarrierActive={def.HasBarrier}");



            Vector3 _currentPos = this.gameObject.transform.position;
            _currentPos.y = 0.0f;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHitBatch);
            EventBus.Subscribe<BarrierHitBatchEvent>(OnBarrierHitBatch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHitBatch);
            EventBus.Unsubscribe<BarrierHitBatchEvent>(OnBarrierHitBatch);
        }

        // ─────────────────────────────────────────
        // イベント受信
        // ─────────────────────────────────────────

        /// <summary>
        /// EnemyHitBatchEventを受信し、現在の状態に応じてダメージをルーティングする。
        /// Normal時: ゲージダメージを適用。
        /// Down時: 本体ダメージを適用。
        /// Defeated時: 無視。
        /// </summary>
        private void OnEnemyHitBatch(EnemyHitBatchEvent ev)
        {
            if (_definition == null || ev.EnemyId != InstanceEnemyId) return;

            _health.ApplyBodyDamage(ev.BodyDamage);

        }

        /// <summary>
        /// BarrierHitBatchEventを受信し、現在の状態に応じてダメージをルーティングする。
        /// Normal時: ゲージダメージを適用。
        /// Down時: 本体ダメージを適用。
        /// Defeated時: 無視。
        /// </summary>
        private void OnBarrierHitBatch(BarrierHitBatchEvent ev)
        {
            if (_definition == null || ev.EnemyId != InstanceEnemyId) return;

            _barrierGauge.ApplyGaugeDamage(ev.GaugeDamage);
        }


        // ─────────────────────────────────────────
        // コールバックハンドラ
        // ─────────────────────────────────────────

        /// <summary>
        /// ゲージが0以下になった（プレイヤーが止めた）場合の処理。
        /// EnemyGaugeBrokenEvent発行 → Down状態へ遷移。
        /// </summary>
        private void HandleGaugeBroken()
        {
            EventBus.Publish(new EnemyGaugeBrokenEvent(InstanceEnemyId));
            TransitionToDown();
        }

        /// <summary>
        /// HP0到達時の処理。撃破状態へ遷移し、EnemyDefeatedEventを発行する。
        /// </summary>
        private void HandleDefeated()
        {
            if (_downTimerCoroutine != null)
            {
                StopCoroutine(_downTimerCoroutine);
                _downTimerCoroutine = null;
            }

            _stateManager.TransitionTo(EnemyState.Defeated);
            _barrierGauge.SetActive(false);

            EventBus.Publish(new EnemyDefeatedEvent(InstanceEnemyId));
            Debug.Log($"[EnemyController] {InstanceEnemyId} 撃破！");

            Destroy(gameObject, _destroyDelay);
        }

        /// <summary>
        /// 上昇終了時の処理。攻撃状態へ遷移する。
        /// </summary>
        private void HandleRose()
        {
            _stateManager.SetRose(true);
            _enemyAttack.SetActiv(_stateManager.CanAttackDefenceLine);
        }

        /// <summary>
        /// 上昇しきった後引きずり落された時の処理。攻撃状態を解除する。
        /// </summary>
        private void HandleRoseLeft()
        {
            _stateManager.SetRose(false);
            _enemyAttack.SetActiv(false);
        }

        // ─────────────────────────────────────────
        // 状態遷移
        // ─────────────────────────────────────────

        /// <summary>
        /// Down状態へ遷移する。ゲージとバリアを停止し、ダウンタイマーを開始する。
        /// </summary>
        private void TransitionToDown()
        {
            _stateManager.TransitionTo(EnemyState.Down);
            _barrierGauge.SetActive(false);

            EventBus.Publish(new EnemyDownStartedEvent(InstanceEnemyId, _definition.DownDuration));

            // 既存タイマーがあれば停止してから再スタート
            if (_downTimerCoroutine != null) StopCoroutine(_downTimerCoroutine);
            _downTimerCoroutine = StartCoroutine(DownTimerRoutine(_definition.DownDuration));
        }

        /// <summary>
        /// DownDuration秒後にNormalへ自動復帰するコルーチン。
        /// </summary>
        private IEnumerator DownTimerRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);

            // 撃破されていなければNormalへ復帰
            if (_stateManager.CurrentState == EnemyState.Down)
            {
                _stateManager.TransitionTo(EnemyState.Normal);
                _barrierGauge.ResetGauge();
                _barrierGauge.SetActive(true);
                _barrierGauge.SetActive(_definition.HasBarrier);

                Debug.Log($"[EnemyController] {InstanceEnemyId} ダウン復帰。ゲージリセット。");
            }

            _downTimerCoroutine = null;
        }
    }
}
