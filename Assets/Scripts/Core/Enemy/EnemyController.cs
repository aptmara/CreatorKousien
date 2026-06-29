// 制作者: 山内陽
using Game.Core.Events;
using Game.Presentation.UI;
using System.Collections;
using System.Threading;
using UnityEngine;

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
        /// <summary>
        /// 初期化等の際に必要な生成情報を受け取るための構造体
        /// </summary>
        public struct SpawnSummary
        {

            public Vector3 TargetPos;
            public float UndergroundOffset;
            public float HPRate;
            public float BarrierRate;

            public SpawnSummary(Vector3 targetPos, float undergroundOffset, float hpRate, float barrierRate)  
            {
                TargetPos = targetPos;
                UndergroundOffset = undergroundOffset;
                HPRate = hpRate;
                BarrierRate = barrierRate;
            }
        }


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
        }

        /// <summary>
        /// EnemyDefinitionで全コンポーネントを初期化する。
        /// Awake後の動的生成（スポーン）でも呼び出せる。
        /// </summary>
        /// <param name="def">適用するEnemyDefinition</param>
        public string Initialize(EnemyDefinition definition, SpawnSummary spawnSummary)
        {
            _definition = definition;
            InstanceEnemyId = $"{definition.EnemyId}_{GetInstanceID()}";

            // 状態管理初期化
            _stateManager = new EnemyStateManager();

            // 体力バー表示のためのView設定
            EnemyWorldStatusView statusView = GetComponent<EnemyWorldStatusView>();
            if (statusView == null)
            {
                gameObject.AddComponent<EnemyWorldStatusView>();
            }
            // TODO 敵オブジェクトの高さを決定できるようにする
            statusView.Initialize(InstanceEnemyId, new Vector3(0.0f, 3.6f, 1.5f));

            // HP管理初期化
            _health = new EnemyHealth();
            _health.Initialize(InstanceEnemyId, definition.MaxHp * spawnSummary.HPRate,
                (current, max) => EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, current, max)),
                HandleDefeated
            );
            
            

            // バリア初期化、 初期化しないのも大変なため、当たり判定オブジェクトが存在しない状態で初期化しておく
            _barrierGauge = new EnemyBarrierGauge();
            _barrierGauge.Initialize
                (InstanceEnemyId,
                definition.MaxGauge * spawnSummary.BarrierRate,
                definition.HealRegenWaitTime,
                definition.HealPower * spawnSummary.BarrierRate,
                null,
                (current, max) =>
                EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max)),
                HandleGaugeBroken,
                definition.barrierBreakMaxLossRate
        );

            // 敵攻撃処理初期化
            _enemyAttack = new EnemyAttack();
            _enemyAttack.Initialize(definition.AttackPower, definition.Attackinterval, false);


            // 初期HP・ゲージをUIに通知
            EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, definition.MaxHp, definition.MaxHp));
            EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, 0f, definition.MaxGauge));

            Debug.Log($"[EnemyController] {InstanceEnemyId} 初期化完了。HP={definition.MaxHp * spawnSummary.HPRate}," +
                      $" MaxGauge={definition.MaxGauge * spawnSummary.BarrierRate}, BarrierActive={definition.HasBarrier}");


            Vector3 _currentPos = this.gameObject.transform.position;
            _currentPos.y = 0.0f;


            // 上昇初期化
            // Risingのみコルーチンを使用しているため、
            // 苦肉の策でMonoBehaviour
            _rising = GetComponent<EnemyRising>();
            if (_rising == null)
            {
                gameObject.AddComponent<EnemyRising>();
            }

            _rising.Initialize(definition.RiseDuration, definition.DropDuration, definition.BarrierBreakDuration, definition.riseCurve, definition.dropCurve, definition.barrierBreakCurve);
            _rising.OnEnemyReachedGoal = HandleRose;
            _rising.OnLeftReachedGoal = HandleRoseLeft;
            _rising.OnEnemyDroped = HandleDroped;

            // 上昇開始
            _rising.StartRise(spawnSummary.TargetPos, spawnSummary.UndergroundOffset, transform);

            return InstanceEnemyId;
        }

        public bool BarrierInitialize(EnemyDefinition definition, SpawnSummary spawnSummary, GameObject barrierObject)
        {

            if (!barrierObject.TryGetComponent(out EnemyBirrerReceiver barrierReceiver))
            {
                Debug.LogWarning("[EnemySpawner] EnemyHitReceiver が付与されていないためバリアの生成を中止します。", barrierObject);
                Destroy(barrierObject);
                return false;
            }

            // バリアゲージを再初期化
            _barrierGauge.Initialize(
                InstanceEnemyId,
                definition.MaxGauge * spawnSummary.BarrierRate,
                definition.HealRegenWaitTime, definition.HealPower * spawnSummary.BarrierRate,
                barrierObject,
                (current, max) => EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max)),
                HandleGaugeBroken,
                definition.barrierBreakMaxLossRate

            );

            // 実体オブジェクトを初期化
            barrierReceiver.Initialize(InstanceEnemyId);

            return true;
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

        private void Update()
        {
            _barrierGauge.UpdateBarrier();
            _enemyAttack.UpdateAttack();
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
        /// HP0到達時の処理。落下状態へ遷移し、Risingを落下させる。
        /// </summary>
        private void HandleDefeated()
        {
            if (_downTimerCoroutine != null)
            {
                StopCoroutine(_downTimerCoroutine);
                _downTimerCoroutine = null;
            }

            _barrierGauge.SetActive(false);

            _stateManager.TransitionTo(EnemyState.OverHit);

            _rising.DropStart(transform);
        }


        /// <summary>
        /// 完全落下の処理。撃破状態へ遷移し、EnemyDefeatedEventを発行する。
        /// </summary>
        private void HandleDroped()
        {
            _stateManager.TransitionTo(EnemyState.Defeated);
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
