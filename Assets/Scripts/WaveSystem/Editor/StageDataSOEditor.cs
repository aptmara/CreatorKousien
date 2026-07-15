// ------------------------------------------------------------
// File		: StageDataSOEditor.cs
// Summary	: StageDataSOのInspectorの拡張
//
// Author	: [浅野勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 「Inspectorが緑」⟺「Playで必ずWave生成成功」を目標に検証します。
// - 検証条件は StageWaveSequenceBuilder と同じにします。
// - ランタイムのコードは変更せず、既存プロパティから計算します。
// ------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Game.WaveSystem.Editor
{
    [CustomEditor(typeof(StageDataSO))]
    public sealed class StageDataSOEditor : UnityEditor.Editor
    {
        /// <summary>
        /// インスペクターのGUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            // 通常のフィールドはそのまま描画
            DrawDefaultInspector();

            StageDataSO stage = (StageDataSO)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Wave構成チェック", EditorStyles.boldLabel);

            DrawWaveCountSummary(stage);
            DrawBossCheck(stage);

            DrawPoolSection(stage.EarlyWavePool,  "序盤WavePool");
            DrawPoolSection(stage.MiddleWavePool, "中盤WavePool");
            DrawPoolSection(stage.LateWavePool,   "終盤WavePool");
        }


        /// <summary>
        /// 合計Wave数のサマリーを表示
        /// </summary>
        /// <param name="stage"></param>
        private static void DrawWaveCountSummary(StageDataSO stage)
        {
            int early = stage.EarlyWavePool != null ? stage.EarlyWavePool.SelectionCount : 0;
            int middle = stage.MiddleWavePool != null ? stage.MiddleWavePool.SelectionCount : 0;
            int late = stage.LateWavePool != null ? stage.LateWavePool.SelectionCount : 0;
            int boss = stage.BossWave != null ? 1 : 0;

            string breakdown = $"序盤 {early} + 中盤 {middle} + 終盤 {late} + Boss {boss} " + $"= 合計 {stage.TotalWaveCount} / 必要 {StageDataSO.RequiredWaveCount}";

            if (stage.HasRequiredWaveCount)
            {
                EditorGUILayout.HelpBox($"✅{breakdown}", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox($"⚠ 合計Wave数が{StageDataSO.RequiredWaveCount} になっていません\n{breakdown}", MessageType.Error);
            }
        }


        /// <summary>
        /// WavePoolの検証を行い、警告やエラーを表示
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="poolName"></param>
        private static void DrawPoolValidation(WavePoolData pool, string poolName)
        {
            if (pool == null)
            {
                EditorGUILayout.HelpBox($"{poolName}が未設定です。Wave生成に失敗する可能性があります", MessageType.Warning);
                return;
            }

            IReadOnlyList<WeightedWaveEntry> candidates = pool.Candidates;

            // 有効候補とユニークWave数をカウント
            int selectableCount = 0;
            bool hasEmptyWaveData = false;
            HashSet<WaveDataSO> uniqueWaves = new HashSet<WaveDataSO>();

            if (candidates != null)
            {
                foreach (WeightedWaveEntry entry in candidates)
                {
                    if (entry == null) continue;

                    // Enabledなのに WaveData 未設定 → 警告用に検出
                    if (entry.IsEnabled && entry.WaveData == null)
                    {
                        hasEmptyWaveData = true;
                    }

                    // 実行時と同じ IsSelectable で有効候補を集計
                    if (entry.IsSelectable)
                    {
                        selectableCount++;
                        uniqueWaves.Add(entry.WaveData);
                    }
                }

                int need = pool.SelectionCount;


                // --- 検証 ---

                if (need == 0)
                {
                    EditorGUILayout.HelpBox($"{poolName}のSelectionCountが0です。Wave生成に失敗する可能性があります", MessageType.Info);
                    return;
                }


                // 有効候補0
                if (selectableCount == 0)
                {
                    EditorGUILayout.HelpBox($"{poolName}の有効候補が0です。Wave生成に失敗する可能性があります", MessageType.Error);
                    return;
                }

                // 有効候補が必要数に足りない
                if (selectableCount < need)
                {
                    EditorGUILayout.HelpBox($"{poolName}の有効候補が必要数({need})に足りません。Wave生成に失敗する可能性があります", MessageType.Error);
                }
                else if (!pool.AllowDuplicateSelection && uniqueWaves.Count < need)
                {
                    EditorGUILayout.HelpBox($"{poolName}：重複禁止ですが、異なるWaveが {uniqueWaves.Count} 種しかなく {need} つ選べません。" + $"Wave Dataを増やすか、Allow Duplicate Selection をONにしてください。", MessageType.Error);
                }

                // WaveData未設定の候補
                if (hasEmptyWaveData)
                {
                    EditorGUILayout.HelpBox($"{poolName}の有効候補の中にWaveDataが未設定のものがあります。Wave生成に失敗する可能性があります", MessageType.Error);
                }
            }
        }


        /// <summary>
        /// 1つのPoolについて、検証と抽選確率の両方を描画
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="poolName"></param>
        private static void DrawPoolSection(WavePoolData pool, string poolName)
        {
            DrawPoolValidation(pool, poolName);
            DrawPoolWeights(pool, poolName);
        }


        private static void DrawPoolWeights(WavePoolData pool, string poolName)
        {
            if (pool == null || pool.Candidates == null)
            {
                return;
            }

            IReadOnlyList<WeightedWaveEntry> candidates = pool.Candidates;

            // 有効候補のWeightの合計を計算
            long totalWeight = 0;
            foreach (WeightedWaveEntry entry in candidates)
            {
                if (entry != null && entry.IsSelectable)
                {
                    totalWeight += entry.Weight;
                }
            }

            // 0除算ガード
            if (totalWeight <= 0)
                return;

            EditorGUILayout.LabelField($"{poolName}の有効候補のWeight合計: {totalWeight}", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            foreach (WeightedWaveEntry entry in candidates)
            {
                if (entry == null || !entry.IsSelectable)
                    continue;

                float percent = (float)entry.Weight / totalWeight * 100f;

                EditorGUILayout.LabelField(entry.WaveData.name, $"Weight {entry.Weight} → {percent: 0.0}%");
            }
            EditorGUI.indentLevel--;

            if (!pool.AllowDuplicateSelection)
            {
                EditorGUILayout.LabelField("※重複禁止のため、同じWaveが複数回選ばれることはありません", EditorStyles.miniLabel);
            }
        }


        private static void DrawBossCheck(StageDataSO stage)
        {
            if (stage.BossWave == null)
            {
                EditorGUILayout.HelpBox("最終Wave(Boss)が未設定です。Wave10が生成できません", MessageType.Error);
            }
        }
    }
}

