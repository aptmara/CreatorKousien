// ------------------------------------------------------------
// File		: TileEffectSystem.cs
// Summary	: タイルの効果を管理するクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// 床効果の発火を担当するクラス
/// </summary>
public class TileEffectSystem
{
    private readonly FieldState _fieldState; /// 盤面の状態への参照

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="fieldState">盤面状態を管理するクラス</param>
    public TileEffectSystem(FieldState fieldState)
    {
        _fieldState = fieldState;
    }


    /// <summary>
    /// キャラクターがマスに[進入]した際に呼ばれる
    /// </summary>
    /// <param name="targetId">ターゲットのID</param>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    public void TriggerOnEnter(int targetId, int x, int y)
    {
        var profile = GetEffectProfile(x, y);
        if (profile == null)
            return;

        // ----- 1. 乗った瞬間の効果 -----
        if (profile.OnEnterEffectId != 1)
        {
            Debug.Log($"[TileEffect] Actor: {targetId} が ({x},{y}) に進入 -> OnEnter効果(ID:{profile.OnEnterEffectId}) 発動!");
            // TODO: はるひこのEffectSystemに送る予定
        }

        // ----- 2. 常時効果の適用開始 -----
        if (profile.WhiteStayEffectId != -1)
        {
            Debug.Log($"[TileEffect] Actor: {targetId} に WhileStay効果(ID: {profile.WhiteStayEffectId}) を付与状態に!");
            // TODO: はるひこのEffectSystemに送る予定
        }
    }


    /// <summary>
    /// キャラクターがマスから[離脱]した際に呼ばれる
    /// </summary>
    /// <param name="targetId">ターゲットのID</param>
    /// <param name="oldX">旧X座標</param>
    /// <param name="oldY">旧Y座標</param>
    public void TriggerOnExit(int targetId, int oldX, int oldY)
    {
        var profile = GetEffectProfile(oldX, oldY);
        if (profile == null)
            return;

        // ----- 1. 離れた瞬間の効果 -----
        if (profile.OnExitEffectId != -1)
        {
            Debug.Log($"[TileEffect] Actor: {targetId} が ({oldX},{oldY}) から離脱 -> OnExit効果(ID:{profile.OnExitEffectId}) 発動!");
        }

        // ----- 2. 常時効果の解除 -----
        if (profile.WhiteStayEffectId != -1)
        {
            Debug.Log($"[TileEffect] Actor: {targetId} の WhileStay効果(ID: {profile.WhiteStayEffectId}) を解除!");
        }
    }


    /// <summary>
    /// [ターン終了時] に呼ばれる。キャラクターがマスに[留まっている]場合の効果を発火させる
    /// </summary>
    /// <param name="targetId">ターゲットのID</param>
    /// <param name="currentX">現在のX座標</param>
    /// <param name="currentY">現在のY座標</param>
    public void TriggerOnTurnEnd(int targetId, int currentX, int currentY)
    {
        var profile = GetEffectProfile(currentX, currentY);
        if (profile != null && profile.OnTurnEndEffectId != -1)
        {
            Debug.Log($"[TileEffect] Actor: {targetId} が ({currentX},{currentY}) にいる -> OnTurnEnd効果(ID:{profile.OnTurnEndEffectId}) 発動!");
        }
    }


    /// <summary>
    /// 指定座標の床効果プロファイルを取得
    /// </summary>
    /// <param name="x">X座標</param>
    /// <param name="y">Y座標</param>
    /// <returns>指定座標のセルがNullではないか確認して床効果を返す</returns>
    private TileEffectProfile GetEffectProfile(int x, int y)
    {
        var cell = _fieldState.GetCell(x, y);
        // セルがNullではないか
        if (cell == null || cell.CurrentTile == null)
            return null;

        return cell.CurrentTile.EffectProfile;
    }
}
