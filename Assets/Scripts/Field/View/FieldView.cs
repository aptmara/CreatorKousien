// ------------------------------------------------------------
// File		: FieldView.cs
// Summary	: FieldStateの状態を元に、盤面全体の見た目を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// - 盤面全体の見た目を管理するクラス。
// - 左上(0,0)を原点とし、右方向へ+X、下方向へ+Y(Unity空間では-Z)として配置するよう修正 (4/13)
// - 乗ったマス、過去のマスを記録して、マスの見た目を変える機能追加 (4/15)
// ------------------------------------------------------------
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 盤面全体の情報を読み取り、画面上に可視化する表示層のクラス
/// </summary>
public class FieldView : MonoBehaviour
{
    [Header("参照オブジェクト")]
    [Tooltip("マスを生成する際の親となる空のGameObject")]
    [SerializeField] private Transform _gridParent;         // ここがマスの親（0,0）になる！

    [Tooltip("1マスのPrefab。GridCellViewコンポーネントがアタッチされている必要がある")]
    [SerializeField] private GridCellView _cellPrefab;      // 1マスのPrefab

    // 生成したセルのリスト
    private Dictionary<Vector2Int, GridCellView> _cellViews = new Dictionary<Vector2Int, GridCellView>();

    private Vector2Int _lastOccupiedPos = new Vector2Int(-1, -1); // 最後にキャラクターがいたマスの座標を保存する変数

    /// <summary>
    /// FieldStateのデータに基づいて初期の盤面モデルを生成・配置
    /// 盤面を左上から順に配置するように修正 (4/13)
    /// </summary>
    /// <param name="state">盤面の実行時データ</param>
    public void BuildView(FieldState state,float cellSize)
    {
        // ----- 1. 既存のセルを削除 -----
        foreach (Transform child in _gridParent)
        {
            Destroy(child.gameObject);
        }
        _cellViews.Clear(); // 辞書もクリア


        // ----- 2. 新しいセルを生成 -----
        for (int x = 0; x < state.Width; x++)
        {
            for (int y = 0; y < state.Height; y++)
            {
                // 座標計算の修正：
                // 右方向へ+X、
                // 下方向へ+Y = Unity空間では-Zとして配置するように修正 (4/13)
                Vector3 localPos = new Vector3(x * cellSize, 0, -y * cellSize);   // マスの位置を計算

                // プレハブを生成
                GridCellView cellView = Instantiate(_cellPrefab, _gridParent);
                cellView.transform.localPosition = localPos;                        // 位置を設定
                cellView.transform.localRotation = Quaternion.identity;             // 回転をリセット

                cellView.Initialize(x, y);                                          // セルの初期化

                // 初期状態を反映
                var cellData = state.GetCell(x, y);
                if (cellData != null)
                {
                    cellView.SetTile(cellData.CurrentTile);
                }

                _cellViews.Add(new Vector2Int(x, y), cellView); // 生成したセルを保存
            }
        }

        Debug.Log($"[FieldView] {state.Width}x{state.Height}の盤面を生成しました。");
    }

    public void HighlightCell(int x, int y)
    {
        // 前のマスを元に戻す
        if (_cellViews.TryGetValue(_lastOccupiedPos, out var oldCell))
        {
            oldCell.SetOccupied(false); // 前のマスの見た目を元に戻す
        }

        // 新しいマスをアニメーション
        Vector2Int newPos = new Vector2Int(x, y);
        if (_cellViews.TryGetValue(newPos, out var newCell))
        {
            newCell.SetOccupied(true);
        }

        _lastOccupiedPos = newPos; // 最後にキャラクターがいたマスの座標を更新
    }


    /// <summary>
    /// 座標から対応するGridCellViewを取得するメソッド
    /// </summary>
    /// <param name="pos">対応する座標</param>
    /// <returns></returns>
    public GridCellView GetCellView(Vector2Int pos)
    {
        if (_cellViews.TryGetValue(pos, out var cell))
        {
            return cell;
        }
        return null;
    }
}
