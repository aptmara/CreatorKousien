// ================================================================================
// File         : RoguelikeCompletionBridge.cs
// Author       : Iwai Shogo
//
// Description  : 完了通知を GameProgressionManager へ中継するクラス。
// Created      : 2026-07-02
// ================================================================================

using UnityEngine;
using Game.Core.Management;

namespace Game.Gameplay.Roguelike
{
    /// <summary>
    /// 作りかけのローグライクUIや、他のスクリプトからの完了通知を GameProgressionManager へ中継するためのブリッジクラス
    /// </summary>
    public sealed class RoguelikeCompletionBridge : MonoBehaviour
    {
        /// <summary>
        /// インスペクターのButtonや、他の人のコードからこれを呼び出す。
        /// </summary>
        public void TriggerComplete()
        {
            if (GameProgressionManagerBase.Instance != null)
            {
                Debug.Log("[Bridge] ローグライク完了ボタンが押されました。進行マネージャーへ通知します。");
                GameProgressionManagerBase.Instance.CompleteRoguelikeSequence();
            }
            else
            {
                Debug.LogWarning("[Bridge] GameProgressionManager がシーン内に見つかりません。通常のバトルシーンから起動してください。");
            }
        }
    }
}
