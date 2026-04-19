// ------------------------------------------------------------
// File        : BattleSetupDataAddressableWatcher.cs
// Summary     : LevelData 配下に BattleSetupData (_Setup.asset) が
//               追加・移動されたとき、自動で Addressables の
//               "StageData" ラベルを付与する AssetPostprocessor。
//
// Author      : 山内
// Created     : 2026-04-19
//
// Input       : Unity がアセットをインポートするたびに OnPostprocessAllAssets が呼ばれる
// Change      : BattleSetupData かつ LevelData 配下のパスを検出してラベル付与
// Output      : AddressableAssetSettings にエントリ追加・StageData ラベルを付与
// ------------------------------------------------------------
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using CreatorKousien.Data;

namespace CreatorKousien.EditorTools
{
    /// <summary>
    /// LevelData 配下に作成された BattleSetupData を
    /// 自動的に Addressables "StageData" ラベルへ登録する。
    /// StageTemplateGenerator で生成した場合も、手動で追加した場合も両方対応。
    /// </summary>
    public class BattleSetupDataAddressableWatcher : AssetPostprocessor
    {
        /// <summary>監視するフォルダ（このパス配下のみ対象）</summary>
        private const string WatchPath = "Assets/Resources/Data/LevelData";

        /// <summary>付与するAddressablesラベル</summary>
        private const string StageLabel = "StageData";

        /// <summary>
        /// アセットのインポート・移動・削除後に呼ばれる。
        /// インポートおよび移動先のアセットを対象にラベル付与を試みる。
        /// </summary>
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (var path in importedAssets)
                TryRegisterAsAddressable(path);

            // 移動先のパスも対象
            foreach (var path in movedAssets)
                TryRegisterAsAddressable(path);
        }

        /// 外部から直接呼べる公開ラッパー（StageTemplateGenerator等から明示的に呼ぶ用）。
        /// </summary>
        public static void RegisterAddressable(string assetPath)
            => TryRegisterAsAddressable(assetPath);

        /// <summary>
        /// 指定パスが LevelData 配下の BattleSetupData なら
        /// Addressables へ登録して StageData ラベルを付与する。
        /// </summary>
        private static void TryRegisterAsAddressable(string assetPath)
        {
            // 監視フォルダ外はスキップ
            if (!assetPath.StartsWith(WatchPath)) return;

            // BattleSetupData か判定
            var asset = AssetDatabase.LoadAssetAtPath<BattleSetupData>(assetPath);
            if (asset == null) return;

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                UnityEngine.Debug.LogWarning(
                    "[BattleSetupDataWatcher] Addressables設定が見つかりません。" +
                    "Window > Asset Management > Addressables > Groups から初期化してください。");
                return;
            }

            // ラベルが未登録なら追加
            if (!settings.GetLabels().Contains(StageLabel))
                settings.AddLabel(StageLabel);

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            var entry = settings.FindAssetEntry(guid)
                        ?? settings.CreateOrMoveEntry(guid, settings.DefaultGroup, false, false);

            // ラベルを付与（既付与でも冪等）
            entry.SetLabel(StageLabel, true, true, false);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log(
                $"<color=cyan>[Addressables自動登録] {assetPath} に \"{StageLabel}\" ラベルを付与しました。</color>");
        }
    }
}
#endif
