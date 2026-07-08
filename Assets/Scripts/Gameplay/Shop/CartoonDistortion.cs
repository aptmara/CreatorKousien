// ================================================================================
// File         : CartoonDistortion.cs
// Author       : Iwai Shogo
//
// Description  : 屋台モデルのメッシュ頂点を動的に操作し、カートゥーン風の演出するクラス。
// Created      : 2026-07-07
// ================================================================================

using UnityEngine;
using System.Collections.Generic;
using System.Xml;

namespace Game.Gameplay.Shop
{
    /// <summary>
    /// 屋台モデルのメッシュ頂点を動的に操作し、カートゥーン風の演出をするクラス
    /// </summary>
    [ExecuteAlways]
    public class CartoonDistortion : MonoBehaviour
    {
        // 内部でメッシュごとのデータを管理するための構造体
        private struct MeshData
        {
            public MeshFilter filter;
            public Mesh originalMesh;
            public Mesh instancedMesh;
            public Vector3[] originalVertices;
            public Vector3[] modifiedVertices;
        }

        [Header("--- カートゥーン変形パラメータ ---")]
        [Tooltip("前方への傾き度合い(走るときにプラス、バックでマイナス)")]
        [Range(-2f, 2f)] public float ShearX = 0f;

        [Tooltip("縦方向の引き延ばし倍率")]
        [Range(-1f, 2f)] public float SquashY = 0f;

        private List<MeshData> _meshDataList = new List<MeshData>();
        private bool _isInitialized = false;

        private void Awake()
        {
            SetupAllMesh();
        }

        /// <summary>
        /// 自分自身とこのオブジェクトから全てのMeshFilterを集めて初期化する
        /// </summary>
        private void SetupAllMesh()
        {
            ClearInstancedMeshes();

            _meshDataList.Clear();

            // 自身を含む全ての子オブジェクトから MeshFilter を取得
            MeshFilter[] filters = GetComponentsInChildren<MeshFilter>(true);

            foreach (var filter in filters)
            {
                if (filter == null || filter.sharedMesh == null) continue;

                MeshData data = new MeshData();
                data.filter = filter;
                data.originalMesh = filter.sharedMesh;
                data.originalVertices = data.originalMesh.vertices;

                // 実行時書き換え用のインスタンスメッシュを生成
                data.instancedMesh = Instantiate(data.originalMesh);
                data.modifiedVertices = new Vector3[data.originalVertices.Length];

                if (Application.isPlaying)
                {
                    filter.mesh = data.instancedMesh;
                }
                else
                {
                    filter.sharedMesh = data.instancedMesh;
                }

                _meshDataList.Add(data);
            }

            _isInitialized = _meshDataList.Count > 0;
        }

        private void LateUpdate()
        {
            ApplyDistortionToAll();
        }

        private void ApplyDistortionToAll()
        {
            // メッシュの初期化が外れていた場合
            if (!_isInitialized || _meshDataList.Count == 0)
            {
                SetupAllMesh();
                if (!_isInitialized) return;
            }

            // クランプ処理
            float safeSquashY = Mathf.Max(SquashY, -0.95f);
            float scaleDiv = Mathf.Sqrt(1f + safeSquashY);
            if (scaleDiv < 0.01f) scaleDiv = 1f;
            float safeShearX = Mathf.Clamp(ShearX, -3f, 3f);

            // 全てのメッシュに対して頂点変形マトリクスの適用
            for (int m = 0; m < _meshDataList.Count; m++)
            {
                MeshData data = _meshDataList[m];

                if (data.filter == null || data.instancedMesh == null) continue;

                for (int i = 0; i < data.originalVertices.Length; i++)
                {
                    Vector3 orig = data.originalVertices[i];
                    Vector3 modified = orig;

                    // 1. 平行四辺形シアー変形
                    modified.x += orig.y * safeShearX;

                    // 2. スクワッシュ & ストレッチ
                    if (safeSquashY != 0f)
                    {
                        modified.y *= (1f + safeSquashY);
                        modified.x /= scaleDiv;
                        modified.z /= scaleDiv;
                    }

                    data.modifiedVertices[i] = modified;
                }

                // 各メッシュを更新
                data.instancedMesh.vertices = data.modifiedVertices;
                data.instancedMesh.RecalculateBounds();
                data.instancedMesh.RecalculateNormals();
            }
        }

        private void ClearInstancedMeshes()
        {
            foreach (var data in _meshDataList)
            {
                if (data.filter != null && data.originalMesh != null)
                {
                    if (!Application.isPlaying)
                    {
                        data.filter.sharedMesh = data.originalMesh;
                    }
                }

                if (data.instancedMesh != null)
                {
                    if (Application.isPlaying) Destroy(data.instancedMesh);
                    else DestroyImmediate(data.instancedMesh);
                }
            }
        }

        private void OnDestroy()
        {
            ClearInstancedMeshes();
        }
    }
}
