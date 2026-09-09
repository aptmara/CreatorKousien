// ------------------------------------------------------------
// File		: BossDownCountJudge.cs
// Summary	: ボスのダウン回数を判定するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - BossBattleFlowControllerの公開APIのみを使用するため、既存ボスへの影響はナッシング！！
// - 使用時はBossBattleFlowController側で以下を設定するよーに！
//     _triggerVictoryOnZeroHp = false  (HP0では勝たない)
//     _useDownSystem          = true
// ------------------------------------------------------------
using System;
using System.Collections;
using Game.Core.Events;
using UnityEngine;


namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボスのダウン回数をカウントし、規定回数に達したら勝利を確定させるコンポーネント。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BossDownCountJudge : MonoBehaviour
    {
        [Header("==== 参照 ====")]

        [SerializeField]
        [Tooltip("対象のボス戦フロー制御。未設定なら同じGameObjectから取得する")]
        private BossBattleFlowController _flowController;


        [Header("==== 勝利条件 ====")]

        [SerializeField]
        [Min(1)]
        [Tooltip("撃退に必要なダウン回数")]
        private int _requiredDownCount = 5;

        [SerializeField]
        [Tooltip("ダウン開始でカウントする。falseならダウン復帰時にカウントする")]
        private bool _countOnDownStart = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("規定回数に達してから勝利を確定させるまでの待ち時間。ダウン演出を見せる用")]
        private float _victoryDelay = 0.0f;


        [Header("==== デバッグ ====")]

        [SerializeField]
        [Tooltip("カウント時にログを出すかどうか")]
        private bool _logCount = true;


        // ランタイム状態
        // ------------------------------------------------------------

        private int _currentDownCount;
        private bool _hasTriggeredVictory;
        private Coroutine _victoryRoutine;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 現在のダウン回数。
        /// </summary>
        public int CurrentDownCount => _currentDownCount;

        /// <summary>
        /// 撃退に必要なダウン回数。
        /// </summary>
        public int RequiredDownCount => _requiredDownCount;

        /// <summary>
        /// ダウン回数が変化した際の通知。&lt;現在回数, 必要回数&gt;
        /// </summary>
        public event Action<int, int> OnDownCountChanged;


        /// <summary>
        /// 撃退に必要なダウン回数を差し替える
        /// </summary>
        public void SetRequiredDownCount(int requiredDownCount) => _requiredDownCount = Mathf.Max(1, requiredDownCount);

        private void Awake()
        {
            if (_flowController == null)
            {
                _flowController = GetComponent<BossBattleFlowController>();
            }

            if (_flowController == null)
            {
                Debug.LogError($"[{nameof(BossDownCountJudge)}] BossBattleFlowControllerが見つかりません。", this);
            }
        }


        private void OnEnable()
        {
            if (_flowController == null)
                return;

            _flowController.OnDownStart += HandleDownStart;
            _flowController.OnDownEnd += HandleDownEnd;
        }


        private void OnDisable()
        {
            if (_flowController == null)
                return;

            _flowController.OnDownStart -= HandleDownStart;
            _flowController.OnDownEnd -= HandleDownEnd;
        }


        // 内部処理
        // ------------------------------------------------------------

        private void HandleDownStart()
        {
            if (!_countOnDownStart)
                return;

            CountUp();
        }


        private void HandleDownEnd()
        {
            if (_countOnDownStart)
                return;
            CountUp();
        }


        private void CountUp()
        {
            if (_hasTriggeredVictory)
                return;

            _currentDownCount++;

            // ダウン回数の変化を通知
            OnDownCountChanged?.Invoke(_currentDownCount, _requiredDownCount);

            EventBus.Publish(new BossDownCountChangedEvent(_flowController != null ? _flowController.BossInstanceId : string.Empty, _currentDownCount, _requiredDownCount));

            // デバッグログでわかるようにしますお
            if (_logCount)
            {
                Debug.Log($"[{nameof(BossDownCountJudge)}] ダウン回数: {_currentDownCount}/{_requiredDownCount}", this);
            }

            if (_currentDownCount < _requiredDownCount)
                return;

            _hasTriggeredVictory = true;

            // 規定回数に達したので勝利を確定させる
            if (_victoryDelay <= 0.0f)
            {
                TriggerVictory();

                return;
            }

            _victoryRoutine = StartCoroutine(TriggerVictoryDelayed());
        }


        private IEnumerator TriggerVictoryDelayed()
        {
            yield return new WaitForSeconds(_victoryDelay);

            _victoryRoutine = null;

            TriggerVictory();
        }


        private void TriggerVictory()
        {
            if (_flowController == null)
                return;

            Debug.Log($"[DownCountJudge] <color=green>ダウン{_requiredDownCount}回達成！撃退成功</color>");

            _flowController.TriggerVictory();
        }


        // デバッグ用
        // ------------------------------------------------------------

        /// <summary>
        /// デバッグ用: 強制的にダウンさせる
        /// </summary>
        [ContextMenu("ぱちチート/強制ダウンさせる！")]
        private void DebugForceDown()
        {
            if (_flowController == null)
                return;

            _flowController.TriggerDown();
        }
    }
}
