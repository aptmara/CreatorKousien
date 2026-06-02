// ================================================================================
// File         : ShardEmissionVector.cs
// Description  : Scene-editable origin, target, and angle for shard emission.
// ================================================================================

using UnityEngine;

namespace Game.Gameplay.Collectibles
{
    /// <summary>
    /// 欠片の射出元、射出先、射出角度を Scene 上で設定するコンポーネント。
    /// 射出処理は持たず、CrystalShardEmitter へ方向情報を提供する。
    /// </summary>
    public sealed class ShardEmissionVector : MonoBehaviour
    {
        /// <summary>
        /// 欠片を生成する位置。未設定の場合はこのコンポーネント自身の Transform を使う。
        /// </summary>
        [SerializeField] private Transform _origin;

        /// <summary>
        /// 欠片を飛ばす中心位置。Scene 上でこの Transform を移動すると射出方向が変わる。
        /// </summary>
        [SerializeField] private Transform _target;

        /// <summary>
        /// Target 方向から何度まで欠片を散らすか。
        /// </summary>
        [SerializeField, Range(0f, 89f)] private float _emissionAngle = 35.0f;

        /// <summary>
        /// 欠片を生成するワールド座標。
        /// </summary>
        public Vector3 OriginPosition => _origin != null ? _origin.position : transform.position;

        /// <summary>
        /// 射出範囲の中心となる正規化済み方向。
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                if (_target == null)
                {
                    return Vector3.forward;
                }

                Vector3 direction = _target.position - OriginPosition;
                return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
            }
        }

        /// <summary>
        /// 欠片を散らす最大角度。
        /// </summary>
        public float EmissionAngle => Mathf.Clamp(_emissionAngle, 0f, 89f);

        /// <summary>
        /// 射出先が設定され、方向計算を行えるかを返す。
        /// </summary>
        public bool HasTarget => _target != null;

        /// <summary>
        /// Scene 上で射出元、中心方向、射出可能範囲を表示する。
        /// オレンジ色の矢印が中心方向、水色の円錐が散らばる範囲を表す。
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_target == null)
            {
                return;
            }

            Vector3 origin = OriginPosition;
            Vector3 targetOffset = _target.position - origin;
            float length = targetOffset.magnitude;
            if (length < 0.01f)
            {
                return;
            }

            Vector3 centerDirection = targetOffset.normalized;
            float radius = Mathf.Tan(EmissionAngle * Mathf.Deg2Rad) * length;

            Gizmos.color = new Color(0.1f, 0.8f, 1.0f, 0.9f);
            Gizmos.DrawWireSphere(origin, 0.05f);

            const int segmentCount = 16;
            Vector3 perpendicular = GetPerpendicular(centerDirection);
            Vector3 previousPoint = Vector3.zero;

            for (int i = 0; i <= segmentCount; i++)
            {
                float rotation = 360f * i / segmentCount;
                Vector3 radialDirection = Quaternion.AngleAxis(rotation, centerDirection) * perpendicular;
                Vector3 point = origin + centerDirection * length + radialDirection * radius;

                Gizmos.DrawLine(origin, point);

                if (i > 0)
                {
                    Gizmos.DrawLine(previousPoint, point);
                }

                previousPoint = point;
            }

            DrawDirectionArrow(origin, centerDirection, length);
        }

        /// <summary>
        /// Scene 上で欠片の中心方向を確認できる矢印 Gizmo を描画する。
        /// </summary>
        private static void DrawDirectionArrow(Vector3 origin, Vector3 direction, float length)
        {
            Vector3 tip = origin + direction * length;
            float headLength = Mathf.Min(length * 0.25f, 0.5f);
            Vector3 perpendicular = GetPerpendicular(direction);
            Vector3 secondPerpendicular = Vector3.Cross(direction, perpendicular).normalized;

            Gizmos.color = new Color(1.0f, 0.35f, 0.1f, 1.0f);
            Gizmos.DrawLine(origin, tip);
            Gizmos.DrawLine(tip, tip - direction * headLength + perpendicular * headLength * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * headLength - perpendicular * headLength * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * headLength + secondPerpendicular * headLength * 0.5f);
            Gizmos.DrawLine(tip, tip - direction * headLength - secondPerpendicular * headLength * 0.5f);
        }

        /// <summary>
        /// 指定方向と直交するベクトルを作る。
        /// </summary>
        private static Vector3 GetPerpendicular(Vector3 direction)
        {
            Vector3 axis = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) < 0.99f
                ? Vector3.up
                : Vector3.right;
            return Vector3.Cross(direction, axis).normalized;
        }
    }
}
