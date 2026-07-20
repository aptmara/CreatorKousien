using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CreatorKousien.Editor.AssetOrganization
{
    public sealed class PackageIntakeWindow : EditorWindow
    {
        private enum WizardStep
        {
            Select,
            Inspect,
            Classify,
            Complete,
        }

        private static readonly string[] CategoryOptions =
        {
            "Data", "Prefabs", "Models", "Materials", "Textures", "Animations", "Audio",
            "Shaders", "VFX/Prefabs", "VFX/Materials", "VFX/Textures", "Scenes", "Other",
        };

        private readonly UnityPackageArchiveReader _reader = new UnityPackageArchiveReader();
        private readonly List<PackageIssue> _issues = new List<PackageIssue>();
        private readonly List<AssetClassificationSuggestion> _suggestions = new List<AssetClassificationSuggestion>();
        private readonly List<string> _planIssues = new List<string>();
        private WizardStep _step;
        private string _packagePath;
        private string _incomingRoot;
        private string _search = string.Empty;
        private string _bulkEntity = "Shared";
        private AssetDomain _bulkDomain = AssetDomain.Enemies;
        private bool _filterReviewOnly;
        private bool _acknowledgedDirectRisk;
        private Vector2 _mainScroll;
        private Vector2 _assetScroll;
        private PackageInspection _inspection;
        private DirectImportReport _directReport;
        private AssetMoveResult _lastMoveResult;

        [MenuItem("Tools/CreatorKousien/Asset Intake %#i", priority = 10)]
        private static void Open()
        {
            GetWindow<PackageIntakeWindow>("Asset Intake");
        }

        [MenuItem("Tools/CreatorKousien/Package Intake", priority = 11)]
        private static void OpenLegacyMenu()
        {
            Open();
        }

        public static void OpenWithPackage(string packagePath)
        {
            PackageIntakeWindow window = GetWindow<PackageIntakeWindow>("Asset Intake");
            window.ResetWorkflow();
            window._packagePath = packagePath;
            window.Repaint();
        }

        public static void OpenIncomingSelection(IEnumerable<string> paths)
        {
            PackageIntakeWindow window = GetWindow<PackageIntakeWindow>("Asset Intake");
            window.ResetWorkflow();
            foreach (string path in paths.Where(path => path.StartsWith("Assets/_Incoming/", StringComparison.Ordinal)))
            {
                window._suggestions.Add(AssetAutoClassifier.ClassifyAsset(path));
            }

            window._incomingRoot = CommonIncomingRoot(window._suggestions.Select(suggestion => suggestion.SourcePath));
            window._step = window._suggestions.Count > 0 ? WizardStep.Classify : WizardStep.Select;
            window.RefreshPlanIssues();
            window.Repaint();
        }

        public static void OpenDirectImportReport(DirectImportReport report)
        {
            PackageIntakeWindow window = GetWindow<PackageIntakeWindow>("Asset Intake");
            window.ResetWorkflow();
            window._directReport = report;
            window._incomingRoot = report.IncomingRoot;
            foreach (string path in report.QuarantinedPaths)
            {
                string original = RecoverOriginalPath(path, report.IncomingRoot);
                window._suggestions.Add(AssetAutoClassifier.ClassifyAsset(path, original));
            }

            window._step = window._suggestions.Count > 0 ? WizardStep.Classify : WizardStep.Complete;
            window.RefreshPlanIssues();
            window.Show();
            window.Focus();
            window.Repaint();
        }

        private void OnEnable()
        {
            minSize = new Vector2(920f, 620f);
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawStepIndicator();
            EditorGUILayout.Space(6f);

            switch (_step)
            {
                case WizardStep.Select:
                    DrawSelectStep();
                    break;
                case WizardStep.Inspect:
                    DrawInspectStep();
                    break;
                case WizardStep.Classify:
                    DrawClassifyStep();
                    break;
                case WizardStep.Complete:
                    DrawCompleteStep();
                    break;
            }
        }

        private void DrawHeader()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 64f);
            EditorGUI.DrawRect(rect, new Color(0.10f, 0.14f, 0.20f));
            Rect titleRect = new Rect(rect.x + 18f, rect.y + 10f, rect.width - 36f, 24f);
            Rect subtitleRect = new Rect(rect.x + 18f, rect.y + 35f, rect.width - 36f, 20f);
            GUIStyle title = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, normal = { textColor = Color.white } };
            GUIStyle subtitle = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(0.72f, 0.80f, 0.90f) } };
            GUI.Label(titleRect, "CreatorKousien Asset Intake", title);
            GUI.Label(subtitleRect, "受け取る → 安全確認 → 自動分類 → 正式配置", subtitle);
        }

        private void DrawStepIndicator()
        {
            string[] labels = { "1 受け取る", "2 安全確認", "3 分類・配置", "4 完了" };
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                for (int index = 0; index < labels.Length; index++)
                {
                    bool active = index == (int)_step;
                    GUIStyle style = new GUIStyle(EditorStyles.toolbarButton);
                    if (active)
                    {
                        style.fontStyle = FontStyle.Bold;
                        style.normal.textColor = new Color(0.25f, 0.75f, 1f);
                    }

                    GUILayout.Label(labels[index], style, GUILayout.ExpandWidth(true));
                }
            }
        }

        private void DrawSelectStep()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            EditorGUILayout.LabelField("UnityPackageを選択", HeaderStyle());
            EditorGUILayout.Space(4f);
            DrawDropArea();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.SelectableLabel(
                    string.IsNullOrWhiteSpace(_packagePath) ? "まだ選択されていません" : _packagePath,
                    EditorStyles.textField,
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));
                if (GUILayout.Button("ファイルを選ぶ…", GUILayout.Width(120f)))
                {
                    string selected = EditorUtility.OpenFilePanel("UnityPackageを選択", GetInitialBrowseDirectory(), "unitypackage");
                    if (!string.IsNullOrWhiteSpace(selected))
                    {
                        SetPackagePath(selected);
                    }
                }
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("直接Importの保護", HeaderStyle());
            DirectUnityPackageImportMonitor.Enabled = EditorGUILayout.ToggleLeft(
                "Explorerなどから直接ImportしたUnityPackageを自動検知する",
                DirectUnityPackageImportMonitor.Enabled);
            EditorGUILayout.HelpBox(
                "直接Importされた新規Assetは自動で _Incoming/DirectImport へ隔離します。既存Assetの変更やScript・DLLは自動処理せず、危険項目として表示します。",
                MessageType.Info);

            EditorGUILayout.Space(18f);
            using (new EditorGUI.DisabledScope(!IsValidPackagePath(_packagePath)))
            {
                if (DrawPrimaryButton("安全確認を開始", 44f))
                {
                    InspectPackage();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0f, 126f, GUILayout.ExpandWidth(true));
            bool hovering = dropArea.Contains(Event.current.mousePosition)
                && (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform);
            EditorGUI.DrawRect(dropArea, hovering ? new Color(0.15f, 0.37f, 0.52f) : new Color(0.16f, 0.18f, 0.22f));
            GUIStyle centered = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white },
            };
            GUI.Label(dropArea, "ここへ .unitypackage をドラッグ＆ドロップ\n\nまたは下のボタンから選択", centered);

            if (!hovering)
            {
                return;
            }

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                string package = DragAndDrop.paths.FirstOrDefault(IsValidPackagePath);
                if (!string.IsNullOrWhiteSpace(package))
                {
                    SetPackagePath(package);
                }
            }

            Event.current.Use();
        }

        private void DrawInspectStep()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            EditorGUILayout.LabelField("安全確認の結果", HeaderStyle());
            DrawPackageSummary();
            DrawIssueSummary();

            int updateCount = CountStatus(PackageAssetStatus.UpdateCandidate);
            if (updateCount > 0)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.HelpBox(
                        $"更新候補 {updateCount}件は既存Assetを維持します。Package側の内容をLibraryへ書き出して比較できます。",
                        MessageType.Warning);
                    if (GUILayout.Button("比較用に書き出す", GUILayout.Width(150f), GUILayout.Height(38f)))
                    {
                        ExportUpdateCandidatesForComparison();
                    }
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("自動分類プレビュー", HeaderStyle());
            EditorGUILayout.HelpBox("この段階ではまだAssetsへ展開していません。分類は隔離展開後に個別修正できます。", MessageType.Info);
            if (_suggestions.Count == 0)
            {
                EditorGUILayout.HelpBox("分類対象となる新規Assetはありません。導入済みAssetは自動スキップされます。", MessageType.Info);
            }

            foreach (AssetClassificationSuggestion suggestion in _suggestions.Take(80))
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(ConfidenceText(suggestion.Confidence), ConfidenceStyle(suggestion.Confidence), GUILayout.Width(64f));
                    GUILayout.Label(suggestion.OriginalPath, GUILayout.ExpandWidth(true));
                    GUILayout.Label($"{DomainLabel(suggestion.Domain)} / {suggestion.Entity} / {suggestion.Category}", GUILayout.Width(270f));
                }
            }

            if (_suggestions.Count > 80)
            {
                EditorGUILayout.LabelField($"ほか {_suggestions.Count - 80} 件", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.Space(14f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("戻る", GUILayout.Height(38f), GUILayout.Width(110f)))
                {
                    _step = WizardStep.Select;
                }

                GUILayout.FlexibleSpace();
                int newCount = CountStatus(PackageAssetStatus.New);
                using (new EditorGUI.DisabledScope(PackageCollisionValidator.HasErrors(_issues) || newCount == 0))
                {
                    if (DrawPrimaryButton($"新規 {newCount}件を一時領域へ展開", 42f, 250f))
                    {
                        ExtractPackage();
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawClassifyStep()
        {
            DrawDirectImportRisk();
            DrawClassificationToolbar();
            DrawBulkControls();

            IEnumerable<AssetClassificationSuggestion> visible = FilteredSuggestions();
            _assetScroll = EditorGUILayout.BeginScrollView(_assetScroll);
            foreach (AssetClassificationSuggestion suggestion in visible)
            {
                DrawSuggestionRow(suggestion);
            }
            EditorGUILayout.EndScrollView();

            DrawPlanFooter();
        }

        private void DrawClassificationToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                _search = GUILayout.TextField(_search, EditorStyles.toolbarSearchField, GUILayout.MinWidth(220f));
                _filterReviewOnly = GUILayout.Toggle(_filterReviewOnly, "要確認のみ", EditorStyles.toolbarButton, GUILayout.Width(90f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("すべて選択", EditorStyles.toolbarButton, GUILayout.Width(82f)))
                {
                    _suggestions.ForEach(suggestion => suggestion.Selected = true);
                    RefreshPlanIssues();
                }

                if (GUILayout.Button("推奨のみ", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                {
                    _suggestions.ForEach(suggestion => suggestion.Selected = !suggestion.RequiresReview);
                    RefreshPlanIssues();
                }

                if (GUILayout.Button("選択解除", EditorStyles.toolbarButton, GUILayout.Width(76f)))
                {
                    _suggestions.ForEach(suggestion => suggestion.Selected = false);
                    RefreshPlanIssues();
                }
            }
        }

        private void DrawBulkControls()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("選択中のAssetを一括変更", EditorStyles.boldLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    _bulkDomain = DrawDomainPopup(_bulkDomain, GUILayout.Width(150f));
                    _bulkEntity = EditorGUILayout.TextField(_bulkEntity, GUILayout.Width(180f));
                    if (GUILayout.Button("DomainとEntityを適用", GUILayout.Width(170f)))
                    {
                        foreach (AssetClassificationSuggestion suggestion in _suggestions.Where(item => item.Selected))
                        {
                            suggestion.Domain = _bulkDomain;
                            suggestion.Entity = PackagePathUtility.SanitizeSegment(_bulkEntity, "Shared");
                            AssetAutoClassifier.RefreshDestination(suggestion);
                        }

                        RefreshPlanIssues();
                    }

                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"選択 {_suggestions.Count(item => item.Selected)} / {_suggestions.Count}", EditorStyles.boldLabel);
                }
            }
        }

        private void DrawSuggestionRow(AssetClassificationSuggestion suggestion)
        {
            Color border = ConfidenceColor(suggestion.Confidence);
            Rect row = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.DrawRect(new Rect(row.x, row.y, 4f, row.height), border);

            using (new EditorGUILayout.HorizontalScope())
            {
                bool selected = EditorGUILayout.Toggle(suggestion.Selected, GUILayout.Width(20f));
                if (selected != suggestion.Selected)
                {
                    suggestion.Selected = selected;
                    RefreshPlanIssues();
                }

                Texture icon = AssetDatabase.GetCachedIcon(suggestion.SourcePath);
                GUILayout.Label(icon, GUILayout.Width(32f), GUILayout.Height(32f));

                using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(280f)))
                {
                    GUILayout.Label(Path.GetFileName(suggestion.SourcePath), EditorStyles.boldLabel);
                    GUILayout.Label(suggestion.OriginalPath, EditorStyles.miniLabel);
                    GUILayout.Label("判定: " + suggestion.Reason, EditorStyles.miniLabel);
                }

                GUILayout.Label(ConfidenceText(suggestion.Confidence), ConfidenceStyle(suggestion.Confidence), GUILayout.Width(64f));

                AssetDomain domain = DrawDomainPopup(suggestion.Domain, GUILayout.Width(128f));
                string entity = EditorGUILayout.TextField(suggestion.Entity, GUILayout.Width(120f));
                int categoryIndex = Math.Max(0, Array.IndexOf(CategoryOptions, suggestion.Category));
                int nextCategory = EditorGUILayout.Popup(categoryIndex, CategoryOptions, GUILayout.Width(120f));
                string category = CategoryOptions[nextCategory];
                if (domain != suggestion.Domain || entity != suggestion.Entity || category != suggestion.Category)
                {
                    suggestion.Domain = domain;
                    suggestion.Entity = PackagePathUtility.SanitizeSegment(entity, "Shared");
                    suggestion.Category = category;
                    AssetAutoClassifier.RefreshDestination(suggestion);
                    RefreshPlanIssues();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(56f);
                GUILayout.Label("配置先", GUILayout.Width(46f));
                EditorGUILayout.SelectableLabel(suggestion.DestinationPath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawPlanFooter()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                if (_planIssues.Count > 0)
                {
                    foreach (string issue in _planIssues.Take(5))
                    {
                        EditorGUILayout.HelpBox(issue, MessageType.Error);
                    }

                    if (_planIssues.Count > 5)
                    {
                        EditorGUILayout.LabelField($"ほか {_planIssues.Count - 5} 件", EditorStyles.miniLabel);
                    }
                }

                int selected = _suggestions.Count(suggestion => suggestion.Selected);
                bool blockedByDirectRisk = HasDirectRisk() && !_acknowledgedDirectRisk;
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("一時領域を表示", GUILayout.Height(38f), GUILayout.Width(140f)))
                    {
                        PingIncomingRoot();
                    }

                    GUILayout.FlexibleSpace();
                    using (new EditorGUI.DisabledScope(selected == 0 || _planIssues.Count > 0 || blockedByDirectRisk))
                    {
                        if (DrawPrimaryButton($"{selected}件を正式配置", 42f, 220f))
                        {
                            PromoteSelected();
                        }
                    }
                }
            }
        }

        private void DrawDirectImportRisk()
        {
            if (_directReport == null)
            {
                return;
            }

            int risks = _directReport.ModifiedExistingPaths.Count + _directReport.BlockedPaths.Count + _directReport.Errors.Count;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"直接Importを検知: {_directReport.PackageName}", HeaderStyle());
                EditorGUILayout.LabelField($"自動隔離 {_directReport.QuarantinedPaths.Count}件 / 要対応 {risks}件");
                if (_directReport.ModifiedExistingPaths.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"既存Assetが {_directReport.ModifiedExistingPaths.Count}件 変更されています。自動復元は行いません。Git差分を確認してください。\n"
                        + string.Join("\n", _directReport.ModifiedExistingPaths.Take(6)),
                        MessageType.Error);
                }

                if (_directReport.BlockedPaths.Count > 0)
                {
                    EditorGUILayout.HelpBox(
                        $"Script／Plugin／UnityPackageが {_directReport.BlockedPaths.Count}件 含まれています。Developer reviewが必要です。\n"
                        + string.Join("\n", _directReport.BlockedPaths.Take(6)),
                        MessageType.Error);
                }

                if (_directReport.Errors.Count > 0)
                {
                    EditorGUILayout.HelpBox(string.Join("\n", _directReport.Errors.Take(6)), MessageType.Error);
                }

                if (risks > 0)
                {
                    _acknowledgedDirectRisk = EditorGUILayout.ToggleLeft(
                        "上記の危険項目を確認し、Git差分を別途確認します",
                        _acknowledgedDirectRisk);
                }
            }
        }

        private void DrawCompleteStep()
        {
            _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(_lastMoveResult?.Succeeded == true ? "配置が完了しました" : "Import結果", CompletionTitleStyle());
            EditorGUILayout.Space(8f);

            if (_lastMoveResult?.Succeeded == true)
            {
                EditorGUILayout.HelpBox(
                    $"{_lastMoveResult.CompletedMoves.Count}件のAssetを移動し、GUIDと依存GUIDを検証しました。",
                    MessageType.Info);
                foreach (AssetMovePlan move in _lastMoveResult.CompletedMoves.Take(80))
                {
                    EditorGUILayout.LabelField("✓ " + move.DestinationPath, EditorStyles.miniLabel);
                }
            }
            else if (_directReport != null)
            {
                DrawDirectImportRisk();
                if (_directReport.QuarantinedPaths.Count == 0)
                {
                    EditorGUILayout.HelpBox("正式配置できる新規Assetはありません。危険項目とGit差分を確認してください。", MessageType.Warning);
                }
            }

            EditorGUILayout.Space(16f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("別のPackageを受け取る", GUILayout.Height(40f), GUILayout.Width(180f)))
                {
                    ResetWorkflow();
                }

                if (GUILayout.Button("配置先をProjectで表示", GUILayout.Height(40f), GUILayout.Width(180f)))
                {
                    PingLastDestination();
                }

                GUILayout.FlexibleSpace();
                bool canUndo = AssetPromotionHistory.CanUndo(out string reason);
                using (new EditorGUI.DisabledScope(!canUndo))
                {
                    if (GUILayout.Button("直前の正式配置を取り消す", GUILayout.Height(40f), GUILayout.Width(190f)))
                    {
                        UndoLastPromotion();
                    }
                }

                if (!canUndo && !string.IsNullOrWhiteSpace(reason))
                {
                    GUILayout.Label(new GUIContent("?", reason), GUILayout.Width(18f));
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void InspectPackage()
        {
            _issues.Clear();
            _suggestions.Clear();
            try
            {
                _inspection = _reader.Inspect(_packagePath);
                _incomingRoot = PackagePlacementPlanner.BuildPackageIncomingRoot(
                    Path.GetFileNameWithoutExtension(_packagePath),
                    DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                _issues.AddRange(PackageCollisionValidator.Validate(_inspection, _incomingRoot));
                _suggestions.AddRange(_inspection.Assets
                    .Where(record => record.Status == PackageAssetStatus.New)
                    .Select(AssetAutoClassifier.ClassifyPackageRecord));
                _step = WizardStep.Inspect;
                _mainScroll = Vector2.zero;
            }
            catch (Exception exception)
            {
                _issues.Add(new PackageIssue
                {
                    Severity = PackageIssueSeverity.Error,
                    SourcePath = _packagePath,
                    Message = exception.Message,
                });
                _step = WizardStep.Inspect;
            }
        }

        private void ExtractPackage()
        {
            List<UnityPackageAssetRecord> newAssets = _inspection.Assets
                .Where(record => record.Status == PackageAssetStatus.New)
                .ToList();
            if (!EditorUtility.DisplayDialog(
                    "安全な一時領域へ展開",
                    $"新規Asset {newAssets.Count}件を次へ展開します。\n\n{_incomingRoot}\n\n導入済み・更新候補・停止対象は展開しません。",
                    "展開する",
                    "キャンセル"))
            {
                return;
            }

            try
            {
                _reader.Extract(_inspection, _incomingRoot, newAssets);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                _suggestions.Clear();
                foreach (UnityPackageAssetRecord record in newAssets)
                {
                    string relative = record.SourcePath == "Assets"
                        ? string.Empty
                        : record.SourcePath.Substring("Assets/".Length);
                    string extractedPath = $"{_incomingRoot}/{relative}".TrimEnd('/');
                    if (AssetDatabase.IsValidFolder(extractedPath))
                    {
                        continue;
                    }

                    AssetClassificationSuggestion suggestion = AssetAutoClassifier.ClassifyAsset(extractedPath, record.SourcePath);
                    suggestion.Guid = record.Guid;
                    _suggestions.Add(suggestion);
                }

                _step = WizardStep.Classify;
                _assetScroll = Vector2.zero;
                RefreshPlanIssues();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("展開に失敗しました", exception.Message, "閉じる");
            }
        }

        private void PromoteSelected()
        {
            RefreshPlanIssues();
            if (_planIssues.Count > 0)
            {
                return;
            }

            List<AssetMovePlan> plans = _suggestions
                .Where(suggestion => suggestion.Selected)
                .Select(PackagePlacementPlanner.PlanPromotion)
                .ToList();
            if (!EditorUtility.DisplayDialog(
                    "正式配置を実行",
                    $"{plans.Count}件を正式フォルダへ移動します。\n\nGUIDと依存GUIDを検証し、失敗した場合はロールバックします。",
                    "配置する",
                    "キャンセル"))
            {
                return;
            }

            _lastMoveResult = AssetMoveExecutor.Execute(plans);
            if (!_lastMoveResult.Succeeded)
            {
                foreach (string error in _lastMoveResult.Errors)
                {
                    Debug.LogError("[Asset Intake] " + error);
                }

                EditorUtility.DisplayDialog("正式配置に失敗しました", string.Join("\n", _lastMoveResult.Errors.Take(8)), "閉じる");
                return;
            }

            AssetPromotionHistory.Record(_lastMoveResult.CompletedMoves);
            _step = WizardStep.Complete;
            _mainScroll = Vector2.zero;
        }

        private void RefreshPlanIssues()
        {
            _planIssues.Clear();
            HashSet<string> destinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (AssetClassificationSuggestion suggestion in _suggestions.Where(item => item.Selected))
            {
                AssetAutoClassifier.RefreshDestination(suggestion);
                if (!destinations.Add(suggestion.DestinationPath))
                {
                    _planIssues.Add("同じ配置先が複数あります: " + suggestion.DestinationPath);
                }

                string existingGuid = AssetDatabase.AssetPathToGUID(suggestion.DestinationPath);
                if (!string.IsNullOrWhiteSpace(existingGuid)
                    && !string.Equals(existingGuid, suggestion.Guid, StringComparison.OrdinalIgnoreCase))
                {
                    _planIssues.Add("配置先が既に使用されています: " + suggestion.DestinationPath);
                }

                if (PackagePathUtility.IsBlockedExtension(suggestion.SourcePath))
                {
                    _planIssues.Add("コード／Pluginは正式配置できません: " + suggestion.SourcePath);
                }
            }
        }

        private void DrawPackageSummary()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(Path.GetFileName(_packagePath), EditorStyles.boldLabel);
                EditorGUILayout.LabelField(_packagePath, EditorStyles.miniLabel);
                using (new EditorGUILayout.HorizontalScope())
                {
                    DrawMetric("新規", CountStatus(PackageAssetStatus.New), new Color(0.25f, 0.65f, 0.95f));
                    DrawMetric("導入済み", CountStatus(PackageAssetStatus.Installed), new Color(0.25f, 0.75f, 0.45f));
                    DrawMetric("更新候補", CountStatus(PackageAssetStatus.UpdateCandidate), new Color(0.95f, 0.68f, 0.20f));
                    DrawMetric(
                        "停止",
                        CountStatus(PackageAssetStatus.Conflict) + CountStatus(PackageAssetStatus.Blocked),
                        new Color(0.95f, 0.30f, 0.30f));
                }

                EditorGUILayout.LabelField("一時展開先", _incomingRoot);
            }
        }

        private void DrawIssueSummary()
        {
            foreach (PackageIssue issue in _issues.Take(30))
            {
                MessageType type = issue.Severity == PackageIssueSeverity.Error
                    ? MessageType.Error
                    : issue.Severity == PackageIssueSeverity.Warning
                        ? MessageType.Warning
                        : MessageType.Info;
                EditorGUILayout.HelpBox($"{issue.SourcePath}\n{issue.Message}", type);
            }

            if (_issues.Count > 30)
            {
                EditorGUILayout.LabelField($"ほか {_issues.Count - 30} 件", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private int CountStatus(PackageAssetStatus status)
        {
            return _inspection?.Assets.Count(record => record.Status == status) ?? 0;
        }

        private void ExportUpdateCandidatesForComparison()
        {
            List<UnityPackageAssetRecord> updates = _inspection.Assets
                .Where(record => record.Status == PackageAssetStatus.UpdateCandidate)
                .ToList();
            if (updates.Count == 0)
            {
                return;
            }

            try
            {
                string name = Path.GetFileNameWithoutExtension(_packagePath)
                    + "_"
                    + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputPath = _reader.ExtractForComparison(_inspection, name, updates);
                EditorUtility.RevealInFinder(outputPath);
                EditorUtility.DisplayDialog(
                    "比較用ファイルを書き出しました",
                    $"Package側の更新候補 {updates.Count}件を次へ書き出しました。\n\n{outputPath}\n\nAssets内の既存ファイルは変更していません。",
                    "閉じる");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("比較用ファイルの書き出しに失敗しました", exception.Message, "閉じる");
            }
        }

        private static void DrawMetric(string label, int value, Color color)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 48f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, 0.16f));
            GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.UpperCenter, fontSize = 18 };
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.LowerCenter };
            GUI.Label(rect, value.ToString(), valueStyle);
            GUI.Label(rect, label, labelStyle);
        }

        private IEnumerable<AssetClassificationSuggestion> FilteredSuggestions()
        {
            return _suggestions.Where(suggestion =>
                (!_filterReviewOnly || suggestion.RequiresReview)
                && (string.IsNullOrWhiteSpace(_search)
                    || suggestion.OriginalPath.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0
                    || suggestion.DestinationPath.IndexOf(_search, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        private static AssetDomain DrawDomainPopup(AssetDomain current, params GUILayoutOption[] options)
        {
            AssetDomain[] values = Enum.GetValues(typeof(AssetDomain)).Cast<AssetDomain>().ToArray();
            string[] labels = values.Select(DomainLabel).ToArray();
            int index = Array.IndexOf(values, current);
            int selected = EditorGUILayout.Popup(Math.Max(0, index), labels, options);
            return values[selected];
        }

        private static string DomainLabel(AssetDomain domain)
        {
            switch (domain)
            {
                case AssetDomain.Enemies: return "通常敵";
                case AssetDomain.Bosses: return "ボス";
                case AssetDomain.Player: return "プレイヤー";
                case AssetDomain.Collectibles: return "収集物";
                case AssetDomain.Stage: return "ステージ";
                case AssetDomain.Roguelike: return "ローグライク";
                case AssetDomain.Shop: return "ショップ";
                case AssetDomain.UI: return "UI";
                case AssetDomain.Audio: return "Audio";
                case AssetDomain.Camera: return "Camera";
                case AssetDomain.VFX: return "VFX";
                case AssetDomain.Shared: return "共通";
                case AssetDomain.Development: return "開発用";
                case AssetDomain.ThirdParty: return "外部Asset";
                default: return domain.ToString();
            }
        }

        private static string ConfidenceText(ClassificationConfidence confidence)
        {
            switch (confidence)
            {
                case ClassificationConfidence.High: return "高信頼";
                case ClassificationConfidence.Medium: return "確認推奨";
                default: return "要確認";
            }
        }

        private static Color ConfidenceColor(ClassificationConfidence confidence)
        {
            switch (confidence)
            {
                case ClassificationConfidence.High: return new Color(0.20f, 0.75f, 0.42f);
                case ClassificationConfidence.Medium: return new Color(0.95f, 0.65f, 0.18f);
                default: return new Color(0.92f, 0.30f, 0.30f);
            }
        }

        private static GUIStyle ConfidenceStyle(ClassificationConfidence confidence)
        {
            return new GUIStyle(EditorStyles.miniBoldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = ConfidenceColor(confidence) },
            };
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        }

        private static GUIStyle CompletionTitleStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
        }

        private static bool DrawPrimaryButton(string label, float height, float width = 0f)
        {
            GUIStyle style = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = 13 };
            Color previous = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.20f, 0.62f, 0.92f);
            bool clicked = width > 0f
                ? GUILayout.Button(label, style, GUILayout.Height(height), GUILayout.Width(width))
                : GUILayout.Button(label, style, GUILayout.Height(height), GUILayout.ExpandWidth(true));
            GUI.backgroundColor = previous;
            return clicked;
        }

        private void SetPackagePath(string path)
        {
            _packagePath = Path.GetFullPath(path);
            _inspection = null;
            _issues.Clear();
            _suggestions.Clear();
        }

        private void ResetWorkflow()
        {
            _step = WizardStep.Select;
            _packagePath = null;
            _incomingRoot = null;
            _inspection = null;
            _directReport = null;
            _lastMoveResult = null;
            _issues.Clear();
            _suggestions.Clear();
            _planIssues.Clear();
            _search = string.Empty;
            _filterReviewOnly = false;
            _acknowledgedDirectRisk = false;
            _mainScroll = Vector2.zero;
            _assetScroll = Vector2.zero;
            Repaint();
        }

        private void UndoLastPromotion()
        {
            if (!EditorUtility.DisplayDialog("直前の正式配置を取り消す", "このUnityセッションで最後に行った正式配置を _Incoming へ戻します。", "取り消す", "キャンセル"))
            {
                return;
            }

            AssetMoveResult result = AssetPromotionHistory.UndoLast();
            if (!result.Succeeded)
            {
                EditorUtility.DisplayDialog("取り消せませんでした", string.Join("\n", result.Errors), "閉じる");
                return;
            }

            _lastMoveResult = null;
            _step = WizardStep.Classify;
            foreach (AssetClassificationSuggestion suggestion in _suggestions)
            {
                suggestion.Selected = false;
            }

            RefreshPlanIssues();
        }

        private void PingIncomingRoot()
        {
            PingAsset(_incomingRoot);
        }

        private void PingLastDestination()
        {
            string path = _lastMoveResult?.CompletedMoves.FirstOrDefault()?.DestinationPath ?? _incomingRoot;
            PingAsset(path);
        }

        private static void PingAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
                asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(parent);
            }

            if (asset != null)
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private bool HasDirectRisk()
        {
            return _directReport != null
                && (_directReport.ModifiedExistingPaths.Count > 0
                    || _directReport.BlockedPaths.Count > 0
                    || _directReport.Errors.Count > 0);
        }

        private string GetInitialBrowseDirectory()
        {
            string root = Directory.GetParent(Application.dataPath)?.FullName;
            string incoming = string.IsNullOrWhiteSpace(root) ? null : Path.Combine(root, "IncomingPackages");
            return !string.IsNullOrWhiteSpace(incoming) && Directory.Exists(incoming) ? incoming : root ?? string.Empty;
        }

        private static bool IsValidPackagePath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && File.Exists(path)
                && string.Equals(Path.GetExtension(path), ".unitypackage", StringComparison.OrdinalIgnoreCase);
        }

        private static string RecoverOriginalPath(string quarantinedPath, string incomingRoot)
        {
            if (string.IsNullOrWhiteSpace(incomingRoot)
                || !quarantinedPath.StartsWith(incomingRoot + "/", StringComparison.Ordinal))
            {
                return quarantinedPath;
            }

            return "Assets/" + quarantinedPath.Substring(incomingRoot.Length + 1);
        }

        private static string CommonIncomingRoot(IEnumerable<string> paths)
        {
            string first = paths.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(first))
            {
                return "Assets/_Incoming";
            }

            string[] segments = first.Split('/');
            return segments.Length >= 3 ? string.Join("/", segments.Take(3)) : "Assets/_Incoming";
        }
    }
}
