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
        public float BaseDoorKeepOpenDuration = 2.5f;
        [Tooltip("カメラが引き戻ってからプレイヤーがポンと復活するまでのディレイ")]
        public float PlayerReviveDelay = 0.5f;
        [Tooltip("プレイヤーが目を回してからリザルトに遷移するまでの待ち時間")]
        public float TransitionResultDelay = 2.5f;

        [Header("--- 敵のなだれ込み設定 ---")]
        [Tooltip("演出用に生成するダミー敵のプレハブ")]
        public GameObject DummyEnemyPrefab;
        [Tooltip("なだれ込ませる敵の総数")]
        public int DummyEnemyCount = 20;

        [Tooltip("基本の移動速度")]
        public float BaseEnemySpeed = 12.0f;
        [Tooltip("速度のランダムな揺らぎ幅")]
        public float SpeedVariation = 3.0f;
        [Tooltip("走り出すまでの最大ランダムディレイ (秒)")]
        public float MaxStartDelay = 0.6f;

        [Tooltip("敵の出現位置を上に浮かせる高さ")]
        public float EnemyVisualYOffset = 1.0f;

        [Header("--- なだれ込みルート設定 ---")]
        [Tooltip("門からどのくらい離れた場所にスポーンラインを置くか")]
        public float SpawnLineDistance = 15.0f;
        [Tooltip("画面外のスポーンラインの横幅")]
        public float SpawnLineWidth = 16.0f;
        [Tooltip("門からどのくらい離れた場所に目標集結ラインを置くか")]
        public float TargetLineDistance = 0.0f;
        [Tooltip("目標集結ラインの横幅）")]
        public float TargetLineWidth = 4.0f;
        [Tooltip("門の中心からどれだけ奥まで進んだら消滅させるか")]
        public float DisappearDepth = 4.0f;
    }
}
