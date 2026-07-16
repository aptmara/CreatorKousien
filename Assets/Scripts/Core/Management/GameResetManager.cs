// ================================================================================
// File         : GameResetManager.cs
// Author       : Iwai Shogo
//
// Description  : 加算シーン一式をクリーンアップしてLoadingから再始動させるマネージャー。
// Created      : 2026-07-02
// ================================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Core.Management
{
    /// <summary>
    /// 加算シーン一式をクリーンアップしてLoadingから再始動させるマネージャー
    /// </summary>
    public class GameResetManager
    {
        public static void TriggerFullReset()
        {
            Debug.Log("[GameResetManager] 全シーンをクリーンアップし、Loadingシーンから完全初期化します。");

            // タイムスケールを通常に戻す
            Time.timeScale = 1f;

            SceneManager.LoadScene("Loading", LoadSceneMode.Single);
        }
    }
}
