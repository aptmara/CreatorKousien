// ------------------------------------------------------------
// File		: StageSceneContext.cs
// Summary	: Sceneで使用するStageDataSOと抽選Seedを保持
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - StageEditorなどのステージSceneに1つだけ配置します。
// - Waveの実行処理は行わず、Scene固有の設定だけを保持します。
// - GameplayShell側の進行管理がこの設定を取得します。
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// 現在のStage Sceneで使用するStageDataSOと
    /// Wave抽選用Seedを保持
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSceneContext : MonoBehaviour
    {
        [Header("--- Stage設定 ---")]
        [SerializeField]
        [Tooltip("このSceneで使用するStageDataSO")]
        private StageDataSO stageData;


        [Header("--- Wave抽選用Seed ---")]
        [SerializeField]
        [Tooltip("有効にすると、毎回同じSeedでWaveを抽選。Wave調整や不具合再現時は有効を推奨!!")]
        private bool useFixedSeed = true;

        [SerializeField]
        [Tooltip("Use Fixed Seedが有効な場合に使用するSeed。同じStage設定とSeedなら同じWave順になります。")]
        private int fixedSeed = 12345;


        public StageDataSO StageData => stageData;      ///< StageDataSOの参照を取得します。
        public bool UseFixedSeed => useFixedSeed;       ///< 同じSeedでWaveを抽選するかどうかを取得します。
        public int FixedSeed => fixedSeed;              ///< UseFixedSeedが有効な場合に使用するSeedの値を取得します。

        /// <summary>
        /// Wave抽選用のSeedを生成します。
        /// </summary>
        /// <returns>Seedの値</returns>
        public int CreateSeed()
        {
            if (useFixedSeed)
            {
                return fixedSeed;
            }

            // プレイするたびにランダムなSeedを生成
            return Environment.TickCount;
        }
    }
}
