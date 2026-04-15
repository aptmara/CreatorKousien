// ------------------------------------------------------------
// File		: PlayerView.cs
// Summary	: プレイヤーのViewクラス
//
// Author	: [浅野勇生]
// Created	: 2026-04-14
//
// Notes	:
// -
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// プレイヤーの見た目とアニメーションを担当するクラス
/// </summary>
public class PlayerView : MonoBehaviour
{
    // ----- Inspectorで設定する変数 -----
    [Header("Playerの描画に関する設定")]
    [Tooltip("アニメーターコンポーネントをアタッチしてください")]
    [SerializeField] private Animator _animator; // プレイヤーのアニメーター

    [Tooltip("目標座標へ向かう際の滑らかさ（Lerpの補間速度）")]
    [SerializeField] private float _lerpSpeed = 10f;

    private Vector3 _targetWorldPos;    // 目標座標（ワールド座標）

    private GridCellView _standingTile; // プレイヤーが立っているマスの参照

    private float _baseHeight;          // プレイヤーの基本的な高さ（マスの高さを考慮する前の高さ）

    /// <summary>
    /// 立っている床をセットする
    /// </summary>
    /// <param name="tile">立っている床の参照</param>
    public void SetStandingTile(GridCellView tile)
    {
        _standingTile = tile;
    }


    /// <summary>
    /// 初期位置のセットアップ
    /// </summary>
    /// <param name="startGridPos"> 初期の盤面座標</param>
    /// <param name="cellSize">     セルのサイズ</param>
    public void Initialize(Vector2Int startGridPos, float cellSize)
    {
        _baseHeight = transform.position.y; // 初期の高さを保存

        UpdateTargetPosition(startGridPos, cellSize);
        transform.position = _targetWorldPos;           // 初期位置を設定
    }


    /// <summary>
    /// PlayerSystemからの移動通知を受け取る
    /// </summary>
    /// <param name="gridPosition"></param>
    /// <param name="cellSize"></param>
    public void UpdateTargetPosition(Vector2Int gridPosition, float cellSize)
    {
        _targetWorldPos = new Vector3(gridPosition.x * cellSize, _baseHeight, -gridPosition.y * cellSize);
    }

    /// <summary>
    /// アクションを再生するためのメソッド
    /// </summary>
    /// <param name="triggerName"></param>
    public void PlayAction(string triggerName)
    {
        if (_animator != null)
        {
            _animator.SetTrigger(triggerName);
        }
    }

    /// <summary>
    /// PlayerSystemからの死亡通知を受け取る
    /// </summary>
    public void OnDeath()
    {
        if (_animator != null) _animator.SetTrigger("Die");
        Debug.Log("[PlayerView] 死亡アニメーション再生");
    }

    /// <summary>
    /// 更新処理：目標位置へ向かって滑らかに移動し、アニメーションの状態を更新する
    /// </summary>
    private void Update()
    {
        // 基本の目標座標
        Vector3 finalTargetPos = _targetWorldPos;

        // もし床が沈んでいたら
        if (_standingTile != null)
        {
            // 立っているマスの高さを考慮して目標座標を調整
            finalTargetPos.y += _standingTile.CurrentVisualOffset; // マスの高さ + プレイヤーの半分の高さ（仮）
        }

        // 現在の位置から目標位置へ滑らかに移動
        transform.position = Vector3.Lerp(transform.position, finalTargetPos, Time.deltaTime * _lerpSpeed);

        // 移動中かどうかの判定をアニメーターに送る
        if (_animator != null)
        {
            float distance = Vector3.Distance(transform.position, _targetWorldPos);
            _animator.SetBool("IsMoving", distance > 0.01f);
        }
    }
}
