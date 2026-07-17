// ------------------------------------------------------------
// File     : BossCorpseHitReceiver.cs
// Summary  : ダウン・撃破演出中のボスへの攻撃をコンボとして換算するクラス
//
// Author   : [浅野勇生]
// Created  : 2026-07-17
//
// Notes:
// - ダメージは一切与えず、EnemyHitBatchEventの発行のみを行う。
// - コンボ加算・VFX・ポップアップは既存のイベント購読側が処理する。
// ------------------------------------------------------------
using System.Collections.Generic;
using Game.Gameplay.Collectibles;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class BossCorpseHitReceiver : MonoBehaviour
    {
        [Header("--- ヒット設定 ---")]

        [SerializeField]
        [Min(0f)]
        [Tooltip("この速度未満の衝突はヒットとして扱わない")]
        private float _minimumHitSpeed = 0.75f;

        [SerializeField]
        [Min(0f)]
        [Tooltip("ポップアップ表示用のダメージの倍率")]
        private float _displayDamageMultiplier = 2.5f;

        private string _bossInstanceId;

        private readonly Dictionary<int, float> _nextHitTimes = new Dictionary<int, float>();

        /// <summary>
        /// ボス個体を識別するためのIDを設定する
        /// </summary>
        /// <param name="bossInstanceId"></param>
        public void Initialize(string bossInstanceId)
        {
            _bossInstanceId = bossInstanceId;
        }


        private void OnDisable()
        {
            _nextHitTimes.Clear();
        }


        private void OnCollisionEnter(Collision collision)
        {
            CollectibleObject collectible = collision.collider.GetComponentInParent<CollectibleObject>();

            if (collectible == null)
            {
                return;
            }

            float hitSpeed = collision.relativeVelocity.magnitude;
            Vector3 hitPosition = collision.contactCount > 0 ? collision.GetContact(0).point : collision.collider.ClosestPoint(transform.position);

            TryApplyCollectibleHit(collectible, hitSpeed, hitPosition);
        }


        private void OnTriggerEnter(Collider other)
        {
            CollectibleObject collectible = other.GetComponentInParent<CollectibleObject>();

            if (collectible == null)
            {
                return;
            }

            Rigidbody attachedRigidbody = other.attachedRigidbody;
            float hitSpeed = attachedRigidbody != null ? attachedRigidbody.linearVelocity.magnitude : 0f;
            Vector3 hitPosition = other.ClosestPoint(transform.position);

            TryApplyCollectibleHit(collectible, hitSpeed, hitPosition);
        }


        /// <summary>
        /// 落し物のヒットをダメージなしのヒットイベントへ変換する
        /// </summary>
        /// <param name="collectible">落し物</param>
        /// <param name="hitSpeed">落とすスピード</param>
        /// <param name="hitPosition">座標</param>
        /// <returns></returns>
        private bool TryApplyCollectibleHit(CollectibleObject collectible, float hitSpeed, Vector3 hitPosition)
        {
            if (collectible == null || string.IsNullOrEmpty(_bossInstanceId))
            {
                return false;
            }

            if (hitSpeed < _minimumHitSpeed)
            {
                return false;
            }

            int itemInstanceId = collectible.GetInstanceID();

            // 同じ落し物が短時間に何度も衝突することを防ぐ
            if (_nextHitTimes.TryGetValue(itemInstanceId, out float nextHitTime) && Time.time < nextHitTime)
            {
                return false;
            }

            _nextHitTimes[itemInstanceId] = Time.time + Mathf.Max(0f, collectible.SameItemCooldown);

            // ポップアップ表示用のダメージ値
            float diplayDamage = Mathf.Max(1f, collectible.DamageAmount) * Mathf.Max(1f, hitSpeed) * _displayDamageMultiplier;

            // EnemyHitBatchEventが発行され、コンボ加算
            return collectible.ExecuteHitImpact(_bossInstanceId, diplayDamage, hitPosition, transform);
        }
    }
}
