using Game.Gameplay.Roguelike.CombatPressure;
using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Roguelike.CombatPressure.Editor
{
    [CustomEditor(typeof(CombatPressureRuleSet))]
    public sealed class CombatPressureRuleSetEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var ruleSet = (CombatPressureRuleSet)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("設定検証", EditorStyles.boldLabel);

            var messages = ruleSet.ValidateRules();
            if (messages.Count == 0)
            {
                EditorGUILayout.HelpBox("検証エラーはありません。", MessageType.Info);
            }
            else
            {
                foreach (string message in messages)
                    EditorGUILayout.HelpBox(message, MessageType.Warning);
            }

            if (GUILayout.Button("Combat Pressure Editorを開く"))
                CombatPressureEditorWindow.Open(ruleSet);
        }
    }
}
