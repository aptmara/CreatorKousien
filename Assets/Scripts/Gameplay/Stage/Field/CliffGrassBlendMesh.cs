// ------------------------------------------------------------
// File		: CliffGrassBlendMesh.cs
// Summary	: 崖と草のブレンドメッシュを生成するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-10
//
// Notes	:
// - ベースのメッシュを生成するためのクラスです。
// ------------------------------------------------------------
using UnityEngine;

namespace Game.Gameplay.Stage
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class CliffGrassBlendMesh : MonoBehaviour
    {
        [Header("崖と草のブレンドメッシュの設定")]
        [Tooltip("崖の幅")]
        [SerializeField] private float _width = 10f;
        [Tooltip("崖の高さ")]
        [SerializeField] private float _height = 5f;


        [Header("丸いふち")]
        [Tooltip("上の丸いするの部分の高さ")]
        [SerializeField] private float _roundHeight = 1f;
        [Tooltip("崖側へ引っ込む奥行。大きいほど丸みが深くなる")]
        [SerializeField] private float _roundDepth = 0.7f;


        [Header("分割数")]
        [Tooltip("横方向の分割数")]
        [SerializeField] private int _segmentCount = 8;
        [Tooltip("まっすぐな崖部分の縦方向の分割数")]
        [SerializeField] private int _straightSegmentCount = 4;
        [Tooltip("丸いふち部分の分割数。大きいほど丸みが滑らかになる")]
        [SerializeField] private int _roundSegmentCount = 6;


        [Header("向き調整")]
        [Tooltip("丸みが逆側にでた時にONにする")]
        [SerializeField] private bool _invertDepthDirection = false;
        [Tooltip("面が裏向きで見えないときにONにする")]
        [SerializeField] private bool _flipFaces = false;


        [Header("自動生成")]
        [SerializeField] private bool _generateOnAwake = true;


        [Header("S字カーブ")]
        [Tooltip("崖の直線からカーブへ入るときの縦方向の引っ張り。大きいほど下側がゆるく曲がる")]
        [SerializeField] private float _lowerTangentRate = 0.45f;
        [Tooltip("草地面へつながるときの横方向の引っ張り。大きいほど上側がゆるく地面へつながる")]
        [SerializeField] private float _upperTangentRate = 0.65f;
        [Tooltip("S字の途中を外側へぐにゃっと膨らませる量。0なら普通の滑らかカーブ")]
        [SerializeField] private float _sCurveBulge = 0.25f;


        private void Awake()
        {
            if (_generateOnAwake)
            {
                Generate();
            }
        }


        /// <summary>
        /// Inspector上で値が変更されたときに呼び出されるメソッド
        /// </summary>
        private void OnValidate()
        {
            _width       = Mathf.Max(0.01f, _width);
            _height      = Mathf.Max(0.01f, _height);
            _roundHeight = Mathf.Clamp(_roundHeight, 0.01f, _height);
            _roundDepth  = Mathf.Max(0.01f, _roundDepth);

            _segmentCount         = Mathf.Max(1, _segmentCount);
            _straightSegmentCount = Mathf.Max(1, _straightSegmentCount);
            _roundSegmentCount    = Mathf.Max(1, _roundSegmentCount);

            _lowerTangentRate = Mathf.Clamp01(_lowerTangentRate);
            _upperTangentRate = Mathf.Clamp01(_upperTangentRate);
            _sCurveBulge = Mathf.Max(0f, _sCurveBulge);

            if (!Application.isPlaying)
            {
                Generate();
            }
        }


        /// <summary>
        /// 崖と草のブレンドメッシュを生成する
        /// </summary>
        public void Generate()
        {
            Mesh mesh = CreateBlendMesh();
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }


        /// <summary>
        /// 崖と草のブレンドメッシュを生成する
        /// </summary>
        /// <returns>Meshのインスタンス</returns>
        private Mesh CreateBlendMesh()
        {
            // メッシュの作成
            Mesh mesh = new Mesh();
            mesh.name = "Rounded Cliff Grass Blend Mesh";

            // メッシュの頂点、UV、三角形を生成
            int columnCount = _segmentCount + 1;

            // 縦方向の断面数。
            // まっすぐ部分 + 丸み部分 + 最初の1列
            int rowCount = _straightSegmentCount + _roundSegmentCount + 1;

            Vector3[] vertices = new Vector3[columnCount * rowCount];
            Vector2[] uvs      = new Vector2[columnCount * rowCount];

            int quadCountX = columnCount - 1;
            int quadCountY = rowCount - 1;
            int[] triangles = new int[quadCountX * quadCountY * 6];

            float halfWidth = _width * 0.5f;


            // 横から見た時の線上の点を作る
            for (int row = 0; row < rowCount; row++)
            {
                // この点はXを持たず、Y/Zだけを持つ断面用の点
                Vector3 profilePoint = GetProfilePoint(row);

                // UV.y は下が0, 上が1になるように正規化する
                float v = Mathf.InverseLerp(0f, _height, profilePoint.y);

                for (int column = 0; column < columnCount; column++)
                {
                    float u = column / (float)_segmentCount;
                    float x = Mathf.Lerp(-halfWidth, halfWidth, u);

                    int index = row * columnCount + column;

                    // X方向に同じ断面を押し出す
                    vertices[index] = new Vector3(x, profilePoint.y, profilePoint.z);

                    // uv.x は横方向、uv.y は崖から草へのブレンド方向
                    uvs[index] = new Vector2(u, v);
                }
            }

            int triIndex = 0;

            for (int row = 0; row < quadCountY; row++)
            {
                for (int column = 0; column < quadCountX; column++)
                {
                    int bottomLeft  = row * columnCount + column;
                    int topLeft     = bottomLeft + columnCount;
                    int bottomRight = bottomLeft + 1;
                    int topRight    = topLeft + 1;

                    AddQuadTriangles(triangles, ref triIndex, bottomLeft, bottomRight, topLeft, topRight);
                }
            }


            // メッシュに頂点、UV、三角形を設定
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            // 頂点を共有しているのから、RecalculateNormalsで丸み部分の光も滑らかに！！
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }


        /// <summary>
        /// 四角形の三角形インデックスを追加するヘルパーメソッド
        /// </summary>
        /// <param name="triangles"></param>
        /// <param name="triIndex"></param>
        /// <param name="bottomLeft"></param>
        /// <param name="bottomRight"></param>
        /// <param name="topLeft"></param>
        /// <param name="topRight"></param>
        private void AddQuadTriangles(int[] triangles, ref int triIndex, int bottomLeft, int bottomRight, int topLeft, int topRight)
        {
            if (!_flipFaces)
            {
                // 2つの三角形を追加
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = bottomRight;

                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
            }
            else
            {
                // 2つの三角形を追加（裏向き）
                triangles[triIndex++] = bottomLeft;
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topLeft;

                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = topLeft;
            }
        }

        /// <summary>
        /// プロファイルポイントを取得するヘルパーメソッド
        /// </summary>
        /// <param name="row">列</param>
        /// <returns>座標</returns>
        private Vector3 GetProfilePoint(int row)
        {
            // 丸み、S字カーブが始まる高さ
            float straightEndY = _height - _roundHeight;

            // falseなら崖側を-Zにする
            float depthSign = _invertDepthDirection ? 1f : -1f;

            // --- 1. まっすぐな崖部分の座標を計算する ---
            if (row <= _straightSegmentCount)
            {
                // まっすぐな崖部分
                // Zは一定なので、横から見ると直線になる
                float t = row / (float)_straightSegmentCount;

                float y = Mathf.Lerp(0f, straightEndY, t);
                float z = depthSign * _roundDepth;

                return new Vector3(0f, y, z);
            }

            // --- 2. S字カーブ部分 ---

            // curveT=0 が直線崖との接続点。
            // curveT=1 が草地面との接続点。
            float curveT = (row - _straightSegmentCount) / (float)_roundSegmentCount;
            curveT = Mathf.Clamp01(curveT);

            Vector2 p0 = new Vector2(depthSign * _roundDepth, straightEndY);
            Vector2 p3 = new Vector2(0f, _height);

            // 下側の制御点
            Vector2 p1 = p0 + new Vector2(0f, _roundHeight * _lowerTangentRate);
            // 上側の制御点
            Vector2 p2 = p3 + new Vector2(depthSign * _roundDepth * _upperTangentRate, 0f);


            // ベジェ曲線で基本の滑らかなS字カーブを作る。
            Vector2 point = EvaluateCubicBezier(p0, p1, p2, p3, curveT);

            // 途中だけ外側へ膨らませる
            float bulgeShape = Mathf.Sin(curveT * Mathf.PI);
            bulgeShape *= bulgeShape;

            point.x += depthSign * _sCurveBulge * bulgeShape;

            return new Vector3(0f, point.y, point.x);
        }



        /// <summary>
        /// ベジェ曲線を評価するヘルパーメソッド
        /// </summary>
        /// <param name="p0">始点</param>
        /// <param name="p1">始点側の曲がり方を決める制御点</param>
        /// <param name="p2">終点側の曲がり方を決める制御点</param>
        /// <param name="p3">終点</param>
        /// <param name="t"></param>
        /// <returns></returns>
        private Vector2 EvaluateCubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            // 0 - 1の範囲に丸める
            t = Mathf.Clamp01(t);

            // ベジェ曲線
            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * oneMinusT * p0 +
                   3f * oneMinusT * oneMinusT * t * p1 +
                   3f * oneMinusT * t * t * p2 +
                   t * t * t * p3;
        }


        /// <summary>
        /// Gizmosを描画するメソッド
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;

            // 縦方向の断面数
            int rowCount = _straightSegmentCount + _roundSegmentCount + 1;
            float halfWidth = _width * 0.5f;

            // 横から見た時の線上の点を描画する
            for (int row = 0; row < rowCount; row++)
            {
                Vector3 p = GetProfilePoint(row);

                Vector3 left  = transform.TransformPoint(new Vector3(-halfWidth, p.y, p.z));
                Vector3 right = transform.TransformPoint(new Vector3(halfWidth, p.y, p.z));

                Gizmos.DrawLine(left, right);
            }
        }
    }
}


