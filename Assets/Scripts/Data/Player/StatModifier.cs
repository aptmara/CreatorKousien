// ------------------------------------------------------------
// File		: StatModifier.cs
// Summary	: 強化の変化量を表す純データ
//
// Author	: [浅野 勇生]
// Created	: 2026-06-19
//
// Notes	:
// - ベースのステータス種別のenumと変化量struct
// ------------------------------------------------------------
using System;

namespace Game.Data.Player
{

    //____________________________________
    // statType

    /// <summary>
    /// 強化対象となるプレイヤーステータスの種別。
    /// PlayerRuntimeDataのプロパティと1対1で対応する。
    /// </summary>
    public enum PlayerStatType
    {
        MaxHp,                      ///< 最大HP
        MoveSpeed,                  ///< 移動速度
        AttachmentScale,            ///< アタッチメントのサイズ倍率
        Looks,

        None,
    }

    public enum CollectableStatType
    {
        BossDamage,
        NormalDamage,
        BarrierPinchDamage,
        AddDropItem,
        Damage,
        SpawnItem,

        None,
    }

    public enum BarrierStatType
    {
        Life,
        RepairSpeed,
        Hard,

        None,
    }

    public enum ShopStatType
    {
        RerollCost,
        CostDown,

        None,
    }


    //__________________________________________
    // 

    /// <summary>
    /// ステータス変化の演算方法。
    /// </summary>
    public enum ModifierOperation
    {
        Add,                        ///< 加算（現在値 + Value）
        Multiply,                   ///< 乗算（現在値 * Value）
        SubTract,                   ///< 減算（現在地 - Value）
    }

    /// <summary>
    /// 1つのステータス変化を表す値型。
    /// 例: { AttachmentScale, Multiply, 1.2f } → アタッチメントを1.2倍にする。
    /// </summary>
    [Serializable]
    public struct StatModifier
    {
        public PlayerStatType TargetStat;       ///< 変化させる対象ステータス
        public ModifierOperation Operation;     ///< 演算方法（加算 or 乗算）
        public float Value;                     ///< 変化量
    }

    [Serializable]
    public struct CollectableStatModifier
    {
        public CollectableStatType TargetStat;       ///< 変化させる対象ステータス
        public ModifierOperation Operation;     ///< 演算方法（加算 or 乗算）
        public float Value;                     ///< 変化量
    }

    [Serializable]
    public struct BarrierStatModifier
    {
        public BarrierStatType TargetStat;       ///< 変化させる対象ステータス
        public ModifierOperation Operation;     ///< 演算方法（加算 or 乗算）
        public float Value;                     ///< 変化量
    }

    [Serializable]
    public struct ShopStatModifier
    {
        public ShopStatType TargetStat;       ///< 変化させる対象ステータス
        public ModifierOperation Operation;     ///< 演算方法（加算 or 乗算）
        public float Value;                     ///< 変化量
    }
}
