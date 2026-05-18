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
        [SerializeField] private PhysicalAttachment _attachmentPrefab;

        [Tooltip("アタッチメントの装着ポイント")]
        [SerializeField] private Transform _attachmentSocket;

        private PhysicalAttachment _currentAttachment;  ///< 現在装備しているアタッチメント



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            // 初期化処理
            SpawnAttachment();
        }



        /// <summary>
        /// アタッチメントを生成してセットアップする
        /// </summary>
        public void SpawnAttachment()
        {
            if (_attachmentPrefab == null || _attachmentSocket == null)
            {
                Debug.LogWarning("[PlayerAttachmentController] PrefabまたはSocketが設定されていません！");
                return;
            }

            // 1. Prefabを生成する（この時点ではプレイヤーの子にはせず、独立したオブジェクトとして生成）
            _currentAttachment = Instantiate(_attachmentPrefab, _attachmentSocket.position, _attachmentSocket.rotation);

            // 2. 生成したアタッチメントに、追従先となるソケット（目印）を教える
            _currentAttachment.Initialize(_attachmentSocket);

            Debug.Log("[PlayerAttachmentController] アタッチメントの生成と紐付けが完了しました。");
        }




        /// <summary>
        /// プレイヤーが破棄されるとき、独立しているアタッチメントも一緒に破棄する
        /// </summary>
        private void OnDestroy()
        {
            if (_currentAttachment != null)
            {
                Destroy(_currentAttachment.gameObject);
            }
        }
    }
}
