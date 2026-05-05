// ------------------------------------------------------------
// File		: PhysicalCollectable.cs
// Summary	: 物理的なコレクタブルアイテムの基本クラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Collectables
{
    /// <summary>
    /// 物理的にプレイヤーの押されるアイテム
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalCollectable : MonoBehaviour
    {
        private Rigidbody _rigidbody;  ///< アイテムのRigidbodyコンポーネント

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // 回転しすぎないように適度な抵抗を設定
            _rigidbody.linearDamping = 1.0f;
            _rigidbody.angularDamping = 1.0f;
        }

        /// <summary>
        /// 落下判定ゾーンなどから呼ばれ、自身を削除(将来はPool返却)
        /// </summary>
        public void OnDropped()
        {
            // TODO: 敵へダメージを与える処理などはここに追加
            Debug.Log($"[PhysicalCollectable] Dropped: {gameObject.name}");
            Destroy(gameObject);
        }
    }
}
