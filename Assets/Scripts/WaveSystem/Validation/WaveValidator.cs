// ------------------------------------------------------------
// File		: WaveValidator.cs
// Summary	: WaveDataSOの内容を検証します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveDataSOの検証ルールは、このクラスだけが持ちます!
// - RuntimeのWaveRunnerとEditorの両方がこのクラスを呼びます!
// - Errorが0件であれば、Playでのウェーブ実行は必ず開始できるようになってます！
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Core.Enemy;
using Game.Gameplay.Enemy.Boss;

namespace Game.WaveSystem
{
    /// <summary>
    /// WaveDataSOの内容を検証するクラスです
    /// </summary>
    public static class WaveValidator
    {
        /// <summary>
        /// WaveDataSOの内容を検証します
        /// </summary>
        /// <param name="waveData">検証するWaveDataSO</param>
        /// <param name="results">検証結果の格納先</param>
        public static void Validate(WaveDataSO waveData, List<ValidationIssue> results)
        {
            if (results == null)
                return;

            results.Clear();

            // WaveDataSOの検証ルール
            if (waveData == null)
            {
                results.Add(ValidationIssue.Error("WaveDataSOがnullです。"));
                return;
            }

            IReadOnlyList<WaveGroupData> groups = waveData.Groups;

            if (groups == null || groups.Count == 0)
            {
                results.Add(ValidationIssue.Error($"Wave「{waveData.WaveName}」にWaveGroupが1つも設定されていません。", "groups", waveData));
                return;
            }

            // Wave全体の設定を検証する
            ValidateWaveSettings(waveData, results);

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                bool isLastGroup = groupIndex == groups.Count - 1;

                ValidateGroup(waveData, groups[groupIndex], groupIndex, isLastGroup, results);
            }
        }


        /// <summary>
        /// GroupのSerializedPropertyパスを返します。
        /// </summary>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <returns>SerializedPropertyのパス</returns>
        private static string GroupPath(int groupIndex)
            => $"groups.Array.data[{groupIndex}]";


        /// <summary>
        /// EnemySpawnEntryのSerializedPropertyパスを返します。
        /// </summary>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <param name="entryIndex">EnemySpawnEntryのインデックス</param>
        /// <returns>SerializedPropertyのパス</returns>
        private static string EntryPath(int groupIndex, int entryIndex)
            => $"{GroupPath(groupIndex)}.spawnEntries.Array.data[{entryIndex}]";


