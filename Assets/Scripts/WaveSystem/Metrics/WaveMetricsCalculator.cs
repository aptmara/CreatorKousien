// ------------------------------------------------------------
// File		: WaveMetricsCalculator.cs
// Summary	: WaveDataSOから見積り値とスポーン時刻を計算します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveRunnerの実行順をそのままなぞって時刻を計算します！
// - 進行条件が「敵の全滅」の場合、それ以降の時刻は下限になります！
// - 集計値はSpawn Countの掛け算で求めるため、体数が多くても重くなりません！
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Enemy;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// WaveDataSOからWaveMetricsを計算するクラス
    /// </summary>
    public static class WaveMetricsCalculator
    {
        /// <summary>
        /// 1つのEnemySpawnEntryに対して、最大で何回のスポーンイベントが発生するかの上限値
        /// Spawn Countに極端な値が入力された場合に、Editorが固まるのを防ぐ
        /// </summary>
        private const int MaxSpawnEventsPerEntry = 256;

        /// <summary>
        /// WaveDataSOの見積り値を計算します
        /// SpawnEventsのリストを渡すと、敵1体ごとの出現時刻も取得
        /// </summary>
        /// <param name="waveData">計算対象のWaveDataSO</param>
        /// <param name="spawnEvents">出現時刻の格納先</param>
        /// <returns>計算して見積り値</returns>
        public static WaveMetrics Calculate(WaveDataSO waveData, List<SpawnEvent> spawnEvents)
        {
            spawnEvents?.Clear();

            if (waveData == null || waveData.Groups == null)
            {
                return default;
            }

            IReadOnlyList<WaveGroupData> groups = waveData.Groups;

            int groupCount = groups.Count;
            int totalEnemyCount = 0;
            int bossCount = 0;
            float totalHp = 0f;
            float totalBarrier = 0f;
            float totalExp = 0f;
            float attackPressure = 0f;

            // Wave開始からの経過時間
            float currentTime = waveData.StartDelay;

            // 「敵の全滅」待ちが一度でも入ると、それ以降の時刻は下限になる
            bool isTimeConfirmed = true;

            float lastSpawnTime = currentTime;

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                WaveGroupData group = groups[groupIndex];

                if (group == null)
                {
                    continue;
                }

                currentTime += group.DelayBeforeStart;

                float groupStartTime = currentTime;
                float groupSpawnEndTime = groupStartTime;

                IReadOnlyList<EnemySpawnEntry> entries = group.SpawnEntries;

                if (entries != null)
                {
                    for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                    {
                        EnemySpawnEntry entry = entries[entryIndex];

                        if (entry == null || entry.SpawnCount <= 0)
                        {
                            continue;
                        }

                        float entryStartTime = groupStartTime + entry.StartDelay;
                        float entryEndTime = entryStartTime + entry.SpawnInterval * (entry.SpawnCount - 1);

                        if (entryEndTime > groupSpawnEndTime)
                        {
                            groupSpawnEndTime = entryEndTime;
                        }

                        if (entryEndTime > lastSpawnTime)
                        {
                            lastSpawnTime = entryEndTime;
                        }

                        AddSpawnEvents(spawnEvents, entry, groupIndex, entryIndex, entryStartTime, isTimeConfirmed);

                        AccumulateEntry(waveData, entry, ref totalEnemyCount, ref bossCount, ref totalHp, ref totalBarrier, ref totalExp, ref attackPressure);
                    }
                }

                // 最後のGroupはWaveRunnerが進行条件を参照しない
                if (groupIndex == groupCount - 1)
                {
                    break;
                }

                if (group.AdvanceType == WaveGroupAdvanceType.TimeAfterGroupStart)
                {
                    currentTime = groupStartTime + group.TimeUntilNextGroup;
                }
                else
                {
                    // 撃破にかかる時間は静的に決まらないため、スポーン完了時刻を下限とする
                    currentTime = groupSpawnEndTime;
                    isTimeConfirmed = false;
                }
            }

            return new WaveMetrics(groupCount, totalEnemyCount, bossCount, totalHp, totalBarrier, totalExp, attackPressure, lastSpawnTime, isTimeConfirmed);
        }


        /// <summary>
        /// 1つのEnemySpawnEntryから、敵1体ごとの出現時刻を追加
        /// </summary>
        /// <param name="spawnEvents">出現時刻の格納先。nullの場合は何もしない</param>
        /// <param name="entry">対象のEnemySpawnEntry</param>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <param name="entryIndex">EnemySpawnEntryのインデックス</param>
        /// <param name="entryStartTime">1体目が出現する時刻</param>
        /// <param name="isTimeConfirmed">時刻が確定値かどうか</param>
        private static void AddSpawnEvents(List<SpawnEvent> spawnEvents, EnemySpawnEntry entry, int groupIndex, int entryIndex, float entryStartTime, bool isTimeConfirmed)
        {
            if (spawnEvents == null)
            {
                return;
            }

            int eventCount = Mathf.Min(entry.SpawnCount, MaxSpawnEventsPerEntry);

            // Spawn Countが大きすぎる場合は、上限値までしか出現時刻を追加しない
            for (int i = 0; i < eventCount; i++)
            {
                float spawnTime = entryStartTime + entry.SpawnInterval * i;

                spawnEvents.Add(new SpawnEvent(groupIndex, entryIndex, entry.EnemyDefinition, spawnTime, isTimeConfirmed));
            }
        }


        /// <summary>
        /// 1つのEnemySpawnEntryの値を集計して、WaveMetricsの各値に加算します
        /// </summary>
        /// <param name="waveData">所属するWaveData</param>
        /// <param name="entry">対象のEnemySpawnEntry</param>
        /// <param name="totalEnemyCount">総敵数</param>
        /// <param name="bossCount">ボス数</param>
        /// <param name="totalHp">総HP</param>
        /// <param name="totalBarrier">総バリア</param>
        /// <param name="totalExp">総経験値</param>
        /// <param name="attackPressure">攻撃圧力</param>
        private static void AccumulateEntry(WaveDataSO waveData, EnemySpawnEntry entry, ref int totalEnemyCount, ref int bossCount, ref float totalHp, ref float totalBarrier, ref float totalExp, ref float attackPressure)
        {
            int spawnCount = entry.SpawnCount;

            totalEnemyCount += spawnCount;

            EnemyDefinition definition = entry.EnemyDefinition;

            // EnemyDefinitionがnullの場合は、集計値に加算しない
            if (definition == null)
            {
                return;
            }

            if (definition.IsBoss)
            {
                bossCount += spawnCount;
            }

            float hp = definition.MaxHp * waveData.HpRate;

            totalHp += hp * spawnCount;
            totalExp += hp * definition.ExpRate * spawnCount;

            if (definition.HasBarrier)
            {
                totalBarrier += definition.MaxGauge * waveData.BarrierRate * spawnCount;
            }

            // 攻撃間隔が0以下だと計算できないため除外する
            if (definition.Attackinterval > 0f)
            {
                attackPressure += definition.AttackPower / definition.Attackinterval * spawnCount;
            }
        }
    }
}
