// ================================================================================
// File         : EnemyActionPattern.cs
// Author       : Iwai Shogo
//
// Description  : EnemyAIが行動を決定するための1手分の思考ユニット。
// Created      : 2026-04-13
//
// Note         : カスタムプロパティドロワーを適用し、
//                CustomTargetShapeを3x3のグリッドUIで操作できるように拡張する予定です。
// ================================================================================

using CreatorKousien.Battle;
using UnityEngine;

namespace CreatorKousien.Data
{
    /// <summary>
    /// 発動条件
    /// </summary>
    public enum ConditionType
    {
        [Tooltip("常に評価される(基本行動)")] Always,
        [Tooltip("自身のHPが指定割合(%)以下の時に発動")] HpUnderPercent,
        [Tooltip("指定されたターン周期で発動")] TurnMultiple,
        [Tooltip("プレイヤーが自機の前方Nマス以内にいる時")] PlayerInDistance
    }

    /// <summary>
    /// 攻撃範囲の基準点
    /// </summary>
    public enum TargetOrigin
    {
        [Tooltip("プレイヤーの現在位置を基準にする")] PlayerPosition,
        [Tooltip("フィールドの中央を基準にする")] FieldCenter,
        [Tooltip("盤面の最前列(敵側)を中央の基準にする")] FrontRowCenter,
        [Tooltip("盤面の最後列(自陣奥)の中央を基準にする")] BackRowCenter,
        [Tooltip("盤面の左端の中央を基準にする")] LeftEdgeCenter,
        [Tooltip("盤面の右端の中央を基準にする")] RightEdgeCenter,
        [Tooltip("ランダムな通行可能マスを基準にする")] RandomPassableCell,
        [Tooltip("自分自身の現在地を動的に基準にする")] SelfPosition,
    }

    /// <summary>
    /// ターゲット指定ルール
    /// </summary>
    public enum TargetSelection
    {
        [Tooltip("基準点1マスのみ")] SingleCell,
        [Tooltip("基準点を中心とした十字")] Cross,
        [Tooltip("エディタ上で指定したローカルグリッド形状")] LocalGridShape
    }

    /// <summary>
    /// AIの1つの行動ルールと、その結果発生する攻撃の内容を定義するクラス
    /// </summary>
    [System.Serializable]
    public class EnemyActionPattern
    {
        [Tooltip("この行動を発動するためのトリガー条件")]
        public ConditionType Condition;

        [Tooltip("条件の基準値 (例: HpUnderPercentならパーセンテージ、TurnMultipleならターン数)")]
        public int ConditionValue;

        [Tooltip("使用後にこの行動を選択できなくなるターン数(クールダウン)")]
        [Min(0)]
        public int CooldownTurns = 0;

        [Tooltip("この行動の選ばれやすさ")]
        [Min(0f)]
        public float Weight = 1.0f;

        [Tooltip("アクションの種類")]
        public ActionType Type = ActionType.FastAttack;

        [Tooltip("攻撃の性質(倍率やヒット数)")]
        public ActionProperty Property;

        [Tooltip("起点を中心に、どのように攻撃範囲を展開するか")]
        public TargetOrigin OriginRule;

        [Tooltip("どのマスを攻撃範囲として展開するのかのルール")]
        public TargetSelection TargetRule;

        [Tooltip("TargetRuleがLocalGridShapeの場合に使用される相対的な攻撃範囲")]
        [SerializeField]
        public bool[] LocalTargetGrid = new bool[25];

        [Min(0)] public int ChargeTurns = 0;

        [Tooltip("Trueの場合、溜め中に攻撃を受けると予告が消滅してひるみます。")]
        public bool IsInterruptible = true;
    }
}
