// ------------------------------------------------------------
// File		: WaveDataSOEditor.cs
// Summary	: WaveDataSOのInspector拡張
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - 検証はWaveValidator、見積りはWaveMetricsCalculatorに任せます。
// - このクラスは判定を持たず、描画だけを担当します。
// - 「検証結果が緑」⟺「Playでウェーブを開始できる」を保証します。
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.WaveSystem.Editor
{
    [CustomEditor(typeof(WaveDataSO))]
    public sealed class WaveDataSOEditor : UnityEditor.Editor
    {
        // 毎フレーム使いまわす
        private readonly List<ValidationIssue> issues = new();
        private readonly List<SpawnEvent> spawnEvents = new();

        // インライン編集を開いている項目のパス
        private string openedPropertyPath;

        /// <summary>
        /// InspectorのGUIを描画する
        /// </summary>
        public override void OnInspectorGUI()
        {
            WaveDataSO waveData = (WaveDataSO)target;

            WaveValidator.Validate(waveData, issues);

            // SpawnEventのリストを作る
            WaveMetrics metrics = WaveMetricsCalculator.Calculate(waveData, spawnEvents);

            // インライン編集とフィールド描画で同じSerializedObjectを使うので、Updateを呼んで最新化しておく
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            // WaveDataSOのInspectorを描画する
            EditorGUILayout.LabelField("検証結果", WaveEditorStyles.SectionHeader);
            openedPropertyPath = ValidationIssueView.Draw(issues, serializedObject, openedPropertyPath);

            EditorGUILayout.LabelField("このウェーブの重さ", WaveEditorStyles.SectionHeader);
            DrawMetrics(metrics);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("タイムライン", WaveEditorStyles.SectionHeader);
            WaveTimelineView.Draw(waveData, spawnEvents, metrics);

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
        /// 見積もり値を一覧で描画する
        /// </summary>
        /// <param name="metrics">描画する見積もり値</param>
        private static void DrawMetrics(WaveMetrics metrics)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            string enemyText = metrics.BossCount > 0
                ? $"敵: {metrics.TotalEnemyCount} + ボス: {metrics.BossCount}"
                : $"敵: {metrics.TotalEnemyCount}";

            DrawMetricsRow("敵の総数", enemyText, "このWaveで出現する敵の合計。WaveGroup構成のSpawn Countを変えると増減するぜよ！");

            DrawMetricsRow("Group数", $"{metrics.GroupCount} 個", "敵のまとまりの数。Groupを増やすと、出現を段階に分けられますお！");

            DrawMetricsRow("HP合計", metrics.TotalHp.ToString("N0"), "倒しきるのに必要な総ダメージ量。Hp Rateをあげるとここが増えます！！");

            if (metrics.TotalBarrier > 0f)
            {
                DrawMetricsRow("バリア合計", metrics.TotalBarrier.ToString("N0"), "バリアゲージの合計です。Barrier Rateをあげるとここが増えます！！");
            }

            DrawMetricsRow("経験値合計", metrics.TotalExp.ToString("N0"), "倒したときに得られる経験値の合計。敵側のExp Rateをあげるとここが増えます！！");

            DrawMetricsRow("防衛ラインへのダメージ", $"{metrics.AttackPressure:0.0} / 秒", "すべての敵が生存し続けた場合に、防衛ラインが毎秒受けるダメージ。実数値じゃなくて理論値ですっ！");

            string durationText = metrics.IsDurationConfirmed
                ? $"{metrics.MinDuration:0.0} 秒"
                : $"{metrics.MinDuration:0.0} 秒 以上";

            DrawMetricsRow("最後の敵が出るまで", durationText, "Wave開始から最後の一体が出現するまでの時間。進行条件に「敵の全滅」が含まれる場合は撃破時間がわかんないから以上って出るようにしてます！");

            EditorGUILayout.EndVertical();

            if (!metrics.IsDurationConfirmed)
            {
                EditorGUILayout.LabelField("※「最後の敵が出るまで」の時間は、進行条件に「敵の全滅」が含まれる場合は撃破時間がわかんないから「理論上の最短値」が出るようにしてます！",WaveEditorStyles.CaptionLabel);
            }
        }



        /// <summary>
        /// 見積もり値を1行描画する
        /// </summary>
        /// <param name="label">項目名</param>
        /// <param name="value">値</param>
        /// <param name="tooltip">ツールチップ</param>
        private static void DrawMetricsRow(string label, string value, string tooltip)
        {
            EditorGUILayout.LabelField(new GUIContent(label, tooltip), new GUIContent(value, tooltip), WaveEditorStyles.ValueLabel);
        }
    }
}
