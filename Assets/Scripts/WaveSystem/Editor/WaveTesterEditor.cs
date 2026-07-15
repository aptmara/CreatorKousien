// ------------------------------------------------------------
// File		: WaveTesterEditor.cs
// Summary	: WaveTesterのエディタ拡張
//
// Author	: [浅野勇生]
// Created	: 2026-07-15
//
// Notes	:
// - WaveTesterのテストやデバッグに使用されるクラスです。
// ------------------------------------------------------------
using UnityEngine;
using UnityEditor;

namespace Game.WaveSystem.Editor
{
    [CustomEditor(typeof(WaveTester))]
    public sealed class WaveTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            WaveTester tester = (WaveTester)target;

            EditorGUILayout.Space();

            // Play中のみ操作可能
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play中に、下のボタンでWaveをテストできます。", MessageType.Info);
                return;
            }

            // testWave未設定ならスポーン不可の警告
            using (new EditorGUI.DisabledScope(tester.TestWave == null))
            {
                if (GUILayout.Button("▶ このWaveをスポーン", GUILayout.Height(30)))
                {
                    tester.SpawnTestWave();
                }
            }

            if (tester.TestWave == null)
            {
                EditorGUILayout.HelpBox("TestWaveが設定されていません。", MessageType.Warning);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("🧹 全ての敵を削除", GUILayout.Height(24)))
            {
                tester.ClearAllEnemies();
            }

            // 実行状態の表示
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Waveの実行状態", tester.IsWaveRunning ? "実行中" : "停止中");
        }
    }
}
