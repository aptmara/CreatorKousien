// ------------------------------------------------------------
// File		: ValidationIssueView.cs
// Summary	: 検証結果をInspectorへ描画するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveDataSOとStageDataSOの両方で同じ見た目を使います！
// - PropertyPathを持つ検証結果は、該当項目まで展開できますお！
// - Infoは件数が多くなりやすいため、折りたたみで隠せるよーにしてまっす！
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.WaveSystem.Editor
{
    /// <summary>
    /// 検証結果のリストをInspectorへ描画
    /// </summary>
    public static class ValidationIssueView
    {
        /// <summary>
        /// Infoを表示するかどうかのEditorPrefsキー
        /// </summary>
        private const string ShowInfoPrefsKey = "Game.WaveSystem.Editor.ValidationIssueView.ShowInfo";

        /// <summary>
        /// 該当項目ボタンの横幅
        /// </summary>
        private const float ButtonWidth = 84f;


        /// <summary>
        /// 検証結果を描画する
        /// </summary>
        /// <param name="issues">描画する検証結果のリスト</param>
        /// <param name="serializedObject">該当項目を編集するためのSerializedObject</param>
        /// <param name="openedPropertyPath">インライン編集を開いている項目のパス</param>
        /// <returns>描画後にインライン編集を開いている項目のパス</returns>
        public static string Draw(IReadOnlyList<ValidationIssue> issues, SerializedObject serializedObject, string openedPropertyPath)
        {
            int errorCount = ValidationIssueUtility.CountOf(issues, IssueSeverity.Error);
            int warningCount = ValidationIssueUtility.CountOf(issues, IssueSeverity.Warning);
            int infoCount = ValidationIssueUtility.CountOf(issues, IssueSeverity.Info);

            DrawSummary(errorCount, warningCount);

            if (issues == null || issues.Count == 0)
            {
                return null;
            }

            bool showInfo = EditorPrefs.GetBool(ShowInfoPrefsKey, false);

            if (infoCount > 0)
            {
                bool nextShowInfo = EditorGUILayout.ToggleLeft($"参考情報も表示する (Info: {infoCount})", showInfo, EditorStyles.miniLabel);

                if (nextShowInfo != showInfo)
                {
                    EditorPrefs.SetBool(ShowInfoPrefsKey, nextShowInfo);
                    showInfo = nextShowInfo;
                }
            }

            string nextOpenedPath = openedPropertyPath;

            for (int i = 0; i < issues.Count; i++)
            {
                ValidationIssue issue = issues[i];

                if (issue.Severity == IssueSeverity.Info && !showInfo)
                {
                    continue;
                }

                nextOpenedPath = DrawIssue(issue, serializedObject, nextOpenedPath);
            }

            return nextOpenedPath;
        }


        private static void DrawSummary(int errorCount, int warningCount)
        {
            if (errorCount > 0)
            {
                EditorGUILayout.HelpBox($"エラー {errorCount}件。この設定ではPlayしてもWaveを開始できません", MessageType.Error);
                return;
            }

            if (warningCount > 0)
            {
                EditorGUILayout.HelpBox($"警告 {warningCount}件。Waveを開始できますが、意図しない挙動になる可能性があります", MessageType.Warning);
                return;
            }

            EditorGUILayout.HelpBox($"検証結果: 問題なし。Waveを開始できます", MessageType.Info);
        }


        /// <summary>
        /// 検証結果を描画する
        /// </summary>
        /// <param name="issue">描画する検証結果</param>
        /// <param name="serializedObject">該当項目へ移動するためのSerializedObject</param>
        /// <param name="openedPropertyPath">インライン編集を開いている項目のパス</param>
        /// <returns>描画後にインライン編集を開いている項目のパス</returns>
        private static string DrawIssue(ValidationIssue issue, SerializedObject serializedObject, string openedPropertyPath)
        {
            SerializedProperty property = null;

            if (serializedObject != null && !string.IsNullOrEmpty(issue.PropertyPath))
            {
                property = serializedObject.FindProperty(issue.PropertyPath);

                // パス指定のミスに速く気づけるように、取得できない場合は警告を出す
                if (property == null)
                {
                    Debug.LogWarning($"プロパティが見つかりませんでした: {issue.PropertyPath}");
                }
            }

            bool isOpened = property != null && openedPropertyPath == issue.PropertyPath;
            bool canPing = issue.Context != null;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.HelpBox(issue.Message, ToMessageType(issue.Severity));

            if (property != null || canPing)
            {
                EditorGUILayout.BeginVertical(GUILayout.Width(ButtonWidth));

                if (property != null && GUILayout.Button(isOpened ? "閉じる" : "ここで直す", EditorStyles.miniButton))
                {
                    // 同じ項目をもう一度押したら閉じる
                    openedPropertyPath = isOpened ? null : issue.PropertyPath;
                    isOpened = !isOpened;
                }

                if (canPing && GUILayout.Button("アセット", EditorStyles.miniButton))
                {
                    EditorGUIUtility.PingObject(issue.Context);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

            if (isOpened)
            {
                DrawInlineProperty(property);
            }

            return openedPropertyPath;
        }


        /// <summary>
        /// 指定されたSerializedPropertyをインラインで描画する
        /// </summary>
        /// <param name="property">描画するプロパティ</param>
        private static void DrawInlineProperty(SerializedProperty property)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUI.indentLevel++;

            // 配列要素や構造体の場合は、中身も展開して表示する
            EditorGUILayout.PropertyField(property, true);

            EditorGUI.indentLevel--;

            EditorGUILayout.EndVertical();
        }


        /// <summary>
        /// 深刻度をMessageTypeへ変換する
        /// </summary>
        /// <param name="severity">検証結果の深刻度</param>
        /// <returns>対応するメッセージタイプ</returns>
        private static MessageType ToMessageType(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Error:
                    return MessageType.Error;

                case IssueSeverity.Warning:
                    return MessageType.Warning;

                default:
                    return MessageType.Info;
            }
        }
    }
}
