// ------------------------------------------------------------
// File		: PlayerFacade.cs
// Summary	: プレイヤー内のコンポーネントを束ねるクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
//
// - 6/19: PlayerRuntimeDataを参照、初期ステータスSOから生成するように
//         ローグライクイベントの配線も追加
// ------------------------------------------------------------
using UnityEngine;
using Game.Data.Player;
using Game.Gameplay.Player.Progression;

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
        [SerializeField] private PlayerController _controller;                              ///< プレイヤーのコントローラーコンポーネント
        [Tooltip("プレイヤーの移動コンポーネント")]
        [SerializeField] private PlayerMotor _motor;                                        ///< プレイヤーの移動コンポーネント
        [Tooltip("プレイヤーの収集コンポーネント")]
        [SerializeField] private PlayerCollector _collector;                                ///< プレイヤーの収集コンポーネント
        [Tooltip("プレイヤーの保持コンポーネント")]
        [SerializeField] private PlayerHolder _holder;                                      ///< プレイヤーの保持コンポーネント
        [Tooltip("プレイヤーのアタッチメントコントローラー")]
        [SerializeField] private PlayerAttachmentController _attachmentController;          ///< プレイヤーのアタッチメントコントローラー


        [Header("育成データ設定")]
        [Tooltip("プレイヤーの初期ステータスSO")]
        [SerializeField] private PlayerStatsData _initialStats;                             ///< 初期ステータスのマスターSO
        [Tooltip("プレイヤーごとの必要経験値テーブルSO")]
        [SerializeField] private PlayerLevelTableData _levelTable;                          ///< プレイヤーのレベルテーブルデータSO



        /// <summary>プレイヤー状態の正本。Initialize後に参照可能。</summary>
        public PlayerRuntimeData RuntimeData { get; private set; }

        private PlayerExpSystem _expSystem;                                                 ///< プレイヤーの経験値管理システムへの参照
        private PlayerStatsService _statsService;                                           ///< プレイヤーのステータス管理サービスへの参照


        // TODO: PlayerDropper等も将来的に追加予定



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// プレイヤーのコンポーネントを初期化する関数
        /// </summary>
        public void Initialize()
        {
            // 初期ステータスSOからRuntimeDataを生成
            if (_initialStats != null)
            {
                RuntimeData = new PlayerRuntimeData(_initialStats);
            }
            else
            {
                Debug.LogError("[PlayerFacade] 初期ステータスSO(_initialStats)が未設定です。");
            }

            // 経験値管理システムの初期化
            if (RuntimeData != null && _levelTable != null)
            {
                var calculator = new PlayerLevelCalculator(_levelTable);
                _expSystem = new PlayerExpSystem(RuntimeData, calculator);
                _expSystem.Subscribe();
            }
            else if (_levelTable == null)
            {
                Debug.LogError("[PlayerFacade] レベルテーブルSO(_levelTable)が未設定です。");
            }

            // ステータス管理サービスの初期化
            if (RuntimeData != null)
            {
                // 初期ステータスSOを渡してPlayerStatsServiceを生成
                _statsService = new PlayerStatsService(RuntimeData, _initialStats);
            }

            // アタッチメントへRuntimeDataを渡す
            if (_attachmentController != null)
            {
                _attachmentController.SetRuntimeData(RuntimeData);
            }
             else
            {
                Debug.LogError("[PlayerFacade] アタッチメントコントローラー(_attachmentController)が未設定です。");
            }

            // 移動コンポーネントへRuntimeDataを渡す
            if (_motor != null)
            {
                _motor.SetRuntimeData(RuntimeData);
            }
            else
            {
                Debug.LogError("[PlayerFacade] 移動コンポーネント(_motor)が未設定です。");
            }


            // TODO: 各コンポーネントの初期化処理をここで呼び出す予定
            Debug.Log("PlayerFacade Initialized");
        }


        // 外部システム向け公開API
        // ------------------------------------------------------------
        /// <summary>
        /// プレイヤーにアップグレードを適用する関数
        /// </summary>
        /// <param name="upgrade">適用するアップグレードデータ</param>
        public void ApplyUpgrade(Game.Data.Player.UpgradeData upgrade)
        {
            _statsService?.ApplyUpgrade(upgrade);
        }


        /// <summary>
        /// プレイヤーのステータスを初期状態にリセットする関数
        /// </summary>
        public void ResetStats()
        {
            _statsService?.ResetStats();
        }


        /// <summary>
        /// プレイヤーの破棄時に呼び出される関数
        /// </summary>
        private void OnDestroy()
        {
            // EventBus購読を解除してリークを防ぐ
            _expSystem?.Dispose();
        }
    }
}
