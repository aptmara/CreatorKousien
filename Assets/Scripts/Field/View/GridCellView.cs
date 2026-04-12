// ------------------------------------------------------------
// File		: GridCellView.cs
// Summary	: ここのマスの見た目を管理・表示するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// - 床モデル、VFXなど管理する予定です
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// ここのマスの見た目を管理するクラス
/// </summary>
public class GridCellView : MonoBehaviour
{
    [Tooltip("床のMeshRenderer。床の色を変えるために使用します。")]
    [SerializeField] private MeshRenderer _renderer; // 床の色を変えるためのMeshRenderer(仮)


    /// <summary>
    ///
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void Initialize(int x, int y)
    {
        // エディタ上で見やすいように名前を変更
        gameObject.name = $"Cell ({x}, {y})";

        // TODO: 床の見た目を床タイプによって変える処理などを追加予定
    }


    /// <summary>
    /// マスの通行可能/不可状態に応じて見た目を更新する処理
    /// </summary>
    /// <param name="isPassable">通行可能かどうか</param>
    public void UpdateState(bool isPassable)
    {
        if (_renderer != null)
        {
            // 仮実装: 通行可能なセルは白、通行不可なセルは黒にする
            _renderer.material.color = isPassable ? Color.white : Color.black;
        }
    }
}
