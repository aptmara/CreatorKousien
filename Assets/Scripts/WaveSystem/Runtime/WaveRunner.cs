// ------------------------------------------------------------
// File     : WaveRunner.cs
// Summary  : WaveDataSOに設定されたWaveGroupを実行
//
// Author   : [浅野 勇生]
// Created  : 2026-07-15
//
// Notes:
// - 1回の実行で1つのWaveDataSOを処理します。
// - 各EnemySpawnEntryは同じGroup内で並行して実行します。
// - 敵の生成処理自体は既存EnemySpawnerへ依頼します。
// - EnemyDefeatedEventを利用して生存敵を追跡します。
// ------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using Game.Core.Enemy;
using Game.Core.Events;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// WaveDataSOに設定されたGroupとEnemySpawnEntryを実行します。
    /// </summary>
    public sealed class WaveRunner : MonoBehaviour
    {
        [Header("--- スポーン再試行設定 ---")]

        [SerializeField]
        [Min(0.05f)]
        [Tooltip("敵同士の距離を確保できず、スポーンに失敗した場合の再試行間隔")]
        private float spawnRetryInterval = 0.25f;

        // 現在のWaveから出現し、まだ倒されていない敵
        private readonly HashSet<string> aliveWaveEnemyIds = new HashSet<string>();

        // EnemyIDとその敵が所属するGroupの実行状態を対応付ける
        private readonly Dictionary<string, GroupRuntimeState> groupByEnemyId = new();

        // まだスポーンが完了していないEnemySpawnEntryの数
        private int activeSpawnRoutineCount;

        public bool IsRunning { get; private set; }             ///< WaveRunnerがWaveを実行中かどうか
        public bool LastRunSucceeded { get; private set; }      ///< WaveRunnerの最後の実行が成功したかどうか
        public int AliveEnemyCount => aliveWaveEnemyIds.Count;  ///< WaveRunnerが管理している生存敵の数


        /// <summary>
        /// 1つのWaveGroupの実行状態を保持するクラスです。
        /// SOへ保存するデータではなく、WaveRunnerの実行中にのみ使用されます。
        /// </summary>
        private sealed class GroupRuntimeState
        {
            public readonly HashSet<string> AliveEnemyIds = new();                                      ///< このGroupから出現した敵で、まだ倒されていない敵のID
            public int ActiveSpawnRoutineCount;                                                         ///< このGroupのEnemySpawnEntryのうち、まだスポーンが完了していないものの数

            public bool IsCompleted => ActiveSpawnRoutineCount == 0 && AliveEnemyIds.Count == 0;        ///< このGroupの実行が完了したかどうか
        }



        // --- イベントの購読と解除 ---
        private void OnEnable()
        {
            EventBus.Subscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            StopAllCoroutines();
            ResetRuntimeState();
        }


        /// <summary>
        /// WaveDataSOに設定されたWaveGroupを順番に実行します。
        /// </summary>
        /// <param name="waveData"></param>
        /// <param name="enemySpawner"></param>
        /// <returns></returns>
        public IEnumerator PlayWave(WaveDataSO waveData, EnemySpawner enemySpawner)
        {
            LastRunSucceeded = false;

            if (IsRunning)
            {
                Debug.LogError("[WaveRunner] すでに別のWaveを実行中です。", this);
                yield break;
            }

            if (enemySpawner == null)
            {
                Debug.LogError("[WaveRunner] EnemySpawnerが設定されていません。", this);
                yield break;
            }

            // WaveDataSOの検証
            if (!TryValidateWaveData(waveData, out string errorMessage))
            {
                Debug.LogError($"[WaveRunner] WaveDataSOの検証に失敗しました。{errorMessage}", waveData);
                yield break;
            }

            // Waveの実行を開始する
            ResetRuntimeState();
            IsRunning = true;

            Debug.Log($"[WaveRunner] Wave「{waveData.WaveName}」の実行を開始します。", waveData);

            // WaveGroupを順番に実行する
            if (waveData.StartDelay > 0f)
            {
                yield return new WaitForSeconds(waveData.StartDelay);
            }

            IReadOnlyList<WaveGroupData> groups = waveData.Groups;

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                WaveGroupData group = groups[groupIndex];

                // 敵をスポーンするまでの遅延時間を待機する
                if (group.DelayBeforeStart > 0f)
                {
                    yield return new WaitForSeconds(group.DelayBeforeStart);
                }

                Debug.Log($"[WaveRunner] Wave「{waveData.WaveName}」のGroup「{group.GroupName}」の実行を開始します。", waveData);

                // このGroupの実行状態を作成する
                GroupRuntimeState groupState = new GroupRuntimeState();

                // WaveGroupのEnemySpawnEntryを順番に実行する
                StartGroupSpawnRoutines(waveData, group, groupState, enemySpawner);


                bool hasNextGroup = groupIndex + 1 < groups.Count;

                // 最後のGroupでなければ、次のGroupに進む条件を待機する
                if (!hasNextGroup)
                {
                    continue;
                }

                switch (group.AdvanceType)
                {
                    case WaveGroupAdvanceType.AllEnemiesDefeated:
                        // このGroupから出現した敵がすべて倒されるまで待機する
                        yield return new WaitUntil(() => groupState.IsCompleted);
                        break;

                    case WaveGroupAdvanceType.TimeAfterGroupStart:
                        // 時間経過で次のGroupに進む
                        if (group.TimeUntilNextGroup > 0f)
                        {
                            yield return new WaitForSeconds(group.TimeUntilNextGroup);
                        }
                        break;

                    default:
                        Debug.LogError($"[WaveRunner] Group「{group.GroupName}」のAdvanceTypeが不正です。", waveData);
                        IsRunning = false;
                        yield break;
                }
            }

            // Wave自体はすべてのGroupの実行が完了した時点で終了とする
            yield return new WaitUntil(() => activeSpawnRoutineCount == 0 && aliveWaveEnemyIds.Count == 0);


            LastRunSucceeded = true;
            IsRunning = false;

            Debug.Log($"[WaveRunner] Wave「{waveData.WaveName}」の実行が完了しました。", waveData);
        }



        /// <summary>
        /// WaveDataSOに設定されたWaveGroupを実行します。
        /// </summary>
        /// <param name="waveData">WaveDataのSO</param>
        /// <param name="group">WaveGroupのデータ</param>
        /// <param name="groupState">グループの実行状態</param>
        /// <param name="enemySpawner">敵スポナー</param>
        private void StartGroupSpawnRoutines(WaveDataSO waveData, WaveGroupData group, GroupRuntimeState groupState, EnemySpawner enemySpawner)
        {
            foreach (EnemySpawnEntry entry in group.SpawnEntries)
            {
                // このGroupのActiveSpawnRoutineCountを増やす
                groupState.ActiveSpawnRoutineCount++;
                activeSpawnRoutineCount++;

                // EnemySpawnEntryのスポーン処理を開始する
                StartCoroutine(SpawnEntryRoutine(waveData, entry, groupState, enemySpawner));
            }
        }



        /// <summary>
        /// WaveDataSOに設定されたWaveGroupを実行します。
        /// </summary>
        /// <param name="waveData">WaveDataのSO</param>
        /// <param name="entry">スポーンエントリ</param>
        /// <param name="groupState">グループの実行状態</param>
        /// <param name="enemySpawner">敵スポナー</param>
        /// <returns></returns>
        private IEnumerator SpawnEntryRoutine(WaveDataSO waveData, EnemySpawnEntry entry, GroupRuntimeState groupState, EnemySpawner enemySpawner)
        {
            if (entry.StartDelay > 0f)
            {
                yield return new WaitForSeconds(entry.StartDelay);
            }

            WaitForSeconds retryWait = new WaitForSeconds(spawnRetryInterval);

            for (int spawnIndex = 0; spawnIndex < entry.SpawnCount; spawnIndex++)
            {
                while (waveData.MaxAliveEnemies > 0 && aliveWaveEnemyIds.Count >= waveData.MaxAliveEnemies)
                {
                    // 最大生存敵数に達している場合は、次のスポーンを待機する
                    yield return null;
                }

                EnemyController spawnedEnemy = null;

                // 距離を確保できない場合は同じ敵を再試行する
                while (!enemySpawner.TrySpawnEnemy(entry.EnemyDefinition, waveData.HpRate, waveData.BarrierRate, waveData.MinDistanceFromOtherEnemies, out spawnedEnemy))
                {
                    yield return retryWait;
                }

                // このEnemySpawnEntryから出現した敵をWaveRunnerに登録する
                RegisterSpawnedEnemy(spawnedEnemy, groupState);

                bool hasNextSpawn = spawnIndex + 1 < entry.SpawnCount;

                if (hasNextSpawn && entry.SpawnInterval > 0f)
                {
                    yield return new WaitForSeconds(entry.SpawnInterval);
                }
            }

            // このEnemySpawnEntryのスポーンが完了したことを通知する
            groupState.ActiveSpawnRoutineCount--;
            activeSpawnRoutineCount--;
        }



        /// <summary>
        /// WaveRunnerが管理している生存敵のうち、指定されたEnemyIdの敵が出現したことを通知します。
        /// </summary>
        /// <param name="enemy">敵のコントローラー</param>
        /// <param name="groupState">ランタイムのステータス</param>
        private void RegisterSpawnedEnemy(EnemyController enemy, GroupRuntimeState groupState)
        {
            if (enemy == null || string.IsNullOrWhiteSpace(enemy.InstanceEnemyId))
            {
                return;
            }

            string enemyId = enemy.InstanceEnemyId;

            aliveWaveEnemyIds.Add(enemyId);
            groupState.AliveEnemyIds.Add(enemyId);
            groupByEnemyId[enemyId] = groupState;
        }



        /// <summary>
        /// WaveRunnerが管理している生存敵のうち、指定されたEnemyIdの敵が倒されたことを通知します。
        /// </summary>
        /// <param name="ev"></param>
        private void OnEnemyDefeated(EnemyDefeatedEvent ev)
        {
            if (!aliveWaveEnemyIds.Remove(ev.EnemyId))
            {
                // 現在のWave以外から出現した敵は無視する
                return;
            }

            // この敵が所属するGroupの状態を更新する
            if (groupByEnemyId.TryGetValue(ev.EnemyId, out GroupRuntimeState groupState))
            {
                groupState.AliveEnemyIds.Remove(ev.EnemyId);
                groupByEnemyId.Remove(ev.EnemyId);
            }
        }



        /// <summary>
        /// WaveDataSOの内容を検証します。
        /// </summary>
        /// <param name="waveData">WaveDataのSO</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <returns></returns>
        private static bool TryValidateWaveData(WaveDataSO waveData, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (waveData == null)
            {
                errorMessage = "WaveDataSOが設定されていません。";

                return false;
            }

            if (waveData.Groups == null || waveData.Groups.Count == 0)
            {
                errorMessage = $"Wave「{waveData.WaveName}」にWaveGroupが設定されていません。";

                return false;
            }

            for (int groupIndex = 0; groupIndex < waveData.Groups.Count; groupIndex++)
            {
                WaveGroupData group = waveData.Groups[groupIndex];

                if (group == null)
                {
                    errorMessage = $"Wave「{waveData.WaveName}」のGroup[{groupIndex}]がnullです。";
                    return false;
                }

                if (group.SpawnEntries == null || group.SpawnEntries.Count == 0)
                {
                    errorMessage = $"Wave「{waveData.WaveName}」のGroup「{group.GroupName}」にEnemySpawnEntryが設定されていません。";
                    return false;
                }

                for (int entryIndex = 0; entryIndex < group.SpawnEntries.Count; entryIndex++)
                {
                    EnemySpawnEntry entry = group.SpawnEntries[entryIndex];
                    if (entry == null)
                    {
                        errorMessage = $"Wave「{waveData.WaveName}」のGroup「{group.GroupName}」のEnemySpawnEntry[{entryIndex}]がnullです。";
                        return false;
                    }

                    if (entry.EnemyDefinition == null)
                    {
                        errorMessage = $"Group 「{group.GroupName}」のEnemySpawnEntry[{entryIndex + 1}]にEnemyDefinitionが設定されていません。";
                        return false;
                    }

                    if (entry.SpawnCount <= 0)
                    {
                        errorMessage = $"Group 「{group.GroupName}」のEnemySpawnEntry[{entryIndex + 1}]のSpawnCountが0以下です。";
                        return false;
                    }

                    if (entry.EnemyDefinition.EnemyBody == null)
                    {
                        errorMessage = $"敵「{entry.EnemyDefinition.EnemyId}」のEnemyDefinitionにEnemyBodyが設定されていません。";
                        return false;
                    }

                    if (!entry.EnemyDefinition.EnemyBody.TryGetComponent(out EnemyBodyController _))
                    {
                        errorMessage = $"敵「{entry.EnemyDefinition.EnemyId}」のEnemyBodyにEnemyBodyControllerがありません。";
                        return false;
                    }
                }
            }

            return true;
        }



        /// <summary>
        /// ランタイム状態をリセットします。
        /// </summary>
        private void ResetRuntimeState()
        {
            aliveWaveEnemyIds.Clear();
            groupByEnemyId.Clear();
            activeSpawnRoutineCount = 0;
            IsRunning = false;
        }
    }
}
