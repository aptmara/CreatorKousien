// ================================================================================
// File         : SaveManager.cs
// Author       : Iwai Shogo
//
// Description  : セーブデータの読み書きと、Stage1からのStage解決を行う
// Created      : 2026-09-08
// ================================================================================

using System;
using System.IO;
using Game.WaveSystem;
using UnityEngine;

namespace Game.Core.Save
{
    /// <summary>
    /// セーブデータの読み書きを担当する静的クラス。
    /// </summary>
    public static class SaveManager
    {
        private const string SaveFileName = "savedata.json";

        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>
        /// セーブデータが存在するかどうか。
        /// </summary>
        public static bool HasSaveData()
        {
            try
            {
                return File.Exists(SaveFilePath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] セーブデータの有無確認に失敗しました: {e}");
                return false;
            }
        }

        /// <summary>
        /// セーブデータを読み込みます。
        /// </summary>
        public static GameSaveData Load()
        {
            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    return null;
                }

                string json = File.ReadAllText(SaveFilePath);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning("[SaveManager] セーブデータの中身が空でした。");
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] セーブデータの読み込みに失敗しました: {e}");
                return null;
            }
        }

        /// <summary>
        /// 現在の進行状況をセーブします。
        /// </summary>
        public static void Save(GameSaveData data)
        {
            if (data == null)
            {
                Debug.LogError("[SaveManager] セーブデータがnullです。");
                return;
            }

            try
            {
                data.saveVersion = 1;
                data.stageIndex = Mathf.Max(0, data.stageIndex);
                data.waveIndex = Mathf.Max(0, data.waveIndex);
                data.money = Mathf.Max(0, data.money);
                data.savedAtUtc = DateTime.UtcNow.ToString("o");

                string json = JsonUtility.ToJson(data, true);
                Directory.CreateDirectory(Application.persistentDataPath);
                File.WriteAllText(SaveFilePath, json);

                int upgradeCount = data.upgrades != null ? data.upgrades.Count : 0;
                Debug.Log($"[SaveManager] セーブしました。StageIndex:{data.stageIndex} WaveIndex:{data.waveIndex} Money:{data.money} 強化数:{upgradeCount} ({data.stageName})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] セーブに失敗しました: {e}");
            }
        }

        /// <summary>
        /// セーブデータを削除します。
        /// </summary>
        public static void DeleteSave()
        {
            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                    Debug.Log("[SaveManager] セーブデータを削除しました。");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] セーブデータの削除に失敗しました: {e}");
            }
        }

        /// <summary>
        /// Stage1(ルート)のStageDataSOから、NextStageをstageIndex回辿った先のStageDataSOを解決します。
        /// </summary>
        /// <param name="rootStage">Stage1に相当するStageDataSO</param>
        /// <param name="stageIndex">辿る回数(0ならrootStageそのもの)</param>
        public static StageDataSO ResolveStageByIndex(StageDataSO rootStage, int stageIndex)
        {
            if (rootStage == null)
            {
                return null;
            }

            StageDataSO current = rootStage;

            for (int i = 0; i < stageIndex; i++)
            {
                if (!current.HasNextStage)
                {
                    Debug.LogWarning($"[SaveManager] StageIndex {stageIndex} まで辿れませんでした。Stage「{current.StageName}」で鎖が終端しています。");
                    break;
                }

                current = current.NextStage;
            }

            return current;
        }
    }
}
