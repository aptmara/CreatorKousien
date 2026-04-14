// ------------------------------------------------------------
// File		: FieldState.cs
// Summary	: 盤面全体の状態を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-12
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 盤面の実行時状態(一次情報)を保持するクラス
/// </summary>
public class FieldState
{
    private readonly int _width;            /// 盤面の幅
    private readonly int _height;           /// 盤面の高さ
    private readonly GridCellData[,] _grid; /// 盤面のセルデータ

    /// <summary>
    /// 盤面の幅を取得します。
    /// </summary>
    public int Width => _width;

    /// <summary>
    /// 盤面の高さを取得します。
    /// </summary>
    public int Height => _height;


    /// <summary>
    /// コンストラクタ。指定サイズで盤面を初期化します。
    /// </summary>
    /// <param name="width">セルの幅</param>
    /// <param name="height">セルの高さ</param>
    public FieldState(int width, int height)
    {
        _width = width;
        _height = height;
        _grid = new GridCellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                _grid[x, y] = new GridCellData();
            }
        }
    }


    /// <summary>
    /// 指定座標のセル情報を取得する
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    /// <returns></returns>
    public GridCellData GetCell(int x, int y)
    {
        if (IsOutOfBounds(x, y))
        {
            return null;
        }
        return _grid[x, y];
    }


    /// <summary>
    /// 座標が盤面外かどうかを判定する
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    /// <returns></returns>
    public bool IsOutOfBounds(int x, int y)
    {
        return x < 0 || x >= _width || y < 0 || y >= _height;
    }
}
