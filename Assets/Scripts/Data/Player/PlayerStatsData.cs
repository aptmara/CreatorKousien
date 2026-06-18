// ------------------------------------------------------------
// File		: PlayerStatsData.cs
// Summary	: プレイヤーのステータスデータを保持するScriptableObject
//
// Author	: [浅野 勇生]
// Created	: 2026-05-
//
// Notes	:
// - 基盤のSO作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Data.Player
{
    /// <summary>
    /// プレイヤーの初期ステータスのデータを保持するScriptableObject
    /// PlayerStatsDataは、プレイヤーの初期ステータスを定義するためのScriptableObjectです。
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Game/PlayerStatsData")]
    public class PlayerStatsData : ScriptableObject
    {
        [Header("基本ステータス")]
        [Tooltip("初期最大HP")]
        public float MaxHp = 100f;                      // 使うかわかんないけど一応持っておきます！

        [Tooltip("移動速度の倍率初期値（1.0で等倍。PlayerMotorの基準速度に掛かる）")]
        public float MoveSpeedMultiplier = 1.0f;

        [Header("アタッチメント")]
        [Tooltip("アタッチメントのサイズ倍率の初期値（1.0で等倍）")]
        public float AttachmentScaleMultiplier = 1.0f;

        [Header("経験値")]
        [Tooltip("敵から受け取った経験値量にかける換算係数（1.0で等倍）")]
        public float ExpConversionRate = 1.0f;
    }
}

