// ------------------------------------------------------------
// File     : GridCellData.cs
// Summary  : グリッドセルのデータを管理する
//
// Author   : [浅野 勇生]
// Created  : 2026-04-12
//
// Notes    :
// - ひとまず仕様が固まってないので、必要に応じてプロパティやメソッドを追加していく予定
// - 自分の元の床と寿命も管理するように (4/16追記)
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
    /// 現在このマスに設定されている床タイプ
    /// </summary>
    public TileTypeDefinition CurrentTile { get; set; }

    /// <summary>
    /// ターン経過で元に戻るためのデータ
    /// </summary>
    public TileTypeDefinition DefaultTile { get; set; }

    /// <summary>
    /// 床の効果が残り何ターン続くか
    /// </summary>
    public int RemainingLifespan { get; set; } = 0;

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="initialTile">初期の床タイプ</param>
    public GridCellData(TileTypeDefinition initialTile = null)
    {
        CurrentTile = initialTile;
        DefaultTile = initialTile;
        // タイルが設定されていればその通行可能フラグを使用、なければ通行可能とする
        IsPassable = initialTile != null ? initialTile.CanStand : true;
    }
}
