// ------------------------------------------------------------
// File		: PlayerAttachmentController.cs
// Summary	: プレイヤーのアタッチメントを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// プレイヤーのアタッチメントを管理するクラス
    /// </summary>
    public class PlayerAttachmentController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("アタッチメント設定")]
        [Tooltip("装備するアタッチメントのプレハブ")]
        [SerializeField] private GameObject _attachmentPrefab;

        [Tooltip("アタッチメントの装着ポイント")]
        [SerializeField] private Transform _attachmentMountPoint;

        private GameObject _currentAttachment;  ///< 現在装備しているアタッチメント



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            // 初期化処理
            EquipAttachment(_attachmentPrefab);
        }

        /// <summary>
        /// 指定されたアタッチメントを生成して装備する関数
        /// </summary>
        /// <param name="prefab">生成するプレハブ</param>
        public void EquipAttachment(GameObject prefab)
        {
            if (prefab == null || _attachmentMountPoint == null)
            {
                return;
            }

            // 既存のものがあれば破棄
            if (_currentAttachment != null)
            {
                Destroy(_currentAttachment);
            }

            // アタッチメントを作成し、マウントポイントの子オブジェクト
            _currentAttachment = Instantiate(prefab, _attachmentMountPoint.position, _attachmentMountPoint.rotation, _attachmentMountPoint);
        }
    }
}
