// ------------------------------------------------------------
// File		: PlayerCollector.cs
// Summary	: プレイヤーのアイテム収集を管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;
using Game.Gameplay.Collectibles;
using UnityEngine.Rendering;


namespace Game.Gameplay.Player
{
    /// <summary>
    /// CollectibleObjectを検出し、データ(HeldItem)に変換してPlayerHolderへ渡すクラス
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PlayerCollector : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("コンポーネント設定")]
        [Tooltip("収集したアイテムを管理するホルダー")]
        [SerializeField] private PlayerHolder _holder;



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 収集範囲にオブジェクトが侵入した際の処理
        /// </summary>
        /// <param name="other">侵入したCollider</param>
        private void OnTriggerEnter(Collider other)
        {
            // 侵入したオブジェクトがCollectibleObjectか確認
            CollectibleObject collectible = other.GetComponent<CollectibleObject>();


            if (collectible != null)
            {
                if (!collectible.CanBeCollectedByPlayer)
                {
                    return;
                }

                if (_holder.CanAdd())
                {
                    // 変更: Poolに返さず、実体をそのまま所有権ごと頂く！
                    HeldItem itemData = collectible.TakeOwnership();


                    if (itemData != null)
                    {
                        _holder.Add(itemData);
                    }
                }
            }
        }
    }
}
