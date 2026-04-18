// ------------------------------------------------------------
// File        : SelectSceneView.cs
// Summary     : ステージ選択UIのView。ステージボタンを動的生成し、
//               選択時にシーン遷移を要求する。
//
// Author      : 山内
// Created     : 2026-04-18
//
// Input       : GameManager（またはBootstrapper）から Setup() を呼ばれる
// Change      : ステージボタンの生成・選択処理
// Output      : SetStageNo() → onSceneChange("Game") でGameシーンへ遷移
// ------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CreatorKousien.Core;
using CreatorKousien.Data;

/// <summary>
/// ステージ選択UIのView。GameManager（またはBootstrapper）からSetup()で起動する。
/// </summary>
public class SelectSceneView : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Transform      _stageListContainer; // ボタンを並べる親
    [SerializeField] private StageButtonEntry _stagButtonPrefab; // ボタンPrefab
    [SerializeField] private GameObject     _loadingIndicator;   // ロード中表示
    [SerializeField] private GameObject     _errorPanel;         // エラー表示

    private StageManager      _stageManager;
    private Action<string>    _onSceneChange;

    // -----------------------------------------------------------------------
    // 外部API
    // -----------------------------------------------------------------------

    /// <summary>
    /// GameManager（またはBootstrapper）から呼ばれる初期化。
    /// StageManagerのロード完了イベントを購読し、完了次第UIを構築する。
    /// </summary>
    public void Setup(StageManager stageManager, Action<string> onSceneChange)
    {
        _stageManager  = stageManager;
        _onSceneChange = onSceneChange;

        SetLoading(true);
        _stageManager.OnStageListLoaded     += OnStageListLoaded;
        _stageManager.OnStageListLoadFailed += OnStageListLoadFailed;
    }

    private void OnDestroy()
    {
        if (_stageManager != null)
        {
            _stageManager.OnStageListLoaded     -= OnStageListLoaded;
            _stageManager.OnStageListLoadFailed -= OnStageListLoadFailed;
        }
    }

    // -----------------------------------------------------------------------
    // ロード完了 → ボタン生成
    // -----------------------------------------------------------------------

    private void OnStageListLoaded(IReadOnlyList<BattleSetupData> stageList)
    {
        SetLoading(false);

        if (stageList == null || stageList.Count == 0)
        {
            Debug.LogError("[SelectSceneView] ステージリストが空です。");
            if (_errorPanel != null) _errorPanel.SetActive(true);
            return;
        }

        // 既存ボタンをクリア
        foreach (Transform child in _stageListContainer)
            Destroy(child.gameObject);

        // ステージ数分ボタンを生成
        for (int i = 0; i < stageList.Count; i++)
        {
            int index = i;
            var entry = Instantiate(_stagButtonPrefab, _stageListContainer);
            entry.Setup(stageList[i], () => OnStageSelected(index));
        }

        Debug.Log($"[SelectSceneView] ボタン生成完了: {stageList.Count}件");
    }

    private void OnStageListLoadFailed(Exception ex)
    {
        Debug.LogError($"[SelectSceneView] ステージデータのロードに失敗しました: {ex?.Message}");
        SetLoading(false);
        if (_errorPanel != null) _errorPanel.SetActive(true);
    }


    // -----------------------------------------------------------------------
    // ステージ選択
    // -----------------------------------------------------------------------

    private void OnStageSelected(int index)
    {
        _stageManager.SetStageNo(index);
        Debug.Log($"[SelectSceneView] ステージ{index}選択 → Gameシーンへ");
        _onSceneChange?.Invoke("Game");
    }

    // -----------------------------------------------------------------------
    // ユーティリティ
    // -----------------------------------------------------------------------

    private void SetLoading(bool on)
    {
        if (_loadingIndicator != null) _loadingIndicator.SetActive(on);
    }
}
