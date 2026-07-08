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
        private EnemyHoldCounter _holdCounter;
        private Coroutine _downTimerCoroutine;

        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float, float> OnGaugeChanged;
        public event System.Action OnDownStarted;
        public event System.Action OnDropStarted;
        public event System.Action OnDefeated;
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
            statusView.Initialize(InstanceEnemyId, new Vector3(0.0f, 3.6f, 1.5f), definition.HasBarrier);

            // HP管理初期化
            _health = new EnemyHealth();
            _health.Initialize(InstanceEnemyId, definition.MaxHp * spawnSummary.HPRate,
                (current, max) => {
                    EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, current, max));
                    OnHealthChanged?.Invoke(current, max);
                },
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
                (current, max) => {
                    EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max));
                    OnGaugeChanged?.Invoke(current, max);
                },
                HandleGaugeBroken,
                definition.barrierBreakMaxLossRate
        );

            // 敵攻撃処理初期化
            _enemyAttack = new EnemyAttack();
            _enemyAttack.Initialize(definition.AttackPower, definition.Attackinterval, false);


            // 初期HP・ゲージをUIに通知
            EventBus.Publish(new EnemyHealthChangedEvent(InstanceEnemyId, definition.MaxHp, definition.MaxHp));
            OnHealthChanged?.Invoke(definition.MaxHp, definition.MaxHp);
            if (definition.HasBarrier)
            {
                EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, 0f, definition.MaxGauge));
                OnGaugeChanged?.Invoke(0f, definition.MaxGauge);
            }

            Debug.Log($"[EnemyController] {InstanceEnemyId} 初期化完了。HP={definition.MaxHp * spawnSummary.HPRate}," +
                      $" MaxGauge={definition.MaxGauge * spawnSummary.BarrierRate}, BarrierActive={definition.HasBarrier}");


            Vector3 _currentPos = this.gameObject.transform.position;
            _currentPos.y = 0.0f;

            _holdCounter = new EnemyHoldCounter();
            _holdCounter.Initialize(0.35f, 0.07f, HandleHoldEnd);

            // 上昇初期化
            // Risingのみコルーチンを使用しているため、
            // 苦肉の策でMonoBehaviour
            _rising = GetComponent<EnemyRising>();
            if (_rising == null)
            {
                gameObject.AddComponent<EnemyRising>();
            }

            _rising.Initialize(definition.RiseDuration, definition.DropDuration, definition.BarrierBreakDuration, definition.DamageDropDuration,
                               definition.RiseCurve, definition.DropCurve, definition.BarrierBreakCurve, definition.DamageDropCurve,
                               definition.BreakDropDistance, definition.DamageDropDistance);
            _rising.OnEnemyReachedGoal = HandleRose;
            _rising.OnLeftReachedGoal = HandleRoseLeft;
            _rising.OnEnemyDroped = HandleDroped;

            // 上昇開始
            _rising.StartRise(spawnSummary.TargetPos, spawnSummary.UndergroundOffset, transform);

            return InstanceEnemyId;
        }

        public bool BarrierInitialize(EnemyDefinition definition, SpawnSummary spawnSummary, GameObject barrierObject)
        {

            if (!barrierObject.TryGetComponent(out EnemyBarrierReceiver barrierReceiver))
            {
                Debug.LogWarning("[EnemySpawner] EnemyBarrierReceiver が付与されていないためバリアの生成を中止します。", barrierObject);
                Destroy(barrierObject);
                return false;
            }

            // バリアゲージを再初期化
            _barrierGauge.Initialize(
                InstanceEnemyId,
                definition.MaxGauge * spawnSummary.BarrierRate,
                definition.HealRegenWaitTime, definition.HealPower * spawnSummary.BarrierRate,
                barrierObject,
                (current, max) => {
                    EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max));
                    OnGaugeChanged?.Invoke(current, max);
                },
                HandleGaugeBroken,
                definition.barrierBreakMaxLossRate

            );

            // 実体オブジェクトを初期化
            barrierReceiver.Initialize(InstanceEnemyId);

            return true;
        }

        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        private void Update()
        {
            _barrierGauge.UpdateBarrier();
            _enemyAttack.UpdateAttack();

            if (_stateManager.CurrentState == EnemyState.OverHit) _holdCounter.UpdateHold();
        }

        // ─────────────────────────────────────────
        // イベント受信
        // ─────────────────────────────────────────

        /// <summary>
        /// EnemyHitReceiverから直接呼ばれ、現在の状態に応じてダメージをルーティングする。
        /// </summary>
        public void OnBodyHit(float bodyDamage)
        {
            if (_definition == null) return;
            if (_stateManager.CurrentState == EnemyState.OverHit) _holdCounter.AddHit();
            _health.ApplyBodyDamage(bodyDamage);
            _rising.DamageDrop(transform);
        }

        /// <summary>
        /// EnemyBarrierReceiverから直接呼ばれ、現在の状態に応じてダメージをルーティングする。
        /// </summary>
        public void OnBarrierHit(float gaugeDamage)
        {
            if (_definition == null) return;
            _barrierGauge.ApplyGaugeDamage(gaugeDamage);
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
        /// HP0到達時の処理。踏ん張り状態へ遷移し、攻撃がやむまでしばらく耐える。
        /// </summary>
        private void HandleDefeated()
        {
            _stateManager.TransitionTo(EnemyState.OverHit);

            if (_downTimerCoroutine != null)
            {
                StopCoroutine(_downTimerCoroutine);
                _downTimerCoroutine = null;
            }

            _barrierGauge.SetActive(false);
            _rising.MoveStop();
            _holdCounter.StartCount(0.35f);

        }

        /// <summary>
        /// 踏ん張り終了時の処理。落下状態へ遷移し、Risingを落下させる。
        /// </summary>
        private void HandleHoldEnd()
        {
            // ステートを遷移し落下
            _stateManager.TransitionTo(EnemyState.Down);
            _rising.DropStart(transform);
            OnDropStarted?.Invoke();
        }



        /// <summary>
        /// 完全落下の処理。撃破状態へ遷移し、EnemyDefeatedEventを発行する。
        /// </summary>
        private void HandleDroped()
        {
            _stateManager.TransitionTo(EnemyState.Defeated);
            EventBus.Publish(new EnemyDefeatedEvent(InstanceEnemyId));
            OnDefeated?.Invoke();
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
            _rising.BreakDrop(transform);
            _stateManager.TransitionTo(EnemyState.Down);
            _barrierGauge.SetActive(false);

            EventBus.Publish(new EnemyDownStartedEvent(InstanceEnemyId, _definition.DownDuration));
            OnDownStarted?.Invoke();

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

    // ─────────────────────────────────────────
    // 統合された純粋クラス群
    // ─────────────────────────────────────────

    public class EnemyAttack
    {
        float _maxAttackInterval;
        float _attackInterval;
        float _attackPower;
        bool _isActiv;

        public void Initialize(float attackPower, float attackInterval, bool isActiv)
        {
            _maxAttackInterval = attackInterval;
            _attackInterval = _maxAttackInterval;
            _attackPower = attackPower;
            _isActiv = isActiv;
        }

        public void UpdateAttack()
        {
            if (!_isActiv) return;
            _attackInterval -= Time.deltaTime;
            if (_attackInterval <= 0.0f)
            {
                _attackInterval = _maxAttackInterval;
                Attack();
            }
        }

        public void SetActiv(bool activ) => _isActiv = activ;

        void Attack()
        {
            EventBus.Publish(new RuleBarrierAttackEvent(_attackPower));
        }
    }

    public class EnemyHoldCounter
    {
        int _enemyHitCounter = 0;
        float _enemyHitTimer = 0.0f;
        float _addHoldDuration = 0.0f;
        float _maxHoldDuration = 0.0f;
        System.Action OnHoldEnd;

        public void Initialize(float maxHoldDuration, float addHoldDuration, System.Action OnHoldEnd)
        {
            _maxHoldDuration = maxHoldDuration;
            _addHoldDuration = addHoldDuration;
            this.OnHoldEnd = OnHoldEnd;
        }

        public void StartCount(float defaultDuration)
        {
            _enemyHitTimer = defaultDuration;
            _enemyHitCounter = 0;
        }

        public void UpdateHold()
        {
            if (_enemyHitTimer <= 0.0f) return;
            _enemyHitTimer -= Time.deltaTime;
            if(_enemyHitTimer <= 0.0f)
            {
                OnHoldEnd?.Invoke();
                ResetHit();
            }
        }

        public void ResetHit()
        {
            _enemyHitCounter = 0;
            _enemyHitTimer = 0.0f;
        }

        public void AddHit()
        {
            _enemyHitCounter++;
            _enemyHitTimer += _addHoldDuration;
            _enemyHitTimer = Mathf.Clamp(_enemyHitTimer, 0.0f, _maxHoldDuration);
        }
    }

    public enum EnemyState
    {
        Normal,
        Down,
        OverHit,
        Drop,
        Defeated,
    }

    public class EnemyStateManager
    {
        public EnemyState CurrentState { get; private set; } = EnemyState.Normal;
        private bool IsRose = false;
        public event System.Action<EnemyState> OnStateChanged;
        public bool IsDown => CurrentState == EnemyState.Down;
        public bool IsDefeated => CurrentState == EnemyState.Defeated;
        public bool CanReceiveGaugeDamage => CurrentState == EnemyState.Normal;
        public bool CanReceiveBodyDamage => CurrentState == EnemyState.Down || CurrentState == EnemyState.Normal;
        public bool CanReceiveBodyCombo => CurrentState == EnemyState.Down || CurrentState == EnemyState.Normal || CurrentState == EnemyState.OverHit || CurrentState == EnemyState.Drop;
        public bool CanAttackDefenceLine => IsRose;

        public void SetRose(bool a_isRised)
        {
            if (IsDefeated) return;
            IsRose = a_isRised;
        }

        public void TransitionTo(EnemyState newState)
        {
            if (CurrentState == EnemyState.Defeated) return;
            if (CurrentState == newState) return;

            var prev = CurrentState;
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
