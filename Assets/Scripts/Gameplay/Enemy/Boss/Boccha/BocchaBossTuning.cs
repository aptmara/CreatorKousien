// ------------------------------------------------------------
// File		: BocchaBossTuning.cs
// Summary	: Waveごとに差し替えるボスの調整値一式
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - ラウンド数が足りない場合は最後のラウンドを使いまわす！
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// キング・ボッチャの調整値。Waveごとに差し替える。
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BocchaBossTuning", menuName = "Boss/Boccha/BossTuning")]
    public sealed class BocchaBossTuning : ScriptableObject
    {
        [SerializeField]
        [Tooltip("ダウン1回ごとに次の行へ進む")]
        private List<BocchaRoundTuning> _round = new List<BocchaRoundTuning>();

        [SerializeField]
        [Min(1f)]
        [Tooltip("撃退に必要なダウン回数")]
        private int _requiredDownCount = 5;

        /// <summary>
        /// 撃退に必要なダウン回数
        /// </summary>
        public int RequiredDownCount => _requiredDownCount;

        /// <summary>
        /// ラウンド数
        /// </summary>
        public int RoundCount => _round.Count;


        /// <summary>
        /// 指定ラウンドの調整値を取得する
        /// </summary>
        /// <param name="index">0始まりのラウンド番号</param>
        /// <returns>1ラウンドの調整値</returns>
        public BocchaRoundTuning GetRound(int index)
        {
            if (_round.Count == 0)
            {
                return null;
            }

            return _round[Mathf.Clamp(index, 0, _round.Count - 1)];
        }
    }
}
