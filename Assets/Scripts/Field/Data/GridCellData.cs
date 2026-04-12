// ------------------------------------------------------------
// File     : GridCellData.cs
// Summary  : グリッドセルのデータを管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-04-12
//
// Notes    :
// - ひとまず設計が固まってないので、必要に応じてプロパティやメソッドを追加していく予定
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 盤面の各マスの状態を保持する純粋なデータクラス
/// </summary>
public class GridCellData
{
    /// <summary>
    /// 通行可能かどうか（障害物や穴がないか）
    /// </summary>
    public bool IsPassable { get; set; }

    /// <summary>
    /// 現在このマスを占有しているエンティティ(プレイヤーや敵)のID。-1なら空きマス。
    /// </summary>
    public int OccupierId { get; set; } = -1;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="isPassable">通行可能フラグの初期値</param>
    public GridCellData(bool isPassable = true)
    {
         IsPassable = isPassable;
    }
}
