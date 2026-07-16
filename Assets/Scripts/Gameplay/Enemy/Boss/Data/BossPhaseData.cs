// ------------------------------------------------------------
// File     : BossPhaseData.cs
// Summary  : ボス戦1フェーズ分の調整値をまとめて保持する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ボス戦1フェーズ分の設定を保持する。
// - 実行中の棘HPや口の現在HPは保持しない。
// - イバラタックルの最大回数は、登録された攻撃段階数で決まる。
// - アングリバイト失敗後は同じフェーズの設定を再利用する。
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// ボス戦1フェーズ分の設定。
    ///
    /// このクラスが担当するもの:
    /// ・フェーズ内で有効にする棘の本数
    /// ・棘1本あたりの最大HP
    /// ・棘が防衛バリアへ与えるダメージ
    /// ・アングリバイト失敗後の棘再抽選設定
    /// ・イバラタックルの各段階設定
    /// ・アングリバイト設定
    /// ・フェーズごとのダウン演出設定
    /// </summary>
    [Serializable]
    public sealed class BossPhaseData
    {
        [Header("--- フェーズ基本情報 ---")]

        [SerializeField]
        [Tooltip("Inspector上でフェーズを識別するための名前")]
        private string _phaseName = "Boss Phase";

        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("フェーズの説明文")]
        private string _plannerMemo = "フェーズの説明や調整内容をここに記入してちょんまげ！";


        [Header("--- 棘の設定 ---")]

        [SerializeField]
        [Range(1, 3)]
        [Tooltip("フェーズ内で有効にする棘の本数")]
        private int _activeThornCount = 1;

        [SerializeField]
        [Min(1f)]
        [Tooltip("このフェーズにおける棘1本あたりの最大HP")]
        private float _thornMaxHp = 100f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("棘が防衛バリアへ与えるダメージ")]
        private float _barrierDamagePerThorn = 10f;

        [SerializeField]
        [Tooltip("アングリバイト失敗時に棘を再抽選するかどうか")]
        private bool _rerollThornsOnRetry = true;


        [Header("--- イバラタックルの段階設定 ---")]

        [SerializeField]
        [Tooltip("イバラタックルの各段階設定")]
        private List<BossThornAttackStepData> _thornAttackSteps =
            new List<BossThornAttackStepData>
            {
                new BossThornAttackStepData(),
                new BossThornAttackStepData(),
                new BossThornAttackStepData()
            };


        [Header("--- アングリバイトの設定 ---")]

        [SerializeField]
        [Tooltip("アングリバイトの各設定")]
        private BossAngryBiteData _angryBiteData = new BossAngryBiteData();


        [Header("--- ダウン演出の設定 ---")]

        [SerializeField]
        [Tooltip("ダウン演出の各設定")]
        private BossDownPresentationData _downPresentationData = new BossDownPresentationData();


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// Inspector上で表示するフェーズ名。
        /// </summary>
        public string PhaseName => _phaseName;

        /// <summary>
        /// プランナー向けのフェーズ説明。
        /// </summary>
        public string PlannerMemo => _plannerMemo;

        /// <summary>
        /// このフェーズで有効にする棘の本数。
        /// </summary>
        public int ActiveThornCount => _activeThornCount;

        /// <summary>
        /// 棘1本あたりの最大HP。
        /// </summary>
        public float ThornMaxHp => _thornMaxHp;

        /// <summary>
        /// 棘1本が防衛バリアへ与えるダメージ。
        /// </summary>
        public float BarrierDamagePerThorn => _barrierDamagePerThorn;

        /// <summary>
        /// 再挑戦時に有効な棘を再抽選するかどうか。
        /// </summary>
        public bool RerollThornsOnRetry => _rerollThornsOnRetry;

        /// <summary>
        /// 順番に実行するイバラタックルの設定一覧。
        /// </summary>
        public IReadOnlyList<BossThornAttackStepData> ThornAttackSteps => _thornAttackSteps;

        /// <summary>
        /// このフェーズで使用するアングリバイト設定。
        /// </summary>
        public BossAngryBiteData AngryBiteData => _angryBiteData;

        /// <summary>
        /// このフェーズで使用するダウン演出設定。
        /// </summary>
        public BossDownPresentationData DownPresentationData => _downPresentationData;
    }
}
