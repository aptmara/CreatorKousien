// ================================================================================
// File         : GameResetManager.cs
// Author       : Iwai Shogo
//
// Description  : 加算シーン一式をクリーンアップしてBootから再始動させるマネージャー。
// Created      : 2026-07-02
// ================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Management
{
    /// <summary>
    /// 加算シーン一式をクリーンアップしてBootから再始動させるマネージャー
    /// </summary>
    public class GameResetManager
    {
        public static void TriggerFullReset()
        {
            Debug.Log("[GameResetManager] 全シーンをクリーンアップし、ゲームをBootシーンから完全初期化するぜようおおおおお！");

            // タイムスケールを通常に戻す
            Time.timeScale = 1f;

            // "Boot" シーンを単一ロード
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
        }
    }
}
