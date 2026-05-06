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
            // 侵入したオブジェクトがCollectibleObjectかどうかをチェック
            CollectibleObject collectible = other.GetComponent<CollectibleObject>();

            if (collectible != null)
            {
                if (!collectible.CanBeCollectedByPlayer)
                {
                    return;
                }

                // 容量チェックなど、拾えるかどうかの判定をHolderに任せる
                if (_holder.CanAdd())
                {
                    // アイテム側から軽量データを抽出し、実体はPoolへ返却させる
                    HeldItem itemData = collectible.OnCollected();

                    // 抽出したデータをHolderのリストに追加
                    if (itemData != null)
                    {
                        _holder.Add(itemData);
                    }
                }
            }
        }
    }
}
