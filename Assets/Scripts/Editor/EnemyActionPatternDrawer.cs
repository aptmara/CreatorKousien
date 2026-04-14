// ================================================================================
// File         : EnemyActionPatternDrawer.cs
// Author       : Iwai Shogo
//
// Description  : EnemyActionPatternクラスのインスペクター表示をカスタマイズするUI拡張。
// Created      : 2026-04-13
//
// Note         : 必ず「Editor」フォルダ内に配置してください。
// ================================================================================

using UnityEngine;
using UnityEditor;
using CreatorKousien.Data;

namespace CreatorKousien.EditorUI
{
    /// <summary>
    /// EnemyActionPatternのインスペクター表示を上書きするカスタムドロワー
    /// </summary>
    [CustomPropertyDrawer(typeof(EnemyActionPattern))]
    public class EnemyActionPatternDrawer : PropertyDrawer
    {
        // 1行の高さと余白
        private const float LineHeight = 20f;
        private const float Spacing = 4f;

        // 5x5グリッドの設定
        private const int GridSize = 5;
        private const float CellSize = 24f;

        /// <summary>
        /// インスペクター上での表示全体の高さを計算する
        /// </summary>
        /// <param name="property"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 8f; // 上の余白

            // 基本のプロパティ数 (Condition, CD, AttackType, Origin, TargetRule, Charge, Interrup) + ヘッダー2行
            height += 9 * (LineHeight + Spacing);

            // ブロック間の区切り隙間
            height += Spacing * 4;

            // TargetRule が LocalGridShapeの時だけ、5x5グリッドの高さを追加
            SerializedProperty targetRuleProp = property.FindPropertyRelative("TargetRule");
            if (targetRuleProp != null && targetRuleProp.enumValueIndex == (int)TargetSelection.LocalGridShape)
            {
                height += (LineHeight + Spacing) * 4;   // カスタム攻撃範囲のラベルの高さ
                height += (GridSize * CellSize);        // 5x5グリッド本体の高さ
                height += Spacing * 2;                  // グリッド下の余白
            }

            return height + 8f; // 下の余白
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Undo/Redo対応のためのブロック開始
            EditorGUI.BeginProperty(position, label, property);

            // リスト表示用に折りたためる枠を追加
            Rect boxRect = new Rect(position.x, position.y, position.width, position.height - Spacing);
            GUI.Box(boxRect, GUIContent.none, EditorStyles.helpBox);

            // 描画開始位置のセット
            Rect currentRect = new Rect(position.x + 8f, position.y + 8f, position.width - 16f, LineHeight);

            // --- プロパティの取得 ---
            SerializedProperty condProp = property.FindPropertyRelative("Condition");
            SerializedProperty condValProp = property.FindPropertyRelative("ConditionValue");
            SerializedProperty cdProp = property.FindPropertyRelative("CooldownTurns");
            SerializedProperty attackTypeProp = property.FindPropertyRelative("AttackType");
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

            EditorGUI.PropertyField(condRect, condProp, new GUIContent("条件タイプ"));
            EditorGUI.PropertyField(valRect, condValProp, GUIContent.none);
            AdvanceRect(ref currentRect);

            EditorGUI.PropertyField(currentRect, cdProp, new GUIContent("クールダウン"));
            AdvanceRect(ref currentRect);

            // 区切り隙間
            currentRect.y += Spacing * 2;

            // 2. 攻撃内容ブロック
            // ============================================================
            EditorGUI.LabelField(currentRect, "▼ 攻撃内容", EditorStyles.boldLabel);
            AdvanceRect(ref currentRect);

            EditorGUI.PropertyField(currentRect, attackTypeProp, new GUIContent("攻撃種別"));
            AdvanceRect(ref currentRect);
            EditorGUI.PropertyField(currentRect, originProp, new GUIContent("基準点 (Origin)"));
            AdvanceRect(ref currentRect);
            EditorGUI.PropertyField(currentRect, targetRuleProp, new GUIContent("範囲ルール"));
            AdvanceRect(ref currentRect);
            EditorGUI.PropertyField(currentRect, chargeProp, new GUIContent("猶予ターン (Charge)"));
            AdvanceRect(ref currentRect);
            EditorGUI.PropertyField(currentRect, interruptProp, new GUIContent("キャンセル可能か"));
            AdvanceRect(ref currentRect);

