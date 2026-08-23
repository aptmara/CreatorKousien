using Game.Gameplay.Roguelike.CombatPressure;
using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Roguelike.CombatPressure.Editor
{
    [CustomEditor(typeof(CombatPressureTester))]
    public sealed class CombatPressureTesterEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var tester = (CombatPressureTester)target;
            EditorGUILayout.Space();
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode中にコンボと状態異常の圧力を再現できます。", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(tester.Controller == null))
            {
                if (GUILayout.Button("コンボを適用"))
                    tester.ApplyCombo();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("+10 Combo"))
                    tester.AddCombo(10);
                if (GUILayout.Button("+50 Combo"))
                    tester.AddCombo(50);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("状態異常圧力を適用"))
                    tester.ApplyStatus();
                using (new EditorGUI.DisabledScope(tester.BindingTarget == null))
                {
                    if (GUILayout.Button("生成先モデルをルールへ紐付け"))
                        tester.BindFocusedCollectible();
                }
                if (GUILayout.Button("圧力をリセット"))
                    tester.ResetPressure();
            }

            if (tester.Controller == null)
                EditorGUILayout.HelpBox("CombatPressureController が見つかりません。", MessageType.Warning);
        }
    }
}
