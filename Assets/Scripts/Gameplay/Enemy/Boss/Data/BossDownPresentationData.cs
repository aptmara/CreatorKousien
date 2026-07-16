// ------------------------------------------------------------
// File     : BossDownPresentationData.cs
// Summary  : ボスのダウン演出に使用するフェーズ別設定を保持する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-16
//
// Notes:
// - ダウン演出1回分の設定を保持する。
// - ダウン演出全体の時間はBossAngryBiteDataが保持する。
// - 各開始時間はダウン演出開始からの経過秒数で指定する。
// - 実際のVFX生成、カメラシェイク、落下処理は行わない。
// ------------------------------------------------------------
using UnityEngine;
using System;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// ボスのダウン演出設定。
    ///
    /// このクラスが担当するもの:
    /// ・フェーズごとのダウンエフェクト
    /// ・エフェクトの生成位置と開始時間
    /// ・カメラシェイクの設定
    /// ・ボスの落下距離と落下カーブ
    /// ・落下中の傾き
    /// ・落とし物を吐き出すタイミング
    /// </summary>
    [Serializable]
    public sealed class BossDownPresentationData
    {
        [Header("--- ダウンエフェクト ---")]

        [SerializeField]
        [Tooltip("このフェーズでアングリバイトに成功したさいに生成するダウンエフェクトプレファブ")]
        private GameObject _downEffectPrefab;

        [SerializeField]
        [Tooltip("ダウンエフェクトの生成位置")]
        private Vector3 _downEffectLocalOffset = Vector3.zero;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ダウン開始からエフェクトを生成するまでの時間")]
        private float _downEffectStartDelay;


        [Header("--- カメラシェイク ---")]

        [SerializeField]
        [Tooltip("ダウン開始からカメラシェイクをするかどうか")]
        private bool _enableCameraShake = true;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ダウン開始からカメラシェイクを開始するまでの時間（秒）")]
        private float _cameraShakeStartDelay;

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("カメラシェイクの持続時間（秒）")]
        private float _cameraShakeDuration = 0.35f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("カメラシェイクの揺らす強さ")]
        private float _cameraShakePositionStrength = 0.2f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("カメラシェイクの揺らす強さ")]
        private float _cameraShakeRotationStrength = 3f;

        [SerializeField]
        [Min(1f)]
        [Tooltip("カメラシェイクの振動数")]
        private float _cameraShakeFrequency = 35f;


        [Header("--- ボスの落下 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("ダウン開始からボスが落下するまでの時間（秒）")]
        private float _fallStartDelay = 0.15f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ダウン演出中にボスが落下する距離")]
        private float _fallDistance = 15f;

        [SerializeField]
        [Tooltip("ボスの落下カーブ, 横軸が落下時間、縦軸が落下の進行度")]
        private AnimationCurve _fallCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        [Tooltip("落下中のボスの傾き（相対角度）")]
        private Vector3 _fallEulerAngles = new Vector3(0f, 0f, 15f);


        [Header("--- 落とし物 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("ダウン開始から落とし物を吐き出すまでの時間（秒）")]
        private float _spitStartDelay = 0.2f;


        // 公開プロパティ
        // ------------------------------------------------------------
        /// <summary>
        /// ダウン時に表示するエフェクトPrefab。
        /// </summary>
        public GameObject DownEffectPrefab => _downEffectPrefab;

        /// <summary>
        /// ダウンエフェクトの生成相対座標。
        /// </summary>
        public Vector3 DownEffectLocalOffset => _downEffectLocalOffset;

        /// <summary>
        /// ダウンエフェクトを生成するまでの時間。
        /// </summary>
        public float DownEffectStartDelay => _downEffectStartDelay;

        /// <summary>
        /// ダウン時にカメラを揺らすかどうか。
        /// </summary>
        public bool EnableCameraShake => _enableCameraShake;

        /// <summary>
        /// カメラシェイクを開始するまでの時間。
        /// </summary>
        public float CameraShakeStartDelay => _cameraShakeStartDelay;

        /// <summary>
        /// カメラシェイクの持続時間。
        /// </summary>
        public float CameraShakeDuration => _cameraShakeDuration;

        /// <summary>
        /// カメラ位置を揺らす強さ。
        /// </summary>
        public float CameraShakePositionStrength => _cameraShakePositionStrength;

        /// <summary>
        /// カメラの回転を揺らす強さ。
        /// </summary>
        public float CameraShakeRotationStrength => _cameraShakeRotationStrength;

        /// <summary>
        /// カメラシェイクの振動数。
        /// </summary>
        public float CameraShakeFrequency => _cameraShakeFrequency;

        /// <summary>
        /// ボスが落下を開始するまでの時間。
        /// </summary>
        public float FallStartDelay => _fallStartDelay;

        /// <summary>
        /// ボスが落下する距離。
        /// </summary>
        public float FallDistance => _fallDistance;

        /// <summary>
        /// ボスの落下速度を制御するカーブ。
        /// </summary>
        public AnimationCurve FallCurve => _fallCurve;

        /// <summary>
        /// 落下完了時に加える相対角度。
        /// </summary>
        public Vector3 FallEulerAngles => _fallEulerAngles;

        /// <summary>
        /// 落とし物を吐き出すまでの時間。
        /// </summary>
        public float SpitStartDelay => _spitStartDelay;
    }
}
