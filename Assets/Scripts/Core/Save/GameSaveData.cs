// ================================================================================
// File         : GameSaveData.cs
// Author       : Iwai Shogo
//
// Description  : セーブファイルに書き出す進行状況データ
// Created      : 2026-09-08
// ================================================================================

using System;
using System.Collections.Generic;

namespace Game.Core.Save
{
    /// <summary>
    /// セーブファイルへ書き出す進行状況データ。
    /// </summary>
    [Serializable]
    public sealed class GameSaveData
    {
        /// <summary>
        /// セーブデータの形式バージョン。
        /// </summary>
        public int saveVersion = 1;

        /// <summary>
        /// Stage1(ルート)からNextStageを何回辿った先のStageかを表す番号(0始まり)。
        /// </summary>
        public int stageIndex;

        /// <summary>
        /// そのStage内で、次に開始すべきWaveの番号(0始まり)。
        /// </summary>
        public int waveIndex;

        /// <summary>
        /// デバッグ表示・確認用のStage名。
        /// </summary>
        public string stageName;

        /// <summary>
        /// セーブした日時(UTC, ISO8601形式)。
        /// </summary>
        public string savedAtUtc;

        /// <summary>
        /// セーブ時点の所持金(MoneyData.moneyOnHand)。
        /// </summary>
        public int money;

        /// <summary>
        /// セーブ時点で取得済みのローグライク強化(ID + レベル)一覧。
        /// </summary>
        public List<UpgradeSaveEntry> upgrades = new();
    }

    /// <summary>
    /// 取得済み強化1件分のセーブデータ。
    /// </summary>
    [Serializable]
    public sealed class UpgradeSaveEntry
    {
        /// <summary>UpgradeData.Idと対応する一意なID。</summary>
        public string upgradeId;

        /// <summary>その強化の取得済みレベル。</summary>
        public int level;
    }
}
