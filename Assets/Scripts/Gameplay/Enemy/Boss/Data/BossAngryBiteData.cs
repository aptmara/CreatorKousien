// ------------------------------------------------------------
// File     : BossAngryBiteData.cs
// Summary  : アングリバイト1回分の位置・口HP・演出時間を保持する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - アングリバイトの移動はアニメーション側で行う。
// - コード側では開始位置と向きを設定してアニメーションを再生する。
// - 口へ入った落とし物は、個数ではなく口へのダメージとして扱う。
// - 口の現在HPなど、実行中の状態は保持しない。
// - このデータはBossPhaseDataからフェーズごとに保持する。
// ------------------------------------------------------------
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// アングリバイト1回分の設定。
    ///
    /// このクラスが担当するもの:
    /// ・アングリバイトの開始位置と向き
    /// ・口開け／口閉じ／ダウンアニメーションの再生速度
    /// ・口を開けて待機する制限時間
    /// ・失敗時に防衛バリアへ与えるダメージ
    /// ・ダウン状態の長さ
    /// ・ダウン時に吐き出す落とし物の個数
    /// </summary>
    [Serializable]
    public sealed class BossAngryBiteData
    {
        [Header("--- 開始位置・向き ---")]

        [SerializeField]
        [Tooltip("アングリバイトの開始位置")]
        private Vector3 _startLocalPosition = new Vector3(0f, -8f, 0f);

        [SerializeField]
        [Tooltip("アングリバイトの開始角度")]
        private Vector3 _startEulerAngles = Vector3.zero;


        [Header("--- アニメーション再生速度 ---")]

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("口開けアニメーションの再生速度")]
        private float _openAnimationSpeed = 1f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("口閉じアニメーションの再生速度")]
        private float _closeAnimationSpeed = 1f;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("ダウンアニメーションの再生速度")]
        private float _downAnimationSpeed = 1f;


        [Header("--- 口の耐久値 ---")]

        [FormerlySerializedAs("_requiredCollectibleCount")]
        [SerializeField]
        [Min(1f)]
        [Tooltip("アングリバイトを阻止するために必要な口のHP")]
        private float _mouthMaxHp = 50f;

        [Header("--- アングリバイトの攻撃猶予時間 ---")]

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("ボスが口を開けて上昇を開始してから、防衛バリアへ到達するまでの時間（秒）")]
        private float _mouthOpenDuration = 8f;


        [Header("--- アングリバイト失敗 ---")]
        [SerializeField]
        [Min(0f)]
        [Tooltip("アングリバイト失敗時に防衛バリアへ与えるダメージ")]
        private float _failureBarrierDamage = 50f;


        [Header("--- アングリバイト成功・ダウン状態 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("アングリバイト成功から、カメラシェイク、エフェクト、落下、次のフェーズへの移行までを含むダウン演出全体の時間（秒）。")]
        private float _downDuration = 3f;

        [SerializeField]
        [Min(0)]
        [Tooltip("ダウン時に吐き出す落とし物の個数")]
        private int _spitCollectibleCount = 10;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// アングリバイトの開始位置
        /// </summary>
        public Vector3 StartLocalPosition => _startLocalPosition;

        /// <summary>
        /// アングリバイトの開始角度
        /// </summary>
        public Vector3 StartEulerAngles => _startEulerAngles;

        /// <summary>
        /// 口開けアニメーションの再生速度
        /// </summary>
        public float OpenAnimationSpeed => _openAnimationSpeed;

        /// <summary>
        /// 口閉じアニメーションの再生速度
        /// </summary>
        public float CloseAnimationSpeed => _closeAnimationSpeed;

        /// <summary>
        /// ダウンアニメーションの再生速度
        /// </summary>
        public float DownAnimationSpeed => _downAnimationSpeed;


        /// <summary>
        /// アングリバイト中の口の最大HP。
        /// </summary>
        public float MouthMaxHp => _mouthMaxHp;

        /// <summary>
        /// 落とし物を受け付ける制限時間。
        /// </summary>
        public float MouthOpenDuration => _mouthOpenDuration;

        /// <summary>
        /// アングリバイト失敗時の防衛バリアダメージ。
        /// </summary>
        public float FailureBarrierDamage => _failureBarrierDamage;

        /// <summary>
        /// アングリバイト成功後のダウン時間。
        /// </summary>
        public float DownDuration => _downDuration;

        /// <summary>
        /// ダウン時に吐き出す落とし物の個数。
        /// </summary>
        public int SpitCollectibleCount => _spitCollectibleCount;
    }
}
