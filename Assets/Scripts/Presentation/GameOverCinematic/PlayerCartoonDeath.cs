// ================================================================================
// File         : PlayerCartoonDeath.cs
// Author       : Iwai Shogo
//
// Description  : プレイヤーのカートゥーン風の死亡演出を制御するスクリプト。
// Created      : 2026-07-10
//
// Note         : ゲームオーバー用プレイヤーモデルのPrefabを生成するような処理を追加します！ - Asano 2026-07-13
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


        [Header("--- 潰れ方 ---")]
        [Tooltip("潰したときの横方向の倍率")]
        [SerializeField] private float _flattenWidthScale = 1.5f;

        [Tooltip("潰したときの縦方向の倍率")]
        [SerializeField] private float _flattenHeightScale = 0.05f;


        [Header("--- 戻る動き ---")]
        [Tooltip("元の形へ戻る時間")]
        [SerializeField] private float _reviveDuration = 0.25f;

        [Tooltip("戻るときに上方向へ伸びる量")]
        [SerializeField] private float _bounceHeight = 0.4f;

        [Tooltip("戻るときに横方向へ縮む量")]
        [SerializeField] private float _bounceWidth = 0.2f;


        [Header("--- 漫符エフェクト ---")]
        [SerializeField] private GameObject _dizzyStarEffect;

        private Vector3 _originalScale = Vector3.one;
        private Vector3 _flattenedPosition;
        private Vector3 _revivedPosition;
        private bool _hasPositionCorrection;

        private void Awake()
        {
            if (_modelTarget == null) _modelTarget = transform;
            _originalScale = _modelTarget.localScale;

            if (_dizzyStarEffect != null) _dizzyStarEffect.SetActive(false);
        }

        /// <summary>
        /// カメラが門を見ている間に、一瞬で潰す
        /// </summary>
        public void FlattenImmediately()
        {
            // カメラに見せたい向きへ固定
            transform.rotation = Quaternion.LookRotation(Vector3.back);

            // 縦を極限で潰す、横を少し広げる
            _modelTarget.localScale = new Vector3(_originalScale.x * _flattenWidthScale, _originalScale.y * _flattenHeightScale, _originalScale.z * _flattenWidthScale);

            // 潰れ状態では出現位置に固定
            if (_hasPositionCorrection)
            {
                transform.position = _flattenedPosition;
            }
        }


        /// <summary>
        /// 潰れたモデルをバウンドさせながら元の形に戻す演出を再生するコルーチン
        /// </summary>
        /// <returns></returns>
        public IEnumerator PlayReviveRoutine()
        {
            Vector3 flattenedScale = _modelTarget.localScale;

            float elapsed = 0f;

            while (elapsed < _reviveDuration)
            {
                elapsed += Time.deltaTime;

                float rate = Mathf.Clamp01(elapsed / _reviveDuration);

                // サイン波で跳ねるような挙動を追加
                float bounce = Mathf.Sin(rate * Mathf.PI);

                float currentX = Mathf.Lerp(flattenedScale.x, _originalScale.x, rate) - (bounce * _bounceWidth);
                float currentY = Mathf.Lerp(flattenedScale.y, _originalScale.y, rate) + (bounce * _bounceHeight);
                float currentZ = Mathf.Lerp(flattenedScale.z, _originalScale.z, rate) - (bounce * _bounceWidth);

                // スケールを更新
                _modelTarget.localScale = new Vector3(currentX, currentY, currentZ);


                // 大きくになるときに、位置を少し上に補正することで、潰れた状態から元の位置に戻るように見せる
                if (_hasPositionCorrection)
                {
                    float positionRate = Mathf.SmoothStep(0f, 1f, rate);

                    transform.position = Vector3.Lerp(_flattenedPosition, _revivedPosition, positionRate);
                }

                yield return null;
            }

            // 最終的に元のスケールに戻す
            _modelTarget.localScale = _originalScale;

            if (_hasPositionCorrection)
            {
                transform.position = _revivedPosition;
            }

            // 漫符エフェクトは未設定なら何もしない
            if (_dizzyStarEffect != null)
            {
                _dizzyStarEffect.SetActive(true);
                StartCoroutine(RotateStarsRoutine());
            }
        }


        /// <summary>
        /// 漫符エフェクトを回転させるコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator RotateStarsRoutine()
        {
            while (_dizzyStarEffect != null && _dizzyStarEffect.activeSelf)
            {
                _dizzyStarEffect.transform.Rotate(Vector3.up, 360f * Time.deltaTime);
                yield return null;
            }
        }


        /// <summary>
        /// プレイヤーの復活位置を補正するためのオフセットを設定する
        /// </summary>
        /// <param name="revivedPositionOffset">元の位置からのオフセット</param>
        public void ConfigurePositionCorrection(Vector3 revivedPositionOffset)
        {
            // 生成された位置を潰れた状態の位置に設定
            _flattenedPosition = transform.position;

            // 元に戻った時は、指定されたオフセットを加えた位置にする
            _revivedPosition = revivedPositionOffset + _flattenedPosition;

            _hasPositionCorrection = true;
        }
    }
}
