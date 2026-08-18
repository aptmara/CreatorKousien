using Game.Gameplay.Roguelike.CombatPressure;
using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Roguelike.CombatPressure.Editor
{
    public sealed class CombatPressureEditorWindow : EditorWindow
    {
        private CombatPressureRuleSet _ruleSet;
        private int _previewCombo = 100;
        private int _previewAffectedEnemies = 5;
        private int _previewStacks = 20;
        private Vector2 _scroll;

        [MenuItem("Game/Roguelike/Combat Pressure Editor")]
        private static void OpenFromMenu()
        {
            Open(Selection.activeObject as CombatPressureRuleSet);
        }

        public static void Open(CombatPressureRuleSet ruleSet)
        {
            var window = GetWindow<CombatPressureEditorWindow>("Combat Pressure");
            window._ruleSet = ruleSet;
            window.minSize = new Vector2(500f, 340f);
            window.Show();
        }

        private void OnGUI()
        {
            _ruleSet = (CombatPressureRuleSet)EditorGUILayout.ObjectField("Rule Set", _ruleSet, typeof(CombatPressureRuleSet), false);
            if (_ruleSet == null)
            {
                EditorGUILayout.HelpBox("CombatPressureRuleSet を選択してください。", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ライブプレビュー入力", EditorStyles.boldLabel);
            _previewCombo = EditorGUILayout.IntSlider("全体コンボ", _previewCombo, 0, 300);
            _previewAffectedEnemies = EditorGUILayout.IntSlider("状態異常中の敵", _previewAffectedEnemies, 0, 30);
            _previewStacks = EditorGUILayout.IntSlider("総スタック", _previewStacks, 0, 100);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("ルール評価", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (CombatPressureRule rule in _ruleSet.Rules)
            {
                if (rule == null)
                    continue;

                int affected = rule.Source == CombatPressureSource.Status ? _previewAffectedEnemies : 0;
                int stacks = rule.Source == CombatPressureSource.Status ? _previewStacks : 0;
                int value = rule.GetMetricValue(_previewCombo, affected, stacks);
                bool active = rule.Enabled && value >= rule.Threshold;
                MessageType messageType = active ? MessageType.Info : MessageType.None;
                EditorGUILayout.HelpBox(
                    $"{rule.DisplayName}: {value} / {rule.Threshold}  {(active ? "発動中" : "未発動")}",
                    messageType);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("選択中のRule SetをInspectorで表示"))
            {
                Selection.activeObject = _ruleSet;
                EditorGUIUtility.PingObject(_ruleSet);
            }
        }
    }
}
