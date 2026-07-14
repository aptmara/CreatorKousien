// ------------------------------------------------------------
// File		: WaveGroupData.cs
// Summary	: Wave内の敵グループと次のグループへ進む条件を管理
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 1つのWaveは、複数のWaveGroupDataによって構成される
// - 各EnemySpawnEntryはGroup開始後、個別にスポーン処理を開始
// - 実行中の生存敵数など、変化する情報は保持しない
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// Wave内における、敵のひとまとまりを定義します。
    ///
    /// 次のGroupへ進む条件として、
    /// ・このGroupの敵をすべて倒す
    /// ・このGroupの開始から一定時間待つ
    /// のどちらかを設定できます。
    /// </summary>
    [Serializable]
    public class WaveGroupData
    {
        [Header("--- 基本情報 ---")]

        [SerializeField]
        [Tooltip("Inspector上での表示名")]
        private string groupName = "New Group";

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Inspector上での説明文。このGroupでの狙いや調整をメモできます。")]
        private string plannerMemo;


        [Header("--- 開始設定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("Groupの開始条件を満たしてから、実際に敵のスポーンを開始するまでの待機時間")]
        private float delayBeforeStart;


        [Header("--- 次Groupへの進行条件 ---")]

        [SerializeField]
        [Tooltip("次のGroupに進む条件。敵の全滅 or 時間経過")]
        private WaveGroupAdvanceType advanceType = WaveGroupAdvanceType.AllEnemiesDefeated;

        [SerializeField]
        [Min(0f)]
        [Tooltip("次のGroupに進む条件が「時間経過」の場合の待機時間")]
        private float timeUntilNextGroup = 10f;


        [Header("--- 出現する敵 ---")]

        [SerializeField]
        [Tooltip("このGroupで出現する敵の一覧")]
        private List<EnemySpawnEntry> spawnEntries = new();


        // --- 公開プロパティ ---
        public string GroupName => groupName;
        public string PlannerMemo => plannerMemo;
        public float DelayBeforeStart => delayBeforeStart;
        public WaveGroupAdvanceType AdvanceType => advanceType;
        public float TimeUntilNextGroup => timeUntilNextGroup;
        public IReadOnlyList<EnemySpawnEntry> SpawnEntries => spawnEntries;
    }
}

