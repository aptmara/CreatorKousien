// ------------------------------------------------------------
// File     : BossThornAttackStepData.cs
// Summary  : イバラタックル1段階分の開始位置・向き・再生設定を保持する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - 弧を描く移動はボスのアニメーション側で行う。
// - コード側ではアニメーション開始前の位置と向きを設定する。
// - 左右それぞれの開始位置と向きを個別に調整できる。
// - 実行中の進行度や現在の攻撃方向は保持しない。
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// イバラタックル1段階分の設定。
    ///
    /// 3段階のイバラタックルを行う場合は、
    /// このデータを3つ用意してBossPhaseDataへ登録する。
    ///
    /// このクラスが担当するもの:
    /// ・左から登場するときの開始位置と向き
    /// ・右から登場するときの開始位置と向き
    /// ・アニメーション再生速度
    /// ・防衛バリアへ攻撃可能な段階かどうか
    /// </summary>
    [Serializable]
    public sealed class BossThornAttackStepData
    {
        [Header("--- 識別情報 ---")]

        [SerializeField]
        [Tooltip("Inspector上で、この攻撃段階を識別するための名前。")]
        private string _stepName = "Thorn Attack Step";


        [Header("--- 左側からの攻撃 ---")]

        [SerializeField]
        [Tooltip("ボスが左側から登場するときの開始位置。")]
        private Vector3 _leftStartLocalPosition = new Vector3(-10f, -5f, 0f);

        [SerializeField]
        [Tooltip("ボスが左側から登場するときの開始角度。")]
        private Vector3 _leftStartEulerAngles = Vector3.zero;


        [Header("--- 右側からの攻撃 ---")]

        [SerializeField]
        [Tooltip("ボスが右側から登場するときの開始位置。")]
        private Vector3 _rightStartLocalPosition = new Vector3(10f, -5f, 0f);

        [SerializeField]
        [Tooltip("ボスが右側から登場するときの開始角度")]
        private Vector3 _rightStartEulerAngles = new Vector3(0f, 90f, 0f);


        [Header("--- アニメーション設定 ---")]

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("この段階でのAnimator再生速度")]
        private float _animationSpeed = 1f;


        [Header("--- 防衛バリアへの攻撃可否 ---")]

        [SerializeField]
        [Tooltip("この段階で防衛バリアへ攻撃可能かどうか。")]
        private bool _canDamageBarrier;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// Inspector上で表示する攻撃段階名
        /// </summary>
        public string StepName => _stepName;

        /// <summary>
        /// ボスが左側から登場するときの開始位置
        /// </summary>
        public Vector3 LeftStartLocalPosition => _leftStartLocalPosition;

        /// <summary>
        /// ボスが右側から登場するときの開始位置
        /// </summary>
        public Vector3 RightStartLocalPosition => _rightStartLocalPosition;

        /// <summary>
        /// ボスが左側から登場するときの開始角度
        /// </summary>
        public Vector3 LeftStartEulerAngles => _leftStartEulerAngles;

        /// <summary>
        /// ボスが右側から登場するときの開始角度
        /// </summary>
        public Vector3 RightStartEulerAngles => _rightStartEulerAngles;

        /// <summary>
        /// Animatorへ設定する再生速度
        /// </summary>
        public float AnimationSpeed => _animationSpeed;

        /// <summary>
        /// この段階で防衛バリアへ攻撃可能かどうか
        /// </summary>
        public bool CanDamageBarrier => _canDamageBarrier;
    }
}

