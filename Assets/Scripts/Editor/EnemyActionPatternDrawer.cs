using UnityEngine;
using UnityEditor;
using CreatorKousien.Data;

namespace CreatorKousien.EditorUI
{
    [CustomPropertyDrawer(typeof(EnemyActionPattern))]
    public class EnemyActionPatternDrawer : PropertyDrawer
    {
        private const float LineHeight = 20f;
        private const float Spacing = 4f;
        private const int GridSize = 5;
        private const float CellSize = 24f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 10f; // 上の余白

            // 基本のプロパティ数: ヘッダー2行 + Cond, CD, Weight, Type, Dmg, Hit, Origin, Target, Charge, Interrupt (計12行)
            height += 12 * (LineHeight + Spacing);
            height += Spacing * 4;

            SerializedProperty targetRuleProp = property.FindPropertyRelative("TargetRule");
            if (targetRuleProp != null && targetRuleProp.enumValueIndex == (int)TargetSelection.LocalGridShape)
            {
                height += (LineHeight + Spacing) * 4;   // カスタム攻撃範囲のラベル等
                height += (GridSize * CellSize);        // 5x5グリッド本体
                height += Spacing * 2;                  // グリッド下の余白
            }

            return height + 10f; // 下の余白
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // 背景ボックス
            Rect boxRect = new Rect(position.x, position.y, position.width, position.height - Spacing);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            Rect currentRect = new Rect(position.x + 8f, position.y + 8f, position.width - 16f, LineHeight);

            // --- プロパティの取得 ---
            SerializedProperty condProp = property.FindPropertyRelative("Condition");
            SerializedProperty condValProp = property.FindPropertyRelative("ConditionValue");
            SerializedProperty cdProp = property.FindPropertyRelative("CooldownTurns");
            SerializedProperty weightProp = property.FindPropertyRelative("Weight");

            // 構造体の中身はドット繋ぎ
            SerializedProperty attackTypeProp = property.FindPropertyRelative("AttackInfo.Type");
            SerializedProperty damageProp = property.FindPropertyRelative("AttackInfo.DamageMultiplier");
            SerializedProperty hitCountProp = property.FindPropertyRelative("AttackInfo.HitCount");

            SerializedProperty originProp = property.FindPropertyRelative("OriginRule");
            SerializedProperty targetRuleProp = property.FindPropertyRelative("TargetRule");
            SerializedProperty chargeProp = property.FindPropertyRelative("ChargeTurns");
            SerializedProperty interruptProp = property.FindPropertyRelative("IsInterruptible");
            SerializedProperty gridProp = property.FindPropertyRelative("LocalTargetGrid");

            // 1. 発動条件ブロック
            // ============================================================
            EditorGUI.LabelField(currentRect, "▼ 発動条件", EditorStyles.boldLabel);
            AdvanceRect(ref currentRect);

            Rect condRect = new Rect(currentRect.x, currentRect.y, currentRect.width * 0.6f, currentRect.height);
            Rect valRect = new Rect(currentRect.x + currentRect.width * 0.6f + 4f, currentRect.y, currentRect.width * 0.4f - 4f, currentRect.height);

            if (condProp != null) EditorGUI.PropertyField(condRect, condProp, new GUIContent("条件タイプ"));
            if (condValProp != null) EditorGUI.PropertyField(valRect, condValProp, GUIContent.none);
            AdvanceRect(ref currentRect);

            if (cdProp != null) EditorGUI.PropertyField(currentRect, cdProp, new GUIContent("クールダウン"));
            AdvanceRect(ref currentRect);

            if (weightProp != null) EditorGUI.PropertyField(currentRect, weightProp, new GUIContent("選ばれやすさ (Weight)"));
            AdvanceRect(ref currentRect);

            currentRect.y += Spacing * 2; // 区切り

            // 2. 攻撃内容ブロック
            // ============================================================
            EditorGUI.LabelField(currentRect, "▼ 攻撃内容", EditorStyles.boldLabel);
            AdvanceRect(ref currentRect);

            if (attackTypeProp != null) EditorGUI.PropertyField(currentRect, attackTypeProp, new GUIContent("攻撃種別"));
            AdvanceRect(ref currentRect);

            if (damageProp != null) EditorGUI.PropertyField(currentRect, damageProp, new GUIContent("威力倍率"));
            AdvanceRect(ref currentRect);

            if (hitCountProp != null) EditorGUI.PropertyField(currentRect, hitCountProp, new GUIContent("ヒット数"));
            AdvanceRect(ref currentRect);

