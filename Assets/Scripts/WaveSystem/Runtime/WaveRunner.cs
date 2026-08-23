// ------------------------------------------------------------
// File     : WaveRunner.cs
// Summary  : WaveDataSOに設定されたWaveGroupを実行
//
// Author   : [浅野 勇生]
// Created  : 2026-07-15
//
// Notes:
// - 1回の実行で1つのWaveDataSOを処理します！
// - 各EnemySpawnEntryは同じGroup内で並行して実行します！
// - 敵の生成処理自体は既存EnemySpawnerへ依頼します！
// - EnemyDefeatedEventを利用して生存敵を追跡します！
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

        // 撃破されて落下を開始した敵（EnemyDefeatedEvent発行前の落下中の敵）
        private readonly HashSet<string> defeatDroppingEnemyIds = new HashSet<string>();

        // EnemyIDとその敵が所属するGroupの実行状態を対応付ける
        private readonly Dictionary<string, GroupRuntimeState> groupByEnemyId = new();

        // まだスポーンが完了していないEnemySpawnEntryの数
        private int activeSpawnRoutineCount;

        private int enemyDeadRoutineCount;
        private int maxEnemySpawnRoutineCount;

        // 検証結果の一時バッファ
        private readonly List<ValidationIssue> validationResults = new();

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
            EventBus.Subscribe<EnemyDefeatDropStartedEvent>(OnEnemyDefeatDropStarted);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyDefeatedEvent>(OnEnemyDefeated);
            EventBus.Unsubscribe<EnemyDefeatDropStartedEvent>(OnEnemyDefeatDropStarted);
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
            WaveValidator.Validate(waveData, validationResults);

            if (ValidationIssueUtility.HasError(validationResults))
            {
                Debug.LogError($"[WaveRunner] WaveDataSOの検証に失敗しました。\n{ValidationIssueUtility.BuildErrorText(validationResults)}", waveData);
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

                EventBus.Publish(new GroupChangeEvent(groupIndex, groups.Count));
                EventBus.Publish(new GameChangeEvent(0, 1));
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
            // 全滅、または残っている敵が全て撃破落下を開始した時点でWave完了とみなす
            // （最後の敵の落下中にクリア演出やスローモーション猶予を開始できるようにするため）
            yield return new WaitUntil(() =>
                activeSpawnRoutineCount == 0 &&
                (aliveWaveEnemyIds.Count == 0 || aliveWaveEnemyIds.IsSubsetOf(defeatDroppingEnemyIds)));

            EventBus.Publish(new GroupChangeEvent(groups.Count, groups.Count));

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
            enemyDeadRoutineCount = 0;
            maxEnemySpawnRoutineCount = 0;
            foreach(EnemySpawnEntry entry in group.SpawnEntries)
            {
                maxEnemySpawnRoutineCount += entry.SpawnCount;
            }


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
        /// WaveRunnerが管理している生存敵のうち、指定されたEnemyIdの敵が撃破落下を開始したことを記録します。
        /// </summary>
        /// <param name="ev"></param>
        private void OnEnemyDefeatDropStarted(EnemyDefeatDropStartedEvent ev)
        {
            if (aliveWaveEnemyIds.Contains(ev.EnemyId))
            {
                defeatDroppingEnemyIds.Add(ev.EnemyId);
            }

            Debug.Log($"[DropDebug] WaveRunner受信: {ev.EnemyId} tracked={aliveWaveEnemyIds.Contains(ev.EnemyId)} alive={aliveWaveEnemyIds.Count} dropping={defeatDroppingEnemyIds.Count} spawning={activeSpawnRoutineCount} time={Time.time:F2}"); // TODO: 動作確認後に削除
        }



        /// <summary>
        /// WaveRunnerが管理している生存敵のうち、指定されたEnemyIdの敵が倒されたことを通知します。
        /// </summary>
        /// <param name="ev"></param>
        private void OnEnemyDefeated(EnemyDefeatedEvent ev)
        {
            defeatDroppingEnemyIds.Remove(ev.EnemyId);

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

            enemyDeadRoutineCount++;
            EventBus.Publish(new GameChangeEvent(enemyDeadRoutineCount, maxEnemySpawnRoutineCount));
        }



        /// <summary>
        /// 実行中の強制中断し、ランタイム状態をリセット
        /// </summary>
        public void AbortAndReset()
        {
            StopAllCoroutines();
            ResetRuntimeState();
            LastRunSucceeded = false;
        }


        /// <summary>
        /// ランタイム状態をリセットします。
        /// </summary>
        private void ResetRuntimeState()
        {
            aliveWaveEnemyIds.Clear();
            defeatDroppingEnemyIds.Clear();
            groupByEnemyId.Clear();
            activeSpawnRoutineCount = 0;
            enemyDeadRoutineCount = 0;
            maxEnemySpawnRoutineCount = 0;
            IsRunning = false;
        }
    }
}
