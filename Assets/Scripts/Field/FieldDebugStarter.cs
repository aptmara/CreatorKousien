// ------------------------------------------------------------
// File		: FieldDebugStarter.cs
// Summary	: フィールド関連のクラスの動作確認用のスクリプト
//
// Author	: [浅野勇生]
// Created	: 2026-04-13
//
// Notes	:
// - デバック用に作成しマスタ。GameManager実装後は削除予定
// ------------------------------------------------------------
using UnityEngine;

/// <summary>
/// デバック用に作成した、フィールド関連のクラスの動作確認用のスクリプト
/// </summary>
public class FieldDebugStarter : MonoBehaviour
{
    [Header("参照オブジェクト")]
    [Tooltip("読み込むステージデータ")]
    [SerializeField] private StageData _debugStageData; // 読み込むステージデータ

    [Tooltip("盤面を描画するViewコンポーネント")]
    [SerializeField] private FieldView _fieldView;      // 盤面を描画するViewコンポーネント

    // テスト用のFieldServiceインスタンス
    private FieldService _fieldService;


    /// <summary>
    /// 初期化処理。ステージデータを読み込み、FieldServiceを初期化し、FieldViewに盤面を描画させる
    /// </summary>
    void Start()
    {
        if (_debugStageData == null || _fieldView == null)
        {
            Debug.LogError("[FieldDebug] ステージデータまたはFieldViewがアサインされていません!");
            return;
        }

        Debug.Log("[FieldDebug]盤面の初期化を開始します...");

        // ----- 1. FieldServiceの初期化 -----
        _fieldService = new FieldService();
        _fieldService.Initialize(_debugStageData);

        // ----- 2. FieldViewに盤面を描画させる -----
        _fieldView.BuildView(_fieldService.State);

        Debug.Log("[FieldDebug]盤面の初期化が完了しました!");
    }
}
