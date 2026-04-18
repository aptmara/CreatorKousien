// ------------------------------------------------------------
// File		: StageTemplateGenerator.cs
// Summary	: 1クリックでステージデータのテンプレート一式を自動生成するエディタ拡張スクリプト
//
// Author	: [浅野勇生]
// Created	: 2026-04-18
//
// Notes	:
// - ステージデータとバトルセットアップデータのテンプレートを自動生成することで、ステージ作成の初期設定を効率化
// ------------------------------------------------------------
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using CreatorKousien.Data;
using System.IO;

namespace CreatorKousien.EditorTools
{
    /// <summary>
    /// StageDataのテンプレート一式を自動生成するエディタ拡張クラス
    /// </summary>
    public class StageTemplateGenerator
    {
        [MenuItem("CreatorKousien/新しいステージセットを作成", priority = 1)]
        public static void CreateNewStageTemplate()
        {
            // 1. 保存先のベースパス
            string basePath = "Assets/Resources/Data/LevelData";

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            // 2. 新しいステージセットの番号を自動計算
            int stageNum = 1;
            string stageFolderName;
            string stageFolderPath;
            do
            {
                stageFolderName = $"Stage_{stageNum:D2}";
                stageFolderPath = $"{basePath}/{stageFolderName}";
                stageNum++;
            }
            while (AssetDatabase.IsValidFolder(stageFolderPath));

            // フォルダを作成
            AssetDatabase.CreateFolder(basePath, stageFolderName);

            // 3. テンプレートのStageDataを作成
            StageData stageData = ScriptableObject.CreateInstance<StageData>();
            string stageDataPath = $"{stageFolderPath}/{stageFolderName}_Field.asset";
            AssetDatabase.CreateAsset(stageData, stageDataPath);

            // 4. battleSetupDataのテンプレートを作成
            BattleSetupData setupData = ScriptableObject.CreateInstance<BattleSetupData>();
            setupData.StageData = stageData; // StageDataを紐づける
            string setupDataPath = $"{stageFolderPath}/{stageFolderName}_Setup.asset";
            AssetDatabase.CreateAsset(setupData, setupDataPath);

            // 変更を保存してアセットデータベースを更新
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 5. 作成したSetupDataを選択状態にする
            Selection.activeObject = setupData;
            EditorGUIUtility.PingObject(setupData);

            Debug.Log($"<color=cyan>[自動生成] {stageFolderName} のテンプレートセットを作成しました！</color>");
        }
    }
}
#endif
