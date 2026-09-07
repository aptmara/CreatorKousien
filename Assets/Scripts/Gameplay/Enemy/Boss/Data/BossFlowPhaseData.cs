using UnityEngine;
using System;
using System.Collections.Generic;


namespace Game.Gameplay.Enemy.Boss
{
    [Serializable]
    public class BossFlowPhaseData
    {
        [Header("===== フェーズ基本情報 =====")]
        [SerializeField] private string _phaseName = "Phase";

        [Header("===== 移行条件 =====")]
        [Tooltip("次のフェーズへ移行するHPの割合")]
        [SerializeField,Range(0.0f,1.0f)] private float _hpThresholdToEnter = 1.0f;

        [Header("===== パラメータ倍率 =====")]
        [SerializeField] private BossPhaseMultipliers _multipliers = BossPhaseMultipliers.Default;

        [Header("===== このフェーズで使用するギミック =====")]
        [Tooltip("ギミックのデータ")]
        [SerializeField] private List<GimmickSlot> _gimmickSlots = new List<GimmickSlot>();

        public string PhaseName => _phaseName;
        public float HpThresholdToEnter => _hpThresholdToEnter;
        public BossPhaseMultipliers Multipliers => _multipliers;
        public List<GimmickSlot> GimmickSlots => _gimmickSlots;

#if UNITY_EDITOR
        /// <summary>
        /// 倍率が未設定（3つとも0）かどうか。
        /// Inspectorのリストへ要素を追加した直後はフィールド初期化子が走らずゼロ埋めされるため、その検出に使う。
        /// </summary>
        internal bool HasUnsetMultipliers =>
            Mathf.Approximately(_multipliers.DamageMultiplier, 0.0f) &&
            Mathf.Approximately(_multipliers.SpeedMultiplier, 0.0f) &&
            Mathf.Approximately(_multipliers.SpawnIntervalMultiplier, 0.0f);

        /// <summary>
        /// 倍率を既定値へ戻す。
        /// </summary>
        internal void ResetMultipliersToDefault() => _multipliers = BossPhaseMultipliers.Default;
#endif
    }

}
