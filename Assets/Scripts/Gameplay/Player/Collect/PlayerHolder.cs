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
using Game.Gameplay.Collectibles;
using Game.Core.Events;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// HeldItemリスト保持、最大保持容量判定を行うクラス
    /// </summary>
    public class PlayerHolder : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("保持設定")]
        [Tooltip("最大保持容量")]
        [SerializeField] private int _maxCapacity = 10;

        private readonly List<HeldItem> _heldItems = new List<HeldItem>();      ///< 保持しているアイテムのリスト

        [Header("イベント設定")]
        [Tooltip("アイテムの保持数が変化したときのイベント")]
        public UnityEvent OnHolderChanged;                                      ///< アイテムの保持数が変化したときのイベント

        /// <summary>
        /// 現在の保持数を取得
        /// </summary>
        public int CurrentCount => _heldItems.Count;

        /// <summary>
        /// 最大保持容量を取得
        /// </summary>
        public int MaxCapacity => _maxCapacity;

        /// <summary>
        /// 外部から保持しているアイテムのリストを読み取り専用で取得するプロパティ
        /// </summary>
        public IReadOnlyList<HeldItem> HeldItems => _heldItems;                 ///< 保持しているアイテムのリスト（読み取り専用）


        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// アイテムを追加できるかどうかを判定する
        /// </summary>
        /// <returns>追加可能な場合はtrue、そうでない場合はfalse</returns>
        public bool CanAdd()
        {
            return _heldItems.Count < _maxCapacity;
        }


        /// <summary>
        /// アイテムを保持リストに追加する
        /// </summary>
        /// <@param name="item">追加するアイテム</param>
        public void Add(HeldItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("[PlayerHolder] nullのHeldItemは追加できません。");
                return;
            }

            _heldItems.Add(item);
            NotifyHolderChanged(); // 保持数が変化したことを通知

            // TODO: 将来的にD担当の ShapeChangeへのイベント発行もここで行う予定
        }

        /// <summary>
        /// 保持している全アイテムを解放する関数
        /// B担当向けに用意しときます。
        /// </summary>
        /// <returns>解放されたアイテムの配列</returns>
        public HeldItem[] ReleaseAll()
        {
            HeldItem[] releasedItems = _heldItems.ToArray();
            _heldItems.Clear();
            NotifyHolderChanged();

            // TODO: HeldItemsReleased イベントの発行
            return releasedItems;
        }

        /// <summary>
        /// 保持数変更をUnityEventと統合EventBusへ通知する。
        /// </summary>
        private void NotifyHolderChanged()
        {
            OnHolderChanged?.Invoke();
            EventBus.Publish(new CollectionChangedEvent(CurrentCount, MaxCapacity));
        }
    }
}
