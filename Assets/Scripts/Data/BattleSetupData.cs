// ------------------------------------------------------------
// File		: BattleSetupData.cs
// Summary	: バトルのセットアップに必要なデータをまとめるクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-18
//
// Notes	:
// - 随時更新される予定
// ------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;
using CreatorKousien.Player;
using CreatorKousien.Enemy;
using CreatorKousien.Field;

namespace CreatorKousien.Data
{
    /// <summary>
    /// 敵の初期配置情報をInspectorで設定するための構造体
    /// </summary>
    [System.Serializable]
    public struct EnemySpawnInfo
    {
        [Tooltip("このバトル中での固有ActorID")]
        public int ActorId;                 // プレイヤーが1想定なので、敵のActorIDは2から始まると仮定

        [Tooltip("スポーンさせる敵のデータ(SO)")]
        public EnemyData EnemyData;         // 敵のデータ

        [Tooltip("初期配置の座標(X, Y)")]
        public Vector2Int SpawnPosition;    // 敵のスポーン位置
    }


    /// <summary>
    /// バトルのセットアップに必要なデータをまとめる
    /// </summary>
    [CreateAssetMenu(fileName = "NewBattleSetup", menuName = "CreatorKousien/Data/BattleSetupData", order = 0)]
    public class BattleSetupData : ScriptableObject
    {
        [Header("ステージ設定")]
        [Tooltip("このバトルで使用する盤面のデータ(SO)")]
        public StageData StageData;

        [Header("プレイヤー設定")]
        [Tooltip("このバトルで使用するプレイヤーのデータ(SO)")]
        public PlayerData PlayerData;

        [Header("敵のスポーン設定")]
        [Tooltip("このバトルでスポーンさせる敵のリスト")]
        public List<EnemySpawnInfo> Enemies = new List<EnemySpawnInfo>();
    }
}
