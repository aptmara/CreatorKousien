// ------------------------------------------------------------
// File		: WeightedWaveEntry.cs
// Summary	: ウェーブの出現条件を定義するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - Weightは選択確率そのものではなく、他候補との比率です。
// - WeightをStage側に持たせることで、同じWaveでもStageごとに
//   異なる選択率を設定できます。
// - ScriptableObjectではなく、StageDataSO内に保存されるデータです。
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// ウェーブの出現条件を定義するクラス
    /// </summary>
    [Serializable]
    public class WeightedWaveEntry
    {
        [SerializeField]
        [Tooltip("このWaveを抽選候補として使用するかを指定。無効にしても、設定したweightは保持される。")]
        private bool isEnabled = true;

        [SerializeField]
        [Tooltip("抽選候補として使用するWaveData")]
        private WaveDataSO waveData;

        [SerializeField]
        [Min(0)]
        [Tooltip("このWaveが選択される確率の比率。0にすると抽選候補から除外される。")]
        private int weight = 100;

        public bool IsEnabled => isEnabled;
        public WaveDataSO WaveData => waveData;
        public int Weight => weight;

        /// <summary>
        /// この設定が抽選可能な状態かを返す
        /// 無効、Wave未設定、Weightが0の場合は抽選不可とする
        /// </summary>
        public bool IsSelectable => isEnabled && waveData != null && weight > 0;
    }
}

