// ================================================================================
// File         : GameOverGateAnchor.cs
// Author       : Iwai Shogo
//
// Description  : Stageシーン内の門を参照し、起動時にコントローラーへ登録するコンポーネント
// Created      : 2026-07-10
// ================================================================================

using UnityEngine;

namespace Game.Presentation.GameOverCinematic
{
    /// <summary>
    /// Stageシーン内の門を参照し、起動時にコントローラーへ登録するコンポーネント
    /// </summary>
    public class GameOverGateAnchor : MonoBehaviour
    {
        [Header("--- 設定データ (範囲表示用) ---")]
        [SerializeField] private SO_GameOverCinematicSettings _settings;

        [Header("--- 門の各コンポーネント参照位置 ---")]
        [SerializeField] private Transform _leftDoorHinge;
        [SerializeField] private Transform _rightDoorHinge;
        [SerializeField] private Transform _dustSpawnPoint;

        public Transform LeftDoorHinge => _leftDoorHinge;
        public Transform RightDoorHinge => _rightDoorHinge;
        public Transform DustSpawnPoint => _dustSpawnPoint;

        private void Start()
        {
            // シーン上にあるコントローラーを探して自分を登録する
            var controller = FindFirstObjectByType<GameOverCinematicController>();
            if (controller != null)
            {
                controller.RegisterGate(this);
            }
        }

        /// <summary>
        /// ギズモによるエディター上での出現エリア可視化
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_settings == null) return;

            Vector3 center = _dustSpawnPoint != null ? _dustSpawnPoint.position : transform.position;
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            center.y += _settings.EnemyVisualYOffset;

            // 1. 遠くの出現ラインの左右の端
            Vector3 spawnLeft = center - (forward * _settings.SpawnLineDistance) - (right * (_settings.SpawnLineWidth * 0.5f));
            Vector3 spawnRight = center - (forward * _settings.SpawnLineDistance) + (right * (_settings.SpawnLineWidth * 0.5f));

            // 2. 目標集結ラインの左右の端
            Vector3 targetLeft = center - (forward * _settings.TargetLineDistance) - (right * (_settings.TargetLineWidth * 0.5f));
            Vector3 targetRight = center - (forward * _settings.TargetLineDistance) + (right * (_settings.TargetLineWidth * 0.5f));

            // 3. 門をくぐり抜けた最終消滅ラインの左右の端
            Vector3 disappearLeft = center + (forward * _settings.DisappearDepth) - (right * (_settings.TargetLineWidth * 0.5f));
            Vector3 disappearRight = center + (forward * _settings.DisappearDepth) + (right * (_settings.TargetLineWidth * 0.5f));

            // 出現エリアを描画
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnLeft, spawnRight);
            Gizmos.DrawLine(spawnLeft, targetLeft);
            Gizmos.DrawLine(spawnRight, targetRight);

            // 目標ラインから消滅までのエリアを描画
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetLeft, targetRight);
            Gizmos.DrawLine(targetLeft, disappearLeft);
            Gizmos.DrawLine(targetRight, disappearRight);
            Gizmos.DrawLine(disappearLeft, disappearRight);
        }
    }
}
