// ------------------------------------------------------------
// File     : CameraFeedbackEvents.cs
// Summary  : カメラ演出を要求するためのイベントを定義する
//
// Author   : [浅野 勇生]
// Created  : 2026-07-17
//
// Notes:
// - イベントはカメラを直接操作しない。
// - 実際のカメラシェイクはPresentation側が担当する。
// ------------------------------------------------------------
namespace Game.Core.Events
{
    /// <summary>
    /// 任意の設定でカメラシェイクを開始するためのイベント
    /// </summary>
    public readonly struct CameraShakeRequestedEvent
    {
        /// <summary>
        /// シェイクの継続時間（秒）
        /// </summary>
        public readonly float Duration;

        /// <summary>
        /// シェイクの位置の強さ（単位：メートル）
        /// </summary>
        public readonly float PositionStrength;

        /// <summary>
        /// シェイクの回転の強さ（単位：度）
        /// </summary>
        public readonly float RotationStrength;

        /// <summary>
        /// シェイクの周波数（単位：Hz）
        /// </summary>
        public readonly float Frequency;

        /// <summary>
        /// カメラシェイク要求を作成する
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="positionStrength"></param>
        /// <param name="rotationStrength"></param>
        /// <param name="frequency"></param>
        public CameraShakeRequestedEvent(float duration, float positionStrength, float rotationStrength, float frequency)
        {
            Duration = duration;
            PositionStrength = positionStrength;
            RotationStrength = rotationStrength;
            Frequency = frequency;
        }
    }
}

