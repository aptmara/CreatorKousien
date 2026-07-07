// ================================================================================
// File         : CartoonDistortion.cs
// Author       : Iwai Shogo
//
// Description  : 屋台モデルのメッシュ頂点を動的に操作し、カートゥーン風の演出するクラス。
// Created      : 2026-07-07
// ================================================================================

using UnityEngine;

namespace Game.Gameplay.Shop
{
    /// <summary>
    /// 屋台モデルのメッシュ頂点を動的に操作し、カートゥーン風の演出をするクラス
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class CartoonDistortion : MonoBehaviour
    {
        [Header("--- コンポーネント参照 ---")]
        [SerializeField] private MeshFilter _targetMeshFilter;

        [Header("--- カートゥーン変形パラメータ ---")]
        [Tooltip("前方への傾き度合い(走るときにプラス、バックでマイナス)")]
        [Range(-2f, 2f)] public float ShearX = 0f;

        [Tooltip("縦方向の引き延ばし倍率")]
        [Range(-1f, 2f)] public float SquashY = 0f;

        private Mesh _originalMesh;
        private Mesh _instancedMesh;
        private Vector3[] _originalVertices;
        private Vector3[] _modifiedVertices;

        // エディタ上でのパラメータ変更を反映するための変数
        private float _lastShearX;
        private float _lastShearY;

        private void Awake()
        {
            SetupMesh();
        }

        private void SetupMesh()
        {
            if (_targetMeshFilter == null)
            {
                _targetMeshFilter = GetComponent<MeshFilter>();
            }

            if (_targetMeshFilter != null && _targetMeshFilter.sharedMesh != null)
            {
                // 既にインスタンス化している場合はリターン
                if (_instancedMesh != null && _targetMeshFilter.sharedMesh == _instancedMesh)
                {
                    return;
                }

                // 元のメッシュデータを退避
                _originalMesh = _targetMeshFilter.sharedMesh;
                _originalVertices = _originalMesh.vertices;

                // 実行時書き換え用のインスタンスメッシュを生成
                _instancedMesh = Instantiate(_originalMesh);
                _modifiedVertices = new Vector3[_originalVertices.Length];
                _targetMeshFilter.mesh = _instancedMesh;
            }
            else
            {
                Debug.LogError($"[{nameof(CartoonDistortion)}] MeshFilterまたはMeshが見つからないぜよ！");
            }
        }

        private void LateUpdate()
        {
            ApplyDistortion();
        }

        private void ApplyDistortion()
        {
            // メッシュの初期化が外れていた場合
            if (_originalVertices == null || _instancedMesh == null)
            {
                SetupMesh();
                if (_originalVertices == null) return;
            }

            // 頂点変形マトリクスの適用
            for (int i = 0; i < _originalVertices.Length; i++)
            {
                Vector3 orig = _originalVertices[i];
                Vector3 modified = orig;

                // 1. 平行四辺形シアー変形
                // 高さが高い頂点ほどずらす
                modified.x += orig.y * ShearX;

                // 2. スクワッシュ & ストレッチ
                if (SquashY != 0f)
                {
                    // SquashY > 0 なら縦長
                    // SquashY < 0 なら平潰れ
                    modified.y *= (1f + SquashY);

                    float scaleDiv = Mathf.Sqrt(1f + SquashY);
                    if (scaleDiv > 0.001f)
                    {
                        modified.x /= scaleDiv;
                        modified.z /= scaleDiv;
                    }
                }

                _modifiedVertices[i] = modified;
            }

            // メッシュの更新
            _instancedMesh.vertices = _modifiedVertices;
            _instancedMesh.RecalculateBounds();
            _instancedMesh.RecalculateNormals();
        }

        private void OnDestroy()
        {
            if (_targetMeshFilter != null && _originalMesh != null)
            {
                _targetMeshFilter.mesh = _originalMesh;
            }
            if (Application.isPlaying)
            {
                Destroy(_instancedMesh);
            }
            else
            {
                DestroyImmediate(_instancedMesh);
            }
        }
    }
}
