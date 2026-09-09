#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Core.Management;
using Game.Data.Collectibles;
using Game.Data.Player;
using Game.Gameplay.Roguelike.Effects;
using Game.WaveSystem;
using UnityEditor;
using UnityEngine;

namespace Game.Gameplay.Roguelike.Editor
{
    public sealed class RoguelikeBalanceEditorWindow : EditorWindow
    {
        private enum Tab
        {
            Overview,
            WaveRewards,
            Upgrades,
            ShopItems,
            SpawnTable,
            Effects,
            Validation,
            DifficultyCurve,
        }

        private static readonly string[] TabLabels =
        {
            "概要", "Wave報酬", "強化一覧", "ショップ商品", "出現物・確率", "特殊効果", "検証・確率確認", "難易度カーブ",
        };

        /// <summary>
        /// RoguelikeUpgradeRuntime.Apply の switch が処理するId。変更したら必ず両方を更新すること。
        /// </summary>
        private static readonly HashSet<string> KnownSystemicUpgradeIds = new HashSet<string>
        {
            "3", "4", "5", "6", "7", "8", "10", "12", "13", "14", "15", "20",
        };

        private SO_RoguelikeBalanceConfig _config;
        private Tab _tab;
        private Vector2 _scroll;
        private Vector2 _upgradeListScroll;
        private UpgradeData _selectedUpgrade;
        private string _upgradeSearch = string.Empty;
        private string _newUpgradeName = "新しい強化";
        private int _wavePreviewSeed = 12345;
        private StageDataSO _cachedPreviewStage;
        private int _cachedPreviewSeed;
        private List<WaveDataSO> _cachedWaveSequence;
        private string _cachedWaveError;
        private StageDataSO _waveSyncPromptedStage;
        private int _waveSyncPromptedCount = -1;
        private string _spawnSearch = string.Empty;
        private Vector2 _difficultyCurveScroll;
        private WaveDataSO _selectedDifficultyWave;

        private SO_CollectibleFlavorNames _flavorNames;

        private readonly struct WaveRewardSnapshot
        {
            public readonly WaveRewardKind Kind;
            public readonly int CandidateCount;

            public WaveRewardSnapshot(SerializedProperty row)
            {
                Kind = (WaveRewardKind)row.FindPropertyRelative("_rewardKind").enumValueIndex;
                CandidateCount = row.FindPropertyRelative("_candidateCount").intValue;
            }
        }

        [MenuItem("Window/Roguelike/統合バランスエディタ")]
        public static void Open()
        {
            RoguelikeBalanceEditorWindow window = GetWindow<RoguelikeBalanceEditorWindow>();
            window.titleContent = new GUIContent("Roguelike Balance");
            window.minSize = new Vector2(980f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            if (_config == null)
                _config = SO_RoguelikeBalanceConfig.LoadDefault();
        }

        private void OnGUI()
        {
            DrawHeader();
            if (_config == null)
            {
                EditorGUILayout.HelpBox(
                    "中央設定がありません。既存アセットを自動検出して初期設定を作成してください。",
                    MessageType.Warning);
                if (GUILayout.Button("既存データから中央設定を作成", GUILayout.Height(36f)))
                    CreateDefaultConfig();
                return;
            }

            _tab = (Tab)GUILayout.Toolbar((int)_tab, TabLabels, GUILayout.Height(28f));
            EditorGUILayout.Space(6f);

            if (_tab == Tab.Upgrades || _tab == Tab.Effects)
            {
                DrawUpgradeWorkspace(_tab == Tab.Effects);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case Tab.Overview: DrawOverview(); break;
                case Tab.WaveRewards: DrawWaveRewards(); break;
                case Tab.ShopItems: DrawShopItems(); break;
                case Tab.SpawnTable: DrawSpawnTable(); break;
                case Tab.Validation: DrawValidation(); break;
                case Tab.DifficultyCurve: DrawDifficultyCurve(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                SO_RoguelikeBalanceConfig next = (SO_RoguelikeBalanceConfig)EditorGUILayout.ObjectField(
                    _config,
                    typeof(SO_RoguelikeBalanceConfig),
                    false,
                    GUILayout.MinWidth(280f));
                if (next != _config)
                {
                    _config = next;
                    _selectedUpgrade = null;
                }

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("選択", EditorStyles.toolbarButton, GUILayout.Width(54f)) && _config != null)
                    Selection.activeObject = _config;
                if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(54f)))
                    AssetDatabase.SaveAssets();
            }
        }

        private void DrawOverview()
        {
            StageDataSO stage = ResolveStageData();
            IReadOnlyList<WaveDataSO> waveSequence = GetWaveSequence(stage);
            EditorGUILayout.LabelField("ローグライク統合管理", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Wave報酬、常設ショップの強化一覧、出現物の確率を一か所で調整します。",
                MessageType.Info);

            DrawConfigReferences();

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                string stageNote = stage != null
                    ? $"{stage.StageName} / 報酬 {stage.RegularWaveCount} + Boss {(stage.BossWave != null ? 1 : 0)}"
                    : "Stageが未設定";
                int actualWaveCount = waveSequence != null ? waveSequence.Count : stage != null ? stage.TotalWaveCount : 0;
                DrawSummaryCard("実Wave", actualWaveCount.ToString(), stageNote, new Color(0.35f, 0.85f, 1f));
                DrawSummaryCard("強化", (_config.UpgradePool != null ? _config.UpgradePool.Count : 0).ToString(), "常設ショップに並ぶ強化", new Color(1f, 0.75f, 0.28f));
                DrawSummaryCard("出現モデル", (_config.CollectibleTable != null ? _config.CollectibleTable.GetAllItems().Count : 0).ToString(), "通常抽選と補正後確率", new Color(0.52f, 1f, 0.58f));
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("関連ツール", EditorStyles.boldLabel);
            if (GUILayout.Button("既存の Wave Editor を開く", GUILayout.Width(240f), GUILayout.Height(28f)))
                EditorApplication.ExecuteMenuItem("Window/Wave System/Wave Editor");
        }

