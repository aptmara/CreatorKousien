// ------------------------------------------------------------
// File		: StageWaveSequenceBuilder.cs
// Summary	: ステージのウェーブシーケンスを構築するためのクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 序盤、中盤、終盤の各Poolから重み付き抽選を行います。
// - 最後に固定のBoss Waveを追加します。
// - 同じSeedを指定すると、同じ抽選結果を再現できます。
// ------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace Game.WaveSystem
{
    /// <summary>
    /// StageDataSOから、実際に使用するWaveの順番を生成
    /// </summary>
    public static class StageWaveSequenceBuilder
    {
        /// <summary>
        /// 指定されたStageDataSOから、ウェーブのシーケンスを構築します。
        /// </summary>
        /// <param name="stageData">抽選元のStageDataSO</param>
        /// <param name="seed">乱数シード</param>
        /// <param name="waveSequence">生成されたウェーブのシーケンス</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <returns>成功した場合はtrue</returns>
        public static bool TryBuild(StageDataSO stageData, int seed, out List<WaveDataSO> waveSequence, out string errorMessage)
        {
            waveSequence = new List<WaveDataSO>(StageDataSO.RequiredWaveCount);

            errorMessage = string.Empty;

            if (stageData == null)
            {
                errorMessage = "StageDataSOが設定されていません。";
                return false;
            }

            if (stageData.BossWave == null)
            {
                errorMessage =
                    $"Stage「{stageData.StageName}」にBoss Waveが設定されていません。";

                return false;
            }

            if (!stageData.HasRequiredWaveCount)
            {
                errorMessage =
                    $"Stage「{stageData.StageName}」のウェーブ数が不足しています。" +
                    $"現在のウェーブ数: {stageData.TotalWaveCount} / " +
                    $"必要なウェーブ数: {StageDataSO.RequiredWaveCount}, ";

                return false;
            }


            // 乱数生成器を初期化
            Random random = new Random(seed);


            // --- 序盤のWavePoolから抽選 ---
            if (!TryAppendPool(stageData.EarlyWavePool, "序盤", random, waveSequence, out errorMessage))
            {
                waveSequence.Clear();
                return false;
            }

            // --- 中盤のWavePoolから抽選 ---
            if (!TryAppendPool(stageData.MiddleWavePool, "中盤", random, waveSequence, out errorMessage))
            {
                waveSequence.Clear();
                return false;
            }

            // --- 終盤のWavePoolから抽選 ---
            if (!TryAppendPool(stageData.LateWavePool, "終盤", random, waveSequence, out errorMessage))
            {
                waveSequence.Clear();
                return false;
            }

            // --- Boss Waveは抽選せずに固定で追加 ---
            waveSequence.Add(stageData.BossWave);

            return true;
        }


        /// <summary>
        /// 1つのWavePoolから指定数のWaveを抽選し、
        /// 実行用リストへ追加します。
        /// </summary>
        /// <param name="pool">Waveのプールデータ</param>
        /// <param name="poolName">プール名</param>
        /// <param name="random">乱数生成器</param>
        /// <param name="destination">追加先リスト</param>
        /// <param name="errorMessage">エラーメッセージ</param>
        /// <returns>成功した場合はtrue</returns>
        private static bool TryAppendPool(WavePoolData pool, string poolName, Random random, List<WaveDataSO> destination, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (pool == null)
            {
                errorMessage = $"ウェーブプール「{poolName}」が設定されていません。";
                return false;
            }

            if (pool.SelectionCount < 0)
            {
                errorMessage = $"ウェーブプール「{poolName}」のSelection Countが不正です。";

                return false;
            }

            // 候補リストを取得
            IReadOnlyList<WeightedWaveEntry> sourceCandidates = pool.Candidates;

            if (sourceCandidates == null)
            {
                errorMessage = $"ウェーブプール「{poolName}」の候補リストが設定されていません。";
                return false;
            }


            // 抽選作業用のListを作成
            List<WeightedWaveEntry> workingCandidates = new();

            foreach (WeightedWaveEntry entry in sourceCandidates)
            {
                if (entry != null && entry.IsSelectable)
                {
                    workingCandidates.Add(entry);
                }
            }

            if (pool.SelectionCount == 0)
            {
                return true;
            }

            if (workingCandidates.Count == 0)
            {
                errorMessage = $"ウェーブプール「{poolName}」に有効な候補が存在しません。";
                return false;
            }

            if (!pool.AllowDuplicateSelection)
            {
                int uniqueWaveCount = CountUniqueWaves(workingCandidates);
                if (uniqueWaveCount < pool.SelectionCount)
                {
                    errorMessage =
                        $"{poolName}は重複禁止ですが、抽選可能なWaveが不足しています。" +
                        $"必要な候補数: {pool.SelectionCount}, " +
                        $"有効な候補数: {uniqueWaveCount}";
                    return false;
                }
            }


            // 重み付き抽選を行い、指定された数のWaveDataSOをdestinationに追加
            for (int i = 0; i < pool.SelectionCount; i++)
            {
                WeightedWaveEntry selectedEntry = SelectByWeight(workingCandidates, random);

                if (selectedEntry == null)
                {
                    errorMessage = $"ウェーブプール「{poolName}」の抽選に失敗しました。";
                    return false;
                }
                destination.Add(selectedEntry.WaveData);
                if (!pool.AllowDuplicateSelection)
                {
                    WaveDataSO selectedWave = selectedEntry.WaveData;

                    // 同じWaveDataSOが候補リストに複数登録されていても
                    // 重複禁止の場合は、同じWaveDataSOを再度選択しないように候補リストから削除する
                    workingCandidates.RemoveAll(entry => entry.WaveData == selectedWave);
                }
            }

            return true;
        }



        /// <summary>
        /// 重み付き抽選を行い、候補の中から1つのWeightedWaveEntryを選択する
        /// </summary>
        /// <param name="candidates">重み付き候補リスト</param>
        /// <param name="random">乱数生成</param>
        /// <returns>選択されたWeightedWaveEntry</returns>
        private static WeightedWaveEntry SelectByWeight(IReadOnlyList<WeightedWaveEntry> candidates, Random random)
        {
            long totalWeight = 0;

            // 候補の総重量を計算
            foreach (WeightedWaveEntry entry in candidates)
            {
                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
            {
                return null; // 総重量が0以下の場合はnullを返す
            }

            double randomValue = random.NextDouble() * totalWeight;
            long currentWeight = 0;

            // 候補の中からランダムにWeightedWaveEntryを選択
            foreach (WeightedWaveEntry entry in candidates)
            {
                currentWeight += entry.Weight;

                // ランダム値が現在の重みを下回った場合、そのエントリを選択
                if (randomValue < currentWeight)
                {
                    return entry;
                }
            }

            // 浮動小数点の丸め誤差に備えた保険
            return candidates[candidates.Count - 1];
        }



        /// <summary>
        /// 候補内に存在する異なるWaveDataSOの数を返す
        /// </summary>
        /// <param name="candidates">対象となる候補リスト</param>
        /// <returns></returns>
        private static int CountUniqueWaves(IReadOnlyList<WeightedWaveEntry> candidates)
        {
            HashSet<WaveDataSO> uniqueWaves = new ();

            foreach (WeightedWaveEntry entry in candidates)
            {
                uniqueWaves.Add(entry.WaveData);
            }

            return uniqueWaves.Count;
        }
    }
}
