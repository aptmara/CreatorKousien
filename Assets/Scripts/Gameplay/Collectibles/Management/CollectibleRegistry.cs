// ================================================================================
// File         : CollectibleRegistry.cs
// Author       : Iwai Shogo
//
// Description  : ステージ上に存在するアクティブな収集物の一覧を保持する台帳。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;
using System.Collections.Generic;
using Game.Gameplay.Collectibles;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// ステージ上に存在する収集物のリストを管理します。
    /// オブジェクトの生成は行わず、SpawnerやObject空の通知を受けて登録・解除のみを行います。
    /// </summary>
    public class CollectibleRegistry : MonoBehaviour
    {
        // 検索や追加・削除を高速に行う
        private HashSet<CollectibleObject> _activeCollectibles = new HashSet<CollectibleObject>();

        public IReadOnlyCollection<CollectibleObject> ActiveCollectibles => _activeCollectibles;
        public int ActiveCount => _activeCollectibles.Count;

        public void Register(CollectibleObject collectible)
        {
            _activeCollectibles.Add(collectible);
        }

        public void Unregister(CollectibleObject collectible)
        {
            _activeCollectibles.Remove(collectible);
        }
    }
}
