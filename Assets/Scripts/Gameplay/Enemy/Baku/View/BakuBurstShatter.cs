// ------------------------------------------------------------
// File		: BakuBurstShatter.cs
// Summary	: 破裂時に本体メッシュを分割して吹き飛ばす
//
// Author	: [浅野 勇生]
// Created	: 2026-08-23
//
// Notes	:
// - MeshRenderer / SkinnedMeshRenderer 両対応
// - Skinnedの場合はBakeMeshで破裂した瞬間のポーズを焼いてから分割する
// ------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;
using static UnityEditor.Searcher.SearcherWindow.Alignment;
using static UnityEngine.UI.Image;

namespace Game.Gameplay.Enemy.Baku
{
    /// <summary>
    /// 本体メッシュの三角形を3Dボロノイでクラスタ分けし、破片として飛散させる
    /// BakuControllerと同じGameObject（EnemyBodyのルート）にアタッチする
    /// </summary>
    public class BakuBurstShatter : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("分割元のRenderer")]
        [SerializeField] private Renderer _sourceRenderer;

        [Header("分裂")]
        [Tooltip("破裂の個性")]
        [SerializeField, Range(2, 256)] private int _pieceCount = 40;
        [Tooltip("分割前に三角形を4分割する回数。低ポリでも細かく砕ける（1で4倍、2で16倍）")]
        [SerializeField, Range(0, 3)] private int _subdivisions = 2;
        [Tooltip("破片形状の乱数シード")]
        [SerializeField] private int _randomSeed = 4815;

        [Header("物理")]
        [SerializeField, Min(0f)] private float _explosionForce = 9f;
        [Tooltip("上方向へのバイアス。0で全方位均等、大きいほど打ち上がる")]
        [SerializeField, Min(0f)] private float _upwardBias = 0.35f;
        [Tooltip("初速のばらつき。0.25なら±25%")]
        [SerializeField, Range(0f, 1f)] private float _forceVariation = 0.25f;
        [SerializeField, Min(0f)] private float _torque = 8f;
        [SerializeField, Min(0.001f)] private float _pieceMass = 0.4f;
        [Tooltip("破片同士・地面との衝突")]
        [SerializeField] private bool _usePieceCollision = false;

        [Header("消滅")]
        [SerializeField, Min(0f)] private float _lifetime = 2.5f;
        [SerializeField, Min(0f)] private float _shrinkDuration = 0.6f;

        [Header("立体化")]
        [Tooltip("破片の厚み")]
        [SerializeField, Range(0f, 1f)] private float _capDepth = 1f;


        /// <summary>
        /// 破片のメッシュとマテリアルインデックスを保持する構造体
        /// </summary>
        private struct PieceData
        {
            public Mesh Mesh;
            public int MaterialIndex;
        }


        private void Awake()
        {
            if (_sourceRenderer != null)
            {
                return;
            }

            // アニメ付きモデルを優先して探す
            _sourceRenderer = GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (_sourceRenderer == null)
            {
                _sourceRenderer = GetComponentInChildren<MeshRenderer>(true);
            }
        }


        /// <summary>
        /// 本体メッシュを破片へ置き換えて吹き飛ばす
        /// </summary>
        /// <param name="explosionCenter">爆心地</param>
        public void Shatter(Vector3 explosionCenter)
        {
            if (_sourceRenderer == null)
            {
                return;
            }

            Mesh source;
            Vector3 pieceScale;
            bool isSnapshot = false;

            if (_sourceRenderer is SkinnedMeshRenderer skinned)
            {
                if (skinned.sharedMesh == null)
                {
                    return;
                }

                // スキニングメッシュの場合はBakeMeshで破裂した瞬間のポーズを焼く
                source = new Mesh { name = "BakuBakedSnapshot" };
                skinned.BakeMesh(source, true);
                pieceScale = Vector3.one;
                isSnapshot = true;
            }
            else
            {
                MeshFilter filter = _sourceRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    return;
                }

                source = filter.sharedMesh;
                pieceScale = filter.transform.lossyScale;
            }

            if (!source.isReadable)
            {
                Debug.LogError($"[BakuBurstShatter] メッシュ '{source.name}' が読み取り不可です。モデルのImport Settingsで Read/Write Enabled をONにしてください。", this);
                if (isSnapshot) Destroy(source);
                return;
            }

            Transform sourceTransform = _sourceRenderer.transform;
            Material[] materials = _sourceRenderer.sharedMaterials;

            // 元の見た目を消してから破片へ差し替える
            _sourceRenderer.enabled = false;

            // 爆心は見た目の中心。BakuControllerが渡す座標は足元基準でズレるため使わない
            Vector3 origin = sourceTransform.TransformPoint(source.bounds.center);

            foreach (PieceData piece in BuildPieces(source))
            {
                SpawnPiece(piece, sourceTransform, pieceScale, materials, origin);
            }

            // ストップショットは破片側へコピー済みなのですてる
            if (isSnapshot)
            {
                Destroy(source);
            }
        }


        private List<PieceData> BuildPieces(Mesh source)
        {
            List < Vector3 > vertices = new List<Vector3>(source.vertices);
            List < Vector3 > normals = new List<Vector3>(source.normals);
            List < Vector2 > uvs = new List<Vector2>(source.uv);

            // 細分化で要素数が増えるので、判定は必ず先に済ませておく
            bool hasNormals = normals.Count == vertices.Count;
            bool hasUvs = uvs.Count == vertices.Count;

            // 三角形を「どのサブメッシュに属するか」を保ったまま集める
            List<int> triangleSubMesh = new List<int>();
            List<int> triangleIndices = new List<int>();

            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                int[] triangles = source.GetTriangles(sub);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    triangleSubMesh.Add(sub);
                    triangleIndices.Add(triangles[i]);
                    triangleIndices.Add(triangles[i + 1]);
                    triangleIndices.Add(triangles[i + 2]);
                }

                // 低ポリのモデルでも細かく砕けるように、事前に三角形を4分割しておく
                for (int i = 0; i < _subdivisions; i++)
                {
                    Subdivide(vertices, normals, uvs, triangleSubMesh, triangleIndices, hasNormals, hasUvs);
                }
            }

            int triangleCount = triangleSubMesh.Count;
            int pieceCount = Mathf.Max(2, _pieceCount);

            // 破片の核をBounds内へランダムに撒く
            Random.State previousState = Random.state;
            Random.InitState(_randomSeed);

            Bounds bounds = source.bounds;
            Vector3[] sites = new Vector3[pieceCount];
            for (int i = 0; i < pieceCount; i++)
            {
                sites[i] = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                );
            }

            Random.state = previousState;

            // 各三角形を、重心が一番近い核へ割り当てる
            List<int>[] buckets = new List<int>[pieceCount];
            for (int i = 0; i < pieceCount; i++)
            {
                buckets[i] = new List<int>();
            }

            for (int t = 0; t < triangleCount; t++)
            {
                Vector3 centroid = (vertices[triangleIndices[t * 3]]
                                  + vertices[triangleIndices[t * 3 + 1]]
                                  + vertices[triangleIndices[t * 3 + 2]]) / 3f;

                int nearest = 0;
                float nearestDistance = float.MaxValue;
                for (int i = 0; i < pieceCount; i++)
                {
                    float distance = (sites[i] - centroid).sqrMagnitude;
                    if (distance >= nearestDistance) continue;
                    nearestDistance = distance;
                    nearest = i;
                }

                buckets[nearest].Add(t);
            }

            List<PieceData> result = new List<PieceData>(pieceCount);
            int[] subMeshVotes = new int[source.subMeshCount];

            // UVシーㇺなどで分裂した頂点を位置で同一視する
            int[] weldIds = BuildWeldIds(vertices);
            Vector3 meshCenter = source.bounds.center;

            foreach (List<int> bucket in buckets)
            {
                if (bucket.Count == 0) continue;

                // 破片の三角形を集める
                List<Vector3> pieceVertices = new List<Vector3>(bucket.Count * 3);
                List<Vector3> pieceNormals = hasNormals ? new List<Vector3>(bucket.Count * 3) : null;
                List<Vector2> pieceUvs = hasUvs ? new List<Vector2>(bucket.Count * 3) : null;
                List<int> pieceTriangles = new List<int>(bucket.Count * 3);

                System.Array.Clear(subMeshVotes, 0, subMeshVotes.Length);

                foreach (int t in bucket)
                {
                    subMeshVotes[triangleSubMesh[t]]++;

                    for (int k = 0; k < 3; k++)
                    {
                        int index = triangleIndices[t * 3 + k];
                        pieceTriangles.Add(pieceVertices.Count);
                        pieceVertices.Add(vertices[index]);
                        if (hasNormals) pieceNormals.Add(normals[index]);
                        if (hasUvs) pieceUvs.Add(uvs[index]);
                    }
                }

                // --- 立体化 ---
                // 1回しか使われていないエッジ = クラスタの境界。そこを中心へ向かって塞ぐ
                Dictionary<long, int> edgeCount = new Dictionary<long, int>();
                Dictionary<long, Vector2Int> edgeSource = new Dictionary<long, Vector2Int>();

                foreach (int t in bucket)
                {
                    int i0 = triangleIndices[t * 3];
                    int i1 = triangleIndices[t * 3 + 1];
                    int i2 = triangleIndices[t * 3 + 2];

                    CountEdge(weldIds, edgeCount, edgeSource, i0, i1);
                    CountEdge(weldIds, edgeCount, edgeSource, i1, i2);
                    CountEdge(weldIds, edgeCount, edgeSource, i2, i0);
                }

                // 錐の頂点。_capDepthが1ならメッシュ中心まで届く
                Vector3 clusterCenter = Vector3.zero;
                foreach (Vector3 v in pieceVertices) clusterCenter += v;
                clusterCenter /= pieceVertices.Count;

                Vector3 apex = Vector3.Lerp(clusterCenter, meshCenter, _capDepth);

                foreach (KeyValuePair<long, int> edge in edgeCount)
                {
                    // 2回使われているエッジはクラスタ内部なので塞ぐ必要がない
                    if (edge.Value != 1) continue;

                    Vector2Int ends = edgeSource[edge.Key];

                    // 表面と逆巻きにすることで、閉じた立体として法線が外を向く
                    Vector3 pa = vertices[ends.y];
                    Vector3 pb = vertices[ends.x];
                    Vector3 faceNormal = Vector3.Cross(pb - pa, apex - pa).normalized;

                    pieceTriangles.Add(pieceVertices.Count);
                    pieceVertices.Add(pa);
                    pieceTriangles.Add(pieceVertices.Count);
                    pieceVertices.Add(pb);
                    pieceTriangles.Add(pieceVertices.Count);
                    pieceVertices.Add(apex);

                    if (hasNormals)
                    {
                        pieceNormals.Add(faceNormal);
                        pieceNormals.Add(faceNormal);
                        pieceNormals.Add(faceNormal);
                    }

                    if (hasUvs)
                    {
                        // 断面のUVは元の頂点から借りる
                        pieceUvs.Add(uvs[ends.y]);
                        pieceUvs.Add(uvs[ends.x]);
                        pieceUvs.Add((uvs[ends.x] + uvs[ends.y]) * 0.5f);
                    }
                }

                // その破片に一番多く含まれるサブメッシュのマテリアルを使う
                int materialIndex = 0;
                for (int i = 1; i < subMeshVotes.Length; i++)
                {
                    if (subMeshVotes[i] > subMeshVotes[materialIndex])
                    {
                        materialIndex = i;
                    }
                }

                Mesh mesh = new Mesh { name = "BakuFragment" };
                mesh.SetVertices(pieceVertices);
                mesh.SetTriangles(pieceTriangles, 0);
                if (hasNormals) mesh.SetNormals(pieceNormals);
                else mesh.RecalculateNormals();

                if (hasUvs)
                {
                    mesh.SetUVs(0, pieceUvs);
                }
                mesh.RecalculateBounds();

                result.Add(new PieceData
                {
                    Mesh = mesh,
                    MaterialIndex = materialIndex
                });
            }

            return result;
        }


        /// <summary>
        /// 位置が同じ頂点へ同じIDを振る
        /// </summary>
        /// <param name="vertices">指定した頂点の配列</param>
        /// <returns>変換したId</returns>
        private static int[] BuildWeldIds(List<Vector3> vertices)
        {
            // 位置の誤差を吸収するために小数点以下を丸める
            const float precision = 10000f;

            int[] ids = new int[vertices.Count];
            Dictionary<Vector3Int, int> table = new Dictionary<Vector3Int, int>(vertices.Count);

            // 頂点の位置を整数化して同一視する
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3Int key = new Vector3Int(
                    Mathf.RoundToInt(vertices[i].x * precision),
                    Mathf.RoundToInt(vertices[i].y * precision),
                    Mathf.RoundToInt(vertices[i].z * precision));

                if (!table.TryGetValue(key, out int id))
                {
                    id = table.Count;
                    table.Add(key, id);
                }

                ids[i] = id;
            }

            return ids;
        }


        /// <summary>
        /// 全三角形を4分割する
        /// </summary>
        /// <param name="vertices">頂点</param>
        /// <param name="normals">法線</param>
        /// <param name="uvs">UV</param>
        /// <param name="triangleSubMesh">サブメッシュ</param>
        /// <param name="triangleIndices">三角形インデックス</param>
        /// <param name="hasNormals">法線の有無</param>
        /// <param name="hasUvs">UVの有無</param>
        private static void Subdivide(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangleSubMesh, List<int> triangleIndices, bool hasNormals, bool hasUvs)
        {
            int triangleCount = triangleSubMesh.Count;

            List<int> newSubMesh = new List<int>(triangleCount * 4);
            List<int> newIndices = new List<int>(triangleCount * 12);

            // 同じ辺から作った中点を使い回すためのテーブル
            Dictionary<long, int> midpoints = new Dictionary<long, int>();

            for (int t = 0; t < triangleCount; t++)
            {
                int sub = triangleSubMesh[t];
                int i0 = triangleIndices[t * 3];
                int i1 = triangleIndices[t * 3 + 1];
                int i2 = triangleIndices[t * 3 + 2];

                // 辺の中点を作る
                int m01 = GetMidpoint(vertices, normals, uvs, midpoints, i0, i1, hasNormals, hasUvs);
                int m12 = GetMidpoint(vertices, normals, uvs, midpoints, i1, i2, hasNormals, hasUvs);
                int m20 = GetMidpoint(vertices, normals, uvs, midpoints, i2, i0, hasNormals, hasUvs);

                // 新しい4つの三角形を追加する
                AddTriangle(newSubMesh, newIndices, sub, i0, m01, m20);
                AddTriangle(newSubMesh, newIndices, sub, m01, i1, m12);
                AddTriangle(newSubMesh, newIndices, sub, m20, m12, i2);
                AddTriangle(newSubMesh, newIndices, sub, m01, m12, m20);
            }

            triangleSubMesh.Clear();
            triangleSubMesh.AddRange(newSubMesh);

            triangleIndices.Clear();
            triangleIndices.AddRange(newIndices);
        }


        /// <summary>
        /// 辺の中点を取得。 すでに作られていれば再利用！
        /// </summary>
        /// <param name="vertices">頂点</param>
        /// <param name="normals">法線</param>
        /// <param name="uvs">UV</param>
        /// <param name="cache">キャッシュ</param>
        /// <param name="a">頂点A</param>
        /// <param name="b">頂点B</param>
        /// <param name="hasNormals">法線の有無</param>
        /// <param name="hasUvs">UVの有無</param>
        /// <returns>頂点のインデックス</returns>
        private static int GetMidpoint(List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, Dictionary<long, int> cache, int a, int b, bool hasNormals, bool hasUvs)
        {
            // 頂点のインデックスを使ってエッジを一意に識別する
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int index)) return index;

            index = vertices.Count;
            vertices.Add((vertices[a] + vertices[b]) * 0.5f);
            if (hasNormals) normals.Add(((normals[a] + normals[b]) * 0.5f).normalized);
            if (hasUvs) uvs.Add((uvs[a] + uvs[b]) * 0.5f);

            cache.Add(key, index);
            return index;
        }


        /// <summary>
        /// 三角形を追加する
        /// </summary>
        private static void AddTriangle(List<int> subMesh, List<int> indices, int sub, int i0, int i1, int i2)
        {
            subMesh.Add(sub);
            indices.Add(i0);
            indices.Add(i1);
            indices.Add(i2);
        }


        /// <summary>
        /// エッジの使用回数を数える
        /// </summary>
        /// <param name="weldIds">ウェルドID</param>
        /// <param name="edgeCount">エッジの使用回数</param>
        /// <param name="edgeSource">エッジの元の頂点</param>
        /// <param name="from">最初の頂点</param>
        /// <param name="to">次の頂点</param>
        private static void CountEdge(int[] weldIds, Dictionary<long, int> edgeCount, Dictionary<long, Vector2Int> edgeSource, int from, int to)
        {
            // 頂点のウェルドIDを使ってエッジを一意に識別する
            int a = weldIds[from];
            int b = weldIds[to];
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;

            // すでに存在するエッジならカウントを増やす
            if (edgeCount.TryGetValue(key, out int count))
            {
                edgeCount[key] = count + 1;
                return;
            }

            edgeCount.Add(key, 1);
            edgeSource.Add(key, new Vector2Int(from, to));
        }


        /// <summary>
        /// 破片GameObjectを生成して爆風を与える
        /// </summary>
        /// <param name="piece">破片の構造体</param>
        /// <param name="sourceTransform">基準座標</param>
        /// <param name="pieceScale">破片のスケール</param>
        /// <param name="materials">マテリアルの配列</param>
        /// <param name="explosionCenter">爆発の中心</param>
        private void SpawnPiece(PieceData piece, Transform sourceTransform, Vector3 pieceScale, Material[] materials, Vector3 explosionCenter)
        {
            GameObject fragment = new GameObject("BakuFragment");

            // 敵ルートは破裂後すぐDestroyされるため、破片は親を持たせずワールド直下に出す
            fragment.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
            fragment.transform.localScale = pieceScale;

            fragment.AddComponent<MeshFilter>().sharedMesh = piece.Mesh;

            MeshRenderer renderer = fragment.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = materials.Length > 0 ? materials[Mathf.Clamp(piece.MaterialIndex, 0, materials.Length - 1)] : null;

            Rigidbody body = fragment.AddComponent<Rigidbody>();
            body.mass = _pieceMass;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // コライダー無しのRigidbodyは重心がローカル原点に固定される
            body.centerOfMass = piece.Mesh.bounds.center;

            // 破片同士の衝突を有効にするかどうか
            if (_usePieceCollision)
            {
                BoxCollider collider = fragment.AddComponent<BoxCollider>();
                collider.center = piece.Mesh.bounds.center;
                collider.size = piece.Mesh.bounds.size;
            }
            else
            {
                body.detectCollisions = false;
            }

            // 破片自身の中心を基準に、爆心から外向きへ飛ばす
            Vector3 pieceCenter = fragment.transform.TransformPoint(piece.Mesh.bounds.center);
            Vector3 direction = pieceCenter - explosionCenter;

            if (direction.sqrMagnitude < 0.0001f)
            {
                // 爆心と破片の中心がほぼ同じ場合は、上方向へ飛ばす
                direction = Random.onUnitSphere;
            }

            direction = (direction.normalized + Vector3.up * _upwardBias).normalized;

            // VelocityChangeなら質量に依存せず、同じ初速で飛ばせる！！
            float speed = _explosionForce * Random.Range(1f - _forceVariation, 1f + _forceVariation);
            body.AddForce(direction * speed, ForceMode.VelocityChange);
            body.AddTorque(Random.insideUnitSphere * _torque, ForceMode.VelocityChange);

            fragment.AddComponent<BakuFragmentLifetime>().Initialize(_lifetime, _shrinkDuration, piece.Mesh);
        }
    }


    /// <summary>
    /// 破片の寿命管理
    /// </summary>
    public class BakuFragmentLifetime : MonoBehaviour
    {
        private float _lifetime;
        private float _shrinkDuration = 0.6f;
        private Mesh _ownedMesh;
        private Vector3 _baseScale = Vector3.one;
        private float _elapsed;


        /// <summary>
        /// 破片の寿命と縮小時間を初期化する
        /// </summary>
        /// <param name="lifetime">生存時間</param>
        /// <param name="shrinkDuration">縮小時間</param>
        /// <param name="ownedMesh">所有するメッシュ</param>
        public void Initialize(float lifetime, float shrinkDuration, Mesh ownedMesh)
        {
            _lifetime = Mathf.Max(0f, lifetime);
            _shrinkDuration = Mathf.Max(0.01f, shrinkDuration);
            _ownedMesh = ownedMesh;
            _baseScale = transform.localScale;
        }


        /// <summary>
        /// 破片の寿命をカウントし、寿命が尽きたらDestroyする
        /// </summary>
        private void Update()
        {
            _elapsed += Time.deltaTime;
            if (_elapsed < _lifetime) return;

            float t = (_elapsed - _lifetime) / _shrinkDuration;
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            transform.localScale = _baseScale * (1f - t);
        }


        /// <summary>
        /// 破片のメッシュを破棄する
        /// </summary>
        private void OnDestroy()
        {
            // new Mesh() は自動解放されないので明示的に破棄する
            if (_ownedMesh != null)
            {
                Destroy(_ownedMesh);
            }
        }
    }
}
