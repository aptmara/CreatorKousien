// ------------------------------------------------------------
// File		: StageDataSOEditor.cs
// Summary	: StageDataSOのInspectorの拡張
//
// Author	: [浅野勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 甲斐のミスをなくすべく「Inspectorが緑」⟺「Playで必ずWave生成成功」を目標に検証します！
// - 検証条件は StageWaveSequenceBuilder と同じにします！！
// - ランタイムのコードは変更せず、既存プロパティから計算する方針でつくります！
// ------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Game.WaveSystem.Editor
{
    /// <summary>
    /// StageDataSOのInspector拡張
    /// </summary>
    [CustomEditor(typeof(StageDataSO))]
    public sealed class StageDataSOEditor : UnityEditor.Editor
    {
        /// <summary>
        /// 抽選確率バーの高さ
        /// </summary>
        private const float WeightBarHeight = 12f;

        /// <summary>
        /// 毎フレーム使いまわして、GCの発生をおさえる
        /// </summary>
        private readonly List<ValidationIssue> issues = new();

        /// <summary>
        /// インライン編集を開いている項目のパス
        /// </summary>
        private string openedPropertyPath;

        /// <summary>
        /// インスペクターのGUI
        /// </summary>
        public override void OnInspectorGUI()
        {
            StageDataSO stage = (StageDataSO)target;

            StagePlanValidator.Validate(stage, issues);

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("検証結果", WaveEditorStyles.SectionHeader);
            openedPropertyPath = ValidationIssueView.Draw(issues, serializedObject, openedPropertyPath);

            EditorGUILayout.LabelField("抽選確率", WaveEditorStyles.SectionHeader);
            DrawPoolWeights(stage.EarlyWavePool, "序盤WavePool");
            DrawPoolWeights(stage.MiddleWavePool, "中盤WavePool");
            DrawPoolWeights(stage.LateWavePool, "終盤WavePool");

            EditorGUILayout.LabelField("設定", WaveEditorStyles.SectionHeader);
            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();

            if (EditorGUI.EndChangeCheck())
            {
                // 値が変わったら、上の検証結果と見積もりを更新するために再描画する
                Repaint();
            }
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

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 各区間のWave数を表示
            EditorGUILayout.LabelField(
                new GUIContent("序盤 / 中盤 / 終盤 / Boss", "各区間から選ばれるWaveの数です。各PoolのSelection Countで変えられます。"),
                new GUIContent($"{early} / {middle} / {late} / {boss}"),
                WaveEditorStyles.ValueLabel);

            // 合計Wave数を表示
            // TODO: 現在は10固定だけど、いつかね、変わるときがくるかもしれんから置いておくぜよ
            EditorGUILayout.LabelField(
                new GUIContent("合計Wave数", $"このゲームの1Stageは{StageDataSO.RequiredWaveCount}Wave構成です。"),
                new GUIContent($"{stage.TotalWaveCount} / {StageDataSO.RequiredWaveCount}"),
                WaveEditorStyles.ValueLabel);

            EditorGUILayout.EndVertical();
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
            if (pool == null || pool.Candidates == null || pool.SelectionCount <= 0)
            {
                return;
            }

            IReadOnlyList<WeightedWaveEntry> candidates = pool.Candidates;

            // 有効候補のWeightの合計を計算
            long totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
            {
                WeightedWaveEntry entry = candidates[i];

                if (entry != null && entry.IsSelectable)
                {
                    totalWeight += entry.Weight;
                }
            }

            // 0除算ガード
            if (totalWeight <= 0)
                return;

            string duplicateText = pool.AllowDuplicateSelection ? "重複可" : "重複不可";

            EditorGUILayout.LabelField($"{poolName}の有効候補のWeight合計: {totalWeight}", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            int colorIndex = 0;

            for (int i = 0; i < candidates.Count; i++)
            {
                WeightedWaveEntry entry = candidates[i];

                if (entry == null || !entry.IsSelectable)
                    continue;

                float ratio = (float)entry.Weight / totalWeight;

                DrawWeightRow(entry, ratio, colorIndex);

                colorIndex++;
            }

            EditorGUI.indentLevel--;
        }


        /// <summary>
        /// 1つの候補の抽選確率バーを描画
        /// </summary>
        /// <param name="entry">描画対象の候補</param>
        /// <param name="ratio">確率の割合</param>
        /// <param name="colorIndex">色のインデックス</param>
        private static void DrawWeightRow(WeightedWaveEntry entry, float ratio, int colorIndex)
        {
            string waveName = string.IsNullOrWhiteSpace(entry.WaveData.WaveName) ? entry.WaveData.name : entry.WaveData.WaveName;

            Rect row = EditorGUILayout.GetControlRect(true, WeightBarHeight + 2f);

            row = EditorGUI.IndentedRect(row);

            // --- 左: Wave名 ---
            Rect nameRect = new Rect(row.x, row.y, row.width * 0.42f, row.height);

            GUI.Label(nameRect, new GUIContent(waveName, waveName), EditorStyles.miniLabel);


            // --- 中央: 確率バー ---
            Rect barArea = new Rect(nameRect.xMax + 4f, row.y + 1f, row.width * 0.36f, WeightBarHeight);

            Color background = WaveEditorStyles.PanelBackgroundColor;

            EditorGUI.DrawRect(barArea, background);

            Rect fill = new Rect(barArea.x, barArea.y, barArea.width * Mathf.Clamp01(ratio), barArea.height);

            EditorGUI.DrawRect(fill, WaveEditorStyles.GetGroupColor(colorIndex));


            // --- 右: 確率テキスト ---
            Rect valueRect = new Rect(barArea.xMax + 4f, row.y, row.xMax - barArea.xMax - 4f, row.height);

            GUI.Label(valueRect, $"{ratio * 100f:0.0}%  (Weight {entry.Weight})", EditorStyles.miniLabel);
        }


        /// <summary>
        /// BossWaveの設定を検証し、未設定の場合はエラーを表示
        /// </summary>
        /// <param name="stage">ステージのデータSO</param>
        private static void DrawBossCheck(StageDataSO stage)
        {
            if (stage.BossWave == null)
            {
                EditorGUILayout.HelpBox("最終Wave(Boss)が未設定です。Wave10が生成できません", MessageType.Error);
            }
        }
    }
}

