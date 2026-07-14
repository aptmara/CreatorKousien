// ------------------------------------------------------------
// File     : WaveSequenceDebugLogger.cs
// Summary  : StageDataSOから抽選されたWave順をConsoleへ表示
//
// Author   : [浅野 勇生]
// Created  : 2026-07-15
//
// Notes:
// - EnemySpawnerを動かさず、Waveの抽選結果だけを確認します。
// - プランナーがWeightやSeedを確認するためのデバッグ機能です。
// ------------------------------------------------------------
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// StageDataSOからWave順を生成し、
    /// 抽選結果をConsoleへ表示します。
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StageSceneContext))]
    public sealed class WaveSequenceDebugLogger : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("有効にすると、Play開始時にWave抽選結果をConsoleへ表示します。")]
        private bool logOnStart = true;

        private StageSceneContext stageContext;

        private void Awake()
        {
            stageContext = GetComponent<StageSceneContext>();
        }

        private void Start()
        {
            if (logOnStart)
            {
                LogWaveSequence();
            }
        }

        /// <summary>
        /// 現在設定されているStageDataSOとSeedから
        /// Wave順を生成してConsoleへ表示します。
        /// </summary>
        [ContextMenu("Log Wave Sequence")]
        public void LogWaveSequence()
        {
            if (stageContext == null)
            {
                stageContext = GetComponent<StageSceneContext>();
            }

            if (stageContext == null)
            {
                Debug.LogError(
                    "[WaveSequenceDebugLogger] " +
                    "StageSceneContextが見つかりません。",
                    this);

                return;
            }

            StageDataSO stageData = stageContext.StageData;

            if (stageData == null)
            {
                Debug.LogError(
                    "[WaveSequenceDebugLogger] " +
                    "StageDataSOが設定されていません。",
                    this);

                return;
            }

            int seed = stageContext.CreateSeed();

            if (!StageWaveSequenceBuilder.TryBuild(
                    stageData,
                    seed,
                    out List<WaveDataSO> waveSequence,
                    out string errorMessage))
            {
                Debug.LogError(
                    $"[WaveSequenceDebugLogger] 抽選失敗\n{errorMessage}",
                    this);

                return;
            }

            StringBuilder builder = new();

            builder.AppendLine("===== Wave抽選結果 =====");
            builder.AppendLine($"Stage：{stageData.StageName}");
            builder.AppendLine($"Seed：{seed}");
            builder.AppendLine($"Wave数：{waveSequence.Count}");

            for (int i = 0; i < waveSequence.Count; i++)
            {
                WaveDataSO wave = waveSequence[i];

                string waveName =
                    wave != null ? wave.WaveName : "未設定";

                builder.AppendLine(
                    $"Wave {i + 1:D2}：{waveName}");
            }

            builder.AppendLine("========================");

            Debug.Log(builder.ToString(), this);
        }
    }
}
