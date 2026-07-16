// ------------------------------------------------------------
// File     : BossBattleDataSO.cs
// Summary  : ボス戦全体のフェーズ設定を保持するScriptableObject
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ボス戦全体で使用する3フェーズ分の設定を保持する。
// - 各フェーズの詳細な設定はBossPhaseDataが保持する。
// - 実行中の現在フェーズやダウン回数は保持しない。
// - 3回目のアングリバイト成功後に勝利となる。
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// ボス戦全体の調整データを保持するScriptableObject。
    ///
    /// このクラスが担当するもの:
    /// ・3フェーズ分のBossPhaseDataの保持
    /// ・フェーズ番号から設定データを取得する機能
    /// ・ボス戦データが正しく設定されているかの検証
    /// </summary>
    [CreateAssetMenu(fileName = "SO_BossBattle_New", menuName = "Game/Boss/Boss Battle Data", order = 0)]
    public sealed class BossBattleDataSO : ScriptableObject
    {
        /// <summary>
        /// 勝利に必要なフェーズ数
        /// </summary>
        public const int RequiredPhaseCount = 3;


        [Header("--- 基本情報 ---")]

        [SerializeField]
        [Tooltip("Inspector上でボス戦データを識別するための名前")]
        private string _battleName = "Jack Flower Boss";

        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("ボス戦の説明文")]
        private string _plannerMemo = "ボス戦の説明や調整内容をここに記入してちょんまげ！";


        [Header("--- 開幕演出設定 ---")]

        [SerializeField]
        [Tooltip("ボス戦開始時の開幕演出設定")]
        private BossIntroPresentationData _introPresentationData = new BossIntroPresentationData();



        [Header("--- フェーズ設定 ---")]

        [SerializeField]
        [Tooltip("ボス戦全体で使用する3フェーズ分の設定")]
        private List<BossPhaseData> _phases =
            new List<BossPhaseData>
            {
                new BossPhaseData(),
                new BossPhaseData(),
                new BossPhaseData()
            };


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// Inspector上で表示するボス戦名。
        /// </summary>
        public string BattleName => _battleName;

        /// <summary>
        /// プランナー向けのボス戦説明。
        /// </summary>
        public string PlannerMemo => _plannerMemo;

        /// <summary>
        /// ボス戦開始時の開幕演出設定。
        /// </summary>
        public BossIntroPresentationData IntroPresentationData => _introPresentationData;

        /// <summary>
        /// 登録されているフェーズ数。
        /// </summary>
        public int PhaseCount => _phases != null ? _phases.Count : 0;

        /// <summary>
        /// 登録されている全フェーズの読み取り専用一覧。
        /// </summary>
        public IReadOnlyList<BossPhaseData> Phases => _phases;


        // フェーズ取得
        // ------------------------------------------------------------

        /// <summary>
        /// 指定されたフェーズ番号のBossPhaseDataを取得する。
        /// </summary>
        /// <param name="phaseIndex">取得するフェーズ番号</param>
        /// <param name="phaseData">取得できたフェーズデータ</param>
        /// <returns>正常に取得できた場合はtrue</returns>
        public bool TryGetPhaseData(int phaseIndex, out BossPhaseData phaseData)
        {
            phaseData = null;

            if (_phases == null || phaseIndex < 0 || phaseIndex >= _phases.Count)
            {
                return false;
            }

            phaseData = _phases[phaseIndex];

            return phaseData != null;
        }


        /// <summary>
        /// 指定されたフェーズ番号が最終フェーズかどうかを判定する。
        /// </summary>
        /// <param name="phaseIndex">判定するフェーズ番号</param>
        /// <returns>最終フェーズの場合はtrue、それ以外はfalse</returns>
        public bool IsFinalPhase(int phaseIndex)
        {
            return PhaseCount > 0 && phaseIndex == PhaseCount - 1;
        }


        // Inspector検証
        // ------------------------------------------------------------

        /// <summary>
        /// Inspector上でボス戦データが正しく設定されているかを検証する。
        /// </summary>
        private void OnValidate()
        {
            if (_introPresentationData == null)
            {
                Debug.LogWarning($"[{nameof(BossBattleDataSO)}] 開幕演出設定がnullです。", this);
            }

            if (_phases == null)
            {
                Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ設定がnullです。", this);
                return;
            }

            if (_phases.Count != RequiredPhaseCount)
            {
                Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ数が{RequiredPhaseCount}ではありません。現在のフェーズ数: {_phases.Count}", this);
            }

            for (int i = 0; i < _phases.Count; i++)
            {
                BossPhaseData phaseData = _phases[i];

                if (phaseData == null)
                {
                    Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ番号 {i + 1} のフェーズデータがnullです。", this);

                    continue;
                }

                if (phaseData.ThornAttackSteps == null || phaseData.ThornAttackSteps.Count == 0)
                {
                    Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ番号 {i + 1} のイバラタックルが設定されていません。", this);
                }

                if (phaseData.AngryBiteData == null)
                {
                    Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ番号 {i + 1} のアングリバイトデータが設定されていません。", this);
                }

                if (phaseData.DownPresentationData == null)
                {
                    Debug.LogWarning($"[{nameof(BossBattleDataSO)}] フェーズ番号 {i + 1} のダウン演出データが設定されていません。", this);
                }
            }
        }
    }
}
