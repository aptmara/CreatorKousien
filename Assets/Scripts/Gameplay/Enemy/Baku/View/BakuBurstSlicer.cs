// ------------------------------------------------------------
// File		: BakuBurstSlicer.cs
// Summary	: EzySliceでランダム平面カットを繰り返し、本体を粉砕する
//
// Author	: [浅野 勇生]
// Created	: 2026-08-23
//
// Notes	:
// - MeshFilter + MeshRenderer のみ対応
// - 破片数は 2^_sliceDepth
// ------------------------------------------------------------
using System.Collections.Generic;
using EzySlice;
using UnityEngine;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// 破裂時に本体メッシュをランダムな平面で再帰的に切り、破片として飛散させる。
    /// BakuControllerと同じGameObject（EnemyBodyのルート）にアタッチする
    /// </summary>
    public class BakuBurstSlicer : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("切る対象。未設定なら子のMeshRendererを自動取得")]
        [SerializeField] private MeshRenderer _sourceRenderer;
        [Tooltip("断面に貼るマテリアル。未設定なら本体と同じものを使う")]
        [SerializeField] private Material _crossSectionMaterial;

        [Header("分割")]
        [Tooltip("スライスの回数。破片数は2のn乗になる（4で16個）")]
        [SerializeField, Range(1, 5)] private int _sliceDepth = 4;
        [Tooltip("切る位置の偏り。0で必ず中央、大きいほど破片サイズがバラつく")]
        [SerializeField, Range(0f, 0.8f)] private float _sliceOffset = 0.35f;

        [Header("物理")]
        [Tooltip("破片の初速[m/s]")]
        [SerializeField, Min(0f)] private float _explosionForce = 9f;
        [Tooltip("上方向へのバイアス。0で全方位均等")]
        [SerializeField, Min(0f)] private float _upwardBias = 0.35f;
        [Tooltip("初速のばらつき。0.25なら±25%")]
        [SerializeField, Range(0f, 1f)] private float _forceVariation = 0.25f;
        [Tooltip("破片の回転速度[rad/s]")]
        [SerializeField, Min(0f)] private float _torque = 8f;
        [Tooltip("破片の質量[kg]")]
        [SerializeField, Min(0.001f)] private float _pieceMass = 0.4f;
        [Tooltip("この秒数だけ重力を切る。球状に広がりきってから落下する")]
        [SerializeField, Min(0f)] private float _gravityDelay = 0.35f;
        [Tooltip("空気抵抗。上げると失速して滞空時間が伸びる")]
        [SerializeField, Min(0f)] private float _linearDamping = 0.2f;
        [SerializeField, Min(0f)] private float _angularDamping = 0.05f;
        [Tooltip("破片同士・地面との衝突。重いのでデフォルトOFF")]
        [SerializeField] private bool _usePieceCollision = false;

        [Header("消滅")]
        [Tooltip("破片が消滅するまでの時間。0で即消滅")]
        [SerializeField, Min(0f)] private float _lifetime = 2.5f;
        [Tooltip("破片が縮小して消滅するまでの時間。0で即消滅")]
        [SerializeField, Min(0.01f)] private float _shrinkDuration = 0.6f;

        // モデル本体のMesh
        private Mesh _sourceMesh;

        private void Awake()
        {
            if (_sourceRenderer == null)
            {
                _sourceRenderer = GetComponentInChildren<MeshRenderer>(true);
            }
        }


        /// <summary>
        /// 本体を破片へ差し替えて吹き飛ばす
        /// </summary>
        public void Shatter()
        {
            if (_sourceRenderer == null)
            {
                Debug.LogWarning("BakuBurstSlicer: SourceRenderer is not assigned.");
                return;
            }

            // 本体のMeshを取得
            MeshFilter filter = _sourceRenderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                Debug.LogWarning("BakuBurstSlicer: SourceRenderer does not have a valid MeshFilter.");
                return;
            }

            _sourceMesh = filter.sharedMesh;

            Material crossSection = _crossSectionMaterial != null ? _crossSectionMaterial : _sourceRenderer.sharedMaterial;

            // 爆心は見た目の中心
            Vector3 origin = _sourceRenderer.bounds.center;

            GameObject work = CloneForSlicing(filter, _sourceRenderer);
            _sourceRenderer.enabled = false;

            List<GameObject> current = new List<GameObject> { work };

            for (int depth = 0; depth < _sliceDepth; depth++)
            {
                List<GameObject> next = new List<GameObject>(current.Count * 2);

                foreach (GameObject target in current)
                {
                    // 切れなかったものはそのまま破片として残す
                    if (!TrySlice(target, crossSection, next))
                    {
                        next.Add(target);
                    }
                }

                current = next;
            }

            foreach (GameObject piece in current)
            {
                SetupPiece(piece, origin);
            }
        }

        /// <summary>
        /// スライス用の複製を作る
        /// </summary>
        /// <param name="filter">対象のMeshFilter</param>
        /// <param name="renderer">対象のMeshRenderer</param>
        /// <returns>複製されたGameObject</returns>
        private static GameObject CloneForSlicing(MeshFilter filter, MeshRenderer renderer)
        {
            GameObject clone = new GameObject("BakuSliceRoot");

            // 親なしなので localPosition == ワールド座標になる
            clone.transform.SetPositionAndRotation(filter.transform.position, filter.transform.rotation);
            clone.transform.localScale = filter.transform.lossyScale;

            clone.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            clone.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;

            return clone;
        }


        /// <summary>
        /// ランダムな平面で1回スライス
        /// </summary>
        /// <param name="target">対象のGameObject</param>
        /// <param name="crossSection">断面材質</param>
        /// <param name="results">結果のリスト</param>
        /// <returns>切れたらtrue</returns>
        private bool TrySlice(GameObject target, Material crossSection, List<GameObject> results)
        {
            if (!target.TryGetComponent(out MeshRenderer renderer) || !target.TryGetComponent(out MeshFilter filter))
            {
                return false;
            }

            Bounds bounds = renderer.bounds;

            // 中心から少しずらした位置をランダムな向きで切る
            Vector3 point = bounds.center + Vector3.Scale(Random.insideUnitSphere * _sliceOffset, bounds.extents);
            Vector3 direction = Random.onUnitSphere;

            SlicedHull hull = target.Slice(point, direction, crossSection);
            if (hull == null)
            {
                return false;
            }

            // Transformはこの中でコピーされる
            GameObject upper = hull.CreateUpperHull(target, crossSection);
            GameObject lower = hull.CreateLowerHull(target, crossSection);

            if (upper == null || lower == null)
            {
                if (upper != null) Destroy(upper);
                if (lower != null) Destroy(lower);
                return false;
            }

            results.Add(upper);
            results.Add(lower);

            // 中間生成物のMeshは自動解放されないので、明示的に捨てる
            Mesh usedMesh = filter.sharedMesh;
            Destroy(target);
            if (usedMesh != null && usedMesh != _sourceMesh)
            {
                Destroy(usedMesh);
            }

            return true;
        }


        /// <summary>
        /// 破片に物理と寿命を設定して吹き飛ばす
        /// </summary>
        /// <param name="piece">破片のGameObject</param>
        /// <param name="origin">元の位置</param>
        private void SetupPiece(GameObject piece, Vector3 origin)
        {
            piece.name = "BakuFragment";

            Mesh mesh = piece.TryGetComponent(out MeshFilter filter) ? filter.sharedMesh : null;

            Vector3 pieceCenter = mesh != null ? piece.transform.TransformPoint(mesh.bounds.center) : piece.transform.position;

            Rigidbody body = piece.AddComponent<Rigidbody>();
            body.mass = _pieceMass;
            body.linearDamping = _linearDamping;
            body.angularDamping = _angularDamping;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            body.detectCollisions = _usePieceCollision;

            // コライダー無しのRigidbodyは重心がローカル原点に固定される
            if (mesh != null)
            {
                body.centerOfMass = mesh.bounds.center;
            }

            if (_usePieceCollision && mesh != null)
            {
                BoxCollider collider = piece.AddComponent<BoxCollider>();
                collider.center = mesh.bounds.center;
                collider.size = mesh.bounds.size;
            }

            // 爆心から外向きへ放射状に飛ばす
            Vector3 outward = pieceCenter - origin;
            if (outward.sqrMagnitude < 0.0001f)
            {
                outward = Random.onUnitSphere;
            }

            outward = (outward.normalized + Vector3.up * _upwardBias).normalized;

            // VelocityChangeなら質量に関係なく同じ初速になり、きれいな球状に広がる
            float speed = _explosionForce * Random.Range(1f - _forceVariation, 1f + _forceVariation);
            body.AddForce(outward * speed, ForceMode.VelocityChange);
            body.AddTorque(Random.insideUnitSphere * _torque, ForceMode.VelocityChange);

            // 本体のMeshアセットは破棄しない
            Mesh ownedMesh = mesh != _sourceMesh ? mesh : null;
            BakuFragmentLifetime lifetime = piece.AddComponent<BakuFragmentLifetime>();
            lifetime.Initialize(_lifetime, _shrinkDuration, ownedMesh);
            lifetime.SetGravityDelay(body, _gravityDelay);
        }
    }
}
