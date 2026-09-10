// ================================================================================
// File         : CollectibleSpawnArea.cs
// Author       : Iwai Shogo
//
// Description  : 収集物が出現するエリアを定義するクラス。
// Created      : 2026-05-06
// ================================================================================

using UnityEngine;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// 収集物のスポーン範囲を定義します。Stage上に配置され、Spawnerに仲地を提供します。
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CollectibleSpawnArea : MonoBehaviour
    {
        private BoxCollider _areaCollider;

        /// <summary>
        /// このエリアのワールド空間での範囲を取得します。
        /// ボスの散布範囲をフィールドへ自動フィットさせる用途で参照します。
        /// Awakeより前に呼ばれても動作するよう、未取得ならここで取得します。
        /// </summary>
        public Bounds Bounds
        {
            get
            {
                if (_areaCollider == null)
                {
                    _areaCollider = GetComponent<BoxCollider>();
                }

                return _areaCollider != null
                    ? _areaCollider.bounds
                    : new Bounds(transform.position, Vector3.zero);
            }
        }

        private void Awake()
        {
            _areaCollider = GetComponent<BoxCollider>();
            _areaCollider.isTrigger = true;
        }

        /// <summary>
        /// コライダーの範囲内からランダムな座標を取得します
        /// </summary>
        public Vector3 GetRandomPosition()
        {
            if (_areaCollider == null) return transform.position;

            Bounds bounds = _areaCollider.bounds;
            float randomX = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
            float randomY = UnityEngine.Random.Range(bounds.min.y, bounds.max.y);
            float randomZ = UnityEngine.Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(randomX, randomY, randomZ);
        }
    }
}
