// ------------------------------------------------------------
// File        : StageManager.cs
// Summary     : ステージ選択結果を保持し、Gameシーンへ渡すクラス。
//               SelectシーンでUIにステージリストを提供し、
//               選択されたBattleSetupDataをGameシーン初期化の起点として返す。
//
// Author      : 山内
// Created     : 2026-04-18
//
// Input       : GameManagerから Initialize() or SetupDirect() を呼ばれる
//               SelectSceneViewから SetStageNo(int) を呼ばれる
// Change      : 選択ステージNoの保持 / BattleSetupDataリストの参照管理
// Output      : GetSelectedBattleSetupData() がGameシーン初期化起点データを返す
// ------------------------------------------------------------
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using CreatorKousien.Data;

namespace CreatorKousien.Core
{
    /// <summary>
    /// ステージ選択結果を保持し、Gameシーン初期化の起点データを提供するクラス。
    /// GameManagerが所有・生成し、SelectシーンとGameシーンをまたいで使用される。
    /// </summary>
    public class StageManager
    {
        // -----------------------------------------------------------------------
        // 内部状態
        // -----------------------------------------------------------------------

        /// <summary>ロードされたステージデータのリスト</summary>
        private List<BattleSetupData> _stageList = new List<BattleSetupData>();

        /// <summary>選択されたステージのインデックス（0始まり）</summary>
        private int _selectedStageNo = 0;

        // -----------------------------------------------------------------------
        // 公開プロパティ
        // -----------------------------------------------------------------------

        /// <summary>ロード完了を通知するイベント。SelectSceneViewが購読してUIを構築する。</summary>
        public event Action<IReadOnlyList<BattleSetupData>> OnStageListLoaded;

        /// <summary>ロード完了フラグ</summary>
        public bool IsLoaded { get; private set; } = false;

        // -----------------------------------------------------------------------
        // 初期化（本番：Addressable）
        // -----------------------------------------------------------------------

        /// <summary>
        /// 本番初期化。AddressableラベルからBattleSetupData一覧を非同期ロードする。
        /// ロード完了後に OnStageListLoaded イベントを発火する。
        /// </summary>
        /// <param name="addressableLabel">Addressableラベル（デフォルト: "StageData"）</param>
        public void Initialize(string addressableLabel = "StageData")
        {
            IsLoaded = false;
            _stageList.Clear();

            var handle = Addressables.LoadAssetsAsync<BattleSetupData>(addressableLabel, null);
            handle.Completed += OnAddressableLoadCompleted;

            Debug.Log($"[StageManager] Addressableロード開始。ラベル: {addressableLabel}");
        }

        private void OnAddressableLoadCompleted(AsyncOperationHandle<IList<BattleSetupData>> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[StageManager] Addressableロード失敗: {handle.OperationException}");
                return;
            }

            _stageList = new List<BattleSetupData>(handle.Result);
            IsLoaded = true;
            Debug.Log($"[StageManager] ステージロード完了。件数: {_stageList.Count}");
            OnStageListLoaded?.Invoke(_stageList.AsReadOnly());
        }

        // -----------------------------------------------------------------------
        // 初期化（プロト：直接セット）
        // -----------------------------------------------------------------------

        /// <summary>
        /// プロトタイプ用初期化。BattleSetupDataを直接セットする（Addressable不要）。
        /// GameManagerを使わないプロト検証・テスト用途に限定する。
        /// TODO: 本番ではこのメソッドを呼ばず Initialize() に統一すること
        /// </summary>
        /// <param name="directList">直接渡すBattleSetupDataのリスト</param>
        public void SetupDirect(IList<BattleSetupData> directList)
        {
            if (directList == null || directList.Count == 0)
            {
                Debug.LogWarning("[StageManager] SetupDirect: 渡されたリストが空です。");
                return;
            }

            _stageList = new List<BattleSetupData>(directList);
            IsLoaded = true;
            Debug.Log($"[StageManager] SetupDirect完了。件数: {_stageList.Count}");

            // 同フレームで呼ばれるため、購読者がいない可能性がある
            // Selectシーンの Start() → Bootstrapper.Start() → SelectSceneView.Setup() の順で動くなら
            // OnStageListLoaded のInvokeはSelectSceneView.Setup()後に呼ぶ必要があるため
            // BootstrapperがSetupDirect後にOnStageListLoadedを発火するタイミングを制御する
            OnStageListLoaded?.Invoke(_stageList.AsReadOnly());
        }

        // -----------------------------------------------------------------------
        // 選択操作
        // -----------------------------------------------------------------------

        /// <summary>
        /// SelectSceneViewのボタン押下時に呼ばれる。ステージNoを保持する。
        /// </summary>
        /// <param name="stageNo">0始まりのステージインデックス</param>
        public void SetStageNo(int stageNo)
        {
            if (!IsLoaded)
            {
                Debug.LogError("[StageManager] 未ロード状態でSetStageNoが呼ばれました。");
                return;
            }

            if (stageNo < 0 || stageNo >= _stageList.Count)
            {
                Debug.LogError($"[StageManager] 無効なステージ番号: {stageNo}（範囲: 0-{_stageList.Count - 1}）");
                return;
            }

            _selectedStageNo = stageNo;
            Debug.Log($"[StageManager] ステージ選択: {stageNo}（{_stageList[stageNo].name}）");
        }

        // -----------------------------------------------------------------------
        // 取得
        // -----------------------------------------------------------------------

        /// <summary>現在選択中のステージインデックスを返す</summary>
        public int GetSelectStageNo() => _selectedStageNo;

        /// <summary>
        /// 選択されたBattleSetupDataを返す。GameManager.InitializeGameScene()が呼び出す。
        /// </summary>
        /// <returns>選択中のBattleSetupData。未ロードまたは異常時はnull。</returns>
        public BattleSetupData GetSelectedBattleSetupData()
        {
            if (!IsLoaded || _stageList.Count == 0)
            {
                Debug.LogError("[StageManager] ステージデータが取得できません（未ロードまたはリストが空）。");
                return null;
            }

            return _stageList[_selectedStageNo];
        }
    }
}
