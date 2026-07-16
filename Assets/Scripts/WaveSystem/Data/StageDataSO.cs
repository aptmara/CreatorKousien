// ------------------------------------------------------------
// File		: StageDataSO.cs
// Summary	: ステージデータを定義するScriptableObject
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 序盤・中盤・終盤のWave抽選候補を管理します。
// - 最終Waveは抽選せず、Boss Waveを固定で使用します。
// - 実際に選択されたWave一覧など、実行中の状態は保持しません。
// ------------------------------------------------------------
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// 1Stage分のWave構成を定義するScriptableObjectです。
    ///
    /// 序盤・中盤・終盤からWaveを抽選し、
    /// 最後に固定のBoss Waveを追加します。
    /// </summary>
    [CreateAssetMenu(
        fileName = "Stage_New",
        menuName = "Game/Wave System/Stage Data",
        order = 10)]
    public class StageDataSO : ScriptableObject
    {
        /// <summary>
        /// このゲームで1Stageに必要な合計Wave数
        /// 序盤・中盤・終盤それぞれ、3Wave + Boss Wave = 10Waveの構成！
        /// </summary>
        public const int RequiredWaveCount = 10;


        [Header("--- 基本情報 ---")]

        [SerializeField]
        [Tooltip("プランナーがこのStageを識別するための名前")]
        private string stageName = "New Stage";

        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("このStageの狙いや調整内容を記入する")]
        private string plannerMemo;


        [Header("--- 序盤 Wave1～3 ---")]

        [SerializeField]
        [Tooltip("Stage序盤で使用するWaveの抽選設定。基本的には3Waveを選択します。")]
        private WavePoolData earlyWavePool = new();


        [Header("--- 中盤 Wave4～6 ---")]

        [SerializeField]
        [Tooltip("Stage中盤で使用するWaveの抽選設定。基本的には3Waveを選択します。")]
        private WavePoolData middleWavePool = new();


        [Header("--- 終盤 Wave7～9 ---")]

        [SerializeField]
        [Tooltip("Stage終盤で使用するWaveの抽選設定。基本的には3Waveを選択します。")]
        private WavePoolData lateWavePool = new();


        [Header("--- 最終 Wave10 ---")]

        [SerializeField]
        [Tooltip("Stage最終Waveで使用するWaveの設定。抽選せず固定で使用します。")]
        private WaveDataSO bossWave;


        // 基本情報
        public string StageName => stageName;
        public string PlannerMemo => plannerMemo;

        // Wave抽選プール
        public WavePoolData EarlyWavePool => earlyWavePool;
        public WavePoolData MiddleWavePool => middleWavePool;
        public WavePoolData LateWavePool => lateWavePool;

        // 最終Wave
        public WaveDataSO BossWave => bossWave;


        /// <summary>
        /// 序盤・中盤・終盤から選択する通常Waveの合計数
        /// </summary>
        public int RegularWaveCount =>
            GetSelectionCount(earlyWavePool) +
            GetSelectionCount(middleWavePool) +
            GetSelectionCount(lateWavePool);



        /// <summary>
        /// 現在の設定から生成される合計Wave数
        /// Boss Waveが未設定の場合、ボス分は加算されない
        /// </summary>
        public int TotalWaveCount =>
            RegularWaveCount + (bossWave != null ? 1 : 0);

        /// <summary>
        /// 合計Wave数がゲーム仕様の10Waveになっているかを返す
        /// </summary>
        public bool HasRequiredWaveCount =>
            TotalWaveCount == RequiredWaveCount;


        /// <summary>
        /// このStageで必要なWave数を返します。
        /// </summary>
        /// <param name="pool">Waveプールデータ</param>
        /// <returns>選ばれたWave数</returns>
        private static int GetSelectionCount(WavePoolData pool)
        {
            return pool != null ? pool.SelectionCount : 0;
        }
    }
}
