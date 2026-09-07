using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay.Enemy.Boss
{

    [CreateAssetMenu(fileName = "BossFlowBattleData", menuName = "Scriptable Objects/BossFlowBattleData")]
    public class BossFlowBattleData : ScriptableObject
    {
        [Tooltip("フェーズデータ")]
        [SerializeField] private List<BossFlowPhaseData> _phaseData = new List<BossFlowPhaseData>();

        public bool TryGetPhaseData(int index, out BossFlowPhaseData phaseData)
        {
            if (index >= 0 && index < _phaseData.Count)
            {
                phaseData = _phaseData[index];
                return true;
            }
            phaseData = null;
            return false;
        }

    }

}
