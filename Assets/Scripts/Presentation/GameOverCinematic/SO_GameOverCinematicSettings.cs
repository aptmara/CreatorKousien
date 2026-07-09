// ================================================================================
// File         : SO_GameOverCinematicSettings.cs
// Author       : Iwai Shogo
//
// Description  : ゲームオーバー演出の設定を格納する ScriptableObject。
// Created      : 2026-07-09
// ================================================================================

using UnityEngine;

namespace Game.Presentation.GameOverCinematic
{
    [CreateAssetMenu(fileName = "SO_GameOverCinematicSettings", menuName = "Game/Result/Game Over Cinematic Settings")]
    public class SO_GameOverCinematicSettings : ScriptableObject
    {
        [Header("--- カメラ設定 ---")]
        [Tooltip("門へズームインする時のカメラのワールド座標")]
        public Vector3 CameraZoomPosition = new Vector3(0f, 0f, 0f);
        [Tooltip("門へズームインする時のカメラの回転角")]
        public Vector3 CameraZoomRotation = new Vector3(20f, 0f, 0f);
        [Tooltip("ズームインにかかる時間")]
        public float ZoomInDuration = 1f;
        [Tooltip("手前にズームアウトする時間")]
        public float ZoomOutDuration = 0.8f;

        [Header("--- 扉の開閉アニメーション ---")]
        [Tooltip("扉が開くアニメーションカーブ (0: 閉 1: 全開")]
        public AnimationCurve DoorOpenCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("扉が開く時間")]
        public float DoorOpenDuration = 0.4f;

        [Tooltip("扉が閉まるアニメーションカーブ (1: 全開 0: 閉")]
        public AnimationCurve DoorCloseCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Tooltip("扉が閉まる時間")]
        public float DoorCloseDuration = 0.2f;

        [Tooltip("左右の扉が全開の時のY軸の回転角度")]
        public float MaxOpenAngle = 110f;

        [Header("--- 演出タイミング ---")]
        [Tooltip("扉が開いてから閉まり切るまでのキープ時間 (敵がなだれ込む時間)")]
        public float BaseDoorKeepOpenDuration = 2.0f;
        [Tooltip("カメラが引き戻ってからプレイヤーがポンと復活するまでのディレイ")]
        public float PlayerReviveDelay = 0.5f;
        [Tooltip("プレイヤーが目を回してからリザルトに遷移するまでの待ち時間")]
        public float TransitionResultDelay = 2.5f;
    }
}