            // 3. 5x5グリッドブロック
            // ============================================================
            if (targetRuleProp.enumValueIndex == (int)TargetSelection.LocalGridShape)
            {
                currentRect.y += Spacing * 2;

                // タイトル表示
                TargetOrigin currentOrigin = (TargetOrigin)originProp.enumValueIndex;
                EditorGUI.LabelField(currentRect, $"▼ カスタム攻撃範囲 (★ = 基準点)", EditorStyles.boldLabel);
                AdvanceRect(ref currentRect);

                // 全クリア & 反転ボタン
                Rect btnClearRect = new Rect(currentRect.x, currentRect.y, currentRect.width * 0.48f, currentRect.height);
                Rect btnInvertRect = new Rect(currentRect.x + currentRect.width * 0.52f, currentRect.y, currentRect.width * 0.48f, currentRect.height);

                if (gridProp.arraySize != 25) gridProp.arraySize = 25;

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

                // 方向ガイド
                GUIStyle centerLabel = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
                EditorGUI.LabelField(currentRect, "▲ 敵陣 / 画面奥 (Y-) ▲", centerLabel);
                currentRect.y += LineHeight;

                // グリッド描画のオフセット計算
                float gridTotalWidth = GridSize * CellSize;
                float startX = currentRect.x + (currentRect.width - gridTotalWidth) / 2f;
                Color defaultBgColor = GUI.backgroundColor;

                // 基準点のインデックスを動的に取得
                int centerIndex = GetCenterIndex(currentOrigin);

                // 5x5グリッド描画
                for (int y = 0; y < GridSize; y++)
                {
                    for (int x = 0; x < GridSize; x++)
                    {
                        int index = y * GridSize + x;
                        SerializedProperty cellProp = gridProp.GetArrayElementAtIndex(index);

                        // セルの座標を計算
                        Rect cellRect = new Rect(startX + x * CellSize, currentRect.y + y * CellSize, CellSize - 2f, CellSize - 2f);

                        // 視覚的なフィードバック
                        bool isCenter = (index == centerIndex); // 中央
                        bool isActive = cellProp.boolValue;

                        if (isActive)
                        {
                            // 赤: 攻撃範囲になっているマス
                            GUI.backgroundColor = isCenter ? new Color(1f, 0.4f, 1f) : new Color(1f, 0.4f, 0.4f);
                        }
                        else
                        {
                            // 暗いグレー: 何もないマス
                            // 水色: 中央
                            GUI.backgroundColor = isCenter ? new Color(0.4f, 0.8f, 1f) : new Color(0.3f, 0.3f, 0.3f);
                        }

                        // ボタンスタイルでトグルを描画
                        string cellLabel = isCenter ? "★" : "";
                        cellProp.boolValue = GUI.Toggle(cellRect, cellProp.boolValue, cellLabel, "Button");
                    }
                }

                // 色を元に戻す
                GUI.backgroundColor = defaultBgColor;
                currentRect.y += (GridSize * CellSize) + Spacing;

                // 自陣側ラベル
                EditorGUI.LabelField(currentRect, "▼ 自陣 / プレイヤー側 (Y+) ▼", centerLabel);
                AdvanceRect(ref currentRect);
            }

            EditorGUI.EndProperty();
        }

        /// <summary>
        /// Y座標を次の行へ進めるヘルパー
        /// </summary>
        /// <param name="currentRect"></param>
        private void AdvanceRect(ref Rect currentRect)
        {
            currentRect.y += LineHeight + Spacing;
        }

        /// <summary>
        /// 基準点のインデックスを動的に返します。
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        private int GetCenterIndex(TargetOrigin origin)
        {
            switch (origin)
            {
                case TargetOrigin.FrontRowCenter: return 2;     // Top-Middle
                case TargetOrigin.BackRowCenter: return 22;     // Bottom-Middle
                case TargetOrigin.LeftEdgeCenter: return 10;    // Left-Middle
                case TargetOrigin.RightEdgeCenter: return 14;   // Right-Middle
                default: return 12;                             // Center
            }
        }
    }
}
