// ------------------------------------------------------------
// File		: WaveEditorStyles.cs
// Summary	: WaveSystemのEditor拡張で共通利用する色とスタイル
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - GUIStyleはドメインリロードで破棄されるため、遅延初期化！
// - Inspectorのスキン（Light/Dark）の両方で読める色を選んでいます！
// - 色はここだけで定義し、各Viewは必ずこのクラスを経由する予定です！
// ------------------------------------------------------------
using UnityEditor;
using UnityEngine;

namespace Game.WaveSystem.Editor
{
    /// <summary>
    /// WaveSystemのEditor拡張で共通利用する色とスタイル
    /// </summary>
    public static class WaveEditorStyles
    {
        /// <summary>
        /// Groupごとの色の配列
        /// </summary>
        private static readonly Color[] GroupColors =
        {
            new Color(0.29f, 0.56f, 0.85f),         // 青系
            new Color(0.90f, 0.53f, 0.25f),         // オレンジ系
            new Color(0.35f, 0.72f, 0.44f),         // 緑系
            new Color(0.72f, 0.45f, 0.78f),         // 紫系
            new Color(0.85f, 0.72f, 0.28f),         // 黄色系
            new Color(0.85f, 0.38f, 0.42f),         // 赤系
        };

        private static GUIStyle sectionHeader;
        private static GUIStyle valueLabel;
        private static GUIStyle captionLabel;

        /// <summary>
        /// Waveの重さなどの見出しに使うスタイル
        /// </summary>
        public static GUIStyle SectionHeader
        {
            get
            {
                if (sectionHeader == null)
                {
                    sectionHeader = new GUIStyle(EditorStyles.boldLabel)
                    {
                        fontSize = 12,
                        margin = new RectOffset(0, 0, 8, 4),
                    };
                }
                return sectionHeader;
            }
        }


        /// <summary>
        /// WaveMetricsの値を表示するラベルのスタイル
        /// </summary>
        public static GUIStyle ValueLabel
        {
            get
            {
                if (valueLabel == null)
                {
                    valueLabel = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleRight,
                    };
                }
                return valueLabel;
            }
        }


        /// <summary>
        /// WaveMetricsの補足説明を表示するラベルのスタイル
        /// </summary>
        public static GUIStyle CaptionLabel
        {
            get
            {
                if (captionLabel == null)
                {
                    captionLabel = new GUIStyle(EditorStyles.miniLabel)
                    {
                        wordWrap = true,
                    };
                }
                return captionLabel;
            }
        }


        /// <summary>
        /// 検証に問題がないことを示す色
        /// </summary>
        public static Color OkColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.42f, 0.78f, 0.48f)
                : new Color(0.16f, 0.55f, 0.24f);


        /// <summary>
        /// タイムラインなどの背景に使う色
        /// </summary>
        public static Color PanelBackgroundColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.78f, 0.78f, 0.78f);


        /// <summary>
        /// 時刻が確定しない区間を示す色
        /// </summary>
        public static Color UnconfirmedColor =>
            EditorGUIUtility.isProSkin
                ? new Color(0.55f, 0.55f, 0.55f)
                : new Color(0.45f, 0.45f, 0.45f);


        /// <summary>
        /// 深刻度に対応する色を返します
        /// </summary>
        /// <param name="severity">検証結果の深刻度</param>
        /// <returns>対応する色</returns>
        public static Color GetSeverityColor(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Error:
                    return EditorGUIUtility.isProSkin
                        ? new Color(0.90f, 0.38f, 0.38f)
                        : new Color(0.72f, 0.15f, 0.15f);

                case IssueSeverity.Warning:
                    return EditorGUIUtility.isProSkin
                        ? new Color(0.92f, 0.72f, 0.30f)
                        : new Color(0.68f, 0.48f, 0.05f);

                default:
                    return EditorGUIUtility.isProSkin
                            ? new Color(0.60f, 0.70f, 0.85f)
                            : new Color(0.25f, 0.38f, 0.58f);
            }
        }


        /// <summary>
        /// Groupのインデックスに対応する色を返します
        /// </summary>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <returns>対応する色</returns>
        public static Color GetGroupColor(int groupIndex)
        {
            if (groupIndex < 0)
            {
                groupIndex = 0;
            }

            return GroupColors[groupIndex % GroupColors.Length];
        }
    }
}
