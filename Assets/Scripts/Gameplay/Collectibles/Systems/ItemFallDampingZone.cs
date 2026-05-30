// ------------------------------------------------------------
// File		: ItemFallDampingZone.cs
// Summary	: Trigger型のアイテム落下減速ゾーン
//
// Author	: [浅野 勇生]
// Created	: 2026-05-30
//
// Notes	:
// - アイテムがこのゾーンに入ると、落下速度が減速する
// ------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

namespace Game.Gameplay.Collectibles
{
    [RequireComponent(typeof(Collider))]
    public class ItemFallDampingZone : MonoBehaviour
    {
        [Header("Horizontal Damping Settings")]
        [Tooltip("Trigger内で横方向速度を減らす強さ")]
        [SerializeField, Min(0f)] private float _horizontalDeceleration = 8.0f;

        [Tooltip("Trigger内で許可する最大横方向速度 (0なら制限なし)")]
        [SerializeField, Min(0f)] private float _maxHorizontalSpeed = 3.0f;

        [Header("下方向への加速設定")]
        [Tooltip("Trigger内で下方向への加速の強さ")]
        [SerializeField, Min(0f)] private float _extraDownwardAcceleration = 5.0f;

        private readonly HashSet<Rigidbody> _targets = new HashSet<Rigidbody>();
        private readonly List<Rigidbody> _removeBuffer = new List<Rigidbody>();

        /// <summary>
        /// AwakeでコライダーをTriggerに設定する
        /// </summary>
        private void Awake()
        {
            // コライダーはTriggerにする
            Collider col = GetComponent<Collider>();
            col.isTrigger = true;
        }


        /// <summary>
        /// Trigger内のRigidbodyの速度を減速する。無効なターゲットはリストから削除する。
        /// </summary>
        private void FixedUpdate()
        {
            _removeBuffer.Clear();

            foreach (Rigidbody target in _targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy)
                {
                    _removeBuffer.Add(target);
                    continue;
                }

                // 横方向の速度を減速
                Vector3 velocity = target.linearVelocity;
                Vector2 horizontalVelocity = new Vector2(velocity.x, velocity.z);

                if (_maxHorizontalSpeed > 0f && horizontalVelocity.magnitude > _maxHorizontalSpeed)
                {
                    horizontalVelocity = horizontalVelocity.normalized * _maxHorizontalSpeed;
                }

                horizontalVelocity = Vector2.MoveTowards(horizontalVelocity, Vector2.zero, _horizontalDeceleration * Time.fixedDeltaTime);

                velocity.x = horizontalVelocity.x;
                velocity.z = horizontalVelocity.y;

                // 下方向への加速
                if (_extraDownwardAcceleration > 0f)
                {
                    velocity.y -= _extraDownwardAcceleration * Time.fixedDeltaTime;
                }

                target.linearVelocity = velocity;
            }

            // 無効なターゲットを削除
            foreach (Rigidbody toRemove in _removeBuffer)
            {
                _targets.Remove(toRemove);
            }
        }

        /// <summary>
        /// CollectibleObjectがTriggerに入ったときに呼ばれる。Rigidbodyをターゲットリストに追加する。
        /// </summary>
        /// <param name="other">入ったオブジェクト</param>
        private void OnTriggerEnter(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();
            if (collectible == null)
            {
                return;
            }

            Rigidbody rb = collectible.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _targets.Add(rb);
            }
        }


        /// <summary>
        /// CollectibleObjectがTriggerから出たときに呼ばれる。Rigidbodyをターゲットリストから削除する。
        /// </summary>
        /// <param name="other">出たオブジェクト</param>
        private void OnTriggerExit(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();
            if (collectible == null)
            {
                return;
            }

            Rigidbody rb = collectible.GetComponent<Rigidbody>();
            if (rb != null)
            {
                _targets.Remove(rb);
            }
        }
    }
}

