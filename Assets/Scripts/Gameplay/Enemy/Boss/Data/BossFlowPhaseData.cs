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

        [Header("===== このフェーズで使用するギミック =====")]
        [Tooltip("ギミックのデータ")]
        [SerializeField] private List<GimmickSlot> _gimmickSlots = new List<GimmickSlot>();

        public string PhaseName => _phaseName;
        public float HpThresholdToEnter => _hpThresholdToEnter;
        public List<GimmickSlot> GimmickSlots => _gimmickSlots;
    }

}
