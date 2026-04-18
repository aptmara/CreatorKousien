// ------------------------------------------------------------
// File        : SelectSceneBootstrapper.cs
// Summary     : プロトタイプ用のSelectシーン起動クラス。
//               本番ではGameManagerがこの責務を担う。
//               GameManagerを使わずに SelectSceneView + StageManager を
//               完全に動作させるためのモノビヘイビア。
//
// Author      : 山内
// Created     : 2026-04-18
//
// Input       : Unityの Start() で自動起動
//               _testStages にInspectorからBattleSetupDataをアサインしておく
// Change      : StageManager生成 → SetupDirect → SelectSceneView.Setup()
// Output      : SelectSceneViewがステージボタンを表示できる状態になる
//
// TODO: 本番フロー移行後は GameManager.InitializeSelectScene() に置き換えること
// ------------------------------------------------------------
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using CreatorKousien.Core;
using CreatorKousien.Data;

/// <summary>
/// プロトタイプ用 SelectシーンBootstrapper。
/// GameManagerが接続されるまでの間、StageManagerとSelectSceneViewを直接つなぐ。
/// </summary>
public class SelectSceneBootstrapper : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector設定（プロト用データ）
    // -----------------------------------------------------------------------

    [Header("【プロト用】テストステージデータ")]
    [Tooltip("Inspectorからテスト用BattleSetupDataをアサインする。本番はAddressableに切り替える。")]
    [SerializeField] private BattleSetupData[] _testStages;

    [Header("シーン参照")]
    [Tooltip("SelectSceneViewコンポーネントを持つGameObject")]
    [SerializeField] private SelectSceneView _selectSceneView;

    // -----------------------------------------------------------------------
    // ライフサイクル
    // -----------------------------------------------------------------------

    private void Start()
    {
        if (_selectSceneView == null)
        {
            Debug.LogError("[SelectSceneBootstrapper] SelectSceneViewがアサインされていません。");
            return;
        }

        if (_testStages == null || _testStages.Length == 0)
        {
            Debug.LogWarning("[SelectSceneBootstrapper] テストステージデータが空です。Inspectorにアサインしてください。");
        }

        // StageManagerを生成し、直接データをセットする（プロト用）
        var stageManager = new StageManager();

        // SelectSceneViewを先にSetupしてイベント購読させてから、SetupDirectを呼ぶ
        // 理由: SetupDirectはOnStageListLoadedを同期的に発火するので、
        //       購読より前に呼ぶとViewがイベントを受け取れない
        _selectSceneView.Setup(stageManager, OnSceneChangeRequested);

        // Setupの後でデータをセット → OnStageListLoadedがViewに届く
        stageManager.SetupDirect(_testStages);

        Debug.Log("[SelectSceneBootstrapper] プロト起動完了。GameManager接続後は本クラスを削除すること。");
    }

    // -----------------------------------------------------------------------
    // シーン遷移（プロト用: GameManager.RequestSceneChange() の代替）
    // -----------------------------------------------------------------------

    /// <summary>
    /// SelectSceneViewからのシーン遷移要求を受け取る。
    /// 本番ではGameManager.RequestSceneChangeが入る場所。
    /// </summary>
    /// <param name="sceneName">遷移先シーン名</param>
    private void OnSceneChangeRequested(string sceneName)
    {
        Debug.Log($"[SelectSceneBootstrapper] シーン遷移: {sceneName}");
        SceneManager.LoadScene(sceneName);
    }
}
