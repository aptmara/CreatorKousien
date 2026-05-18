// ------------------------------------------------------------
// File		: PhysicalAttachment.cs
// Summary	: プレイヤーのソケットに追従しながら物理的に回転するアタッチメント
//
// Author	: [浅野 勇生]
// Created	: 2026-05-18
//
// Notes	:
// - ソケット追従によるすり抜け防止対策版
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// 物理演算に則ってソケットへ追従・回転するアタッチメントクラス
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicalAttachment : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        private Rigidbody _rigidbody;           ///< 自身のリジットボディ
        private Transform _targetSocket;        ///< 追従対象のソケット（Transform）

        private Rigidbody _playerRigidbody;     ///< プレイヤー本体のリジットボディ
        private Vector3 _localSocketPosition;   ///< ソケットのローカル位置
        private Quaternion _localSocketRotation; ///< ソケットのローカル回転


        // 関数処理
        // ------------------------------------------------------------

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // 物理設定を強制（設定漏れ防止）
            _rigidbody.isKinematic = true;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }


        /// <summary>
        /// 初期化処理 - 追従対象のソケットを設定
        /// </summary>
        /// <param name="targetSocket">追従対象のソケット</param>
        public void Initialize(Transform targetSocket)
        {
            _targetSocket = targetSocket;

            if (_targetSocket != null)
            {
                // ソケットの親オブジェクトからRigidbodyを取得
                _playerRigidbody = _targetSocket.GetComponentInParent<Rigidbody>();

                // プレイヤーからみたソケットの相対的な位置・回転を保存
                _localSocketPosition = _targetSocket.localPosition;
                _localSocketRotation = _targetSocket.localRotation;
            }
        }


        /// <summary>
        /// 物理演算の更新処理
        /// </summary>
        private void FixedUpdate()
        {
            if (_targetSocket == null || _playerRigidbody == null)
            {
                return;
            }

            // 1. 位置の計算
            Vector3 calculatedTargetPos = _playerRigidbody.position + (_playerRigidbody.rotation * _localSocketPosition);
            _rigidbody.MovePosition(calculatedTargetPos);

            // 2. 回転の計算
            Quaternion calculatedTargetRot = _playerRigidbody.rotation * _localSocketRotation;
            _rigidbody.MoveRotation(calculatedTargetRot);
        }
    }
}
