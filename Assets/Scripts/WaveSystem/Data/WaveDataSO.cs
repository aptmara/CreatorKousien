// ------------------------------------------------------------
// File		: WaveDataSO.cs
// Summary	: 1Wave分の敵出現構成と調整値を管理
//
// Author	: [浅野 勇生]
// Created	: 2026-07-15
//
// Notes	:
// - 1つのアセットが、ゲーム内の1Waveを表します。
// - 敵の基本性能はEnemyDefinitionが管理します。
// - このSOはWave固有の敵構成や補正値を管理します。
// - 実行中のGroup番号や生存敵数などは保持しません。
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace Game.WaveSystem
{
    /// <summary>
    /// 1Wave分の敵構成を定義するScriptableObjectです。
    /// </summary>
    [CreateAssetMenu(
        fileName = "Wave_New",
        menuName = "Game/Wave System/Wave Data",
        order = 0)]
    public class WaveDataSO : ScriptableObject
    {
        [Header("--- 基本情報 ---")]

        [SerializeField]
        [Tooltip("プランナーがこのWaveを識別するための名前")]
        private string waveName = "New Wave";

        [SerializeField]
        [TextArea(2, 5)]
        [Tooltip("このWaveの狙いや調整内容を記入する, プランナー向けのメモ欄")]
        private string plannerMemo;


        [Header("--- Wave全体の時間設定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("Waveが開始してから、最初のGroupを開始するまでの待ち時間")]
        private float startDelay = 1f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("Wave内の敵をすべて倒してから、Wave完了として次の処理へ進むまでの待ち時間です。")]
        private float completeDelay = 2f;


        [Header("--- 敵ステータス補正 ---")]

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("このWave内の敵の体力を補正する倍率です。1.0で補正なし、2.0で2倍、0.5で半分になります。")]
        private float hpRate = 1f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("このWaveで出現する敵のバリアゲージとバリア回復量にかける倍率")]
        private float barrierRate = 1f;


        [Header("--- スポーン制限 ---")]

        [SerializeField]
        [Min(0)]
        [Tooltip("このWave中に同時に存在できる敵の最大数です。0の場合は制限しません。")]
        private int maxAliveEnemies;

        [SerializeField]
        [Min(0f)]
        [Tooltip("敵を出現させる際、すでに存在する敵との間に確保する最小距離")]
        private float minDistanceFromOtherEnemies = 3f;


        [Header("--- WaveGroup構成 ---")]

        [SerializeField]
        [Tooltip("このWaveを構成するWaveGroupのリスト")]
        private List<WaveGroupData> groups = new();

        // 基本情報
        public string WaveName => waveName;
        public string PlannerMemo => plannerMemo;

        // 時間設定
        public float StartDelay => startDelay;
        public float CompleteDelay => completeDelay;

        // 敵ステータス補正
        public float HpRate => hpRate;
        public float BarrierRate => barrierRate;

        // スポーン制限
        public int MaxAliveEnemies => maxAliveEnemies;
        public float MinDistanceFromOtherEnemies => minDistanceFromOtherEnemies;


        // WaveGroup構成
        public IReadOnlyList<WaveGroupData> Groups => groups;
    }
}

