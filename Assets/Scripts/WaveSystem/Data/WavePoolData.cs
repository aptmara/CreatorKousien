// ------------------------------------------------------------
// File		: WavePoolData.cs
// Summary	: ウェーブプールのデータを定義します。
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 序盤・中盤・終盤の分類自体は持ちません。
// - このクラスをStageDataSOの序盤・中盤・終盤フィールドで使用します。
// - ScriptableObjectではなく、StageDataSO内に保存されるデータです。
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// Stage内の1区間で使用するWave抽選設定です。
    ///
    /// 抽選するWave数、同一Waveの重複可否、
    /// 抽選候補とWeightをまとめて管理します。
    /// </summary>
    [Serializable]
    public class WavePoolData
    {
        [Header("--- 抽選設定 ---")]

        [SerializeField]
        [Min(0)]
        [Tooltip("この候補一覧から選択するWaveの数")]
        private int selectionCount = 3;

        [SerializeField]
        [Tooltip("同一Waveを複数回抽選することを許可するか")]
        private bool allowDuplicateSelection;


        [Header("--- Wave候補 ---")]

        [SerializeField]
        [Tooltip("この区間で抽選対象となるWaveとWeightの一覧")]
        private List<WeightedWaveEntry> candidates = new();

        public int SelectionCount => selectionCount;
        public bool AllowDuplicateSelection => allowDuplicateSelection;
        public IReadOnlyList<WeightedWaveEntry> Candidates => candidates;
    }
}

