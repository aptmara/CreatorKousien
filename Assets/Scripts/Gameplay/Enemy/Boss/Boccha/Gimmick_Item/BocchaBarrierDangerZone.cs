// ------------------------------------------------------------
// File		: BocchaBarrierDangerZone.cs
// Summary	: ボッチャのバリアにダメージを与える危険圏
//
// Author	: [浅野 勇生]
// Created	: 2026-09-07
//
// Notes	:
// - 防衛ラインのDefenceLineはコライダーを持たないため、危険圏はシーン上に別途配置!!
// - staticで一覧を持つので、判定側はシーンを検索しなくて済むよーに！
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Enemy.Boss
{
    /// <summary>
    /// トゲ玉などを落とすとバリアにダメージが入る範囲
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public sealed class BocchaBarrierDangerZone : MonoBehaviour
    {
        private static readonly List<BocchaBarrierDangerZone> _zones = new List<BocchaBarrierDangerZone>();

        [SerializeField]
        [Tooltip("この危険圏でバリアへ与えるダメージの倍率")]
        [Min(0f)]
        private float _damageMultiplier = 1.0f;

        [SerializeField]
        [Tooltip("シーンビューでの表示色")]
        private Color _gizmoColor = new Color(1.0f, 0.2f, 0.2f, 0.25f);

        private BoxCollider _zoneCollider;


        /// <summary>この危険圏のダメージ倍率</summary>
        public float DamageMultiplier => _damageMultiplier;

        /// <summary>危険圏が1つでも配置されているかどうか</summary>
        public static bool HasAnyZone => _zones.Count > 0;


        private void Awake()
        {
            _zoneCollider = GetComponent<BoxCollider>();
            _zoneCollider.isTrigger = true;
        }

        private void OnEnable() => _zones.Add(this);

        private void OnDisable() => _zones.Remove(this);


        /// <summary>
        /// 指定座標が危険圏内かどうかを判定
        /// </summary>
        /// <param name="position">判定する座標</param>
        /// <param name="damageMultiplier">入っていた危険圏のダメージ倍率</param>
        /// <returns>いずれかの危険圏に入っていればtrue</returns>
        public static bool TryGetZone(Vector3 position, out float damageMultiplier)
        {
            for (int i = 0; i < _zones.Count; ++i)
            {
                BocchaBarrierDangerZone zone = _zones[i];

                if (zone == null || !zone.Contains(position)) continue;

                damageMultiplier = zone._damageMultiplier;

                return true;
            }

            damageMultiplier = 0.0f;

            return false;
        }


        /// <summary>
        /// この危険圏に指定座標が含まれるかどうか。
        /// </summary>
        public bool Contains(Vector3 position)
        {
            if (_zoneCollider == null)
            {
                _zoneCollider = GetComponent<BoxCollider>();
            }

            return _zoneCollider != null && _zoneCollider.bounds.Contains(position);
        }


#if UNITY_EDITOR
        // シーンビューで危険圏を可視化するためのGizmos描画
        private void OnDrawGizmos()
        {
            BoxCollider box = _zoneCollider != null ? _zoneCollider : GetComponent<BoxCollider>();

            if (box == null) return;

            // Gizmosの色を設定して、ローカル座標系で描画する
            Gizmos.color = _gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);

            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 1.0f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
#endif
    }
}
