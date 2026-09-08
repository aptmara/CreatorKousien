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
//
// - Stage2対応 (8/13 - Asano)
// ------------------------------------------------------------
using System;
using Game.Core.Save;
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


        [Header("--- デバッグ ---")]
        [SerializeField]
        [Tooltip("ONにすると、このSceneでは自動でWaveを進行しません。WaveTesterで手動実行するデバック用モードです")]
        private bool manualTestMode = false;


        /// <summary>
        /// デバック用モードかどうか
        /// </summary>
        public bool ManualTestMode => manualTestMode;


        private bool isRegisteredStageData;            ///< データが登録済みかどうかを取得します。
        private bool hasCreatedSeed;        ///< Seedを生成済みかどうかを取得します。
        private int createdSeed;            ///< 生成済みのSeedの値を取得します。

        private bool isRegisteredResumeState;           ///< セーブからの再開情報が登録済みかどうか。



        public StageDataSO StageData => stageData;      ///< StageDataSOの参照を取得します。
        public bool UseFixedSeed => useFixedSeed;       ///< 同じSeedでWaveを抽選するかどうかを取得します。
        public int FixedSeed => fixedSeed;              ///< UseFixedSeedが有効な場合に使用するSeedの値を取得します。

        /// <summary>
        /// 「つづきから」で再開する場合のセーブデータ。
        /// 通常の新規開始(New Game / Tutorial)の場合はnullのままです。
        /// </summary>
        public GameSaveData ResumeData { get; private set; }

        /// <summary>
        /// Wave抽選用のSeedを生成します。
        /// </summary>
        /// <returns>Seedの値</returns>
        public int CreateSeed()
        {
            if (hasCreatedSeed) return createdSeed;

            // UseFixedSeedが有効な場合は、固定のSeedを使用します。
            createdSeed = useFixedSeed ? fixedSeed : Environment.TickCount;
            hasCreatedSeed = true;

            return createdSeed;
        }

        /// <summary>
        /// 一度のみステージデータを登録します。
        /// </summary>
        public void RegisterStageData(StageDataSO stageData)
        {
            if (isRegisteredStageData) return;

            // nullを登録すると、Inspectorで設定した内容まで消えてしまう
            if (stageData == null)
            {
                Debug.LogError("StageSceneContextにnullのStageDataSOを登録しようとしました。");
                return;
            }

            isRegisteredStageData = true;
            this.stageData = stageData;

            Debug.Log("StageContextに" + stageData.name + "を登録しました。");
        }

        /// <summary>
        /// セーブデータからの再開情報を一度のみ登録します。
        /// 通常の新規開始(New Game / Tutorial)の場合はnullを渡してください。
        /// </summary>
        /// <param name="resumeData">「つづきから」のセーブデータ。新規開始の場合はnull。</param>
        public void RegisterResumeState(GameSaveData resumeData)
        {
            if (isRegisteredResumeState) return;

            isRegisteredResumeState = true;
            ResumeData = resumeData;
        }
    }
}
