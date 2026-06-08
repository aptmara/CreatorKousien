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
        [SerializeField] private TMPro.TMP_Text _comboText;

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
                    feedback.Initialize(_comboTextRect, _comboText);
                    _activeFeedbacks.Add(feedback);
                }
            }

            // 3. ロジックのイベントコールバックを演出へ接続
            _comboManager.OnComboUpdated += HandleComboUpdated;
            _comboManager.OnComboReset += HandleComboReset;

            // 初期状態は非表示
            if (_comboText != null) _comboText.gameObject.SetActive(false);
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

        private void OnEnemyHit(EnemyHitBatchEvent ev)
        {
            // 敵へのヒットをコンボ数として加算
            _comboManager.AddCombo(ev.HitCount);
        }

        private void OnBarrierHit(BarrierHitBatchEvent ev)
        {
            // バリアへのヒットも同様に加算
            _comboManager.AddCombo(ev.HitCount);
        }

        private void HandleComboUpdated(int currentCombo, float durationRatio)
        {
            if (_comboText == null) return;

            // テキストの有効化と表示更新
            if (!_comboText.gameObject.activeSelf)
            {
                _comboText.gameObject.SetActive(true);
            }

            _comboText.text = $"{currentCombo} Combo!";

            // 登録されているすべての演出パターンを一斉更新
            foreach (var feedback in _activeFeedbacks)
            {
                feedback.OnUpdate(currentCombo, durationRatio);
            }
        }

        private void HandleComboReset()
        {
            if (_comboText != null)
            {
                _comboText.gameObject.SetActive(false);
            }

            // すべての演出パターンをリセット
            foreach (var feedback in _activeFeedbacks)
            {
                feedback.OnReset();
            }
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
