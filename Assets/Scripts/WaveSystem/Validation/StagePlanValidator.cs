// ------------------------------------------------------------
// File		: StagePlanValidator.cs
// Summary	: StageDataSOの内容を検証する
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - StageDataSOの検証ルールは、このクラスだけでやりもす！
// - 抽選が成立するかに加えて、候補のWave自体の設定も検証するようにします！
// - Errorが0件であれば、Stageの全Waveが最後まで実行できます！
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Game.WaveSystem
{
    /// <summary>
    /// StageDataSOの内容を検証する
    /// </summary>
    public static class StagePlanValidator
    {
        /// <summary>
        /// 参照先のWaveを検証するときに使う一時バッファ
        /// </summary>
        private static readonly List<ValidationIssue> waveIssueBuffer = new();

        /// <summary>
        /// 同じWaveを何度も報告しないための記録
        /// </summary>
        private static readonly HashSet<WaveDataSO> checkedWaves = new();

        /// <summary>
        /// Next Stageの循環を調べるときに使う記録
        /// </summary>
        private static readonly HashSet<StageDataSO> visitedStages = new();

        /// <summary>
        /// StageDataSOを検証し、結果をresultsに追加する
        /// resultsの中身は呼び出し時にクリアされる
        /// </summary>
        /// <param name="stageData">検証するStageDataSO</param>
        /// <param name="results">検証結果の格納先</param>
        public static void Validate(StageDataSO stageData, List<ValidationIssue> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            checkedWaves.Clear();

            if (stageData == null)
            {
                results.Add(ValidationIssue.Error("StageDataSO is null"));
                return;
            }

            ValidateBossWave(stageData, results);
            ValidateWaveCount(stageData, results);
            ValidateProgression(stageData, results);

            ValidatePool(stageData.EarlyWavePool, "序盤WavePool", "earlyWavePool", results);
            ValidatePool(stageData.MiddleWavePool, "中盤WavePool", "middleWavePool", results);
            ValidatePool(stageData.LateWavePool, "終盤WavePool", "lateWavePool", results);
        }


        private static void ValidateBossWave(StageDataSO stageData, List<ValidationIssue> results)
        {
            if (stageData.BossWave == null)
            {
                results.Add(ValidationIssue.Error("BossWaveが設定されていません。", "bossWave", stageData));
                return;
            }

            ValidateReferencedWave(stageData.BossWave, "最終Wave", "bossWave", results);
        }


        /// <summary>
        /// 合計Wave数がゲーム仕様通りか検証する
        /// </summary>
        /// <param name="stageData">検証するStageDataSO</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateWaveCount(StageDataSO stageData, List<ValidationIssue> results)
        {
            if (stageData.HasRequiredWaveCount)
                return;

            int early = GetSelectionCount(stageData.EarlyWavePool);
            int middle = GetSelectionCount(stageData.MiddleWavePool);
            int late = GetSelectionCount(stageData.LateWavePool);
            int boss = stageData.BossWave != null ? 1 : 0;

            // 合計Wave数と設定値の差を計算する
            int total = stageData.TotalWaveCount;
            int required = stageData.RequiredWaveCount;
            int diff = total - required;

            string diffText = diff > 0 ? $"多すぎます(+{diff})" : $"少なすぎます({diff})";

            results.Add(ValidationIssue.Error($"合計Wave数が設定値と一致していません({diffText})。序盤: {early} + 中盤: {middle} + 終盤: {late} + Boss: {boss} = 合計: {total} / 設定値: {required}。各PoolのSelection Countを調整するか、Required Wave Countを変更してください。", "requiredWaveCount", stageData));
        }


        /// <summary>
        /// Stage進行の設定を検討する
        /// </summary>
        /// <param name="stageData">検証するStageDataSO</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateProgression(StageDataSO stageData, List<ValidationIssue> results)
        {
            // Sceneの名前が空だと、このStageを読み込まない
            if (string.IsNullOrWhiteSpace(stageData.StageSceneName))
            {
                results.Add(ValidationIssue.Error("StageSceneNameが空です。Stageを読み込めません。", "stageSceneName", stageData));
            }

            // 自分自身を指していると、同じStageを無限に繰り返してしまう
            if (stageData.NextStage == stageData)
            {
                results.Add(ValidationIssue.Error("NextStageが自分自身を指しています。無限ループします。", "nextStage", stageData));
                return;
            }

            if (stageData.NextStage == null)
            {
                results.Add(ValidationIssue.Warning("NextStageが設定されていません。Stageをクリアしても次のStageに進めません。", "nextStage", stageData));
                return;
            }

            // NextStageを辿っていき、一度通ったStageに戻ってこないか調べる
            visitedStages.Clear();
            visitedStages.Add(stageData);

            StageDataSO current = stageData.NextStage;

            while (current != null)
            {
                if (!visitedStages.Add(current))
                {
                    results.Add(ValidationIssue.Error($"NextStageの設定が循環しています。Stage '{current.StageName}' に戻っててるぞ！。これガチ無限。", "nextStage", stageData));
                    return;
                }
                current = current.NextStage;
            }
        }


        private static void ValidatePool(WavePoolData pool, string poolLabel, string poolPath, List<ValidationIssue> results)
        {
            if (pool == null)
            {
                results.Add(ValidationIssue.Error($"WavePool '{poolLabel}' が設定されていません。", poolPath));
                return;
            }

            IReadOnlyList<WeightedWaveEntry> candidates = pool.Candidates;

            if (candidates == null)
            {
                results.Add(ValidationIssue.Error($"WavePool '{poolLabel}' の候補がnullです。", $"{poolPath}.candidates"));
                return;
            }

            int need = pool.SelectionCount;

            if (need == 0)
            {
                results.Add(ValidationIssue.Warning($"WavePool '{poolLabel}' の選択数が0件です。", $"{poolPath}.selectionCount"));
                return;
            }

            int selectableCount = 0;

            HashSet<WaveDataSO> uniqueWaves = new HashSet<WaveDataSO>();

            for (int i = 0; i < candidates.Count; i++)
            {
                WeightedWaveEntry entry = candidates[i];

                if (entry == null)
                {
                    continue;
                }

                string entryPath = $"{poolPath}.candidates.Array.data[{i}]";

                if (entry.IsSelectable)
                {
                    selectableCount++;

                    if (!uniqueWaves.Add(entry.WaveData))
                    {
                        results.Add(ValidationIssue.Info($"{poolLabel}に「{entry.WaveData.WaveName}」が複数登録されています。抽選ではWeightが合算されたのと同じになるぜよ！", entryPath));
                    }

                    ValidateReferencedWave(entry.WaveData, poolLabel, entryPath, results);

                    continue;
                }

                // 以下は抽選対象外になっている候補の理由を伝える
                if (!entry.IsEnabled)
                    continue;

                if (entry.WaveData == null)
                {
                    results.Add(ValidationIssue.Warning($"{poolLabel}の候補[{i + 1}]が有効になっていますが、WaveDataがnullです。抽選対象じゃなくなってるぞ！", $"{entryPath}.waveData"));

                    continue;
                }

                results.Add(ValidationIssue.Info($"{poolLabel}の「{entry.WaveData.WaveName}」はWeightが0なので抽選対象外です。", $"{entryPath}.weight"));
            }

            ValidateSelectionCount(pool, poolLabel, poolPath, need, selectableCount, uniqueWaves.Count, results);
        }


        /// <summary>
        /// 必要な数だけWaveを選択できるか検証する
        /// </summary>
        /// <param name="pool">検証するWavePool</param>
        /// <param name="poolLabel">表示用のPool名</param>
        /// <param name="poolPath">プロパティのパス</param>
        /// <param name="need">必要なWave数</param>
        /// <param name="selectableCount">選択可能なWave数</param>
        /// <param name="uniqueWaveCount">抽選可能な候補に含まれる異なるWaveの数</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateSelectionCount(WavePoolData pool, string poolLabel, string poolPath, int need, int selectableCount, int uniqueWaveCount, List<ValidationIssue> results)
        {
            if (selectableCount == 0)
            {
                results.Add(ValidationIssue.Error($"WavePool '{poolLabel}' の候補が0件です。", $"{poolPath}.candidates"));
                return;
            }

            // 重複を許可している場合、候補が1つでも必要な数だけ選べる
            if (pool.AllowDuplicateSelection)
            {
                if (uniqueWaveCount < need)
                {
                    results.Add(ValidationIssue.Info($"{poolLabel}は候補が{uniqueWaveCount}件しかないため、{need}件の選択を行うと重複が発生します。", $"{poolPath}.allowDuplicateSelection"));
                }

                return;
            }

            if (uniqueWaveCount < need)
            {
                results.Add(ValidationIssue.Error($"{poolLabel}は重複禁止ですが、異なるWaveが{uniqueWaveCount}種類しかなく、{need}個を選べません。候補を増やすか、Allow Duplicate SelectionをONにしてクレメンス！", $"{poolPath}.candidates"));
            }
        }


        /// <summary>
        /// 参照先のWaveDataSOにエラーがないか検証する
        /// </summary>
        /// <param name="waveData">検証するWaveDataSO</param>
        /// <param name="label">エラーメッセージに含めるラベル</param>
        /// <param name="propertyPath">エラーが発生したプロパティのパス</param>
        /// <param name="results">検証結果の格納先</param>
        private static void ValidateReferencedWave(WaveDataSO waveData, string label, string propertyPath, List<ValidationIssue> results)
        {
            if (waveData == null)
                return;

            // 同じWaveが複数のPoolに入っていても、報告は1回だけにする
            if (!checkedWaves.Add(waveData))
            {
                return;
            }

            WaveValidator.Validate(waveData, waveIssueBuffer);

            int errorCount = ValidationIssueUtility.CountOf(waveIssueBuffer, IssueSeverity.Error);

            if (errorCount == 0)
            {
                return;
            }

            results.Add(ValidationIssue.Error($"{label}の「{waveData.WaveName}」に設定エラーが{errorCount}件あります。このWaveが選ばれるとPlayで失敗します。アセットを開いて修正してください!!\n" + ValidationIssueUtility.BuildErrorText(waveIssueBuffer), propertyPath, waveData));
        }


        /// <summary>
        /// WavePoolの選択数を取得する
        /// </summary>
        /// <param name="pool">対象のWavePoolData</param>
        /// <returns>選択するWave数</returns>
        private static int GetSelectionCount(WavePoolData pool)
        {
            return pool != null ? pool.SelectionCount : 0;
        }
    }
}
