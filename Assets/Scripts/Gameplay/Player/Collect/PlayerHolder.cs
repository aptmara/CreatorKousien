// ------------------------------------------------------------
// File		: PlayerHolder.cs
// Summary	: プレイヤーが収集したアイテムを保持・管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Game.Core.Contracts;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーが収集したアイテムを保持・管理するクラス
    /// </summary>
    public class PlayerHolder : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("保持設定")]
        [Tooltip("最大保持容量")]
        [SerializeField] private int _maxCapacity = 100;

        // TODO: 本来はD担当のしょーご、たきせふの作成するHeldItemデータクラスに置き換える
        //       今回はobject型で代用します～
        private readonly List<object> _heldItems = new List<object>();      ///< 保持しているアイテムのリスト

        [Header("イベント設定")]
        [Tooltip("アイテムの保持数が変化したときのイベント")]
        public UnityEvent OnHolderChanged;                                  ///< アイテムの保持数が変化したときのイベント



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// アイテムを保持リストに追加する
        /// </summary>
        public void Add(ICollectible collectible)
        {
            if (_heldItems.Count >= _maxCapacity)
            {
                return; // 最大容量を超える場合は追加しない
            }

            // TODO: D担当のPayloadFactoryで collectible を HeldItem(Data) に変換して追加する!!
            //       今回はとりあえず new object() で代用します～
            _heldItems.Add(new object());

            OnHolderChanged?.Invoke(); // 保持数が変化したことを通知
        }

        /// <summary>
        /// 保持している全アイテムを解放する関数
        /// B担当向けに用意しときます。
        /// </summary>
        public void ReleaseAll()
        {
            _heldItems.Clear();
            OnHolderChanged?.Invoke(); // 保持数が変化したことを通知

            // TODO: HeldItemsRelease イベントの発行予定
        }
    }
}
