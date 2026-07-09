// ------------------------------------------------------------
// File		: SO_GameClearCinematicSettings.cs
// Summary	: ゲームクリアシネマティックの設定を保持するスクリプタブルオブジェクト
//
// Author	: [浅野 勇生]
// Created	: 2026-07-09
//
// Notes	:
// - クリア時のカメラ設定をまとめました！
//   SlowMotionTimeScale     : スローの強さ。0.2 なら20%速度。
//   SlowMotionDuration      : スロー演出そのものの長さ。
//   SlowMotionCameraOffset  : スロー中にカメラをどれだけズラすか。
//   FlashTimings            : スロー開始から何秒後に白フラッシュを出すか。
//   ZoomDuration            : スローが終わったあと、プレイヤーへカメラが寄る時間。
//   AfterClearAnimationDelay: Clear アニメーションを再生してからResult UIへ行くまでの待ち時間。
//   FlashTiming.StartTime   : スローモーション開始からの実時間秒
// ------------------------------------------------------------
using System;
using UnityEngine;

namespace Game.Presentation.GameClearCinematic
{
    [CreateAssetMenu(fileName = "SO_GameClearCinematicSettings", menuName = "Scriptable Objects/Game Clear Cinematic Settings")]
    public sealed class SO_GameClearCinematicSettings : ScriptableObject
    {
        [Header("前半: スローモーション")]
        [Tooltip("スローモーション中の時間の進み方を制御するためのTime.timeScaleの値")]
        [SerializeField, Range(0.01f, 1f)]
        private float _slowMotionTimeScale = 0.2f;

        [Tooltip("スローモーションの持続時間（秒）")]
        [SerializeField, Min(0f)]
        private float _slowMotionDuration = 1.2f;

        [Tooltip("スローモーション中のカメラの位置オフセット")]
        [SerializeField]
        private Vector3 _slowMotionCameraOffset = new Vector3(1.5f, 0.4f, -0.8f);

        [Header("前半: カメラシェイク")]
        [Tooltip("カメラシェイクの強さ")]
        [SerializeField, Min(0f)]
        private float _shakeStrength = 0.15f;

        [Tooltip("カメラシェイクの回転強さ")]
        [SerializeField, Min(0f)]
        private float _shakeRotationStrength = 2.5f;

        [Tooltip("カメラシェイクの周波数")]
        [SerializeField, Min(1f)]
        private float _shakeFrequency = 35f;


        [Header("前半: 白フラッシュ")]
        [SerializeField]
        private FlashTiming[] _flashTimings =
        {
            new FlashTiming(0.15f, 0.08f, 0.9f),
            new FlashTiming(0.45f, 0.06f, 0.75f),
            new FlashTiming(0.8f, 0.1f, 1f)
        };


        [Header("後半: プレイヤー寄り")]
        [SerializeField, Min(0f)]
        private float _zoomDuration = 1.0f;

        [Tooltip("プレイヤーのカメラ位置オフセット")]
        [SerializeField]
        private Vector3 _playerCameraOffset = new Vector3(0f, 1.6f, -3.0f);

        [Tooltip("プレイヤーの注視点オフセット")]
        [SerializeField]
        private Vector3 _playerLookAtOffset = new Vector3(0f, 1.0f, 0f);

        [Tooltip("プレイヤーの回転速度")]
        [SerializeField, Min(0f)]
        private float _playerTurnSpeed = 8.0f;


        [Header("クリアアニメーション後")]
        [Tooltip("クリア後のアニメーションが終わった後の遅延時間（秒）")]
        [SerializeField, Min(0f)]
        private float _afterClearAnimationDelay = 2.0f;

        public float SlowMotionTimeScale => _slowMotionTimeScale;               ///< スローモーション中の時間の進み方を制御するためのTime.timeScaleの値
        public float SlowMotionDuration => _slowMotionDuration;                 ///< スローモーションの持続時間（秒）
        public Vector3 SlowMotionCameraOffset => _slowMotionCameraOffset;       ///< スローモーション中のカメラの位置オフセット

        public float ShakeStrength => _shakeStrength;                           ///< カメラシェイクの強さ
        public float ShakeRotationStrength => _shakeRotationStrength;           ///< カメラシェイクの回転強さ
        public float ShakeFrequency => _shakeFrequency;                         ///< カメラシェイクの周波数

        public FlashTiming[] FlashTimings => _flashTimings;                     ///< フラッシュのタイミングを表す構造体の配列

        public float ZoomDuration => _zoomDuration;                             ///< ズームの持続時間（秒）
        public Vector3 PlayerCameraOffset => _playerCameraOffset;               ///< プレイヤーのカメラ位置オフセット
        public Vector3 PlayerLookAtOffset => _playerLookAtOffset;               ///< プレイヤーの注視点オフセット
        public float PlayerTurnSpeed => _playerTurnSpeed;                       ///< プレイヤーの回転速度

        public float AfterClearAnimationDelay => _afterClearAnimationDelay;     ///< クリア後のアニメーションが終わった後の遅延時間（秒）


        /// <summary>
        /// フラッシュのタイミングを表す構造体
        /// </summary>
        [Serializable]
        public struct FlashTiming
        {
            [Min(0f)]
            public float StartTime;

            [Min(0.01f)]
            public float Duration;

            [Range(0f, 1f)]
            public float Alpha;

            /// <summary>
            /// フラッシュのタイミングを設定する
            /// </summary>
            /// <param name="startTime">開始時間</param>
            /// <param name="duration">持続時間</param>
            /// <param name="alpha">アルファ値</param>
            public FlashTiming(float startTime, float duration, float alpha = 1f)
            {
                StartTime = startTime;
                Duration = duration;
                Alpha = alpha;
            }
        }
    }
}

