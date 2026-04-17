// ------------------------------------------------------------
// File		: DebugGameManager.cs
// Summary	: デバッグ用のゲームマネージャー
//
// Author	: [浅野勇生]
// Created	: 2026-04-17
//
// Notes	:
// - デバッグ用のゲームマネージャー
// ------------------------------------------------------------
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugGameManager : MonoBehaviour
{
    [Header("テスト環境セットアップ")]
    [Tooltip("バトルマネージャーのプレハブ")]
    [SerializeField] private BattleManager _battleManagerPrefab;  // バトルマネージャーのプレハブ

    [Tooltip("呼び出したいステージのID")]
    [SerializeField] private int _testStageId = 0;                // 呼び出したいステージのID

    private BattleManager _currentBattle;                         // 生成されたバトルマネージャーを保持

    private void Update()
    {
        // テスト用のキー入力（例: Enter or Spaceキーでバトル開始）
        if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (_currentBattle == null)
            {
                StartDebugBattle();
            }
            else
            {
                Debug.LogWarning("[Debug]既にバトルが開始されています。");
            }
        }

        // テスト用のキー入力（例: Backspaceキーでバトル終了）
        if (Keyboard.current.backspaceKey.wasPressedThisFrame)
        {
            if (_currentBattle != null)
            {
                Destroy(_currentBattle.gameObject);
                _currentBattle = null;
                Debug.Log("[Debug]バトルが終了しました。");
            }
        }
    }

    private void StartDebugBattle()
    {
        if (_battleManagerPrefab == null)
        {
            Debug.LogError("[Debug]バトルマネージャーのプレハブが設定されていません！");
            return;
        }

        // 1. バトルマネージャーを生成して初期化
        _currentBattle = Instantiate(_battleManagerPrefab);

        // 2. 指定したステージIDでバトルを初期化
        _currentBattle.Initialize(_testStageId);

        Debug.Log($"[Debug]ステージID {_testStageId} のバトルを開始しました。");
    }
}
