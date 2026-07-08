// ------------------------------------------------------------
// File		: MockEnemy.cs
// Summary	: アイテム落下時の手触り検証用
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using System.Collections;
using UnityEngine;

namespace Game.Core.Enemy
{
    /// <summary>
    /// アイテム落下時の手触り検証用
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MockEnemy : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("演出設定")]
        [Tooltip("入りを変える対象のMeshRenderer")]
        [SerializeField] private MeshRenderer _meshRenderer;

        [Tooltip("揺れと点滅の継続時間")]
        [SerializeField] private float _feedbackDuration = 0.15f;

        [Tooltip("揺れの強さ")]
        [SerializeField] private float _shakeAmount = 0.5f;

        [Tooltip("点滅のカラー")]
        [SerializeField] private Color _flashColor = Color.red;

        private Color _originalColor;
        private Vector3 _originalPosition;
        private Coroutine _feedbackCoroutine;



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            if (_meshRenderer != null)
            {
                _originalColor = _meshRenderer.material.color;
            }
            _originalPosition = transform.position;
        }

        /// <summary>
        /// 落下してきたアイテムが触れた際の処理
        /// </summary>
        /// <param name="other">触れたオブジェクト</param>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Collectable"))
            {
                Destroy(other.gameObject);

                // 演出コルーチンの再生（連続ヒット時はリセットして再始動）
                if (_feedbackCoroutine != null)
                {
                    StopCoroutine(_feedbackCoroutine);
                    transform.position = _originalPosition;
                    if (_meshRenderer != null) _meshRenderer.material.color = _originalColor;
                }
                _feedbackCoroutine = StartCoroutine(PlayFeedbackRoutine());
            }
        }


        private IEnumerator PlayFeedbackRoutine()
        {
            float elapsed = 0f;

            while (elapsed < _feedbackDuration)
            {
                elapsed += Time.deltaTime;

                // 1. ランダムな横揺れ
                float offsetX = Random.Range(-_shakeAmount, _shakeAmount);
                float offsetZ = Random.Range(-_shakeAmount, _shakeAmount);
                transform.position = _originalPosition + new Vector3(offsetX, 0f, offsetZ);

                // 2. 点滅カラーの適用
                if (_meshRenderer != null)
                {
                    float flashRatio = Mathf.PingPong(elapsed * 15f, 1f);
                    _meshRenderer.material.color = Color.Lerp(_originalColor, _flashColor, flashRatio);
                }

                yield return null;
            }

            // 終了後は元の状態に戻す
            transform.position = _originalPosition;
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = _originalColor;
            }
        }
    }
}


