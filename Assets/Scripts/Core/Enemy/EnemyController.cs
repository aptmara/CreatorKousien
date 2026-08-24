// 制作者: 山内陽
// 7.16 - BOSSの情報追加
using Game.Core.Events;
using Game.Presentation.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;


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
        public bool IsBoss => _definition != null && _definition.IsBoss;

        /// <summary>
        /// 現在の状態
        /// </summary>
        public EnemyState CurrentState => _stateManager != null ? _stateManager.CurrentState : EnemyState.Normal;

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
        private EnemyDebuffManager _debuffManager;
        private EnemyBodyController _bodyController;
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
        public string Initialize(EnemyDefinition definition, SpawnSummary spawnSummary, EnemyBodyController bodyController)
        {
            _definition = definition;
            _bodyController = bodyController;
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
            statusView.Initialize(InstanceEnemyId, new Vector3(0.0f, 1.6f, 0.5f), definition.HasBarrier);

            // HP管理初期化
            _health = new EnemyHealth();
            _health.Initialize(InstanceEnemyId, definition.MaxHp * spawnSummary.HPRate,
                (current, max) =>
                {
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
                (current, max) =>
                {
                    EventBus.Publish(new EnemyGaugeChangedEvent(InstanceEnemyId, current, max));
                    OnGaugeChanged?.Invoke(current, max);
                },
                HandleGaugeBroken,
                definition.barrierBreakMaxLossRate);

            // 敵攻撃処理初期化
            _enemyAttack = new EnemyAttack();
            _enemyAttack.Initialize(
                InstanceEnemyId,
                definition.AttackPower,
                definition.Attackinterval,
                false,
                definition.AttackMotionTime,
                definition.AttackStartUpTime,
                attackPower => EventBus.Publish(new RuleBarrierAttackEvent(attackPower, transform.position)));


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

            _rising.Initialize(definition.RiseDuration,definition.LateralDuration, definition.DropDuration, definition.BarrierBreakDuration, definition.DamageDropDuration,
                               definition.RiseCurve, definition.LateralCurve, definition.DropCurve, definition.BarrierBreakCurve, definition.DamageDropCurve,
                               definition.BreakDropDistance, definition.DamageDropDistance);
            _rising.OnEnemyReachedGoal = HandleRose;
            _rising.OnLeftReachedGoal = HandleRoseLeft;
            _rising.OnEnemyDroped = HandleDroped;

            // 上昇開始
            _rising.StartRise(spawnSummary.TargetPos, spawnSummary.UndergroundOffset, transform);

            _debuffManager = new EnemyDebuffManager(
                HandleDamageOverTime,
                HandleFreeze,
                HandleFreezeEnd,
                (statusType, stackCount, isActive) =>
                    EventBus.Publish(new EnemyStatusChangedEvent(InstanceEnemyId, statusType, stackCount, isActive)),
                statusType => EventBus.Publish(
                    new EnemyFreezeBrokenEvent(InstanceEnemyId, transform.position)));

            // VFX コンポーネントはオプション（不要な敵はコンポーネントを外す）
            var statusVFX = GetComponent<EnemyStatusAilmentVFX>();
            if (statusVFX != null)
            {
                statusVFX.Initialize(_debuffManager, this);
            }

            return InstanceEnemyId;
        }


        /// <summary>
        /// ボス用のEnemyDefinitionで初期化する。通常の敵と異なり、Update処理は無効化される。
        /// </summary>
        /// <param name="definition">ボスに設定されているEnemyDefinition</param>
        /// <returns>Wave管理やイベント通知に使用するボス固有のID</returns>
        public string InitializeForBoss(EnemyDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[EnemyController] Boss用のEnemyDefinitionがnullです。", this);
                return string.Empty;
            }

            if (!definition.IsBoss)
            {
                Debug.LogError("[EnemyController] Boss用のEnemyDefinitionではありません。", this);
                return string.Empty;
            }

            _definition = definition;
            InstanceEnemyId = $"{definition.EnemyId}_{GetInstanceID()}";

            // 通常敵用のUpdate処理はボスでは使用しない
            enabled = false;

            Debug.Log($"[{nameof(EnemyController)}] ボス「{InstanceEnemyId}」をWave管理用として初期化しました。", this);

            return InstanceEnemyId;
        }


        public bool BarrierInitialize(EnemyDefinition definition, SpawnSummary spawnSummary, GameObject barrierObject)
        {
            EnemyBarrierReceiver barrierReceiver = barrierObject.GetComponentInChildren<EnemyBarrierReceiver>(true);

            if (barrierReceiver == null)
            {
                Debug.LogWarning("[EnemySpawner] EnemyBarrierReceiver が子階層にも見つからないためバリアの生成を中止します。", barrierObject);
                Destroy(barrierObject);
                return false;
            }

            // バリアゲージを再初期化
            _barrierGauge.Initialize(
                InstanceEnemyId,
                definition.MaxGauge * spawnSummary.BarrierRate,
                definition.HealRegenWaitTime, definition.HealPower * spawnSummary.BarrierRate,
                barrierObject,
                (current, max) =>
                {
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
            _debuffManager.UpdateDebuff();

            if (_stateManager.CurrentState == EnemyState.OverHit) _holdCounter.UpdateHold();
        }

        /// <summary>
        /// ギミックから即時に防衛ライン攻撃を発生させる。
        /// </summary>
        public void AttackNow()
        {
            _enemyAttack?.AttackNow();
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
            _debuffManager.AddHit();
            if (_stateManager.CurrentState == EnemyState.OverHit) _holdCounter.AddHit();
            _health.ApplyBodyDamage(bodyDamage);
            _rising.DamageDrop(transform);
        }

        public void OnAddDebuff(EnemyDebuffConfig _config)
        {
            // デバフを追加
            if (_stateManager.CanAddDebuff)
            {
                // デバフを追加！
                _debuffManager.AddDebuff(_config);
            }
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

        private void HandleDamageOverTime(float damage)
        {
            _health.ApplyBodyDamage(damage);
        }

        private void HandleFreeze(float freezeDuration)
        {
            _rising.StopMove();
        }

        private void HandleFreezeEnd()
        {
            _rising.ResumeMove();
        }

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
            _enemyAttack.ResetAttack();
            _enemyAttack.SetActiv(false);

            if (_downTimerCoroutine != null)
            {
                StopCoroutine(_downTimerCoroutine);
                _downTimerCoroutine = null;
            }

            EventBus.Publish(new EnemyDefeatStatusSnapshotEvent(
                InstanceEnemyId,
                transform.position,
                _debuffManager.GetActiveDebuffTypes()));
            _debuffManager.RemoveAllDebuffs();
            _barrierGauge.SetActive(false);
            _rising.StopMove();
            _holdCounter.StartCount(0.35f);

        }

        /// <summary>
        /// 踏ん張り終了時の処理。落下状態へ遷移し、Risingを落下させる。
        /// </summary>
        private void HandleHoldEnd()
        {
            _stateManager.TransitionTo(EnemyState.Drop);

            if (!_definition.IsBoss && _bodyController != null)
            {
                _bodyController.PlayDropPose(BeginDefeatDrop);
                return;
            }

            BeginDefeatDrop();
        }

        private void BeginDefeatDrop()
        {
            _rising.ResumeMove();
            _rising.DropStart(transform);
            OnDropStarted?.Invoke();

            // 撃破落下の開始を通知する（Waveの最後の敵ならクリア演出がここから始まる）
            EventBus.Publish(new EnemyDefeatDropStartedEvent(InstanceEnemyId, transform));
            Debug.Log($"[DropDebug] {InstanceEnemyId} 撃破落下開始 time={Time.time:F2}"); // TODO: 動作確認後に削除
        }



        /// <summary>
        /// 完全落下の処理。撃破状態へ遷移し、EnemyDefeatedEventを発行する。
        /// </summary>
        private void HandleDroped()
        {
            _stateManager.TransitionTo(EnemyState.Defeated);

            try
            {
                EventBus.Publish(new EnemyDefeatedEvent(InstanceEnemyId, transform.position));
                OnDefeated?.Invoke();
                Debug.Log($"[EnemyController] {InstanceEnemyId} 撃破！");
            }
            finally
            {
                Destroy(gameObject, _destroyDelay);
            }
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
            _enemyAttack.ResetAttack();
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
            _enemyAttack.ResetAttack();
            _enemyAttack.SetActiv(false);
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
                _enemyAttack.SetActiv(_stateManager.CanAttackDefenceLine);

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
        string _enemyID;
        Action<float> _onAttack;

        float _maxAttackInterval;
        float _attackInterval;
        float _attackPower;
        bool _isActiv;

        float _attackStartUpLag;
        float _attackMotionTime;


        float _attackTimer;
        float _attackMotionTimer;

        bool _isAttackMotion;
        bool _hasAttacked;

        public void Initialize(
            string enemyID,
            float attackPower,
            float attackInterval,
            bool isActiv,
            float attackMotionTime,
            float startUpLag,
            Action<float> onAttack)
        {
            _enemyID = enemyID;
            _onAttack = onAttack;
            _attackStartUpLag = startUpLag;
            _attackMotionTime = attackMotionTime;
            _maxAttackInterval = attackInterval;
            _attackInterval = _maxAttackInterval;
            _attackPower = attackPower;
            _isActiv = isActiv;
        }

        public void UpdateAttack()
        {
            if (!_isActiv) return;

            if (_isAttackMotion) UpdateAttackMotion();
            else UpdateAttackWait();
        }

        private void UpdateAttackWait()
        {
            // タイマーを進め、0になったら攻撃開始をコールバックする
            _attackInterval -= Time.deltaTime;
            if (_attackInterval <= 0.0f)
            {

                _attackInterval = _maxAttackInterval;

                _attackMotionTimer = _attackMotionTime;
                _attackTimer = _attackStartUpLag;
                EventBus.Publish(new EnemyAttackMotionStartedEvent(_enemyID));
                _isAttackMotion = true;
                _hasAttacked = false;
            }
        }

        private void UpdateAttackMotion()
        {
            // 前隙終了
            _attackTimer -= Time.deltaTime;
            if (!_hasAttacked && _attackTimer <= 0.0f)
            {
                AttackNow();
                _hasAttacked = true;
            }

            // 攻撃終了
            _attackMotionTimer -= Time.deltaTime;
            if (_attackMotionTimer <= 0.0f)
            {
                // もしモーション終了時に前隙攻撃が行われていなかったら攻撃する
                if (!_hasAttacked)
                {
                    AttackNow();
                }

                EventBus.Publish(new EnemyAttackMotionEndedEvent(_enemyID));

                _isAttackMotion = false;
                _hasAttacked = false;
            }
        }

        public void SetActiv(bool activ)
        {
            _isActiv = activ;
        }

        public void ResetAttack()
        {
            if (_isAttackMotion) EventBus.Publish(new EnemyAttackMotionEndedEvent(_enemyID));
            _isAttackMotion = false;
            _hasAttacked = false;
            _attackInterval = _maxAttackInterval;
            _attackTimer = _attackStartUpLag;
            _attackMotionTimer = _attackMotionTime;

        }

        public void AttackNow()
        {
            _onAttack?.Invoke(_attackPower);
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
            if (_enemyHitTimer <= 0.0f)
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
        public bool CanAddDebuff => CurrentState == EnemyState.Normal || CurrentState == EnemyState.Down;

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

    public class EnemyDebuffManager
    {
        // デバフ管理Map
        private Dictionary<string, EnemyDebuffRuntime> _activeDebuffs = new Dictionary<string, EnemyDebuffRuntime>();
        // コールバックハンドラ
        private Action<float> _damageRequest;
        private Action<float> _freezeRequest;
        private Action _freezeEndRequest;
        private readonly Action<string, int, bool> _statusChanged;
        private readonly Action<string> _freezeBroken;

        /// <summary>デバフが新規付与されたときに発火する（debuffType）。</summary>
        public event Action<string> OnDebuffAdded;
        /// <summary>デバフが解除されたときに発火する（debuffType）。</summary>
        public event Action<string> OnDebuffRemoved;

        public EnemyDebuffManager(
            Action<float> damageRequest,
            Action<float> freezeRequest,
            Action freezeEndRequest,
            Action<string, int, bool> statusChanged = null,
            Action<string> freezeBroken = null)
        {
            _damageRequest = damageRequest;
            _freezeRequest = freezeRequest;
            _freezeEndRequest = freezeEndRequest;
            _statusChanged = statusChanged;
            _freezeBroken = freezeBroken;
        }

        public void AddHit()
        {
            foreach(var debuff in _activeDebuffs.Values.Reverse().ToList())
            {
                debuff.AddHit();
            }
        }

        public void AddDebuff(EnemyDebuffConfig debuffConfig)
        {
            EnemyDebuffRuntime currentActivDebuff;
            if (_activeDebuffs.TryGetValue(debuffConfig.DebuffType, out currentActivDebuff))
            {
                // 状態異常効果時間をリセットする
                currentActivDebuff.MergeDebuffEffect(debuffConfig);
                _statusChanged?.Invoke(debuffConfig.DebuffType, currentActivDebuff.StackCount, true);

                // 追加された状態異常に応じて効果リクエストを送信
                if (debuffConfig.UseFreeze)
                {
                    _freezeRequest.Invoke(0.8f);
                }
            }
            else
            {
                // 新たに状態異常を追加する
                EnemyDebuffRuntime data = new EnemyDebuffRuntime(debuffConfig, HandleDamageOverTime, HandleFreezeEnd, HandleFreezeBreak, HandleDebuffTimeOver);
                _activeDebuffs[debuffConfig.DebuffType] = data;

                Debug.Log(debuffConfig.DebuffType.ToString() + "を追加！");

                OnDebuffAdded?.Invoke(debuffConfig.DebuffType);
                _statusChanged?.Invoke(debuffConfig.DebuffType, data.StackCount, true);

                // 追加された状態異常に応じて効果リクエストを送信
                if(data.IsFreezeActive)
                {
                    _freezeRequest.Invoke(0.8f);
                }
            }

        }
        public void RemoveDebuff(string debuffKey)
        {
            // デバフの効果を解除する処理
            _activeDebuffs.Remove(debuffKey);

            OnDebuffRemoved?.Invoke(debuffKey);
            _statusChanged?.Invoke(debuffKey, 0, false);

            // 解除後の状況に応じてコールバックを返す
            if (!IsFreeze())
            {
                _freezeEndRequest.Invoke();
            }
        }

        public void RemoveAllDebuffs()
        {
            // すべてのデバフの効果を先に解除する
            if(IsFreeze())
            {
                _freezeEndRequest.Invoke();
            }

            foreach (var key in _activeDebuffs.Keys.ToList())
            {
                OnDebuffRemoved?.Invoke(key);
                _statusChanged?.Invoke(key, 0, false);
            }
            _activeDebuffs.Clear();
        }
        public bool HasDebuff(string debuffType)
        {
            return _activeDebuffs.ContainsKey(debuffType);
        }

        public IReadOnlyList<string> GetActiveDebuffTypes()
        {
            return _activeDebuffs.Keys.ToArray();
        }

        public void UpdateDebuff()
        {
            foreach(var debuff in _activeDebuffs.Values.Reverse().ToList())
            {
                debuff.Tick(Time.deltaTime);
            }
        }

        private void HandleDamageOverTime(string debuffType)
        {
            // 対応するデバフを取得
            EnemyDebuffRuntime data;
            if (!_activeDebuffs.TryGetValue(debuffType, out data))
            {
                // 存在しなかったら抜ける
                return;
            }
            // ダメージを適用
            float rand =  UnityEngine.Random.Range(0.0f, 1.0f);
            rand %= 1.0f;
            float damage = Mathf.Lerp(data.OverTimeMinDamage, data.OverTimeMaxDamage, rand);
            _damageRequest?.Invoke(damage);
        }

        private void HandleFreezeEnd(string debuffType)
        {
            // 現状行動制限系の処理があるか確認
            if (IsFreeze()) return;
            Debug.Log("行動解除リクエストを送るよ");
            // 行動制限解除処理を送る
            _freezeEndRequest?.Invoke();

        }

        private void HandleFreezeBreak(string debuffType)
        {
            // 対応するデバフを取得
            EnemyDebuffRuntime data;
            if (!_activeDebuffs.TryGetValue(debuffType, out data))
            {
                // 存在しなかったら抜ける
                return;
            }

            // ダメージ要求を送る
            float breakDamage = data.FreezeBreakDamage;
            _damageRequest?.Invoke(breakDamage);
            _freezeBroken?.Invoke(debuffType);

            // 他に行動制限系の処理があるか確認
            if (IsFreeze()) return;

            // 行動制限解除処理を送る
            _freezeEndRequest?.Invoke();

        }

        private void HandleDebuffTimeOver(string debuffType)
        {
            EnemyDebuffRuntime debuff;
            // デバフ取得
            if(!_activeDebuffs.TryGetValue(debuffType, out debuff))
            {
                return;
            }

            // 値を削除
            _activeDebuffs.Remove(debuffType);
            OnDebuffRemoved?.Invoke(debuffType);
            _statusChanged?.Invoke(debuffType, 0, false);
            // 凍結解除
            if (debuff.UseFreeze)
            {
                HandleFreezeEnd(debuffType);
            }
        }

        private bool IsFreeze()
        {
            // 現状行動制限系の処理があるか確認
            foreach (var debuff in _activeDebuffs.Values)
            {
                if (debuff.IsFreezeActive)
                {
                    return true;
                }
            }

            return false;
        }


        public class EnemyDebuffRuntime
        {
            // 共通デバフ項目
            public string DebuffType { get; private set; }
            public float Duration { get; private set; }
            public int StackCount { get; private set; }

            Action<string> OnDamageOverTime;
            Action<string> OnFreezeEnd;
            Action<string> OnFreezeBreak;
            Action<string> OnDebuffTimeOver;

            // デバフ要素ごとのステータス
            // 継続ダメージ
            public bool UseDamageOverTime { get; private set; }
            //! 継続ダメージの継続時間
            public float OverTimeDuration { get; private set; }
            //! 継続ダメージの最大値
            public float OverTimeMaxDamage { get; private set; }
            //! 継続ダメージの最小値
            public float OverTimeMinDamage { get; private set; }
            //! 継続ダメージの間隔
            public float OverTimeInterval { get; private set; }

            public float OverTimeCounter { get; private set; }

            // 行動制限
            public bool UseFreeze { get; private set; }
            //! 凍結時間
            public float FreezeDuration { get; private set; } // 凍結時間
                                                              //! 凍結解除に必要なヒット数
            public float FreezeHitDurability { get; private set; } // 凍結解除に必要なヒット数

            public int FreezeHitCount { get; private set; }// 現状のヒット数

            //! 凍結解除時のダメージ
            public float FreezeBreakDamage { get; private set; } // 凍結解除時のダメージ

            public bool IsDebuffActive => Duration > 0.0f;
            public bool IsDamageOverTimeActive => UseDamageOverTime && OverTimeDuration > 0.0f;
            public bool IsFreezeActive => UseFreeze && FreezeDuration > 0.0f;

            public EnemyDebuffRuntime(
                EnemyDebuffConfig config,
                Action<string> OnDamageOverTime,
                Action<string> OnFreezeEnd,
                Action<string> OnFreezeBreak,
                Action<string> OnDebuffTimeOver
             )
            {
                DebuffType = config.DebuffType;
                Duration = config.Duration;
                StackCount = 1;

                // 継続ダメージの初期化
                UseDamageOverTime = config.UseDamageOverTime;
                OverTimeDuration = config.OverTimeDuration;
                OverTimeMaxDamage = config.OverTimeMaxDamage;
                OverTimeMinDamage = config.OverTimeMinDamage;
                OverTimeInterval = config.OverTimeInterval;
                OverTimeCounter = OverTimeInterval;

                // 行動制限の初期化
                UseFreeze = config.UseFreeze;
                FreezeDuration = config.FreezeDuration;
                FreezeHitDurability = config.FreezeHitDurability;
                FreezeBreakDamage = config.FreezeBreakDamage;
                FreezeHitCount = 0;

                this.OnDamageOverTime = OnDamageOverTime;
                this.OnFreezeEnd = OnFreezeEnd;
                this.OnFreezeBreak = OnFreezeBreak;
                this.OnDebuffTimeOver = OnDebuffTimeOver;
            }

            public void MergeDebuffEffect(EnemyDebuffConfig debuff)
            {
                // デバフの種類が違う場合抜ける
                if (debuff == null || debuff.DebuffType != this.DebuffType)
                {
                    if (debuff != null)
                        Debug.LogWarning(debuff.DebuffType + "と" + this.DebuffType + "は異なるデバフのため、統合できません。");
                    return;
                }

                // 全体の効果時間をリセット
                Duration = debuff.Duration;
                StackCount++;

                // 継続ダメージの効果をリセットする
                if (debuff.UseDamageOverTime)
                {
                    MergeDamageOverTime(debuff.OverTimeDuration, debuff.OverTimeMaxDamage, debuff.OverTimeMinDamage, debuff.OverTimeInterval);
                }
                // 行動制限の効果を加算する
                if (debuff.UseFreeze)
                {
                    MergeFreeze(debuff.FreezeDuration, debuff.FreezeHitDurability, debuff.FreezeBreakDamage);
                }
            }

            public void Tick(float deltaTime)
            {

                if (IsDamageOverTimeActive)
                {
                    OverTimeDuration -= deltaTime;

                    OverTimeCounter -= deltaTime;
                    if(OverTimeDuration <= 0.0f)
                    {
                        UseDamageOverTime = false;

                        TryNotifyDebuffExpired();
                    }
                    else if (OverTimeCounter < 0)
                    {
                        OverTimeCounter = OverTimeInterval;
                        OnDamageOverTime(DebuffType);

                    }



                }

                if (IsFreezeActive)
                {
                    FreezeDuration -= deltaTime;
                    if (FreezeDuration <= 0)
                    {
                        UseFreeze = false;
                        FreezeDuration = 0;
                        FreezeHitCount = 0;
                        OnFreezeEnd(DebuffType);

                        TryNotifyDebuffExpired();
                    }
                }

                Duration -= deltaTime;
                if (Duration <= 0)
                {
                    if (UseFreeze)
                    {
                        UseFreeze = false;

                        OnFreezeEnd(DebuffType);
                    }
                    OnDebuffTimeOver(DebuffType);
                }
            }

            public void AddHit()
            {
                if (IsFreezeActive)
                {
                    FreezeHitCount++;

                    if (FreezeHitCount >= FreezeHitDurability)
                    {
                        // 凍結解除
                        UseFreeze = false;
                        FreezeDuration = 0.0f;
                        FreezeHitCount = 0;

                        OnFreezeBreak(DebuffType);
                        TryNotifyDebuffExpired();
                    }

                }
            }

            private void MergeFreeze(
                float newFreezeDuration,
                float newFreezeHitDurability,
                float newFreezeBreakDamage
                )
            {
                UseFreeze = true;
                FreezeDuration = newFreezeDuration; // 効果時間リセット
                FreezeHitDurability = newFreezeHitDurability;
                FreezeBreakDamage = newFreezeBreakDamage;
            }

            private void MergeDamageOverTime(
                float newOverTimeDuration,
                float newOverTimeMaxDamage,
                float newOverTimeMinDamage,
                float newOverTimeInterval
                )
            {
                UseDamageOverTime = true;
                OverTimeDuration = newOverTimeDuration; // 効果時間リセット
                OverTimeMaxDamage = newOverTimeMaxDamage;
                OverTimeMinDamage = newOverTimeMinDamage;
                OverTimeInterval = newOverTimeInterval;
            }

            private void　TryNotifyDebuffExpired()
            {
                if (!CheckDebuffEnd()) OnDebuffTimeOver(DebuffType);
            }

            private bool CheckDebuffEnd()
            {
                if (UseDamageOverTime) return true;

                if(UseFreeze) return true;

                return false;
            }
        }
    }

    public class EnemyDebuffConfig
    {
        Action<float> OnDamageRequest;
        Action OnStopRequest;
        Action OnColorRequest;

        public EnemyDebuffConfig(string debuffType, float debuffDuration)
        {
            DebuffType = debuffType;
            Duration = debuffDuration;

            // 継続ダメージの初期化
            UseDamageOverTime = false;
            // 行動制限の初期化
            UseFreeze = false;
        }

        // 共通デバフ項目
        public string DebuffType { get; private set; }
        public float Duration { get; set; }

        // デバフ要素ごとのステータス
        // 継続ダメージ
        public bool UseDamageOverTime { get; private set; }
        //! 継続ダメージの継続時間
        public float OverTimeDuration { get; private set; }
        //! 継続ダメージの最大値
        public float OverTimeMaxDamage { get; private set; }
        //! 継続ダメージの最小値
        public float OverTimeMinDamage { get; private set; }
        //! 継続ダメージの間隔
        public float OverTimeInterval { get; private set; }

        // 行動制限
        public bool UseFreeze { get; private set; }
        //! 凍結時間
        public float FreezeDuration { get; private set; }
        //! 凍結解除に必要なヒット数
        public float FreezeHitDurability { get; private set; }
        //! 凍結解除時のダメージ
        public float FreezeBreakDamage { get; private set; }

        public void InitDamageOverTime(
            float overTimeDuration,
            float overTimeMaxDamage,
            float overTimeMinDamage,
            float overTimeInterval
        )
        {
            UseDamageOverTime = true;
            OverTimeDuration = overTimeDuration;
            OverTimeMaxDamage = overTimeMaxDamage;
            OverTimeMinDamage = overTimeMinDamage;
            OverTimeInterval = overTimeInterval;
        }

        public void InitFreeze(
            float freezeDuration,
            float freezeHitDurability,
            float freezeBreakDamage
        )
        {
            UseFreeze = true;
            FreezeDuration = freezeDuration;
            FreezeHitDurability = freezeHitDurability;
            FreezeBreakDamage = freezeBreakDamage;
        }

    }

}
