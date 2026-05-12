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

        /// <summary>
        /// PlayerHolderのデータをもとに、保持しているアイテムのビジュアルを更新する関数
        /// </summary>
        private void UpdateVisuals()
        {
            if (_holder == null) return;

            // 保持しているすべてのアイテムの定位置を計算する
            for (int i = 0; i < _holder.CurrentCount; i++)
            {
                HeldItem currentItem = _holder.HeldItems[i];

                // D担当側に持たせた OriginalInstance（元の実体）を取得
                CollectibleObject instance = currentItem.OriginalInstance;

                if (instance == null) continue;

                // プレイヤー（このスクリプトがついているオブジェクト）の子にする
                if (instance.transform.parent != transform)
                {
                    instance.transform.SetParent(transform);
                }

                // --- 配置位置の計算 ---
                Random.InitState(i);
                int itemsPerRow = Mathf.Max(1, Mathf.FloorToInt(_areaWidth / _spacing));
                int itemsPerLayer = itemsPerRow * _depthRows;

                int layer = i / itemsPerLayer;
                int indexInLayer = i % itemsPerLayer;

                int row = indexInLayer / itemsPerRow;
                int col = indexInLayer % itemsPerRow;

                float startX = -(_areaWidth / 2f) + (_spacing / 2f);

                float x = startX + col * _spacing + Random.Range(-_randomJitter, _randomJitter);
                float z = _startOffsetZ + row * _spacing + Random.Range(-_randomJitter, _randomJitter);
                float y = _startY + layer * _spacing + Random.Range(-_randomJitter, _randomJitter);

                Vector3 targetPos = new Vector3(x, y, z);
                Quaternion targetRot = Random.rotation;

                // TODO: ここに HeldVisualMover をアタッチして targetPos, targetRot に滑らかに吸い込ませる処理を入れる

                // 現状は未実装なので、一瞬で定位置へワープさせる
//                instance.transform.localPosition = targetPos;
  //              instance.transform.localRotation = targetRot;
            }
        }
    }
}
