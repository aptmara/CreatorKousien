// ------------------------------------------------------------
// File		: PlayerRuntimeData.cs
// Summary	: プレイヤーのランタイムデータを保持するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - ランタイムデータの基盤クラス作成！
// ------------------------------------------------------------
using Game.Core;
using Game.Data.Player;

namespace Game.Gameplay.Player.Progression
{
    /// <summary>
    /// プレイヤーのレベル・経験値・各ステータスを保持する実行時データ。
    /// SOの値をコピーして生成し、シーン内では恒久的に保持される予定です！！ワンちゃん変わるかも…
    /// </summary>
    public class PlayerRuntimeData : IRuntimeData
    {
        // ----- レベル / 経験値 -----
        /// <summary>
        /// プレイヤーの現在のレベル
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// プレイヤーの現在の経験値量
        /// </summary>
        public float CurrentExp { get; set; }

        /// <summary>
        /// 次のレベルに必要な経験値量
        /// </summary>
        public float ExpToNextLevel { get; set; }


        // ----- ステータス -----
        /// <summary>
        /// プレイヤーの現在の最大HP
        /// </summary>
        public float MaxHp { get; set; }

        /// <summary>
        /// プレイヤーの現在の移動速度倍率（基準速度に掛ける）
        /// </summary>
        public float MoveSpeedMultiplier { get; set; }

        /// <summary>
        /// プレイヤーの現在のアタッチメントのサイズ倍率
        /// </summary>
        public float AttachmentScaleMultiplier { get; set; }


        // ----- 換算係数 -----
        /// <summary>
        /// 敵から受け取った経験値量にかける換算係数
        /// </summary>
        public float ExpConversionRate { get; private set; }


        /// <summary>
        /// PlayerStatsDataの値をコピーして初期化するコンストラクタ
        /// </summary>
        /// <param name="initial">初期値</param>
        public PlayerRuntimeData(PlayerStatsData initial)
        {
            ResetToInitial(initial);
        }


        /// <summary>
        /// ステータスとレベルをSOの初期値へ戻す（Q6: リトライ/新ラン開始時に呼ぶ）。
        /// </summary>
        /// <param name="initial">初期値</param>
        public void ResetToInitial(PlayerStatsData initial)
        {
            if (initial == null) return;

            Level           = 1;     // レベルは1からスタート
            CurrentExp      = 0f;    // 経験値は0からスタート
            ExpToNextLevel  = 0f;    // レベルテーブル接続後にPlayerExpSystemが設定する

            // ステータスはSOの値をコピー
            MaxHp                       = initial.MaxHp;
            MoveSpeedMultiplier         = initial.MoveSpeedMultiplier;
            AttachmentScaleMultiplier   = initial.AttachmentScaleMultiplier;

            ExpConversionRate = initial.ExpConversionRate;
        }
    }
}

