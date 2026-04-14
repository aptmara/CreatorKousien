// ------------------------------------------------------------
// File		: GridCellView1.cs
// Summary	: ここのマスの見た目を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;

public class GridCellView : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("モデルを生成して配置する親オブジェクト")]
    [SerializeField] private Transform _modelContainer;

    private GameObject _currentModel;   // 現在表示しているモデル

    /// <summary>
    /// 初期化処理。マスの座標を名前に設定してわかりやすくする。
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    public void Initialize(int x, int y)
    {
        gameObject.name = $"Cell({x},{y})";
    }


    /// <summary>
    /// 指定された床タイプのプレファブを生成し、見た目を更新する
    /// </summary>
    /// <param name="tile">指定したい床タイル</param>
    public void SetTile(TileTypeDefinition tile)
    {
        if (tile == null)
            return;

        // 既存のモデルがあれば削除
        if (_currentModel != null)
        {
            Destroy(_currentModel);
        }

        // プレハブが設定されている場合のみ生成
        if (tile.ModelPrefab != null)
        {
            _currentModel = Instantiate(tile.ModelPrefab, _modelContainer != null ? _modelContainer : transform);
            _currentModel.transform.localPosition = Vector3.zero;
            _currentModel.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"[GridCellView] {tile.TileName} にモデルプレハブが設定されていません。");
        }
    }
}
