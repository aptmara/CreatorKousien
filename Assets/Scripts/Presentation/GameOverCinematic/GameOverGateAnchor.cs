// ================================================================================
// File         : GameOverGateAnchor.cs
// Author       : Iwai Shogo
//
// Description  : Stageシーン内の門を参照し、起動時にコントローラーへ登録するコンポーネント
// Created      : 2026-07-10
// ================================================================================

using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [SerializeField] private Transform _gateEffectRoot;

        [Header("--- 扉開閉 (Fキー) ---")]
        [Tooltip("Fキーによる扉のトグル開閉を有効にするか")]
        [SerializeField] private bool _enableFKeyToggle = true;
        [Tooltip("開閉アニメーションにかかる時間（秒）")]
        [SerializeField, Min(0.01f)] private float _toggleDuration = 0.35f;
        [Tooltip("最大開放角度（度）")]
        [SerializeField] private float _customOpenAngle = 110f;

        public Transform LeftDoorHinge => _leftDoorHinge;
        public Transform RightDoorHinge => _rightDoorHinge;
        public Transform DustSpawnPoint => _dustSpawnPoint;
        public GameObject GateEffectRoot => _gateEffectRoot != null ? _gateEffectRoot.gameObject : null;

        private bool _isOpen;
        private Coroutine _doorCoroutine;

        private void Start()
        {
            // シーン上にあるコントローラーを探して自分を登録する
            var controller = FindFirstObjectByType<GameOverCinematicController>();
            if (controller != null)
            {
                controller.RegisterGate(this);
            }
        }

        private void Update()
        {
            if (!_enableFKeyToggle) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.fKey.wasPressedThisFrame)
            {
                ToggleDoor();
            }
        }

        /// <summary>
        /// 扉の開閉状態をトグル切り替えします
        /// </summary>
        public void ToggleDoor()
        {
            float targetAngle = _settings != null ? _settings.MaxOpenAngle : _customOpenAngle;
            _isOpen = !_isOpen;

            if (_doorCoroutine != null)
            {
                StopCoroutine(_doorCoroutine);
            }

            _doorCoroutine = StartCoroutine(AnimateDoorRoutine(_isOpen ? targetAngle : 0f));
        }

        private IEnumerator AnimateDoorRoutine(float targetAngle)
        {
            float startAngle = 0f;
            if (_leftDoorHinge != null)
            {
                startAngle = _leftDoorHinge.localEulerAngles.y;
                if (startAngle > 180f)
                {
                    startAngle -= 360f;
                }
            }

            float duration = Mathf.Max(0.01f, _toggleDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float currentAngle = Mathf.Lerp(startAngle, targetAngle, eased);

                SetDoorAngle(currentAngle);
                yield return null;
            }

            SetDoorAngle(targetAngle);
            _doorCoroutine = null;
        }

        private void SetDoorAngle(float angle)
        {
            if (_leftDoorHinge != null)
            {
                _leftDoorHinge.localRotation = Quaternion.Euler(0f, angle, 0f);
            }

            if (_rightDoorHinge != null)
            {
                _rightDoorHinge.localRotation = Quaternion.Euler(0f, -angle, 0f);
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
