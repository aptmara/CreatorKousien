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
using Game.Core.Contracts;


namespace Game.Gameplay.Player
{
    /// <summary>
    /// ICollectible検出、PlayerHolderへAdd依頼を行うクラス
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
            ICollectible collectible = other.GetComponent<ICollectible>();

            if (collectible != null && collectible.CanCollect())
            {
                // アイテムをホルダーに追加
                _holder.Add(collectible);

                // アイテムの収集処理を呼び出す
                collectible.OnCollected();
            }
        }
    }
}
