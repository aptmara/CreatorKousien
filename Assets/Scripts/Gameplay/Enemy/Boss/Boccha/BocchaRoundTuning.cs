// ------------------------------------------------------------
// File		: BocchaRoundTuning.cs
// Summary	: ボス戦1ラウンド分の調整値をまとめる
//
// Author	: [浅野 勇生]
// Created	: 2026-09-09
//
// Notes	:
// - ダウン1回で次のラウンドへ進む。後半ほど強くする想定。
// - 爆破範囲・ダメージ・散布範囲などの固定値はギミックSO側に置いたままにする。
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// 1ラウンド分の調整値。
    /// </summary>
    [System.Serializable]
    public class BocchaRoundTuning
    {
        [SerializeField]
        [Tooltip("Inspectorで識別するための名前")]
        private string _roundName = "Round";


        [Header("--- キャパ ---")]

        [SerializeField]
        [Min(1f)]
        [Tooltip("何個当てたら分身フェーズへ移行するか")]
        private float _capacityMax = 15.0f;


        [Header("--- 何が出るかな ---")]

        [SerializeField]
        [Min(1)]
        [Tooltip("1回に散布するオカシの数")]
        private int _candyCount = 12;

        [SerializeField]
        [Min(0f)]
        [Tooltip("石化キャンディの抽選重み")]
        private float _stoneWeight = 1.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("トゲ玉の抽選重み")]
        private float _spikeWeight = 1.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ドクロの抽選重み")]
        private float _skullWeight = 1.0f;


        [Header("--- ハザードの数 ---")]

        [SerializeField]
        [Min(0)]
        [Tooltip("石化キャンディの数")]
        private int _stoneCandyCount = 2;

        [SerializeField]
        [Min(0)]
        [Tooltip("トゲ玉の数")]
        private int _spikeBallCount = 2;

        [SerializeField]
        [Min(0)]
        [Tooltip("ドクロの数")]
        private int _skullCount = 3;


        [Header("--- 分身 ---")]

        [SerializeField]
        [Min(0)]
        [Tooltip("分身の数")]
        private int _cloneCount = 2;

        [SerializeField]
        [Min(1f)]
        [Tooltip("本体のHP")]
        private float _mainHp = 100.0f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("分身のHP")]
        private float _cloneHp = 30.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("本体の攻撃力")]
        private float _mainAttack = 25.0f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("分身の攻撃力")]
        private float _cloneAttack = 10.0f;



        // 公開プロパティ
        // ------------------------------------------------------------

        public string RoundName => _roundName;
        public float CapacityMax => _capacityMax;

        public int CandyCount => _candyCount;
        public float StoneWeight => _stoneWeight;
        public float SpikeWeight => _spikeWeight;
        public float SkullWeight => _skullWeight;

        public int StoneCandyCount => _stoneCandyCount;
        public int SpikeBallCount => _spikeBallCount;
        public int SkullCount => _skullCount;

        public int CloneCount => _cloneCount;
        public float MainHp => _mainHp;
        public float CloneHp => _cloneHp;
        public float MainAttack => _mainAttack;
        public float CloneAttack => _cloneAttack;
    }
}
