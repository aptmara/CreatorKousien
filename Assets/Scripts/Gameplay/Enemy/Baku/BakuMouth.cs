// ------------------------------------------------------------
// File		: BakuMouth.cs
// Summary	: バクの口の管理を行う純粋なクラス
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// ------------------------------------------------------------
using System;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// バクの口に設置するTrigger
    /// 責務は検出の通知のみで、消費・ダメージ・状態判定は一切行わない
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class BakuMouth : MonoBehaviour
    {
        /// <summary>
        /// 口の範囲へオトシモノが入った時に発火するイベント
        /// </summary>
        public event Action<CollectibleObject> CollectibleEntered;

        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
        }


        /// <summary>
        /// 口の開閉。閉じるとTriggerが無効化されるので、入ってきたオトシモノは検出されなくなる。
        /// </summary>
        /// <param name="isOpen"></param>
        public void SetOpen(bool isOpen)
        {
            if (_collider == null)
                return;

            _collider.enabled = isOpen;
        }


        private void OnTriggerEnter(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();
            if (collectible == null)
                return;

            CollectibleEntered?.Invoke(collectible);
        }
    }
}
