// ------------------------------------------------------------
// File		: PlayerFacade.cs
// Summary	: プレイヤー内のコンポーネントを束ねるクラス
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
    /// プレイヤー内のコンポーネントを束ねるクラス
    /// </summary>

    public class PlayerFacade : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("コンポーネント設定")]
        [Tooltip("プレイヤーのコントローラーコンポーネント")]
        [SerializeField] private PlayerController _controller;          ///< プレイヤーのコントローラーコンポーネント
        [Tooltip("プレイヤーの移動コンポーネント")]
        [SerializeField] private PlayerMotor _motor;                    ///< プレイヤーの移動コンポーネント
        [Tooltip("プレイヤーの収集コンポーネント")]
        [SerializeField] private PlayerCollector _collector;            ///< プレイヤーの収集コンポーネント
        [Tooltip("プレイヤーの保持コンポーネント")]
        [SerializeField] private PlayerHolder _holder;                  ///< プレイヤーの保持コンポーネント

        // TODO: PlayerDropper等も将来的に追加予定



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// プレイヤーのコンポーネントを初期化する関数
        /// </summary>
        public void Initialize()
        {
            // TODO: 各コンポーネントの初期化処理をここで呼び出す予定
            Debug.Log("PlayerFacade Initialized");
        }

    }
}
