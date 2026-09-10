// ------------------------------------------------------------
// File		: BocchaTuningApplier.cs
// Summary	: ラウンドごとの調整値を適用するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - Waveごとの差しかえはSetTuningで行う！
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ラウンド進行と調整値の適用を担当する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BocchaTuningApplier : MonoBehaviour
    {
        [Header("==== 参照 ====")]

        [SerializeField] private BossBattleFlowController _flowController;
        [SerializeField] private BossDownCountJudge _downCountJudge;
        [SerializeField] private BocchaCapacityGauge _capacityGauge;


        [Header("==== 調整値 ====")]

        [SerializeField]
        [Tooltip("既定の調整値。WaveDataSOから注入された場合はそちらが優先される")]
        private BocchaBossTuning _defaultTuning;

        [SerializeField]
        [Tooltip("ラウンド切り替え時にログを出すかどうか")]
        private bool _logRound = true;


        private BocchaBossTuning _tuning;
        private int _roundIndex;


        /// <summary>現在のラウンド番号(0始まり)。</summary>
        public int RoundIndex => _roundIndex;


        private void Awake()
        {
            if (_flowController == null) _flowController = GetComponent<BossBattleFlowController>();
            if (_downCountJudge == null) _downCountJudge = GetComponent<BossDownCountJudge>();
            if (_capacityGauge == null) _capacityGauge = GetComponent<BocchaCapacityGauge>();

            if (_tuning == null) _tuning = _defaultTuning;
        }


        private void OnEnable()
        {
            if (_flowController == null)
                return;

            _flowController.OnPhaseStarted += HandlePhaseStarted;
            _flowController.OnDownEnd += HandleDownEnd;
        }
        private void OnDisable()
        {
            if (_flowController == null) return;

            _flowController.OnPhaseStarted -= HandlePhaseStarted;
            _flowController.OnDownEnd -= HandleDownEnd;
        }


        /// <summary>
        /// Waveから調整値を注入する。StartBattleより前に呼ぶこと!!!
        /// </summary>
        /// <param name="tuning">この戦闘で使う調整値</param>
        public void SetTuning(BocchaBossTuning tuning)
        {
            if (tuning == null) return;

            _tuning = tuning;
        }


        // 内部処理
        // ------------------------------------------------------------

        // フェーズ開始時はruntimeGimmickが作り直されるので、そこで必ず適用し直す
        private void HandlePhaseStarted(int phaseIndex, BossFlowPhaseData phaseData) => ApplyCurrentRound();

        // ダウンから復帰したタイミングで次のラウンドへ進む
        private void HandleDownEnd()
        {
            _roundIndex++;

            ApplyCurrentRound();
        }


        /// <summary>
        /// ラウンド切り替え時の処理
        /// </summary>
        private void ApplyCurrentRound()
        {
            if (_tuning == null)
            {
                return;
            }

            BocchaRoundTuning round = _tuning.GetRound(_roundIndex);

            if (round == null)
            {
                return;
            }

            if (_logRound)
            {
                Debug.Log($"[Tuning] ラウンド {_roundIndex + 1} 「{round.RoundName}」を適用", this);
            }

            _capacityGauge?.SetCapacityMax(round.CapacityMax);
            _downCountJudge?.SetRequiredDownCount(_tuning.RequiredDownCount);

            if (_flowController?.GimmickSlots == null)
            {
                return;
            }

            foreach (GimmickSlot slot in _flowController.GimmickSlots)
            {
                if (slot?.runtimeGimmick is IBocchaTunable tunable)
                {
                    tunable.ApplyTuning(round);
                }
            }
        }

    }
}
