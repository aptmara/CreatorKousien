// ------------------------------------------------------------
// File		: ValidationIssue.cs
// Summary	: WaveやStageの検証結果1件を表します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - RuntimeとEditorの両方で同じ型を使い、判定ルールを一本化します！
// - PropertyPathはInspectorで該当項目へ誘導するために使います！
// - Runtime側ではPropertyPathを無視しておけまる
// ------------------------------------------------------------
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// 検証結果1件を表す読み取り専用データ
    /// </summary>
    public readonly struct ValidationIssue
    {
        public readonly IssueSeverity Severity;        ///< 検証結果の深刻度を取得します。
        public readonly string Message;                ///< 検証結果のメッセージを取得します。
        public readonly string PropertyPath;           ///< 検証対象のプロパティパスを取得します。
        public readonly Object Context;                ///< 検証対象のUnityEngine.Objectを取得します。

        public ValidationIssue(IssueSeverity severity, string message, string propertyPath = null, Object context = null)
        {
            Severity = severity;
            Message = message;
            PropertyPath = propertyPath;
            Context = context;
        }


        public static ValidationIssue Info(string message, string propertyPath = null, Object context = null)
            => new ValidationIssue(IssueSeverity.Info, message, propertyPath, context);

        public static ValidationIssue Warning(string message, string propertyPath = null, Object context = null)
            => new ValidationIssue(IssueSeverity.Warning, message, propertyPath, context);

        public static ValidationIssue Error(string message, string propertyPath = null, Object context = null)
            => new ValidationIssue(IssueSeverity.Error, message, propertyPath, context);
    }


    /// <summary>
    /// ValidationIssueのユーティリティクラスです
    /// </summary>
    public static class ValidationIssueUtility
    {
        /// <summary>
        /// Errorが1件でも含まれているかどうかを判定します。
        /// </summary>
        /// <param name="issues">検証結果リスト</param>
        /// <returns>Errorが含まれる場合はtrue</returns>
        public static bool HasError(IReadOnlyList<ValidationIssue> issues)
        {
            if (issues == null)
                return false;

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == IssueSeverity.Error)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 指定した深刻度の件数を返します。
        /// </summary>
        /// <param name="issues">検証結果リスト</param>
        /// <param name="severity">数えたい深刻度</param>
        /// <returns>該当件数</returns>
        public static int CountOf(IReadOnlyList<ValidationIssue> issues, IssueSeverity severity)
        {
            if (issues == null)
                return 0;

            int count = 0;

            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == severity)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 検証結果リストを文字列に変換します。
        /// </summary>
        /// <param name="issues">検証結果リスト</param>
        /// <returns>改行区切りのエラーメッセージ</returns>
        public static string BuildErrorText(IReadOnlyList<ValidationIssue> issues)
        {
            if (issues == null)
                return string.Empty;

            // 文字列連結用のStringBuilderを作成します。
            StringBuilder sb = new StringBuilder();

            // Errorのみを抽出して改行区切りで連結します。
            for (int i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity != IssueSeverity.Error)
                    continue;

                if (sb.Length > 0)
                    sb.Append('\n');

                sb.Append(issues[i].Message);
            }

            return sb.ToString();
        }
    }
}

