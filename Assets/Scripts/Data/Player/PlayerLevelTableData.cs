// ------------------------------------------------------------
// File		: PlayerLevelTableData.cs
// Summary	: プレイヤーのレベルテーブルデータを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - 6/19: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Data.Player
{
    /// <summary>
    /// レベルごとの必要経験値テーブルのマスターデータ
    /// PlayerLevelCalculatorがこの値を参照して必要経験値を返す
    /// </summary>
    [CreateAssetMenu(fileName = "SO_PlayerLevelTable_New", menuName = "Game/Player Level Table")]
    public class PlayerLevelTableData : ScriptableObject
    {
        [Header("必要経験値テーブル")]
        [Tooltip("各レベルから次レベルへ上がるのに必要な経験値。[0]=Lv1→Lv2, [1]=Lv2→Lv3 ...")]
        public int[] RequiredExpPerLevel =
        {
            10, 20, 35, 55, 80, 110, 150, 200, 260, 330,
        };

        /// <summary>テーブルが定義する最大レベル（これ以上は上がらない）</summary>
        public int MaxLevel => RequiredExpPerLevel != null ? RequiredExpPerLevel.Length + 1 : 1;        // 経験値テーブルに応じて最大レベルが変わるようになってます！
    }
}

