// 制作者: 山内陽
using System.Collections;
using Game.Core.Events;
using UnityEngine;

namespace Game.DebugTools
{
    /// <summary>
    /// 敵システムのイベント連携をカメラキャプチャで確認するためのデモ表示。
    /// 既存のEnemyControllerには介入せず、EventBusの結果をワールド上の模型へ反映する。
    /// </summary>
    public sealed class EnemySystemCaptureDemo : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private string _enemyId = "Enemy_01";

        [Header("Auto Demo")]
        [SerializeField] private bool _playAutomatically = true;
        [SerializeField] private float _initialDelay = 1.0f;
        [SerializeField] private float _downDelay = 2.0f;
        [SerializeField] private float _defeatDelay = 3.5f;
        [SerializeField] private int _hitCount = 24;
        [SerializeField] private float _forceDownGaugeDamage = 999f;
        [SerializeField] private float _forceDefeatBodyDamage = 999f;

        [Header("Visuals")]
        [SerializeField] private Renderer _enemyBodyRenderer;
        [SerializeField] private Renderer _barrierRenderer;
        [SerializeField] private Transform _gaugeFill;
        [SerializeField] private Transform _hpFill;
        [SerializeField] private TextMesh _stateText;
        [SerializeField] private TextMesh _gaugeText;
        [SerializeField] private TextMesh _hpText;
        [SerializeField] private TextMesh _eventText;

        private const float MinFillScale = 0.001f;
        private Coroutine _autoDemoCoroutine;
        private float _lastGaugeRatio;
        private float _lastHpRatio = 1f;

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyGaugeChangedEvent>(OnGaugeChanged);
            EventBus.Subscribe<EnemyHealthChangedEvent>(OnHealthChanged);
            EventBus.Subscribe<EnemyGaugeBrokenEvent>(OnGaugeBroken);
            EventBus.Subscribe<EnemyDownStartedEvent>(OnDownStarted);
            EventBus.Subscribe<EnemyDefeatedEvent>(OnDefeated);
            EventBus.Subscribe<EnemyAttackFiredEvent>(OnAttackFired);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyGaugeChangedEvent>(OnGaugeChanged);
            EventBus.Unsubscribe<EnemyHealthChangedEvent>(OnHealthChanged);
            EventBus.Unsubscribe<EnemyGaugeBrokenEvent>(OnGaugeBroken);
            EventBus.Unsubscribe<EnemyDownStartedEvent>(OnDownStarted);
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnDefeated);
            EventBus.Unsubscribe<EnemyAttackFiredEvent>(OnAttackFired);

            if (_autoDemoCoroutine != null)
            {
                StopCoroutine(_autoDemoCoroutine);
                _autoDemoCoroutine = null;
            }
        }

        private void Start()
        {
            SetState("NORMAL", new Color(0.3f, 0.8f, 1.0f), true);
            SetGaugeRatio(_lastGaugeRatio);
            SetHpRatio(_lastHpRatio);
            SetEventText("Auto demo: wait -> force down -> defeat");

            if (_playAutomatically)
            {
                _autoDemoCoroutine = StartCoroutine(AutoDemoRoutine());
            }
        }

        /// <summary>
        /// ゲージ破壊と撃破を順番に発火し、キャプチャ上で状態遷移を確認可能にする。
        /// </summary>
        private IEnumerator AutoDemoRoutine()
        {
            yield return new WaitForSecondsRealtime(_initialDelay);
            SetEventText("Hit batch: gauge damage");
            EventBus.Publish(new BarrierHitBatchEvent(_enemyId, _hitCount, _forceDownGaugeDamage, transform.position, transform));

            yield return new WaitForSecondsRealtime(_downDelay);
            SetEventText("Down: body damage enabled");
            EventBus.Publish(new EnemyHitBatchEvent(_enemyId, _hitCount, _forceDefeatBodyDamage, transform.position, transform));

            yield return new WaitForSecondsRealtime(_defeatDelay);
            SetEventText("Demo complete: enemy defeated");
            _autoDemoCoroutine = null;
        }

        private void OnGaugeChanged(EnemyGaugeChangedEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetGaugeRatio(ev.Ratio);
            SetEventText($"Gauge {ev.CurrentGauge:0}/{ev.MaxGauge:0}");
        }

        private void OnHealthChanged(EnemyHealthChangedEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetHpRatio(ev.Ratio);
            SetEventText($"HP {ev.CurrentHp:0}/{ev.MaxHp:0}");
        }

        private void OnGaugeBroken(EnemyGaugeBrokenEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetGaugeRatio(0f);
            SetEventText("Gauge broken");
        }

        private void OnDownStarted(EnemyDownStartedEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetState($"DOWN {ev.Duration:0.0}s", new Color(1.0f, 0.55f, 0.15f), false);
            SetEventText("Down started: attack gauge stopped");
        }

        private void OnDefeated(EnemyDefeatedEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetHpRatio(0f);
            SetState("DEFEATED", new Color(0.6f, 0.6f, 0.6f), false);
            SetEventText("Defeated: HP reached zero");
        }

        private void OnAttackFired(EnemyAttackFiredEvent ev)
        {
            if (!IsTarget(ev.EnemyId)) return;

            SetState("ATTACK", new Color(1.0f, 0.2f, 0.2f), true);
            SetEventText("Attack fired: gauge reached max");
        }

        private bool IsTarget(string enemyId)
        {
            return string.Equals(enemyId, _enemyId, System.StringComparison.Ordinal);
        }

        private void SetState(string label, Color bodyColor, bool barrierVisible)
        {
            if (_stateText != null)
            {
                _stateText.text = $"STATE: {label}";
            }

            if (_enemyBodyRenderer != null)
            {
                _enemyBodyRenderer.material.color = bodyColor;
            }

            if (_barrierRenderer != null)
            {
                _barrierRenderer.enabled = barrierVisible;
            }
        }

        private void SetGaugeRatio(float ratio)
        {
            _lastGaugeRatio = Mathf.Clamp01(ratio);
            SetFill(_gaugeFill, _lastGaugeRatio);

            if (_gaugeText != null)
            {
                _gaugeText.text = $"ATTACK GAUGE: {_lastGaugeRatio:P0}";
            }
        }

        private void SetHpRatio(float ratio)
        {
            _lastHpRatio = Mathf.Clamp01(ratio);
            SetFill(_hpFill, _lastHpRatio);

            if (_hpText != null)
            {
                _hpText.text = $"HP: {_lastHpRatio:P0}";
            }
        }

        private void SetFill(Transform fill, float ratio)
        {
            if (fill == null) return;

            Vector3 scale = fill.localScale;
            scale.x = Mathf.Max(MinFillScale, ratio);
            fill.localScale = scale;
        }

        private void SetEventText(string message)
        {
            if (_eventText != null)
            {
                _eventText.text = message;
            }
        }
    }
}
