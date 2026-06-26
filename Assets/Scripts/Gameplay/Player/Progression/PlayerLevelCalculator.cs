// ------------------------------------------------------------
// File		: PlayerLevelCalculator.cs
// Summary	: プレイヤーのレベル計算を行うクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - 6/19: ベース作成
// ------------------------------------------------------------
using Game.Data.Player;

namespace Game.Gameplay.Player.Progression
{
    /// <summary>
    /// レベルテーブルを参照し、必要経験値・最大レベル判定を提供する純関数クラス
    /// </summary>
    public class PlayerLevelCalculator
    {
        private readonly PlayerLevelTableData _table;       ///< レベルテーブルデータへの参照

        /// <summary>
        /// プレイヤーのレベルを計算するためのコンストラクタ
        /// </summary>
        public PlayerLevelCalculator(PlayerLevelTableData table)
        {
            _table = table;
        }

        /// <summary>テーブルが定義する最大レベル。</summary>
        public int MaxLevel => _table != null ? _table.MaxLevel : 1;


        /// <summary>
        /// 指定レベルから次レベルへ上がるのに必要な経験値を返す
        /// 最大レベル到達時はfloat.PositiveInfinityを返し、これ以上レベルが上がらないようにするようにしてるけど、もしかしたらなくなるかも()
        /// </summary>
        /// <param name="level">現在レベル（1始まり）</param>
        public float RequiredExp(int level)
        {
            // テーブルがない、または必要経験値テーブルがない場合は、これ以上レベルアップできないようにする
            if (_table == null || _table.RequiredExpPerLevel == null) return float.PositiveInfinity;

            int index = level - 1;  // Lv1 → index0

            // レベルがテーブルの範囲外の場合は、これ以上レベルアップできないようにする
            if (index < 0 || index >= _table.RequiredExpPerLevel.Length)
            {
                return float.PositiveInfinity;
            }

            // レベルに対応する必要経験値を返す
            return _table.RequiredExpPerLevel[index];
        }


        /// <summary>
        /// 指定レベルがテーブルの最大レベル以上かどうかを判定する
        /// </summary>
        /// <param name="level">レベル</param>
        /// <returns>到達していたらtrue</returns>
        public bool IsMaxLevel(int level)
        {
            return level >= MaxLevel;
        }
    }
}