        /// <summary>
        /// メッセージ表示用のGroup名を返します。
        /// </summary>
        /// <param name="group">対象のGroup</param>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <returns>表示用の名前</returns>
        private static string GroupLabel(WaveGroupData group, int groupIndex)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.GroupName))
                return $"Group[{groupIndex + 1}]";

            return $"Group「{group.GroupName}」";
        }


        /// <summary>
        /// メッセージ表示用の敵の名前を返します。
        /// </summary>
        /// <param name="definition">対象のEnemyDefinition</param>
        /// <param name="entryIndex">EnemySpawnEntryのインデックス</param>
        /// <returns>表示用の名前</returns>
        private static string EntryLabel(EnemyDefinition definition, int entryIndex)
        {
            if (definition == null)
                return $"敵[{entryIndex + 1}]";

            return $"敵「{definition.EnemyId}」";
        }


        /// <summary>
        /// 1つのWaveGroupDataを検証します
        /// </summary>
        /// <param name="waveData">所属するWaveDataSO</param>
        /// <param name="group">検証するWaveGroupData</param>
        /// <param name="groupIndex">WaveGroupのインデックス</param>
        /// <param name="isLastGroup">最後のGroupかどうか</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateGroup(WaveDataSO waveData, WaveGroupData group, int groupIndex, bool isLastGroup, List<ValidationIssue> results)
        {
            string groupLabel = GroupLabel(group, groupIndex);
            string groupPath = GroupPath(groupIndex);

            if (group == null)
            {
                results.Add(ValidationIssue.Error($"Wave「{waveData.WaveName}」の{groupLabel}がnullです。", groupPath, waveData));
                return;
            }

            IReadOnlyList<EnemySpawnEntry> entries = group.SpawnEntries;

            if (entries == null || entries.Count == 0)
            {
                results.Add(ValidationIssue.Error($"{groupLabel}に出現する敵が1体も設定されていません。", $"{groupPath}.spawnEntries", waveData));
                return;
            }

            // 進行条件が実際に使用される設定になっているかを検証する
            ValidateGroupAdvance(waveData, group, groupIndex, isLastGroup, results);

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ValidateEntry(waveData, groupLabel, entries[entryIndex], groupIndex, entryIndex, results);
            }
        }


        /// <summary>
        /// 1つのEnemySpawnEntryを検証します
        /// </summary>
        /// <param name="waveData">所属するWaveDataSO</param>
        /// <param name="groupLabel">所属するGroupの表示名</param>
        /// <param name="entry">検証するEnemySpawnEntry</param>
        /// <param name="groupIndex">WaveGroupのインデックス</param>
        /// <param name="entryIndex">EnemySpawnEntryのインデックス</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateEntry(WaveDataSO waveData, string groupLabel, EnemySpawnEntry entry, int groupIndex, int entryIndex, List<ValidationIssue> results)
        {
            string entryPath = EntryPath(groupIndex, entryIndex);

            if (entry == null)
            {
                results.Add(ValidationIssue.Error($"{groupLabel}の{EntryLabel(null, entryIndex)}がnullです。", entryPath, waveData));
                return;
            }

            EnemyDefinition definition = entry.EnemyDefinition;

            if (definition == null)
            {
                results.Add(ValidationIssue.Error($"{groupLabel}の{EntryLabel(null, entryIndex)}にEnemyDefinitionが設定されていません。", $"{entryPath}.enemyDefinition", waveData));
                return;
            }

            string entryLabel = EntryLabel(definition, entryIndex);

            if (entry.SpawnCount <= 0)
            {
                results.Add(ValidationIssue.Error($"{groupLabel}の{entryLabel}のSpawn Countが0以下です。", $"{entryPath}.spawnCount", waveData));
            }

            // EnemyBodyはEnemyDefinition側の設定なので、Ping対象をEnemyDefinitionにする
            if (definition.EnemyBody == null)
            {
                results.Add(ValidationIssue.Error($"{entryLabel}のEnemyDefinitionにEnemy Bodyが設定されていません。", null, definition));
                return;
            }

            if (definition.IsBoss)
            {
                // ボスはフィールド中央から出現するため、複数体だと重なる
                if (entry.SpawnCount > 1)
                {
                    results.Add(ValidationIssue.Warning($"{groupLabel}のボス{entryLabel}のSpawn Countが{entry.SpawnCount}体です。" + "ボスはフィールド中央から出現するため、複数体が重なる可能性があります。", $"{entryPath}.spawnCount", waveData));
                }

                if (!definition.EnemyBody.TryGetComponent(out BossBattleController _))
                {
                    results.Add(ValidationIssue.Error($"{entryLabel}はボスとして設定されていますが、Enemy BodyにBossBattleControllerがアタッチされていません。", null, definition));
                }

                return;
            }

            if (!definition.EnemyBody.TryGetComponent(out EnemyBodyController _))
            {
                results.Add(ValidationIssue.Error($"{entryLabel}は通常の敵として設定されていますが、Enemy BodyにEnemyBodyControllerがアタッチされていません。", null, definition));
            }
        }


        /// <summary>
        /// Wave全体の設定を検証します
        /// </summary>
        /// <param name="waveData">検証するWaveDataSO</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateWaveSettings(WaveDataSO waveData, List<ValidationIssue> results)
        {
            if (string.IsNullOrWhiteSpace(waveData.WaveName))
            {
                results.Add(ValidationIssue.Warning("WaveDataSOのWave Nameが空です。", "waveName", waveData));
            }

            int totalEnemyCount = CountTotalEnemies(waveData);

            // 同時出現数の上限に達すると、敵が倒されるまで次のスポーンが待機する
            if (waveData.MaxAliveEnemies > 0 && waveData.MaxAliveEnemies < totalEnemyCount)
            {
                results.Add(ValidationIssue.Warning($"Wave「{waveData.WaveName}」のMax Alive Enemiesが{waveData.MaxAliveEnemies}体です。" + $"しかし、Wave内の敵の総数は{totalEnemyCount}体です。Max Alive Enemiesを超えると、敵が倒されるまで次のスポーンが待機します。", "maxAliveEnemies", waveData));
            }

            // 同時出現数が無制限だと、最小距離を確保できずスポーンが再試行を繰り返す場合がある
            if (waveData.MaxAliveEnemies <= 0 && waveData.MinDistanceFromOtherEnemies > 0f)
            {
                results.Add(ValidationIssue.Warning($"Wave「{waveData.WaveName}」のMax Alive Enemiesが無制限です。" + $"しかし、Min Distance From Other Enemiesが{waveData.MinDistanceFromOtherEnemies}mに設定されています。Max Alive Enemiesを無制限にすると、最小距離を確保できずスポーンが再試行を繰り返す場合があります。", "maxAliveEnemies", waveData));
            }
        }


        /// <summary>
        /// WaveDataSO内の敵の総数をカウントします
        /// </summary>
        /// <param name="waveData">WaveDataのSO</param>
        /// <returns>敵の総数</returns>
        private static int CountTotalEnemies(WaveDataSO waveData)
        {
            int total = 0;

            foreach (WaveGroupData group in waveData.Groups)
            {
                if (group == null || group.SpawnEntries == null)
                    continue;

                foreach (EnemySpawnEntry entry in group.SpawnEntries)
                {
                    if (entry == null)
                        continue;

                    total += entry.SpawnCount;
                }
            }

            return total;
        }


        /// <summary>
        /// WaveGroupDataの進行条件が適切に設定されているかを検証します
        /// </summary>
        /// <param name="waveData">所属するWaveDataSO</param>
        /// <param name="group">検証するWaveGroupData</param>
        /// <param name="groupIndex">グループのインデックス</param>
        /// <param name="isLastGroup">最後のグループかどうか</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateGroupAdvance(WaveDataSO waveData, WaveGroupData group, int groupIndex, bool isLastGroup, List<ValidationIssue> results)
        {
            string groupLabel = GroupLabel(group, groupIndex);
            string groupPath = GroupPath(groupIndex);

            // 最後のGroupの進行条件はWaveRunnerが参照
            if (isLastGroup)
            {
                if (group.AdvanceType == WaveGroupAdvanceType.TimeAfterGroupStart)
                {
                    results.Add(ValidationIssue.Warning($"{groupLabel}は最後のGroupですが、進行条件がTime After Group Startに設定されています。" + "最後のGroupの進行条件はWaveRunnerが参照しないため、この設定は無視されます。" +  "Waveの終了は、出現した敵がすべて倒されたかどうかで判定されます。", $"{groupPath}.advanceType", waveData));
                }

                return;
            }

            // 待機時間が0秒だと次のGroupが同時に開始する
            if (group.AdvanceType == WaveGroupAdvanceType.TimeAfterGroupStart && group.TimeUntilNextGroup <= 0f)
            {
                results.Add(ValidationIssue.Warning($"{groupLabel}の進行条件がTime After Group Startに設定されていますが、待機時間が0秒です。" + "次のGroupが同時に開始するため、意図しない挙動になる可能性があります。", $"{groupPath}.timeUntilNextGroup", waveData));
            }
        }
    }
}

