// ------------------------------------------------------------
// File		: DropZone.cs
// Summary	: ステージ端からのアイテム落下を検知するトリガークラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Collectables;


namespace Game.Gameplay.Stage
{
    /// <summary>
    /// ステージ端からのアイテム落下を検知するトリガークラス
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DropZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            PhysicalCollectable item = other.GetComponent<PhysicalCollectable>();
            if (item != null)
            {
                item.OnDropped();
            }
        }
    }
}
