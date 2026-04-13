// ================================================================================
// File         : EnemyData.cs
// Author       : Iwai Shogo
//
// Description  : 敵の基礎能力とAI行動パターンのリストを定義するScriptableObject。
// Created      : 2026-04-13
//
// Note         : プランナーはこのデータをインスペクターから作成・調整します。
// ================================================================================

using System.Collections.Generic;
using UnityEngine;

namespace CreatorKousien.Data
{
    /// <summary>
    /// 敵個体の基礎能力と行動アルゴリズムを定義するデータ
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "CreatorKousien/Data/EnemyData", order = 1)]
    public class EnemyData : ScriptableObject
    {
        [Header("基本情報")]

        [Tooltip("敵を一位に特定するためのマスタID")]
        public int EnemyId;

        [Tooltip("ゲーム内に表示される敵の名前")]
        public string EnemyName;

        [Header("戦闘パラメータ")]

        [Tooltip("敵の最大体力")]
        [Min(1)]
        public int MaxHp = 100;

        [Tooltip("敵の基礎となる攻撃力")]
        [Min(0)]
        public int Attack = 10;

        [Header("表示アセット")]

        [Tooltip("シーン上に生成される敵キャラクターのプレハブ")]
        public GameObject EnemyPrefab;

        [Header("AI 行動パターン")]

        [Tooltip("リストの上にある行動程優先して評価されます。条件を満たした最初の行動が実行されます。")]
        public List<EnemyActionPattern> ActionPatterns = new List<EnemyActionPattern>();
    }
}
