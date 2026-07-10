// ================================================================================
// File         : PlayerCartoonDeath.cs
// Author       : Iwai Shogo
//
// Description  : プレイヤーのカートゥーン風の死亡演出を制御するスクリプト。
// Created      : 2026-07-10
// ================================================================================

using Game.Gameplay.Player;
using System.Collections;
using UnityEngine;

namespace Game.Presentation.GameOverCinematic
{
    /// <summary>
    /// プレイヤーのカートゥーン風の死亡演出を制御するスクリプト。
    /// </summary>
    public sealed class PlayerCartoonDeath : MonoBehaviour
    {
        [Header("--- 変形対象のモデルルート ---")]
        [SerializeField] private Transform _modelTarget;

        [Header("--- 漫符エフェクト ---")]
        [SerializeField] private GameObject _dizzyStarEffect;

        private Vector3 _originalScale = Vector3.one;
        private PlayerAnimationController _animationController;

        private void Awake()
        {
            if (_modelTarget == null) _modelTarget = transform;
            _originalScale = _modelTarget.localScale;
            _animationController = GetComponentInChildren<PlayerAnimationController>();

            if (_dizzyStarEffect != null) _dizzyStarEffect.SetActive(false);
        }

        /// <summary>
        /// カメラが門を見ている間に、一瞬で潰す
        /// </summary>
        public void FlattenImmediately()
        {
            // 縦を極限で潰す、横を少し広げる
            _modelTarget.localScale = new Vector3(_originalScale.x * 1.5f, _originalScale.y * 0.05f, _originalScale.z * 1.5f);
        }

        /// <summary>
        /// 跳ねて比率が戻り、目を回す一連の挙動
        /// </summary>
        public IEnumerator PlayReviveAndDizzyRoutine()
        {
            // 1. スケールを戻す
            float elapsed = 0f;
            float duration = 0.25f;

            Vector3 flattenedScale = _modelTarget.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // サイン波で跳ねるような挙動を追加
                float bounceY = Mathf.Sin(t * Mathf.PI);
                float currentY = Mathf.Lerp(flattenedScale.y, _originalScale.y, t) + (bounceY * 0.4f);
                float currentXZ = Mathf.Lerp(flattenedScale.x, _originalScale.x, t) - (bounceY * 0.2f);

                _modelTarget.localScale = new Vector3(currentXZ, currentY, currentXZ);
                yield return null;
            }

            _modelTarget.localScale = _originalScale;

            // 2. 座り込みアニメーション
            if (_animationController != null)
            {
                // 検証用として、手持ちのアニメーションを再生
                _animationController.PlayPunch();
            }

            // 3. 漫符エフェクトを表示
            if (_dizzyStarEffect != null)
            {
                _dizzyStarEffect.SetActive(true);
                StartCoroutine(RotateStarsRoutine());
            }
        }

        private IEnumerator RotateStarsRoutine()
        {
            while (_dizzyStarEffect != null && _dizzyStarEffect.activeSelf)
            {
                _dizzyStarEffect.transform.Rotate(Vector3.up, 360f * Time.deltaTime);
                yield return null;
            }
        }
    }
}
