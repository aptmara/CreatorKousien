// ------------------------------------------------------------
// File		: StageDataEditor.cs
// Summary	: ステージデータを編集するためのエディタ
//
// Author	: [浅野勇生]
// Created	: 2026-04-16
//
// Notes	:
// - エディタ拡張のためのクラス。StageDataの内容を編集するためのUIを提供する予定 (4/16)
// ------------------------------------------------------------
using UnityEngine;
using UnityEditor;
using CreatorKousien.Data;

[CustomEditor(typeof(StageData))]
public class StageDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        StageData stageData = (StageData)target;

        GUILayout.Space(20);
        GUILayout.Label("🎮 盤面ペイントエディタ", EditorStyles.boldLabel);
        GUILayout.Label("マスをクリックして障害物(■)を配置！", EditorStyles.label);

        GUIStyle gridStyle = new GUIStyle(GUI.skin.button)
        {
            fixedWidth  = 40,
            fixedHeight = 40,
            fontSize    = 18
        };

        // 縦横のグリッドを描画
        for (int y = 0; y < stageData.Height; y++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();      // 中央寄せ

            for (int x = 0; x < stageData.Width; x++)
            {
                GUI.backgroundColor = (x < stageData.BorderX) ? new Color(0.6f, 0.8f, 1f) : new Color(1f, 0.6f, 0.6f); // 自陣は青、敵陣は赤

                Vector2Int pos = new Vector2Int(x, y);
                bool isObstacle = stageData.ObstaclePositions.Contains(pos);
                string buttonText = isObstacle ? "■" : "□";

                // ボタンが押された時の処理
                if (GUILayout.Button(buttonText, gridStyle))
                {
                    Undo.RecordObject(stageData, "Toggle Obstacle");    // Ctrl+Zで元に戻せるようにする

                    if (isObstacle)
                    {
                        stageData.ObstaclePositions.Remove(pos);        // 障害物を削除
                    }
                    else
                    {
                        stageData.ObstaclePositions.Add(pos);           // 障害物を追加
                    }

                    EditorUtility.SetDirty(stageData);                  // 変更を保存するために必要
                }

                // 境界線で少し隙間を空ける
                if (x == stageData.BorderX - 1)
                {
                    GUILayout.Space(15);
                }
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white; // 色をリセット
        GUILayout.Space(20);
    }
}
