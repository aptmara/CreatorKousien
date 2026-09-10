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

#if UNITY_EDITOR
        /// <summary>
        /// Inspectorでフェーズを追加した直後の倍率ゼロ埋めを検出し、既定値へ補正する。
        /// 3つとも0のときだけ「未設定」とみなすため、意図して設定した調整値は上書きしない。
        /// </summary>
        private void OnValidate()
        {
            for (int i = 0; i < _phaseData.Count; ++i)
            {
                BossFlowPhaseData phase = _phaseData[i];

                if (phase == null || !phase.HasUnsetMultipliers) continue;

                phase.ResetMultipliersToDefault();

                UnityEditor.EditorUtility.SetDirty(this);

                Debug.LogWarning(
                    $"[BossFlowBattleData] {name} の Phase{i} の倍率が未設定だったため、既定値(1.0)へ補正しました。", this);
            }
        }
#endif

    }

}
