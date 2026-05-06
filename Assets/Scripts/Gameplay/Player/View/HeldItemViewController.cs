// ------------------------------------------------------------
// File		: HeldItemViewController.cs
// Summary	: 保持しているアイテムのビジュアルを管理するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-05-06
//
// Notes	:
// - 5/6: ベース作成
// ------------------------------------------------------------
using Game.Gameplay.Collectibles;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>
    /// PlayerHolderのデータ(HeldItem)を受け取り、保持しているアイテムのビジュアルを管理するクラス
    /// </summary>
    public class HeldItemViewController : MonoBehaviour
    {
        // 変数宣言
        // ------------------------------------------------------------
        [Header("参照設定")]
        [Tooltip("データを保持しているホルダー")]
        [SerializeField] private PlayerHolder _holder;

        [Tooltip("アイテムのビジュアルを表示するためのプレハブ")]
        [SerializeField] private GameObject _dummyVisualPrefab;

        [Header("表示設定")]
        [Tooltip("最大でいくつまで画面に描画するか")]
        [SerializeField] private int _maxVisuals = 50;

        [Header("配置設定")]
        [Tooltip("配置を開始するプレイヤーの前方オフセット")]
        [SerializeField] private float _startOffsetZ = 1f;

        [Tooltip("配置する高さの底面Y")]
        [SerializeField] private float _startY = 0.5f;

        [Tooltip("配置する横幅")]
        [SerializeField] private float _areaWidth = 4f;

        [Tooltip("配置する奥行の最大列数")]
        [SerializeField] private int _depthRows = 4;

        [Tooltip("アイテムの見た目の間隔")]
        [SerializeField] private float _spacing = 0.8f;

        [Tooltip("きっちり並ばないようにするためのランキングなずれ幅")]
        [SerializeField] private float _randomJitter = 0.2f;

        private readonly List<GameObject> _visuals = new List<GameObject>();            ///< 現在表示しているビジュアルのリスト



        // 関数処理
        // ------------------------------------------------------------
        /// <summary>
        ///　PlayerHolderのデータが増えた際に呼び出される関数
        /// </summary>
        private void OnEnable()
        {
            if (_holder != null)
            {
                _holder.OnHolderChanged.AddListener(UpdateVisuals);
            }
        }

        /// <summary>
        /// PlayerHolderのデータが減った際に呼び出される関数
        /// </summary>
        private void OnDisable()
        {
            if (_holder != null)
            {
                _holder.OnHolderChanged.RemoveListener(UpdateVisuals);
            }
        }

        private void UpdateVisuals()
        {
            if (_dummyVisualPrefab == null)
            {
                return;
            }

            // 保持データの数
            int targetCount = Mathf.Min(_holder.CurrentCount, _maxVisuals);

            // 足りなければ生成
            while (_visuals.Count < targetCount)
            {
                HeldItem currentItemData = _holder.HeldItems[_visuals.Count];
                GameObject prefabToSpawn = currentItemData.VisualPrefab != null ? currentItemData.VisualPrefab : _dummyVisualPrefab;

                GameObject visual = Instantiate(prefabToSpawn, transform);

                // 物理演算は一切不要なのでColliderがあれば削除
                Collider col = visual.GetComponent<Collider>();
                if (col != null)
                {
                    Destroy(col);
                }

                _visuals.Add(visual);
            }

            // 多すぎれば削除
            while (_visuals.Count > targetCount)
            {
                GameObject toRemove = _visuals[_visuals.Count - 1];
                _visuals.RemoveAt(_visuals.Count - 1);
                Destroy(toRemove);
            }

            // プレイヤーの前方にアイテムを配置
            for (int i = 0; i < _visuals.Count; i++)
            {
                Random.InitState(i); // アイテムごとにランダムのシードを固定して、見た目が安定するようにする

                // 1段あたりのアイテム数を計算
                int itemsPerRow     = Mathf.Max(1, Mathf.FloorToInt(_areaWidth / _spacing));
                int itemsPerLayer   = itemsPerRow * _depthRows;

                // 現在のアイテムが何段目の、どこにいるか
                int layer = i / itemsPerLayer;
                int indexInLayer = i % itemsPerLayer;

                int row = indexInLayer / itemsPerRow;
                int col = indexInLayer % itemsPerRow;

                float startX = -(_areaWidth / 2f) + (_spacing / 2f);

                // 基本座標にランダムなずれを加える
                float x = startX + col * _spacing + Random.Range(-_randomJitter, _randomJitter);
                float z = _startOffsetZ + row * _spacing + Random.Range(-_randomJitter, _randomJitter);
                float y = _startY + layer * _spacing + Random.Range(-_randomJitter, _randomJitter);

                _visuals[i].transform.localPosition = new Vector3(x, y, z);

                // アイテムの見た目をランダムに回転させる
                _visuals[i].transform.localRotation = Random.rotation;
            }
        }
    }
}
