// ================================================================================
// File         : ComboViewControl.cs
// Author       : Iwai Shogo
//
// Description  : EventBusからヒットイベントを受け取り、コンボロジックの更新と演出一斉適用を統括するUIコンポーネント。
// Created      : 2026-06-08
// ================================================================================

using Game.Core.Events;
using Game.Gameplay.Combo;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Presentation.UI.Combo
{
    public sealed class ComboViewControl : MonoBehaviour
    {
        [Header("--- Combo Logic Settings ---")]
        [Tooltip("1ヒットあたりに増える猶予時間（秒）")]
        [SerializeField] private float _addDurationPerHit = 0.5f;

        [Tooltip("基準となる最大猶予時間（秒）")]
        [SerializeField] private float _maxDuration = 4.0f;

        [Tooltip("猶予時間の上限ルール設定")]
        [SerializeField] private ComboManager.DurationLimitMode _limitMode = ComboManager.DurationLimitMode.ClampToMax;

        [Header("--- UI References ---")]
        [Tooltip("コンボテキストを表示するオブジェクトのRectTransform")]
        [SerializeField] private RectTransform _comboTextRect;

        [Tooltip("コンボ数テキスト")]
        [SerializeField] private TMPro.TMP_Text _comboTextBase;

        [Tooltip("前面にあるエネルギー充填テキスト")]
        [SerializeField] private TMPro.TMP_Text _comboTextFill;

        [SerializeField,Tooltip("コンボゲージのUI")]
        private ComboGaugeUI _comboGaugeUI;

        [Header("Feedback Patterns (検証用演出リスト)")]
        [RequireInterface(typeof(IComboFeedback))]
        [SerializeField] private List<MonoBehaviour> _feedbacks = new List<MonoBehaviour>();

        private ComboManager _comboManager;
        private readonly List<IComboFeedback> _activeFeedbacks = new List<IComboFeedback>();

        private void Awake()
        {
            // 1. コアロジックのインスタンス化
            _comboManager = new ComboManager(_addDurationPerHit, _maxDuration, _limitMode);

            // 2. インスペクターで設定された演出コンポーネントをインターフェース経由で集約・初期化
            foreach (var fb in _feedbacks)
            {
                if (fb is IComboFeedback feedback)
                {
                    feedback.Initialize(_comboTextRect, _comboTextBase);
                    _activeFeedbacks.Add(feedback);
                }
            }

            // 3. ロジックのイベントコールバックを演出へ接続
            _comboManager.OnComboUpdated += HandleComboUpdated;
            _comboManager.OnComboReset += HandleComboReset;

            // 初期状態は非表示
            SetTextActive(false);
        }

        private void OnEnable()
        {
            // 既存の敵ヒットイベントを購読
            EventBus.Subscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Subscribe<BarrierHitBatchEvent>(OnBarrierHit);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyHitBatchEvent>(OnEnemyHit);
            EventBus.Unsubscribe<BarrierHitBatchEvent>(OnBarrierHit);
        }

        private void Update()
        {
            // 毎フレームコンボの猶予時間を減少させる
            _comboManager.Tick(Time.deltaTime);
        }

        /// <summary>
        /// 本体ヒットイベント受信
        /// </summary>
        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            int hits = ev.HitCount;
            _comboManager.AddCombo(hits);
            _comboGaugeUI?.GaugeUpdate(hits);
        }

        /// <summary>
        /// バリアヒットイベント受信
        /// </summary>
        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            int hits = ev.HitCount;
            _comboManager.AddCombo(hits);
            _comboGaugeUI?.GaugeUpdate(hits);
        }

        private void HandleComboUpdated(int currentCombo, float durationRatio)
        {
            SetTextActive(true);

            string textString = $"{currentCombo}";// Combo!";
            if (_comboTextBase != null) _comboTextBase.text = textString;
            if (_comboTextFill != null) _comboTextFill.text = textString;
            

            // 登録されているすべての演出パターンを一斉更新
            foreach (var feedback in _activeFeedbacks)
            {
                if (feedback is MonoBehaviour mono && !mono.enabled)
                {
                    continue;
                }

                feedback.OnUpdate(currentCombo, durationRatio);
            }
        }

        private void HandleComboReset()
        {
            SetTextActive(false);
            _comboGaugeUI?.resetGauge();

            // すべての演出パターンをリセット
            foreach (var feedback in _activeFeedbacks)
            {
                if (feedback is MonoBehaviour mono && !mono.enabled)
                {
                    continue;
                }

                feedback.OnReset();
            }
        }

        /// <summary>
        /// テキストオブジェクトのアクティブ状態を一括管理する
        /// </summary>
        private void SetTextActive(bool active)
        {
            if (_comboTextBase != null && _comboTextBase.gameObject.activeSelf != active)
                _comboTextBase.gameObject.SetActive(active);

            // Fill文字側は、親のBaseが消えるなら一緒に消す
            if (_comboTextFill != null && _comboTextFill.gameObject.activeSelf != active)
                _comboTextFill.gameObject.SetActive(active);
        }
    }

    /// <summary>
    /// インスペクター上で特定のインターフェースを持ったMonoBehaviourのみを選択可能にするための属性
    /// </summary>
    public class RequireInterfaceAttribute : PropertyAttribute
    {
        public System.Type InterfaceType { get; }
        public RequireInterfaceAttribute(System.Type interfaceType) { InterfaceType = interfaceType; }
    }
}