            if (originProp != null) EditorGUI.PropertyField(currentRect, originProp, new GUIContent("基準点 (Origin)"));
            AdvanceRect(ref currentRect);

            if (targetRuleProp != null) EditorGUI.PropertyField(currentRect, targetRuleProp, new GUIContent("範囲ルール"));
            AdvanceRect(ref currentRect);

            if (chargeProp != null) EditorGUI.PropertyField(currentRect, chargeProp, new GUIContent("猶予ターン (Charge)"));
            AdvanceRect(ref currentRect);

            if (interruptProp != null) EditorGUI.PropertyField(currentRect, interruptProp, new GUIContent("キャンセル可能か"));
            AdvanceRect(ref currentRect);

            // 3. 5x5グリッドブロック
            // ============================================================
            if (targetRuleProp != null && targetRuleProp.enumValueIndex == (int)TargetSelection.LocalGridShape)
            {
                currentRect.y += Spacing * 2;

                TargetOrigin currentOrigin = (TargetOrigin)originProp.enumValueIndex;
                EditorGUI.LabelField(currentRect, $"▼ カスタム攻撃範囲 (★ = 基準点)", EditorStyles.boldLabel);
                AdvanceRect(ref currentRect);

                Rect btnClearRect = new Rect(currentRect.x, currentRect.y, currentRect.width * 0.48f, currentRect.height);
                Rect btnInvertRect = new Rect(currentRect.x + currentRect.width * 0.52f, currentRect.y, currentRect.width * 0.48f, currentRect.height);

                if (gridProp != null && gridProp.arraySize != 25) gridProp.arraySize = 25;

                if (GUI.Button(btnClearRect, "全クリア (Clear)"))
                {
                    for (int i = 0; i < 25; i++) gridProp.GetArrayElementAtIndex(i).boolValue = false;
                }
                if (GUI.Button(btnInvertRect, "反転 (Invert)"))
                {
                    for (int i = 0; i < 25; i++)
                    {
                        var cell = gridProp.GetArrayElementAtIndex(i);
                        cell.boolValue = !cell.boolValue;
                    }
                }
                AdvanceRect(ref currentRect);

                GUIStyle centerLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUI.LabelField(currentRect, "▲ 敵陣 / 画面奥 (Y-) ▲", centerLabel);
                currentRect.y += LineHeight;

                float gridTotalWidth = GridSize * CellSize;
                float startX = currentRect.x + (currentRect.width - gridTotalWidth) / 2f;
                Color defaultBgColor = GUI.backgroundColor;
                int centerIndex = GetCenterIndex(currentOrigin);

                for (int y = 0; y < GridSize; y++)
                {
                    for (int x = 0; x < GridSize; x++)
                    {
                        int index = y * GridSize + x;
                        SerializedProperty cellProp = gridProp.GetArrayElementAtIndex(index);
                        Rect cellRect = new Rect(startX + x * CellSize, currentRect.y + y * CellSize, CellSize - 2f, CellSize - 2f);

                        bool isCenter = (index == centerIndex);
                        if (cellProp.boolValue)
                        {
                            GUI.backgroundColor = isCenter ? new Color(1f, 0.4f, 1f) : new Color(1f, 0.4f, 0.4f);
                        }
                        else
                        {
                            GUI.backgroundColor = isCenter ? new Color(0.4f, 0.8f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                        }

                        string cellLabel = isCenter ? "★" : "";
                        cellProp.boolValue = GUI.Toggle(cellRect, cellProp.boolValue, cellLabel, "Button");
                    }
                }

                GUI.backgroundColor = defaultBgColor;
                currentRect.y += (GridSize * CellSize) + Spacing;

                EditorGUI.LabelField(currentRect, "▼ 自陣 / プレイヤー側 (Y+) ▼", centerLabel);
                AdvanceRect(ref currentRect);
            }

            EditorGUI.EndProperty();
        }

        private void AdvanceRect(ref Rect currentRect)
        {
            currentRect.y += LineHeight + Spacing;
        }

        private int GetCenterIndex(TargetOrigin origin)
        {
            switch (origin)
            {
                case TargetOrigin.FrontRowCenter: return 2;
                case TargetOrigin.BackRowCenter: return 22;
                case TargetOrigin.LeftEdgeCenter: return 10;
                case TargetOrigin.RightEdgeCenter: return 14;
                default: return 12;
            }
        }
    }
}