        private void DrawConfigReferences()
        {
            SerializedObject serialized = new SerializedObject(_config);
            serialized.Update();
            SerializedProperty stageProperty = serialized.FindProperty("_stageData");
            StageDataSO previousStage = stageProperty.objectReferenceValue as StageDataSO;
            EditorGUILayout.PropertyField(stageProperty, new GUIContent("対象Stage"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_upgradePool"), new GUIContent("強化プール"));
            EditorGUILayout.PropertyField(serialized.FindProperty("_collectibleTable"), new GUIContent("出現テーブル"));
            serialized.ApplyModifiedProperties();
            StageDataSO nextStage = stageProperty.objectReferenceValue as StageDataSO;
            if (nextStage != previousStage)
            {
                InvalidateWavePreview();
                SynchronizeWaveRewardsToStage(nextStage);
            }
        }

        private static void DrawSummaryCard(string title, string value, string note, Color accent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(180f), GUILayout.Height(108f)))
            {
                GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13 };
                GUIStyle number = CreateMetricStyle(31, accent, TextAnchor.MiddleLeft);
                GUIStyle noteStyle = new GUIStyle(EditorStyles.label) { fontSize = 12, wordWrap = true };
                SetTextColor(noteStyle, new Color(0.82f, 0.84f, 0.88f));
                EditorGUILayout.LabelField(title, titleStyle, GUILayout.Height(20f));
                EditorGUILayout.LabelField(value, number, GUILayout.Height(39f));
                EditorGUILayout.LabelField(note, noteStyle, GUILayout.Height(31f));
            }
        }

        private void DrawWaveRewards()
        {
            SerializedObject serialized = new SerializedObject(_config);
            serialized.Update();
            SerializedProperty rewards = serialized.FindProperty("_waveRewards");
            StageDataSO stage = ResolveStageData();
            IReadOnlyList<WaveDataSO> waveSequence = GetWaveSequence(stage);

            EditorGUILayout.LabelField("Waveクリア報酬", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "常設ショップが開くタイミングの目安です（Standard固定）。ボスWaveには報酬行を作らない想定です。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(Application.isPlaying))
                {
                    StageDataSO selected = (StageDataSO)EditorGUILayout.ObjectField(
                        "対象StageDataSO",
                        _config.StageData,
                        typeof(StageDataSO),
                        false);
                    if (selected != _config.StageData)
                    {
                        SetStageData(selected);
                        return;
                    }

                    if (GUILayout.Button("SO一覧", GUILayout.Width(72f)))
                        ShowStageMenu();
                }
                if (Application.isPlaying)
                    GUILayout.Label("Play中はロード済みStageを表示", EditorStyles.miniLabel, GUILayout.Width(170f));
            }

            if (stage != null && !Application.isPlaying && TryAutoSyncWaveRewards(stage))
                return;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        stage != null ? $"読込中: {stage.StageName}" : "対象Stageが未設定",
                        new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 });
                    GUILayout.FlexibleSpace();
                    EditorGUI.BeginChangeCheck();
                    GUIStyle seedStyle = new GUIStyle(EditorStyles.numberField)
                    {
                        fontSize = 13,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                    SetTextColor(seedStyle, new Color(0.42f, 0.9f, 1f));
                    GUILayout.Label("表示Seed", EditorStyles.boldLabel, GUILayout.Width(62f));
                    using (new EditorGUI.DisabledScope(Application.isPlaying))
                        _wavePreviewSeed = EditorGUILayout.IntField(_wavePreviewSeed, seedStyle, GUILayout.Width(72f), GUILayout.Height(23f));
                    if (EditorGUI.EndChangeCheck()) InvalidateWavePreview();
                }
                if (stage != null)
                {
                    GUIStyle metric = CreateMetricStyle(16, new Color(0.35f, 0.85f, 1f), TextAnchor.MiddleLeft);
                    EditorGUILayout.LabelField(
                        $"全 {stage.TotalWaveCount} Wave　通常 {stage.RegularWaveCount}　Boss {(stage.BossWave != null ? 1 : 0)}",
                        metric,
                        GUILayout.Height(24f));
                }
                if (!string.IsNullOrEmpty(_cachedWaveError))
                    EditorGUILayout.HelpBox(_cachedWaveError, MessageType.Error);

                if (stage != null && _config.WaveRewards.Count != stage.RegularWaveCount)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.HelpBox(
                            $"報酬設定数（{_config.WaveRewards.Count}）と実Wave数（{stage.RegularWaveCount}）が一致していません。" +
                            "自動同期が保留中か、確認ダイアログでキャンセルされています。",
                            MessageType.Warning);
                        if (GUILayout.Button("今すぐ同期", GUILayout.Width(90f)))
                        {
                            serialized.ApplyModifiedProperties();
                            _waveSyncPromptedStage = null;
                            _waveSyncPromptedCount = -1;
                            if (TryAutoSyncWaveRewards(stage))
                                return;
                        }
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Wave", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("実際のWaveデータ", EditorStyles.boldLabel, GUILayout.Width(220f));
                GUILayout.Label("報酬", EditorStyles.boldLabel, GUILayout.Width(110f));
                GUILayout.Label("候補", EditorStyles.boldLabel, GUILayout.Width(55f));
            }

            int remove = -1;
            for (int index = 0; index < rewards.arraySize; index++)
            {
                SerializedProperty row = rewards.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    DrawIntProperty(row.FindPropertyRelative("_clearedWave"), 55f);
                    int waveNumber = row.FindPropertyRelative("_clearedWave").intValue;
                    WaveDataSO actualWave = waveSequence != null && waveNumber > 0 && waveNumber <= waveSequence.Count
                        ? waveSequence[waveNumber - 1]
                        : null;
                    GUIStyle waveNameStyle = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Bold };
                    SetTextColor(waveNameStyle, actualWave != null ? Color.white : new Color(1f, 0.62f, 0.35f));
                    GUILayout.Label(actualWave != null ? actualWave.WaveName : "未解決", waveNameStyle, GUILayout.Width(220f));
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("_rewardKind"), GUIContent.none, GUILayout.Width(110f));
                    DrawIntProperty(row.FindPropertyRelative("_candidateCount"), 55f);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                }
            }

            if (stage != null && stage.BossWave != null)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUIStyle bossNumber = CreateMetricStyle(15, new Color(1f, 0.48f, 0.35f), TextAnchor.MiddleCenter);
                    GUILayout.Label(stage.TotalWaveCount.ToString(), bossNumber, GUILayout.Width(55f), GUILayout.Height(24f));
                    GUILayout.Label(stage.BossWave.WaveName, EditorStyles.boldLabel, GUILayout.Width(220f));
                    GUILayout.Label("Boss / 報酬なし", EditorStyles.boldLabel, GUILayout.Width(150f));
                }
            }

            if (remove >= 0)
                rewards.DeleteArrayElementAtIndex(remove);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+ 報酬Waveを追加", GUILayout.Width(150f)))
                {
                    int index = rewards.arraySize;
                    rewards.InsertArrayElementAtIndex(index);
                    SerializedProperty row = rewards.GetArrayElementAtIndex(index);
                    row.FindPropertyRelative("_clearedWave").intValue = NextWaveNumber(rewards, index);
                    row.FindPropertyRelative("_rewardKind").enumValueIndex = (int)WaveRewardKind.Standard;
                    row.FindPropertyRelative("_candidateCount").intValue = 3;
                }
                if (GUILayout.Button("Wave順に並べ替え", GUILayout.Width(140f)))
                {
                    serialized.ApplyModifiedProperties();
                    SortWaveRewards();
                    return;
                }
            }

            serialized.ApplyModifiedProperties();
        }

        private void DrawShopItems()
        {
            EditorGUILayout.LabelField("ショップ商品(アイテム出現率アップ)", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "「キャンディ」枠に並ぶ各アイテムの商品名(フレーバーネーム)と、対応する出現率アップ強化を管理します。" +
                "商品名を編集後、「未生成の出現率アップを一括作成」でCollectibleType毎のUpgradeDataを自動生成し、強化プールへ登録します。",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                _flavorNames = (SO_CollectibleFlavorNames)EditorGUILayout.ObjectField(
                    "商品名テーブル", _flavorNames, typeof(SO_CollectibleFlavorNames), false);
                if (GUILayout.Button("新規作成", GUILayout.Width(80f)))
                    _flavorNames = CreateFlavorNamesAsset();
            }

            if (_flavorNames == null)
            {
                EditorGUILayout.HelpBox("商品名テーブル(SO_CollectibleFlavorNames)が未設定です。", MessageType.Warning);
                return;
            }

            if (_config.CollectibleTable == null)
            {
                EditorGUILayout.HelpBox("出現テーブルが未設定です。", MessageType.Error);
                return;
            }

            EditorGUILayout.Space(8f);
            SerializedObject flavorSerialized = new SerializedObject(_flavorNames);
            flavorSerialized.Update();
            SerializedProperty entries = flavorSerialized.FindProperty("_entries");
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("Type"), GUIContent.none, GUILayout.Width(120f));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("FlavorName"), GUIContent.none, GUILayout.MinWidth(160f));

                    CollectibleType type = (CollectibleType)entry.FindPropertyRelative("Type").enumValueIndex;
                    UpgradeData existing = FindSpawnRateUpgrade(type);
                    GUILayout.Label(existing != null ? "強化: 作成済み" : "強化: 未作成",
                        CreateMetricStyle(12, existing != null ? new Color(0.52f, 1f, 0.58f) : new Color(1f, 0.62f, 0.35f), TextAnchor.MiddleRight),
                        GUILayout.Width(110f));
                    if (existing != null && GUILayout.Button("選択", GUILayout.Width(50f)))
                        Selection.activeObject = existing;
                }
            }
            flavorSerialized.ApplyModifiedProperties();

            EditorGUILayout.Space(10f);
            if (GUILayout.Button("未生成の出現率アップを一括作成", GUILayout.Height(30f)))
                CreateMissingSpawnRateUpgrades();
        }

        private SO_CollectibleFlavorNames CreateFlavorNamesAsset()
        {
            string folder = GetUpgradeFolder(_config.UpgradePool);
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/SO_CollectibleFlavorNames.asset");
            SO_CollectibleFlavorNames asset = CreateInstance<SO_CollectibleFlavorNames>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private UpgradeData FindSpawnRateUpgrade(CollectibleType type)
        {
            if (_config.UpgradePool == null) return null;
            foreach (UpgradeData upgrade in _config.UpgradePool.Upgrades)
            {
                if (upgrade == null || upgrade.Category != UpgradeCategory.Consumable || upgrade.Effects == null)
                    continue;
                foreach (RoguelikeEffectModule effect in upgrade.Effects)
                {
                    if (effect is ItemSpawnRateUpEffect && GetEffectCollectibleType(effect) == type)
                        return upgrade;
                }
            }
            return null;
        }

        private static CollectibleType GetEffectCollectibleType(RoguelikeEffectModule effect)
        {
            FieldInfo field = effect.GetType().GetField("_collectibleType", BindingFlags.Instance | BindingFlags.NonPublic);
            return field != null ? (CollectibleType)field.GetValue(effect) : default;
        }

        private void CreateMissingSpawnRateUpgrades()
        {
            if (_config.CollectibleTable == null || _config.UpgradePool == null || _flavorNames == null)
                return;

            foreach (CollectibleData item in _config.CollectibleTable.GetAllItems())
            {
                if (item == null || item.Type == CollectibleType.BossWeak)
                    continue;
                if (FindSpawnRateUpgrade(item.Type) != null)
                    continue;

                string flavorName = _flavorNames.GetFlavorName(item.Type);
                string folder = GetUpgradeFolder(_config.UpgradePool);
                string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/UPGRADE_SpawnRate_{item.Type}.asset");

                UpgradeData upgrade = CreateInstance<UpgradeData>();
                upgrade.Id = $"spawn-rate-{item.Type}".ToLowerInvariant();
                upgrade.DisplayName = $"【{flavorName} 出現率アップ】";
                upgrade.Description = $"{flavorName}が出現しやすくなる！";
                upgrade.OfferType = UpgradeOfferType.Standard;
                upgrade.MaxLevel = 10;
                upgrade.Cost = 30;
                upgrade.CostMagni = 1.2f;
                upgrade.Category = UpgradeCategory.Consumable;

                var effect = new ItemSpawnRateUpEffect();
                typeof(ItemSpawnRateUpEffect)
                    .GetField("_collectibleType", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(effect, item.Type);
                upgrade.Effects.Add(effect);

                AssetDatabase.CreateAsset(upgrade, path);
                Undo.RegisterCreatedObjectUndo(upgrade, "出現率アップ強化を作成");
                AddUpgradeToPool(_config.UpgradePool, upgrade);
            }

            AssetDatabase.SaveAssets();
        }

        private void DrawUpgradeWorkspace(bool effectsOnly)
        {
            SO_UpgradePool pool = _config.UpgradePool;
            if (pool == null)
            {
                EditorGUILayout.HelpBox("強化プールが未設定です。", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(350f)))
                {
                    DrawUpgradeListToolbar(pool);
                    _upgradeListScroll = EditorGUILayout.BeginScrollView(_upgradeListScroll, EditorStyles.helpBox);
                    DrawUpgradeList(pool);
                    EditorGUILayout.EndScrollView();
                    DrawUpgradeCreation(pool);
                }

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (_selectedUpgrade == null)
                    {
                        EditorGUILayout.HelpBox("左の一覧から強化を選択してください。", MessageType.Info);
                        return;
                    }

                    if (effectsOnly)
                        DrawEffectsEditor(_selectedUpgrade);
                    else
                        DrawUpgradeInspector(_selectedUpgrade);
                }
            }
        }

        private void DrawUpgradeListToolbar(SO_UpgradePool pool)
        {
            _upgradeSearch = EditorGUILayout.TextField("検索", _upgradeSearch);
            if (GUILayout.Button("未登録を追加", GUILayout.Width(90f)))
                RegisterMissingUpgrades(pool);
        }

        private void DrawUpgradeList(SO_UpgradePool pool)
        {
            List<UpgradeData> visible = pool.Upgrades
                .Where(item => item != null)
                .Where(item => string.IsNullOrWhiteSpace(_upgradeSearch) ||
                               item.DisplayName.IndexOf(_upgradeSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.Id.IndexOf(_upgradeSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(item => item.Category)
                .ThenBy(item => item.DisplayName)
                .ToList();

            foreach (UpgradeData item in visible)
            {
                Color previous = GUI.backgroundColor;
                if (_selectedUpgrade == item) GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(34f)))
                {
                    if (GUILayout.Button(item.Icon != null ? item.Icon.texture : Texture2D.grayTexture, GUILayout.Width(28f), GUILayout.Height(28f)))
                        SelectUpgrade(item);
                    if (GUILayout.Button($"{item.DisplayName}\n{item.Category}  Lv.{item.MaxLevel}", EditorStyles.label, GUILayout.MinWidth(175f), GUILayout.Height(31f)))
                        SelectUpgrade(item);
                }
                GUI.backgroundColor = previous;
            }
        }

        private void DrawUpgradeCreation(SO_UpgradePool pool)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("新規追加", EditorStyles.boldLabel);
            _newUpgradeName = EditorGUILayout.TextField(_newUpgradeName);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("作成して自動登録"))
                    CreateUpgrade(pool);
                using (new EditorGUI.DisabledScope(_selectedUpgrade == null))
                {
                    if (GUILayout.Button("選択を複製"))
                        DuplicateUpgrade(pool);
                }
            }
        }

        private void DrawUpgradeInspector(UpgradeData upgrade)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(upgrade.DisplayName, new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
                if (GUILayout.Button("Projectで選択", GUILayout.Width(105f))) Selection.activeObject = upgrade;
            }

            EditorGUILayout.LabelField("実際に起きること（プレビュー）", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(BuildUpgradeEffectSummary(upgrade), MessageType.None);

            SerializedObject serialized = new SerializedObject(upgrade);
            serialized.Update();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
            DrawProperty(serialized, "Id", "ID");
            DrawProperty(serialized, "DisplayName", "表示名");
            DrawProperty(serialized, "Description", "基本説明");
            DrawProperty(serialized, "LevelDescriptions", "Lv別の詳細説明");
            DrawProperty(serialized, "LevelCardDescriptions", "Lv別のカード短文");
            EditorGUILayout.Space(5f);
            DrawProperty(serialized, "MaxLevel", "最大Lv");
            DrawProperty(serialized, "Icon", "アイコン");
            DrawProperty(serialized, "Category", "カテゴリ");
            DrawProperty(serialized, "Cost", "基礎コスト");
            DrawProperty(serialized, "CostMagni", "コスト倍率");

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("① プレイヤーステータス変化", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "PlayerStatsServiceが対応するTargetStat（MaxHp/MoveSpeed/AttachmentScale）のみ実際に反映されます。それ以外を選ぶと取得しても何も起きません。",
                MessageType.None);
            DrawProperty(serialized, "Modifiers", "ステータス変化");

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("② システム全体の数値", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"IdがRoguelikeUpgradeRuntime側の既知ID（{string.Join(", ", KnownSystemicUpgradeIds.OrderBy(id => id))}）のいずれかの場合のみGameplayValueが反映されます。" +
                "新しいシステム値を増やすにはコード側の対応が必要です。",
                MessageType.None);
            DrawProperty(serialized, "GameplayValue", "ゲーム効果値");

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("③ 特殊効果モジュール（アイテム出現率アップ等）", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("特殊効果タブへ", GUILayout.Width(110f)))
                {
                    serialized.ApplyModifiedProperties();
                    _tab = Tab.Effects;
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.LabelField($"設定済み: {upgrade.Effects.Count} 個", EditorStyles.miniLabel);

            serialized.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Lv.1～10 表示確認", EditorStyles.boldLabel);
            int max = Mathf.Min(10, Mathf.Max(1, upgrade.MaxLevel));
            for (int level = 1; level <= max; level++)
                EditorGUILayout.LabelField($"Lv.{level}", upgrade.GetCardText(level), EditorStyles.helpBox);
        }

        private string BuildUpgradeEffectSummary(UpgradeData upgrade)
        {
            var lines = new List<string>();

            if (upgrade.Modifiers != null)
            {
                foreach (StatModifier modifier in upgrade.Modifiers)
                {
                    if (modifier.TargetStat == PlayerStatType.None) continue;
                    string op = modifier.Operation switch
                    {
                        ModifierOperation.Add => "+",
                        ModifierOperation.Multiply => "×",
                        ModifierOperation.SubTract => "-",
                        _ => string.Empty,
                    };
                    lines.Add($"① {modifier.TargetStat}  {op}{modifier.Value}（1Lvあたり）");
                }
            }

            if (Mathf.Abs(upgrade.GameplayValue) > 0.0001f)
            {
                lines.Add(KnownSystemicUpgradeIds.Contains(upgrade.Id)
                    ? $"② システム値（Id={upgrade.Id}）に GameplayValue={upgrade.GameplayValue} を適用"
                    : $"② GameplayValue={upgrade.GameplayValue} が設定されていますが、Id '{upgrade.Id}' が未対応のため無視されます");
            }

            if (upgrade.Effects != null)
            {
                foreach (RoguelikeEffectModule effect in upgrade.Effects)
                {
                    if (effect == null) continue;
                    lines.Add(effect.Enabled ? $"③ {effect.Summary}" : $"③ (無効) {effect.Summary}");
                }
            }

            return lines.Count > 0
                ? string.Join("\n", lines)
                : "このLvで有効な効果が見つかりません。①～③のいずれかを設定してください。";
        }

        private void DrawEffectsEditor(UpgradeData upgrade)
        {
            EditorGUILayout.LabelField($"{upgrade.DisplayName} — 特殊効果", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "アイテム出現率アップなど、追加のゲームルールを部品として組み合わせます。",
                MessageType.Info);

            SerializedObject serialized = new SerializedObject(upgrade);
            serialized.Update();
            SerializedProperty effects = serialized.FindProperty("Effects");
            int remove = -1;
            for (int index = 0; index < effects.arraySize; index++)
            {
                SerializedProperty effect = effects.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string label = effect.managedReferenceValue is RoguelikeEffectModule module
                            ? GetEffectMenuName(module.GetType())
                            : "未設定";
                        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                        if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                    }
                    EditorGUILayout.PropertyField(effect, GUIContent.none, true);
                    if (effect.managedReferenceValue is RoguelikeEffectModule current)
                        EditorGUILayout.HelpBox(current.Summary, MessageType.None);
                }
            }
            if (remove >= 0) effects.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("+ 効果モジュールを追加", GUILayout.Height(30f)))
                ShowEffectMenu(effects, serialized);
            serialized.ApplyModifiedProperties();
        }

        private void DrawSpawnTable()
        {
            CollectibleTable table = _config.CollectibleTable;
            if (table == null)
            {
                EditorGUILayout.HelpBox("出現テーブルが未設定です。", MessageType.Error);
                return;
            }

            SerializedObject serialized = new SerializedObject(table);
            serialized.Update();
            SerializedProperty baseItems = serialized.FindProperty("_baseItems");
            SerializedProperty specialItems = serialized.FindProperty("_specialItems");
            float specialTotal = 0f;
            for (int index = 0; index < specialItems.arraySize; index++)
                specialTotal += Mathf.Max(0f, specialItems.GetArrayElementAtIndex(index).FindPropertyRelative("DropChancePercent").floatValue);
            float baseBudget = Mathf.Max(0f, 100f - specialTotal);

            EditorGUILayout.LabelField("出現するもの・確率", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "特殊枠を先に確率指定し、100%から引いた残りをベース枠で均等分配します。実行中はショップ購入済みの出現率アップも反映した実効確率を確認できます。",
                MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSummaryCard("特殊枠 合計", $"{specialTotal:0.##}%", specialTotal > 100f ? "100%を超えています" : "直接指定の合計", specialTotal > 100f ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.75f, 0.28f));
                DrawSummaryCard("ベース枠 残り", $"{baseBudget:0.##}%", baseItems.arraySize > 0 ? $"1種あたり約 {baseBudget / baseItems.arraySize:0.##}%" : "ベースモデル未登録", new Color(0.35f, 0.85f, 1f));
            }
            if (specialTotal > 100f)
                EditorGUILayout.HelpBox("特殊枠の合計が100%を超えています。ベース枠が抽選されません。", MessageType.Error);

            EditorGUILayout.Space(8f);
            _spawnSearch = EditorGUILayout.TextField("検索（モデル名/ID/種類）", _spawnSearch);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("ベース枠（残り確率を均等分配）", EditorStyles.boldLabel);
            DrawCollectibleArray(baseItems, table, baseBudget, _spawnSearch);
            if (GUILayout.Button("+ ベース枠", GUILayout.Width(120f))) baseItems.InsertArrayElementAtIndex(baseItems.arraySize);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("特殊枠（個別確率）", EditorStyles.boldLabel);
            int remove = -1;
            for (int index = 0; index < specialItems.arraySize; index++)
            {
                SerializedProperty row = specialItems.GetArrayElementAtIndex(index);
                SerializedProperty data = row.FindPropertyRelative("Data");
                SerializedProperty chance = row.FindPropertyRelative("DropChancePercent");
                CollectibleData item = data.objectReferenceValue as CollectibleData;
                if (!MatchesSpawnSearch(item, _spawnSearch)) continue;
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(data, GUIContent.none, GUILayout.MinWidth(250f));
                    EditorGUILayout.PropertyField(chance, new GUIContent("設定%"), GUILayout.Width(125f));
                    if (item != null && Application.isPlaying)
                        GUILayout.Label($"実効 {table.GetEffectiveDropPercent(item.Type):0.00}%", CreateMetricStyle(13, new Color(0.42f, 0.9f, 1f), TextAnchor.MiddleRight), GUILayout.Width(105f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                }
            }
            if (remove >= 0) specialItems.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("+ 特殊枠", GUILayout.Width(120f))) specialItems.InsertArrayElementAtIndex(specialItems.arraySize);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("未登録モデルを追加", GUILayout.Width(150f)))
                {
                    serialized.ApplyModifiedProperties();
                    RegisterMissingCollectibles(table);
                    return;
                }
                if (GUILayout.Button("種類順に並べ替え", GUILayout.Width(140f)))
                {
                    serialized.ApplyModifiedProperties();
                    SortCollectibles(table);
                    return;
                }
            }
            serialized.ApplyModifiedProperties();
        }

        private static void DrawCollectibleArray(SerializedProperty items, CollectibleTable table, float baseBudget, string search)
        {
            int remove = -1;
            float each = items.arraySize > 0 ? baseBudget / items.arraySize : 0f;
            for (int index = 0; index < items.arraySize; index++)
            {
                SerializedProperty itemProperty = items.GetArrayElementAtIndex(index);
                CollectibleData item = itemProperty.objectReferenceValue as CollectibleData;
                if (!MatchesSpawnSearch(item, search)) continue;
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(itemProperty, GUIContent.none, GUILayout.MinWidth(250f));
                    GUILayout.Label($"設定目安 {each:0.00}%", CreateMetricStyle(13, new Color(1f, 0.78f, 0.3f), TextAnchor.MiddleRight), GUILayout.Width(125f));
                    if (item != null && Application.isPlaying)
                        GUILayout.Label($"実効 {table.GetEffectiveDropPercent(item.Type):0.00}%", CreateMetricStyle(13, new Color(0.42f, 0.9f, 1f), TextAnchor.MiddleRight), GUILayout.Width(105f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                }
            }
            if (remove >= 0) items.DeleteArrayElementAtIndex(remove);
        }

        private static bool MatchesSpawnSearch(CollectibleData item, string search)
        {
            if (string.IsNullOrWhiteSpace(search))
                return true;
            if (item == null)
                return false;
            return item.name.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   (item.Id != null && item.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   item.Type.ToString().IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawDifficultyCurve()
        {
            StageDataSO stage = ResolveStageData();
            IReadOnlyList<WaveDataSO> waveSequence = GetWaveSequence(stage);

            EditorGUILayout.LabelField("難易度カーブ プレビュー", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "各WaveのAttackPressure（防衛ラインへの秒間ダメージ見積り、WaveMetricsCalculator算出）と敵総数、Wave報酬の配置を並べて確認します。",
                MessageType.Info);

            if (stage == null)
            {
                EditorGUILayout.HelpBox("対象Stageが未設定です。Wave報酬タブでStageを設定してください。", MessageType.Warning);
                return;
            }
            if (waveSequence == null || waveSequence.Count == 0)
            {
                EditorGUILayout.HelpBox($"実Wave構成を生成できません。{_cachedWaveError}", MessageType.Error);
                return;
            }

            var metrics = new List<WaveMetrics>(waveSequence.Count);
            float maxAttackPressure = 0f;
            for (int index = 0; index < waveSequence.Count; index++)
            {
                WaveMetrics metric = WaveMetricsCalculator.Calculate(waveSequence[index], null);
                metrics.Add(metric);
                maxAttackPressure = Mathf.Max(maxAttackPressure, metric.AttackPressure);
            }

            const float chartHeight = 150f;
            const float barWidth = 34f;

            EditorGUILayout.Space(6f);
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(_difficultyCurveScroll, GUILayout.Height(chartHeight + 110f)))
            {
                _difficultyCurveScroll = scrollScope.scrollPosition;
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int index = 0; index < waveSequence.Count; index++)
                    {
                        WaveDataSO wave = waveSequence[index];
                        WaveMetrics metric = metrics[index];
                        int waveNumber = index + 1;
                        WaveRewardDefinition reward = _config.GetRewardForWave(waveNumber);
                        bool isBossWave = stage.BossWave != null && waveNumber == stage.TotalWaveCount;

                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(barWidth)))
                        {
                            GUIStyle rewardStyle = CreateMetricStyle(9, new Color(1f, 0.75f, 0.28f), TextAnchor.LowerCenter);
                            GUILayout.Label(reward != null ? $"報{reward.CandidateCount}" : string.Empty, rewardStyle, GUILayout.Width(barWidth), GUILayout.Height(14f));

                            GUILayout.FlexibleSpace();
                            float barHeight = maxAttackPressure > 0f
                                ? Mathf.Max(2f, metric.AttackPressure / maxAttackPressure * chartHeight)
                                : 2f;
                            Color barColor = isBossWave ? new Color(1f, 0.42f, 0.35f) : new Color(0.35f, 0.85f, 1f);
                            Color previousColor = GUI.color;
                            GUI.color = barColor;
                            bool clicked = GUILayout.Button(GUIContent.none, GUILayout.Width(barWidth - 6f), GUILayout.Height(barHeight));
                            GUI.color = previousColor;
                            if (clicked)
                                _selectedDifficultyWave = wave;

                            GUIStyle enemyCountStyle = CreateMetricStyle(9, new Color(0.82f, 0.84f, 0.88f), TextAnchor.UpperCenter);
                            GUILayout.Label($"{metric.TotalEnemyCount}体", enemyCountStyle, GUILayout.Width(barWidth));
                            GUIStyle waveNumberStyle = CreateMetricStyle(10, _selectedDifficultyWave == wave ? new Color(0.42f, 0.9f, 1f) : Color.white, TextAnchor.UpperCenter);
                            if (GUILayout.Button(waveNumber.ToString(), waveNumberStyle, GUILayout.Width(barWidth)))
                                _selectedDifficultyWave = wave;
                        }
                    }
                }
            }
            EditorGUILayout.HelpBox(
                $"棒の高さ = AttackPressure（最大値 {maxAttackPressure:0.#} で正規化）。赤棒はBoss Wave。「報N」はそのWaveクリア後の報酬候補数。" +
                "棒かWave番号をクリックすると下に簡易編集欄が出ます。",
                MessageType.None);

            if (_selectedDifficultyWave != null)
                DrawWaveMiniEditor(_selectedDifficultyWave);
        }

        private void DrawWaveMiniEditor(WaveDataSO wave)
        {
            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Wave簡易編集: {wave.WaveName}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Projectで選択", GUILayout.Width(105f)))
                    {
                        Selection.activeObject = wave;
                        EditorGUIUtility.PingObject(wave);
                    }
                    if (GUILayout.Button("Wave Editorで詳細を開く", GUILayout.Width(160f)))
                    {
                        Selection.activeObject = wave;
                        EditorApplication.ExecuteMenuItem("Window/Wave System/Wave Editor");
                    }
                }
                EditorGUILayout.HelpBox(
                    "Group/敵構成などの詳細はWave Editorで編集してください。ここではWave全体の主要パラメータのみ調整できます。",
                    MessageType.None);

                SerializedObject serialized = new SerializedObject(wave);
                serialized.Update();
                DrawProperty(serialized, "waveName", "Wave名");
                DrawProperty(serialized, "plannerMemo", "プランナーメモ");
                DrawProperty(serialized, "startDelay", "開始待ち時間");
                DrawProperty(serialized, "completeDelay", "完了待ち時間");
                DrawProperty(serialized, "hpRate", "HP倍率");
                DrawProperty(serialized, "barrierRate", "バリア倍率");
                DrawProperty(serialized, "maxAliveEnemies", "同時出現数上限");
                DrawProperty(serialized, "minDistanceFromOtherEnemies", "敵間の最小距離");
                serialized.ApplyModifiedProperties();
            }
        }

        private void DrawValidation()
        {
            List<(MessageType Type, string Message)> messages = ValidateConfig();
            EditorGUILayout.LabelField("設定検証", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            if (messages.Count == 0)
                EditorGUILayout.HelpBox("設定上の問題は見つかりませんでした。", MessageType.Info);
            else
                foreach ((MessageType type, string message) in messages)
                    EditorGUILayout.HelpBox(message, type);
        }

        private List<(MessageType Type, string Message)> ValidateConfig()
        {
            var messages = new List<(MessageType, string)>();
            if (_config.StageData == null)
            {
                messages.Add((MessageType.Error, "対象StageDataSOが未設定です。"));
            }
            else
            {
                if (_config.WaveRewards.Count != _config.StageData.RegularWaveCount)
                {
                    messages.Add((
                        MessageType.Error,
                        $"報酬Wave数 {_config.WaveRewards.Count} が実Stageの通常Wave数 {_config.StageData.RegularWaveCount} と一致していません。Wave報酬タブで同期してください。"));
                }
                if (GetWaveSequence(_config.StageData) == null)
                    messages.Add((MessageType.Error, $"実Wave構成を生成できません。{_cachedWaveError}"));
            }
            if (_config.UpgradePool == null) messages.Add((MessageType.Error, "強化プールが未設定です。"));
            if (_config.CollectibleTable == null) messages.Add((MessageType.Error, "出現テーブルが未設定です。"));

            foreach (IGrouping<int, WaveRewardDefinition> duplicate in _config.WaveRewards.Where(item => item != null).GroupBy(item => item.ClearedWave).Where(group => group.Count() > 1))
                messages.Add((MessageType.Error, $"Wave {duplicate.Key} の報酬が重複しています。"));

            if (_config.UpgradePool != null)
            {
                foreach (IGrouping<string, UpgradeData> duplicate in _config.UpgradePool.Upgrades.Where(item => item != null).GroupBy(item => item.Id).Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
                    messages.Add((MessageType.Error, $"強化ID '{duplicate.Key}' が未設定または重複しています。"));

                foreach (UpgradeData upgrade in _config.UpgradePool.Upgrades.Where(item => item != null))
                {
                    bool hasModifiers = upgrade.Modifiers != null && upgrade.Modifiers.Length > 0;
                    bool hasEffects = upgrade.Effects != null && upgrade.Effects.Count > 0;
                    bool hasGameplayValue = Mathf.Abs(upgrade.GameplayValue) > 0.0001f;
                    bool idIsKnownToRuntime = KnownSystemicUpgradeIds.Contains(upgrade.Id);
                    if (hasGameplayValue && !hasModifiers && !hasEffects && !idIsKnownToRuntime)
                    {
                        messages.Add((
                            MessageType.Warning,
                            $"{upgrade.DisplayName}: GameplayValueが設定されていますが、Modifiers/Effectsが空でIdもRoguelikeUpgradeRuntime側で" +
                            "処理されていません。取得しても効果が発生しません（Idの追加実装が必要です）。"));
                    }
                }

                var exclusiveGroupOwners = new Dictionary<string, List<string>>();
                foreach (UpgradeData upgrade in _config.UpgradePool.Upgrades)
                {
                    if (upgrade?.Effects == null) continue;
                    foreach (RoguelikeEffectModule effect in upgrade.Effects)
                    {
                        if (effect == null || string.IsNullOrEmpty(effect.ExclusiveGroup)) continue;
                        if (!exclusiveGroupOwners.TryGetValue(effect.ExclusiveGroup, out List<string> owners))
                        {
                            owners = new List<string>();
                            exclusiveGroupOwners[effect.ExclusiveGroup] = owners;
                        }
                        if (!owners.Contains(upgrade.DisplayName))
                            owners.Add(upgrade.DisplayName);
                    }
                }
                foreach (KeyValuePair<string, List<string>> pair in exclusiveGroupOwners.Where(p => p.Value.Count > 1))
                {
                    messages.Add((
                        MessageType.Warning,
                        $"排他グループ '{pair.Key}' を複数の強化が使用しています（{string.Join(", ", pair.Value)}）。" +
                        "同時に所持すると後から取得した方だけが有効になり、残りは無言で無効化されます。"));
                }
            }

            if (_config.CollectibleTable != null)
            {
                SerializedObject table = new SerializedObject(_config.CollectibleTable);
                SerializedProperty special = table.FindProperty("_specialItems");
                float total = 0f;
                for (int index = 0; index < special.arraySize; index++)
                    total += Mathf.Max(0f, special.GetArrayElementAtIndex(index).FindPropertyRelative("DropChancePercent").floatValue);
                if (total > 100f) messages.Add((MessageType.Error, $"特殊出現率の合計が {total:0.##}% です。100%以下にしてください。"));
            }
            return messages;
        }

        private void CreateDefaultConfig()
        {
            const string folder = "Assets/Resources/Roguelike";
            EnsureFolder("Assets/Resources", "Roguelike");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/SO_RoguelikeBalance_Default.asset");
            SO_RoguelikeBalanceConfig config = CreateInstance<SO_RoguelikeBalanceConfig>();
            AssetDatabase.CreateAsset(config, path);
            AssetDatabase.SaveAssets();
            _config = config;
            Selection.activeObject = config;
        }

        private void CreateUpgrade(SO_UpgradePool pool)
        {
            string folder = GetUpgradeFolder(pool);
            string safeName = string.Concat((_newUpgradeName ?? "New").Select(character => char.IsLetterOrDigit(character) ? character : '_'));
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/UPGRADE_{safeName}.asset");
            UpgradeData upgrade = CreateInstance<UpgradeData>();
            upgrade.Id = Guid.NewGuid().ToString("N");
            upgrade.DisplayName = string.IsNullOrWhiteSpace(_newUpgradeName) ? "新しい強化" : _newUpgradeName;
            upgrade.Description = "効果説明を入力";
            upgrade.OfferType = UpgradeOfferType.Standard;
            upgrade.MaxLevel = 10;
            AssetDatabase.CreateAsset(upgrade, path);
            Undo.RegisterCreatedObjectUndo(upgrade, "強化を作成");
            AddUpgradeToPool(pool, upgrade);
            AssetDatabase.SaveAssets();
            SelectUpgrade(upgrade);
        }

        private void DuplicateUpgrade(SO_UpgradePool pool)
        {
            if (_selectedUpgrade == null) return;
            UpgradeData duplicate = Instantiate(_selectedUpgrade);
            duplicate.Id = Guid.NewGuid().ToString("N");
            duplicate.DisplayName += " コピー";
            string sourcePath = AssetDatabase.GetAssetPath(_selectedUpgrade);
            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{System.IO.Path.GetDirectoryName(sourcePath)?.Replace('\\', '/')}/{System.IO.Path.GetFileNameWithoutExtension(sourcePath)}_Copy.asset");
            AssetDatabase.CreateAsset(duplicate, path);
            Undo.RegisterCreatedObjectUndo(duplicate, "強化を複製");
            AddUpgradeToPool(pool, duplicate);
            AssetDatabase.SaveAssets();
            SelectUpgrade(duplicate);
        }

        private static void RegisterMissingUpgrades(SO_UpgradePool pool)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:UpgradeData"))
            {
                UpgradeData upgrade = AssetDatabase.LoadAssetAtPath<UpgradeData>(AssetDatabase.GUIDToAssetPath(guid));
                if (upgrade != null && !pool.Upgrades.Contains(upgrade)) AddUpgradeToPool(pool, upgrade);
            }
            AssetDatabase.SaveAssets();
        }

        private static void AddUpgradeToPool(SO_UpgradePool pool, UpgradeData upgrade)
        {
            SerializedObject serialized = new SerializedObject(pool);
            SerializedProperty list = serialized.FindProperty("_upgrades");
            for (int index = 0; index < list.arraySize; index++)
                if (list.GetArrayElementAtIndex(index).objectReferenceValue == upgrade) return;
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = upgrade;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(pool);
        }

        private void SelectUpgrade(UpgradeData upgrade)
        {
            _selectedUpgrade = upgrade;
            GUI.FocusControl(null);
            Repaint();
        }

        private static void DrawProperty(SerializedObject serialized, string propertyName, string label)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) EditorGUILayout.PropertyField(property, new GUIContent(label), true);
        }

        private static void ShowEffectMenu(SerializedProperty effects, SerializedObject serialized)
        {
            GenericMenu menu = new GenericMenu();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<RoguelikeEffectModule>().Where(type => !type.IsAbstract).OrderBy(GetEffectMenuName))
            {
                Type captured = type;
                menu.AddItem(new GUIContent(GetEffectMenuName(type)), false, () =>
                {
                    serialized.Update();
                    int index = effects.arraySize;
                    effects.InsertArrayElementAtIndex(index);
                    effects.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(captured);
                    serialized.ApplyModifiedProperties();
                });
            }
            menu.ShowAsContext();
        }

        private static string GetEffectMenuName(Type type)
            => type.GetCustomAttribute<RoguelikeEffectMenuAttribute>()?.Name ?? type.Name;

        private static int NextWaveNumber(SerializedProperty rewards, int excludedIndex)
        {
            int max = 0;
            for (int index = 0; index < rewards.arraySize; index++)
                if (index != excludedIndex) max = Mathf.Max(max, rewards.GetArrayElementAtIndex(index).FindPropertyRelative("_clearedWave").intValue);
            return max + 1;
        }

        private void SortWaveRewards()
        {
            SerializedObject serialized = new SerializedObject(_config);
            SerializedProperty rewards = serialized.FindProperty("_waveRewards");
            for (int left = 0; left < rewards.arraySize - 1; left++)
                for (int right = left + 1; right < rewards.arraySize; right++)
                    if (rewards.GetArrayElementAtIndex(left).FindPropertyRelative("_clearedWave").intValue > rewards.GetArrayElementAtIndex(right).FindPropertyRelative("_clearedWave").intValue)
                        rewards.MoveArrayElement(right, left);
            serialized.ApplyModifiedProperties();
        }

        private static void RegisterMissingCollectibles(CollectibleTable table)
        {
            SerializedObject serialized = new SerializedObject(table);
            SerializedProperty baseItems = serialized.FindProperty("_baseItems");
            HashSet<CollectibleData> registered = new HashSet<CollectibleData>();
            for (int index = 0; index < baseItems.arraySize; index++) registered.Add(baseItems.GetArrayElementAtIndex(index).objectReferenceValue as CollectibleData);
            SerializedProperty special = serialized.FindProperty("_specialItems");
            for (int index = 0; index < special.arraySize; index++) registered.Add(special.GetArrayElementAtIndex(index).FindPropertyRelative("Data").objectReferenceValue as CollectibleData);
            foreach (string guid in AssetDatabase.FindAssets("t:CollectibleData"))
            {
                CollectibleData item = AssetDatabase.LoadAssetAtPath<CollectibleData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item == null || registered.Contains(item)) continue;
                int index = baseItems.arraySize;
                baseItems.InsertArrayElementAtIndex(index);
                baseItems.GetArrayElementAtIndex(index).objectReferenceValue = item;
            }
            serialized.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        private static void SortCollectibles(CollectibleTable table)
        {
            SerializedObject serialized = new SerializedObject(table);
            SortObjectReferenceArray(serialized.FindProperty("_baseItems"), property => property.objectReferenceValue as CollectibleData);
            SerializedProperty special = serialized.FindProperty("_specialItems");
            for (int left = 0; left < special.arraySize - 1; left++)
                for (int right = left + 1; right < special.arraySize; right++)
                {
                    CollectibleData a = special.GetArrayElementAtIndex(left).FindPropertyRelative("Data").objectReferenceValue as CollectibleData;
                    CollectibleData b = special.GetArrayElementAtIndex(right).FindPropertyRelative("Data").objectReferenceValue as CollectibleData;
                    if (a != null && b != null && a.Type > b.Type) special.MoveArrayElement(right, left);
                }
            serialized.ApplyModifiedProperties();
        }

        private static void SortObjectReferenceArray(SerializedProperty array, Func<SerializedProperty, CollectibleData> getData)
        {
            for (int left = 0; left < array.arraySize - 1; left++)
                for (int right = left + 1; right < array.arraySize; right++)
                {
                    CollectibleData a = getData(array.GetArrayElementAtIndex(left));
                    CollectibleData b = getData(array.GetArrayElementAtIndex(right));
                    if (a != null && b != null && a.Type > b.Type) array.MoveArrayElement(right, left);
                }
        }

        private StageDataSO ResolveStageData()
        {
            if (Application.isPlaying)
            {
                StageSceneContext context = UnityEngine.Object.FindFirstObjectByType<StageSceneContext>();
                if (context != null && context.StageData != null)
                    return context.StageData;
            }

            return _config != null ? _config.StageData : null;
        }

        private IReadOnlyList<WaveDataSO> GetWaveSequence(StageDataSO stage)
        {
            if (Application.isPlaying && GameProgressionManager.Instance != null)
            {
                FieldInfo sequenceField = typeof(GameProgressionManager).GetField(
                    "_waveSequence",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (sequenceField?.GetValue(GameProgressionManager.Instance) is List<WaveDataSO> runtimeSequence &&
                    runtimeSequence.Count > 0)
                {
                    _cachedWaveError = string.Empty;
                    return runtimeSequence;
                }
            }

            if (stage == null)
            {
                _cachedPreviewStage = null;
                _cachedWaveSequence = null;
                _cachedWaveError = string.Empty;
                return null;
            }

            int previewSeed = ResolvePreviewSeed();
            if (_cachedPreviewStage == stage &&
                _cachedPreviewSeed == previewSeed &&
                (_cachedWaveSequence != null || !string.IsNullOrEmpty(_cachedWaveError)))
            {
                return _cachedWaveSequence;
            }

            _cachedPreviewStage = stage;
            _cachedPreviewSeed = previewSeed;
            if (StageWaveSequenceBuilder.TryBuild(stage, previewSeed, out List<WaveDataSO> sequence, out string error))
            {
                _cachedWaveSequence = sequence;
                _cachedWaveError = string.Empty;
            }
            else
            {
                _cachedWaveSequence = null;
                _cachedWaveError = error;
            }

            return _cachedWaveSequence;
        }

        private int ResolvePreviewSeed()
        {
            if (Application.isPlaying)
            {
                StageSceneContext context = UnityEngine.Object.FindFirstObjectByType<StageSceneContext>();
                if (context != null && context.UseFixedSeed)
                    return context.FixedSeed;
            }
            return _wavePreviewSeed;
        }

        private void InvalidateWavePreview()
        {
            _cachedPreviewStage = null;
            _cachedWaveSequence = null;
            _cachedWaveError = string.Empty;
            Repaint();
        }

        /// <summary>
        /// 報酬設定数と実Wave数のズレを検知し、必要なら自動で同期する。
        /// 既存の報酬設定が削除される（Wave数が減った）場合のみ確認ダイアログを出す。
        /// 同じズレに対して毎フレーム確認ダイアログを出さないよう、直近の対応状況を記憶する。
        /// </summary>
        private bool TryAutoSyncWaveRewards(StageDataSO stage)
        {
            if (_config.WaveRewards.Count == stage.RegularWaveCount)
            {
                _waveSyncPromptedStage = null;
                _waveSyncPromptedCount = -1;
                return false;
            }

            bool alreadyHandledThisMismatch =
                _waveSyncPromptedStage == stage && _waveSyncPromptedCount == stage.RegularWaveCount;
            if (alreadyHandledThisMismatch)
                return false;

            _waveSyncPromptedStage = stage;
            _waveSyncPromptedCount = stage.RegularWaveCount;

            bool losesCustomRows = _config.WaveRewards.Any(reward => reward != null && reward.ClearedWave > stage.RegularWaveCount);
            if (losesCustomRows)
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Wave報酬の自動同期",
                    $"対象Stageの通常Wave数（{stage.RegularWaveCount}）が現在の報酬設定数（{_config.WaveRewards.Count}）より少ないため、" +
                    "超過分の報酬設定（カスタム設定を含む可能性があります）が削除されます。続行しますか？",
                    "同期する",
                    "キャンセル");
                if (!confirmed)
                    return false;
            }

            SynchronizeWaveRewardsToStage(stage);
            return true;
        }

        private void SynchronizeWaveRewardsToStage(StageDataSO stage)
        {
            if (stage == null)
                return;

            SerializedObject serialized = new SerializedObject(_config);
            serialized.Update();
            SerializedProperty rewards = serialized.FindProperty("_waveRewards");
            var current = new Dictionary<int, WaveRewardSnapshot>();
            for (int index = 0; index < rewards.arraySize; index++)
            {
                SerializedProperty row = rewards.GetArrayElementAtIndex(index);
                int wave = row.FindPropertyRelative("_clearedWave").intValue;
                if (wave > 0 && !current.ContainsKey(wave))
                    current.Add(wave, new WaveRewardSnapshot(row));
            }

            rewards.ClearArray();
            for (int wave = 1; wave <= stage.RegularWaveCount; wave++)
            {
                int index = rewards.arraySize;
                rewards.InsertArrayElementAtIndex(index);
                SerializedProperty row = rewards.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("_clearedWave").intValue = wave;
                if (current.TryGetValue(wave, out WaveRewardSnapshot snapshot))
                {
                    row.FindPropertyRelative("_rewardKind").enumValueIndex = (int)snapshot.Kind;
                    row.FindPropertyRelative("_candidateCount").intValue = snapshot.CandidateCount;
                }
                else
                {
                    row.FindPropertyRelative("_rewardKind").enumValueIndex = (int)WaveRewardKind.Standard;
                    row.FindPropertyRelative("_candidateCount").intValue = 3;
                }
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
        }

        private void SetStageData(StageDataSO stage)
        {
            SerializedObject serialized = new SerializedObject(_config);
            serialized.Update();
            serialized.FindProperty("_stageData").objectReferenceValue = stage;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(_config);
            InvalidateWavePreview();
            SynchronizeWaveRewardsToStage(stage);
        }

        private void ShowStageMenu()
        {
            GenericMenu menu = new GenericMenu();
            string[] guids = AssetDatabase.FindAssets("t:StageDataSO");
            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("StageDataSOがありません"));
            }
            else
            {
                foreach (string guid in guids)
                {
                    StageDataSO stage = AssetDatabase.LoadAssetAtPath<StageDataSO>(AssetDatabase.GUIDToAssetPath(guid));
                    if (stage == null) continue;
                    StageDataSO captured = stage;
                    menu.AddItem(
                        new GUIContent($"{stage.StageName}  ({stage.name})"),
                        stage == _config.StageData,
                        () => SetStageData(captured));
                }
            }
            menu.ShowAsContext();
        }

        private static void DrawIntProperty(SerializedProperty property, float width)
        {
            GUIStyle style = new GUIStyle(EditorStyles.numberField)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            SetTextColor(style, new Color(0.42f, 0.9f, 1f));
            property.intValue = EditorGUILayout.IntField(property.intValue, style, GUILayout.Width(width), GUILayout.Height(24f));
        }

        private static GUIStyle CreateMetricStyle(int fontSize, Color color, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.label)
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = alignment,
                wordWrap = false,
            };
            SetTextColor(style, color);
            return style;
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }

        private static string GetUpgradeFolder(SO_UpgradePool pool)
            => System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(pool))?.Replace('\\', '/') ?? "Assets";

        private static void EnsureFolder(string parent, string name)
        {
            string path = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
