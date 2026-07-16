// ------------------------------------------------------------
// File     : BossIntroPresentationData.cs
// Summary  : ボス戦開始時の演出設定を保持する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-17
//
// Notes:
// - 開幕演出はボス戦全体で1回だけ再生する。
// - イバラタックルと同じAnimatorステートを使用する。
// - 演出中は棘を表示せず、バリアへの攻撃判定も開始しない。
// - カメラ位置は通常の固定位置からの相対オフセットで指定する。
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Data.Enemy.Boss
{
    /// <summary>
    /// ボス戦開始時のお披露目演出設定。
    ///
    /// このクラスが担当するもの:
    /// ・開幕演出を再生するかどうか
    /// ・ボスが登場する方向
    /// ・通常攻撃位置からのボス位置オフセット
    /// ・開幕アニメーションの再生速度
    /// ・通常カメラ位置からのオフセット
    /// ・カメラの移動時間と復帰時間
    /// </summary>
    [Serializable]
    public sealed class BossIntroPresentationData
    {
        [Header("--- 開幕演出の設定 ---")]

        [SerializeField]
        [Tooltip("ボス戦開始時に開幕演出を再生するかどうか")]
        private bool _isEnabled = true;

        [SerializeField]
        [Tooltip("ONの左側、OFFの場合は右側からボスを登場させる")]
        private bool _playFromLeft = true;


        [Header("--- ボスアニメーション設定 ---")]

        [SerializeField]
        [Tooltip("Phase 1の最初のイバラタックル開始位置へ加えるローカル座標オフセット")]
        private Vector3 _bossStartLocalPositionOffset = new Vector3(0f, 4f, 0f);

        [SerializeField]
        [Min(0.01f)]
        [Tooltip("開幕アニメーションの再生速度")]
        private float _animationSpeed = 0.25f;


        [Header("--- カメラ設定 ---")]

        [SerializeField]
        [Tooltip("通常カメラ位置からのオフセット")]
        private Vector3 _cameraPositionOffset = new Vector3(0f, 4f, -8f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("通常位置から開幕演出用の位置へカメラを移動させる時間(秒)")]
        private float _cameraMoveDuration = 0.6f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("開幕演出用の位置から通常位置へカメラを戻す時間(秒)")]
        private float _cameraReturnDuration = 0.6f;

        [SerializeField]
        [Tooltip("カメラ移動の進み方を制御するカーブ")]
        private AnimationCurve _cameraBlendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [SerializeField]
        [Min(0f)]
        [Tooltip("カメラ移動の進み方を制御するカーブの時間をスケーリングする係数")]
        private float _afterAnimationDelay = 0.25f;


        // 公開プロパティ
        // ------------------------------------------------------------

        /// <summary>
        /// 開幕演出を再生するかどうか。
        /// </summary>
        public bool IsEnabled => _isEnabled;

        /// <summary>
        /// 左側からボスを登場させるかどうか。
        /// </summary>
        public bool PlayFromLeft => _playFromLeft;

        /// <summary>
        /// 通常攻撃位置へ加えるボスのローカル座標オフセット。
        /// </summary>
        public Vector3 BossStartLocalPositionOffset =>
            _bossStartLocalPositionOffset;

        /// <summary>
        /// 開幕アニメーションの再生速度。
        /// </summary>
        public float AnimationSpeed => _animationSpeed;

        /// <summary>
        /// 通常の固定カメラ位置へ加えるオフセット。
        /// </summary>
        public Vector3 CameraPositionOffset => _cameraPositionOffset;

        /// <summary>
        /// 開幕用カメラ位置への移動時間。
        /// </summary>
        public float CameraMoveDuration => _cameraMoveDuration;

        /// <summary>
        /// 通常カメラ位置への復帰時間。
        /// </summary>
        public float CameraReturnDuration => _cameraReturnDuration;

        /// <summary>
        /// カメラ移動に使用する補間カーブ。
        /// </summary>
        public AnimationCurve CameraBlendCurve => _cameraBlendCurve;

        /// <summary>
        /// アニメーション終了後の待機時間。
        /// </summary>
        public float AfterAnimationDelay => _afterAnimationDelay;
    }
}

