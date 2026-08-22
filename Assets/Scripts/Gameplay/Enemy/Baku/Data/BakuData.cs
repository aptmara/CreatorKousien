// ------------------------------------------------------------
// File		: BakuData.cs
// Summary	: 敵「バク」のデータを管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-08-22
//
// Notes	:
// - ベース作成
// - EnemyDefinitionだけでは表現できない固有のパラメータを保持するためのクラス
// ------------------------------------------------------------
using Game.Data.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// バクが食べられる落し物の種類を表すビットマスク
    /// 値はCollectibleTypeの定義から自動で計算！
    /// CollectibleTypeに種類が増えたらここへ1行追加してクレメンス！！
    /// </summary>
    [System.Flags]
    public enum BakuEatableType
    {
        None = 0,
        Candy = 1 << (int)CollectibleType.Candy,
        Toge = 1 << (int)CollectibleType.Toge,
        Poison = 1 << (int)CollectibleType.Poison,
        Ice = 1 << (int)CollectibleType.Ice,
        Cross = 1 << (int)CollectibleType.Cross,
        Gummy = 1 << (int)CollectibleType.Gummy,

        All = Candy | Toge | Poison | Ice | Cross | Gummy,
    }


    /// <summary>
    /// BakuEatableTypeの拡張メソッド
    /// </summary>
    public static class BakuEatableTypeExtensions
    {
        /// <summary>
        /// 指定した出井がマスクに含まれるかどうか
        /// </summary>
        /// <param name="mask">バクが食べられる落し物</param>
        /// <param name="type">落し物のタイプ</param>
        /// <returns></returns>
        public static bool Contains(this BakuEatableType mask, CollectibleType type)
        {
            return (mask & (BakuEatableType)(1 << (int)type)) != 0;
        }
    }


    /// <summary>
    /// 敵「バク」の固有データ
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BakuData", menuName = "Game/Enemy/BakuData")]
    public class BakuData : ScriptableObject
    {
        [Header("--- 捕食 ---")]
        [Tooltip("食べられる最大量。この個数までは耐え、これを超えるとバ・ク・レ・ツ")]
        [Min(1)] public int MaxEatCount = 5;

        [Tooltip("食べられる落し物の種類")]
        public BakuEatableType EatableType = BakuEatableType.All;

        [Tooltip("一回食べるごとに立ち止まる時間[秒]")]
        [Min(0f)] public float EatPauseDuration = 0.6f;

        [Tooltip("連続で食べられる最小間隔[秒]")]
        [Min(0f)] public float EatCooldown = 0.1f;

        [Header("--- 膨張 ---")]
        [Tooltip("満腹 (FillRatio = 1.0) の時の膨らみの倍率")]
        public Vector3 MaxBellyScale = new Vector3(1.6f, 1.6f, 1.6f);

        [Tooltip("食べた量[0-1]に対する膨らみのかかり方")]
        public AnimationCurve BellyScaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("ふくらみが目標サイズへ追いつくまでのおおよその時間[秒]")]
        [Min(0.01f)] public float BellyScaleLerpTime = 0.25f;

        [Header("--- バ・ク・レ・ツ ---")]
        [Tooltip("食べ過ぎてから実際に破裂するまでの予兆時間[秒]")]
        [Min(0f)] public float BurstDelay = 0.4f;

        [Tooltip("予兆中に出すVFXのPrefab")]
        public GameObject BurstWarningVfxPrefab;

        [Tooltip("破裂時に周囲の敵へ与えるダメージ")]
        [Min(0f)] public float BurstDamage = 80f;

        [Tooltip("破裂の影響半径[m]")]
        [Min(0f)] public float BurstRadius = 4f;

        [Tooltip("破裂時に生成するVFXのPrefab")]
        public GameObject BurstVfxPrefab;

        [Tooltip("破裂VFXの位置")]
        public Vector3 BurstVfxOffset = new Vector3(0f, 0.5f, 0f);

        [Tooltip("破裂VFXの寿命[秒] 0なら自動破棄しない")]
        [Min(0f)] public float BurstVfxLifetime = 3f;
    }
}

