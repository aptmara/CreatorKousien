// ------------------------------------------------------------
// File		: UpgradeData.cs
// Summary	: プレイヤーのアップグレードデータを管理するSO
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - アップグレードデータの作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Data.Player
{
    /// <summary>
    /// 1つの強化を表すマスターデータ。
    /// ローグライク担当が候補として提示し、選ばれたものをPlayerStatsServiceへ渡す。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Upgrade_New", menuName = "Game/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("アップグレードデータ")]
        [Tooltip("アップグレードの一意なID")]
        public string Id;

        [Tooltip("表示名（UI用）")]
        public string DisplayName;

        [Tooltip("説明文（UI用）")]
        [TextArea]
        public string Description;

        [Header("Effects")]
        [Tooltip("この強化で適用するステータス変化（複数可）")]
        public StatModifier[] Modifiers;
    }
}
