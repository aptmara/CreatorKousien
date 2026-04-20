// ================================================================================
// File         : EffectData.cs
// Author       : Iwai Shogo
//
// Description  : これが発動したら何が起きるかという純粋なパラメータ。
// Created      : 2026-04-20
// ================================================================================

using UnityEngine;
using CreatorKousien.Battle;
using UnityEngine.InputSystem;

namespace CreatorKousien.Data
{
    [CreateAssetMenu(fileName = "NewEffectData", menuName = "CreatorKousien/Data/EffectData")]
    public class EffectData : ScriptableObject
    {
        [Header("基本情報")]
        public int EffectId;
        public string EffectName;
        [TextArea(2, 4)]
        public string Description;
        public Sprite EffectIcon;

        [Header("アクション設定")]
        [Tooltip("この効果の種類")]
        public ActionType Type;

        [Header("移動パラメータ (Category = Move の場合)")]
        [Tooltip("移動するマス数")]
        public int MoveDistance = 1;

        [Header("アクションパラメータ (Category = Attack/Guard 等の場合)")]
        [Tooltip("威力やヒット数")]
        public ActionProperty Property;

        [Tooltip("効果が及ぶ範囲の形状")]
        public TargetAreaType AreaType = TargetAreaType.Front1;
    }
}
