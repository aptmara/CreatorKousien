// ------------------------------------------------------------
// File		: WaveTimelineView.cs
// Summary	: Waveの敵出現タイミングを時間軸で描画します。
//
// Author	: [浅野 勇生]
// Created	: 2026-08-04
//
// Notes	:
// - WaveMetricsCalculatorが出したSpawnEventをそのまま描画します！
// - Group単位で1レーンを使い、色はWaveEditorStylesから取得します！
// - 時刻が確定しないGroupは、色を薄くして区別できるようにします！
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Game.WaveSystem.Editor
{
    /// <summary>
    /// Waveの敵出現タイミングを時間軸で描画
    /// </summary>
    public static class WaveTimelineView
    {
        /// <summary>
        /// Group名を表示する左側の幅
        /// </summary>
        private const float LabelWidth = 72f;

        /// <summary>
        /// Groupごとのレーンの高さ
        /// </summary>
        private const float LaneHeight = 18f;

        /// <summary>
        /// 時間軸の目盛りの高さ
        /// </summary>
        private const float RulerHeight = 16f;

        /// <summary>
        /// パネル内側の余白
        /// </summary>
        private const float Padding = 4f;

        /// <summary>
        /// 敵1体を表す縦線の幅
        /// </summary>
        private const float SpawnMarkWidth = 2f;

        /// <summary>
        /// 目盛りの間隔の候補値（秒）
        /// </summary>
        private static readonly float[] TickIntervals = { 1f, 2f, 5f, 10f, 15f, 30f, 60f, 120f, 300f };


        /// <summary>
        /// タイムラインを描画します
        /// </summary>
        /// <param name="waveData">描画対象のWaveDataSO</param>
        /// <param name="spawnEvents">敵一体ごとの出現時刻</param>
        /// <param name="metrics">Waveの見張り値</param>
        public static void Draw(WaveDataSO waveData, IReadOnlyList<SpawnEvent> spawnEvents, WaveMetrics metrics)
        {
            if (waveData == null || spawnEvents == null || spawnEvents.Count == 0)
            {
                EditorGUILayout.HelpBox("出現する敵が無い為、タイムラインを表示できません。", MessageType.None);
                return;
            }

            int groupCount = Mathf.Max(metrics.GroupCount, 1);

            // 最後の敵が右端に張り付かないように、少しだけ余白を追加する
            float displayTime = Mathf.Max(metrics.MinDuration, 1f) * 1.08f;

            float panelHeight = RulerHeight + LaneHeight * groupCount + Padding * 2f;

            // --- パネルの背景を描画 ---
            Rect panel = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(panelHeight), GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(panel, WaveEditorStyles.PanelBackgroundColor);

            // --- Group名を描画 ---
            Rect track = new Rect(panel.x + LabelWidth, panel.y + Padding, panel.width - LabelWidth - Padding, panel.height - Padding * 2f);

            DrawRuler(track, displayTime);

            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                DrawLane(panel, track, waveData, spawnEvents, groupIndex, displayTime);
            }

            DrawLegend(metrics);
            DrawGroupSummaries(waveData, spawnEvents, groupCount);
        }


        /// <summary>
        /// 時間軸の目盛りと縦線を描画
        /// </summary>
        /// <param name="track">目盛りを描く領域</param>
        /// <param name="displayTime">全体の表示時間</param>
        private static void DrawRuler(Rect track, float displayTime)
        {
            float interval = ChooseTickInterval(displayTime);

            Color lineColor = WaveEditorStyles.UnconfirmedColor;
            lineColor.a = 0.35f;

            int tickCount = Mathf.FloorToInt(displayTime / interval);

            for (int i = 0; i <= tickCount; i++)
            {
                float time = i * interval;
                float x = TimeToX(track, time, displayTime);

                // レーン全体を貫く縦線を描画
                EditorGUI.DrawRect(new Rect(x, track.y + RulerHeight, 1f, track.height - RulerHeight), lineColor);

                GUI.Label(new Rect(x + 2f, track.y, 44f, RulerHeight), $"{time:0.0}s", EditorStyles.miniLabel);
            }
        }



        // 描画関数
        // ------------------------------------------------------------

        /// <summary>
        /// 1つのGroupのレーンを描画
        /// </summary>
        /// <param name="panel">パネル全体の領域</param>
        /// <param name="track">トラックの領域</param>
        /// <param name="waveData">Waveデータ</param>
        /// <param name="spawnEvents">敵一体ごとの出現時刻</param>
        /// <param name="groupIndex">グループインデックス</param>
        /// <param name="displayTime">全体の表示時間</param>
        private static void DrawLane(Rect panel, Rect track, WaveDataSO waveData, IReadOnlyList<SpawnEvent> spawnEvents, int groupIndex, float displayTime)
        {
            if (!TryGetGroupRange(spawnEvents, groupIndex, out float firstTime, out float lastTime, out bool isConfirmed))
            {
                return;
            }

            float laneY = track.y + RulerHeight + groupIndex * LaneHeight;

            Color groupColor = WaveEditorStyles.GetGroupColor(groupIndex);

            // 時刻が確定しないGroupは、灰色に寄せて区別する
            if (!isConfirmed)
            {
                groupColor = Color.Lerp(groupColor, WaveEditorStyles.UnconfirmedColor, 0.55f);
            }

            // 最初の出現から最後の出現までの帯を描画
            float startX = TimeToX(track, firstTime, displayTime);
            float endX = TimeToX(track, lastTime, displayTime);

            Color bandColor = groupColor;
            bandColor.a = 0.3f;

            // 帯の描画
            EditorGUI.DrawRect(new Rect(startX, laneY + 4f,Mathf.Max(endX - startX, 1f), LaneHeight - 8f), bandColor);

            // 敵1体ごとの出現時刻を縦線で描画
            for (int i = 0; i < spawnEvents.Count; i++)
            {
                SpawnEvent spawnEvent = spawnEvents[i];

                if (spawnEvent.GroupIndex != groupIndex)
                {
                    continue;
                }

                float x = TimeToX(track, spawnEvent.Time, displayTime);

                EditorGUI.DrawRect(new Rect(x, laneY + 2f, SpawnMarkWidth, LaneHeight - 4f), groupColor);
            }

            // 左側にGroup名を描画
            string groupName = GetGroupName(waveData, groupIndex);

            Rect labelRect = new Rect(panel.x + Padding, laneY, LabelWidth - Padding, LaneHeight);

            GUI.Label(labelRect, new GUIContent(groupName, groupName), EditorStyles.miniLabel);
        }


        /// <summary>
        /// タイムラインの凡例を描画
        /// </summary>
        /// <param name="metrics">Waveの見張り値</param>
        private static void DrawLegend(WaveMetrics metrics)
        {
            string legend = $"縦線 1本 = 敵1体の出現  /  帯 = そのGroupが敵を出し続けている区間";

            if (!metrics.IsDurationConfirmed)
            {
                legend += "\n色が薄いグループは、手前に敵の全滅待ちがあるため、開始時刻が確定してないです！表示は最短の理論値ケースなり！";
            }

            EditorGUILayout.LabelField(legend, WaveEditorStyles.CaptionLabel);
        }


        /// <summary>
        /// Groupごとの出現時刻の範囲、敵数、進行条件を表示します
        /// </summary>
        /// <param name="waveData">描画対象のWaveDataSO</param>
        /// <param name="spawnEvents">敵1体ごとの出現時刻</param>
        /// <param name="groupCount">グループ数</param>
        private static void DrawGroupSummaries(WaveDataSO waveData, IReadOnlyList<SpawnEvent> spawnEvents, int groupCount)
        {
            for (int groupIndex = 0; groupIndex < groupCount; groupIndex++)
            {
                // 該当Groupの出現時刻の範囲を取得
                if (!TryGetGroupRange(spawnEvents, groupIndex, out float firstTime, out float lastTime, out bool isConfirmed))
                {
                    continue;
                }

                int enemyCount = CountGroupEnemies(spawnEvents, groupIndex);

                // 出現時刻が確定していない場合は「以降」を付ける
                string suffix = isConfirmed ? string.Empty : " 以降";

                // Group名、敵数、出現時刻の範囲、進行条件を表示
                string label = $"{GetGroupName(waveData, groupIndex)}";
                string value = $"敵 {enemyCount}体, {firstTime:0.0}秒 ～ {lastTime:0.0}秒{suffix}, {GetAdvanceText(waveData, groupIndex, groupCount)}";

                EditorGUILayout.LabelField(label, value, EditorStyles.miniLabel);
            }
        }



        // ヘルパー関数
        // ------------------------------------------------------------

        /// <summary>
        /// 指定Groupの次のGroupへの進行条件を取得します
        /// </summary>
        /// <param name="waveData">描画対象のWaveDataSO</param>
        /// <param name="groupIndex">グループインデックス</param>
        /// <param name="groupCount">グループ数</param>
        /// <returns>進行条件の説明</returns>
        private static string GetAdvanceText(WaveDataSO waveData, int groupIndex, int groupCount)
        {
            if (groupIndex >= groupCount - 1)
            {
                return "最終Group";
            }

            WaveGroupData group = GetGroup(waveData, groupIndex);

            if (group == null)
            {
                return "Group不明";
            }

            if (group.AdvanceType == WaveGroupAdvanceType.TimeAfterGroupStart)
            {
                return $"{group.TimeUntilNextGroup:0.0}秒後に次のGroupへ";
            }

            return "全滅で次へ";
        }


        /// <summary>
        /// 指定Groupの出現時刻の範囲を取得
        /// </summary>
        /// <param name="spawnEvents">敵1体ごとの出現時刻</param>
        /// <param name="groupIndex">グループのインデックス</param>
        /// <param name="firstTime">最初の出現時刻</param>
        /// <param name="lastTime">最後の出現時刻</param>
        /// <param name="isConfirmed">出現時刻が確定しているかどうか</param>
        /// <returns>該当する出現が1件以上あればtrue</returns>
        private static bool TryGetGroupRange(IReadOnlyList<SpawnEvent> spawnEvents, int groupIndex, out float firstTime, out float lastTime, out bool isConfirmed)
        {
            // 該当するGroupの出現時刻の範囲を取得します
            firstTime = float.MaxValue;
            lastTime = float.MinValue;
            isConfirmed = true;

            bool found = false;

            // Groupに属する敵の出現時刻を走査
            for (int i = 0; i < spawnEvents.Count; i++)
            {
                SpawnEvent spawnEvent = spawnEvents[i];

                if (spawnEvent.GroupIndex != groupIndex)
                {
                    continue;
                }

                // 出現時刻の範囲を更新
                firstTime = Mathf.Min(firstTime, spawnEvent.Time);
                lastTime = Mathf.Max(lastTime, spawnEvent.Time);

                isConfirmed &= spawnEvent.IsTimeConfirmed;
                found = true;
            }

            return found;
        }


        /// <summary>
        /// 指定されたGroupに属する敵の数を数えます
        /// </summary>
        /// <param name="spawnEvents">敵一体ごとの出現時刻</param>
        /// <param name="groupIndex">Groupのインデックス</param>
        /// <returns>Groupに属する敵の数</returns>
        private static int CountGroupEnemies(IReadOnlyList<SpawnEvent> spawnEvents, int groupIndex)
        {
            int count = 0;

            for (int i = 0; i < spawnEvents.Count; i++)
            {
                if (spawnEvents[i].GroupIndex == groupIndex)
                {
                    count++;
                }
            }

            return count;
        }


        /// <summary>
        /// 指定されたインデックスのGroupを取得します
        /// </summary>
        /// <param name="waveData">対象のWaveDataSO</param>
        /// <param name="groupIndex">グループインデックス</param>
        /// <returns>該当するGroup</returns>
        private static WaveGroupData GetGroup(WaveDataSO waveData, int groupIndex)
        {
            if (waveData.Groups == null || groupIndex < 0 || groupIndex >= waveData.Groups.Count)
            {
                return null;
            }

            return waveData.Groups[groupIndex];
        }


        /// <summary>
        /// Group名を取得します
        /// </summary>
        /// <param name="waveData">対象のWaveDataSO</param>
        /// <param name="groupIndex">グループインデックス</param>
        /// <returns>グループ名</returns>
        private static string GetGroupName(WaveDataSO waveData, int groupIndex)
        {
            WaveGroupData group = GetGroup(waveData, groupIndex);

            if (group == null || string.IsNullOrEmpty(group.GroupName))
            {
                return $"Group {groupIndex + 1}";
            }

            return group.GroupName;
        }


        /// <summary>
        /// 表示する時間に応じて、目盛りの間隔を選択します
        /// </summary>
        /// <param name="displayTime">表示する時間の全体幅</param>
        /// <returns>目盛りの間隔(秒)</returns>
        private static float ChooseTickInterval(float displayTime)
        {
            for (int i = 0; i < TickIntervals.Length; i++)
            {
                // 目盛りが10本以内に収まる間隔を選ぶ
                if (displayTime / TickIntervals[i] <= 10f)
                {
                    return TickIntervals[i];
                }
            }
            return TickIntervals[TickIntervals.Length - 1];
        }


        /// <summary>
        /// 時刻を描画領域のX座標に変換します
        /// </summary>
        /// <param name="track">時間軸の領域</param>
        /// <param name="time">変換する時間</param>
        /// <param name="displayTime">表示する時間</param>
        /// <returns>X座標</returns>
        private static float TimeToX(Rect track, float time, float displayTime)
        {
            return track.x + track.width * Mathf.Clamp01(time / displayTime);
        }
    }
}
