// ------------------------------------------------------------
// File     : BossBattleTypes.cs
// Summary  : ボス戦全体で使用する状態と攻撃方向を定義する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ボス戦の状態をboolの組み合わせで管理しないために使用する。
// - BossBattleControllerやアニメーション制御クラスから参照する。
// ------------------------------------------------------------

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// ボス戦の状態を表す列挙型。
    /// </summary>
    public enum BossBattleState
    {
        /// <summary>
        /// ボス戦が開始されていない状態。
        /// </summary>
        Inactive = 0,

        /// <summary>
        /// ボスが棘攻撃を行う状態
        /// </summary>
        ThornAttack = 1,

        /// <summary>
        /// ボスが怒り状態で噛みつき攻撃を行う状態
        /// </summary>
        AngryBite = 2,

        /// <summary>
        /// ボスがダウンしている状態
        /// </summary>
        Down = 3,

        /// <summary>
        /// ボスが撃破された状態
        /// </summary>
        Defeated = 4,
    }

    /// <summary>
    /// イバラタックルでボスが登場する際の攻撃方向を表す列挙型。
    /// </summary>
    public enum BossAttackSide
    {
        /// <summary>
        /// ボスが左側から登場する状態
        /// </summary>
        Left = 0,

        /// <summary>
        /// ボスが右側から登場する状態
        /// </summary>
        Right = 1,
    }
}
