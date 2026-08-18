#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Game.Core.Management;
using Game.Data.Collectibles;
using Game.Data.Player;
using Game.Gameplay.Roguelike.CombatPressure;
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
            SpawnTable,
            CombatPressure,
            Effects,
            Validation,
        }

        private static readonly string[] TabLabels =
        {
            "概要", "Wave報酬", "強化一覧", "出現物・確率", "コンボ・状態異常", "特殊効果", "検証・確率確認",
        };

        private SO_RoguelikeBalanceConfig _config;
        private Tab _tab;
        private Vector2 _scroll;
        private Vector2 _upgradeListScroll;
        private UpgradeData _selectedUpgrade;
        private string _upgradeSearch = string.Empty;
        private UpgradeOfferType? _offerFilter;
        private string _newUpgradeName = "新しい強化";
        private UpgradeOfferType _newUpgradeType = UpgradeOfferType.Standard;
        private int _simulationWave = 1;
        private int _simulationTrials = 1000;
        private Dictionary<UpgradeData, int> _simulationResult;
        private int _wavePreviewSeed = 12345;
        private StageDataSO _cachedPreviewStage;
        private int _cachedPreviewSeed;
        private List<WaveDataSO> _cachedWaveSequence;
        private string _cachedWaveError;

        private readonly struct WaveRewardSnapshot
        {
            public readonly WaveRewardKind Kind;
            public readonly int CandidateCount;
            public readonly int EvolutionCandidateCount;
            public readonly bool AllowDeepening;
            public readonly int DeepeningLevelGain;

            public WaveRewardSnapshot(SerializedProperty row)
            {
                Kind = (WaveRewardKind)row.FindPropertyRelative("_rewardKind").enumValueIndex;
                CandidateCount = row.FindPropertyRelative("_candidateCount").intValue;
                EvolutionCandidateCount = row.FindPropertyRelative("_evolutionCandidateCount").intValue;
                AllowDeepening = row.FindPropertyRelative("_allowDeepening").boolValue;
                DeepeningLevelGain = row.FindPropertyRelative("_deepeningLevelGain").intValue;
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
                case Tab.SpawnTable: DrawSpawnTable(); break;
                case Tab.CombatPressure: DrawCombatPressure(); break;
                case Tab.Validation: DrawValidation(); break;
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
                    _simulationResult = null;
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
                "Wave報酬、強化候補、出現物の確率、コンボ・状態異常ルール、遺物・契約・進化の特殊効果を一か所で調整します。追加降下の種類は別指定せず、発動元ビルドの出力モデルを引き継ぎます。",
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
                DrawSummaryCard("強化", (_config.UpgradePool != null ? _config.UpgradePool.Count : 0).ToString(), "Lv.1～10と特殊強化", new Color(1f, 0.75f, 0.28f));
                DrawSummaryCard("出現モデル", (_config.CollectibleTable != null ? _config.CollectibleTable.GetAllItems().Count : 0).ToString(), "通常抽選と補正後確率", new Color(0.52f, 1f, 0.58f));
                DrawSummaryCard("圧力ルール", (_config.CombatPressureRuleSet != null ? _config.CombatPressureRuleSet.Rules.Count : 0).ToString(), "コンボ・状態異常累計", new Color(0.92f, 0.58f, 1f));
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
            EditorGUILayout.PropertyField(serialized.FindProperty("_combatPressureRuleSet"), new GUIContent("コンボ・状態異常ルール"));
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
                "同じステージ内のWaveクリア後に出す選択を定義します。ボスWaveには報酬行を作らない想定です。",
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
                    using (new EditorGUI.DisabledScope(stage == null))
                    {
                        if (GUILayout.Button("実Wave構成に同期", GUILayout.Width(140f), GUILayout.Height(26f)))
                        {
                            serialized.ApplyModifiedProperties();
                            SynchronizeWaveRewardsToStage(stage);
                            return;
                        }
                    }
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
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Wave", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("実際のWaveデータ", EditorStyles.boldLabel, GUILayout.Width(220f));
                GUILayout.Label("報酬", EditorStyles.boldLabel, GUILayout.Width(110f));
                GUILayout.Label("候補", EditorStyles.boldLabel, GUILayout.Width(55f));
                GUILayout.Label("進化枠", EditorStyles.boldLabel, GUILayout.Width(65f));
                GUILayout.Label("深化", EditorStyles.boldLabel, GUILayout.Width(45f));
                GUILayout.Label("上昇Lv", EditorStyles.boldLabel, GUILayout.Width(65f));
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
                    DrawIntProperty(row.FindPropertyRelative("_evolutionCandidateCount"), 65f);
                    EditorGUILayout.PropertyField(row.FindPropertyRelative("_allowDeepening"), GUIContent.none, GUILayout.Width(45f));
                    DrawIntProperty(row.FindPropertyRelative("_deepeningLevelGain"), 65f);
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
                    row.FindPropertyRelative("_evolutionCandidateCount").intValue = 2;
                    row.FindPropertyRelative("_allowDeepening").boolValue = false;
                    row.FindPropertyRelative("_deepeningLevelGain").intValue = 2;
                }
                if (GUILayout.Button("Wave順に並べ替え", GUILayout.Width(140f)))
                {
                    serialized.ApplyModifiedProperties();
                    SortWaveRewards();
                    return;
                }
            }

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("共通ドラフト数値", EditorStyles.boldLabel);
            SerializedProperty draft = serialized.FindProperty("_draft");
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTuningValue("通常候補数", draft.FindPropertyRelative("_defaultCandidateCount"), new Color(0.35f, 0.85f, 1f));
                DrawTuningValue("再抽選コスト", draft.FindPropertyRelative("_rerollBaseCost"), new Color(1f, 0.75f, 0.28f));
                DrawTuningValue("取得Lvウェイト", draft.FindPropertyRelative("_ownedLevelWeight"), new Color(0.52f, 1f, 0.58f));
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawTuningValue("相性ボーナス", draft.FindPropertyRelative("_synergyWeightBonus"), new Color(0.52f, 1f, 0.58f));
                DrawTuningValue("抑制倍率", draft.FindPropertyRelative("_suppressedWeightMultiplier"), new Color(1f, 0.58f, 0.42f));
                DrawTuningValue("最低ウェイト", draft.FindPropertyRelative("_minimumWeight"), new Color(0.92f, 0.58f, 1f));
            }
            serialized.ApplyModifiedProperties();
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
            using (new EditorGUILayout.HorizontalScope())
            {
                string filterLabel = _offerFilter.HasValue ? _offerFilter.Value.ToString() : "すべて";
                if (GUILayout.Button($"種別: {filterLabel}"))
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem(new GUIContent("すべて"), !_offerFilter.HasValue, () => { _offerFilter = null; Repaint(); });
                    foreach (UpgradeOfferType type in Enum.GetValues(typeof(UpgradeOfferType)))
                    {
                        UpgradeOfferType captured = type;
                        menu.AddItem(new GUIContent(type.ToString()), _offerFilter == type, () => { _offerFilter = captured; Repaint(); });
                    }
                    menu.ShowAsContext();
                }
                if (GUILayout.Button("未登録を追加", GUILayout.Width(90f)))
                    RegisterMissingUpgrades(pool);
            }
        }

        private void DrawUpgradeList(SO_UpgradePool pool)
        {
            List<UpgradeData> visible = pool.Upgrades
                .Where(item => item != null)
                .Where(item => !_offerFilter.HasValue || item.OfferType == _offerFilter.Value)
                .Where(item => string.IsNullOrWhiteSpace(_upgradeSearch) ||
                               item.DisplayName.IndexOf(_upgradeSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                               item.Id.IndexOf(_upgradeSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(item => item.OfferType)
                .ThenBy(item => item.DisplayName)
                .ToList();
            float totalWeight = visible.Sum(item => Mathf.Max(0.001f, item.DraftWeight));

            foreach (UpgradeData item in visible)
            {
                Color previous = GUI.backgroundColor;
                if (_selectedUpgrade == item) GUI.backgroundColor = new Color(0.55f, 0.8f, 1f);
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox, GUILayout.Height(34f)))
                {
                    if (GUILayout.Button(item.Icon != null ? item.Icon.texture : Texture2D.grayTexture, GUILayout.Width(28f), GUILayout.Height(28f)))
                        SelectUpgrade(item);
                    if (GUILayout.Button($"{item.DisplayName}\n{item.OfferType}  Lv.{item.MaxLevel}", EditorStyles.label, GUILayout.MinWidth(175f), GUILayout.Height(31f)))
                        SelectUpgrade(item);
                    GUILayout.FlexibleSpace();
                    float chance = totalWeight > 0f ? Mathf.Max(0.001f, item.DraftWeight) / totalWeight * 100f : 0f;
                    GUIStyle probabilityStyle = CreateMetricStyle(13, new Color(0.42f, 0.9f, 1f), TextAnchor.MiddleRight);
                    GUILayout.Label($"W {item.DraftWeight:0.##}\n{chance:0.0}%", probabilityStyle, GUILayout.Width(66f), GUILayout.Height(31f));
                }
                GUI.backgroundColor = previous;
            }
        }

        private void DrawUpgradeCreation(SO_UpgradePool pool)
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("新規追加", EditorStyles.boldLabel);
            _newUpgradeName = EditorGUILayout.TextField(_newUpgradeName);
            _newUpgradeType = (UpgradeOfferType)EditorGUILayout.EnumPopup(_newUpgradeType);
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

            SerializedObject serialized = new SerializedObject(upgrade);
            serialized.Update();
            DrawProperty(serialized, "Id", "ID");
            DrawProperty(serialized, "DisplayName", "表示名");
            DrawProperty(serialized, "Description", "基本説明");
            DrawProperty(serialized, "LevelDescriptions", "Lv別の詳細説明");
            DrawProperty(serialized, "LevelCardDescriptions", "Lv別のカード短文");
            EditorGUILayout.Space(5f);
            DrawProperty(serialized, "OfferType", "提示種別");
            DrawProperty(serialized, "MaxLevel", "最大Lv");
            DrawProperty(serialized, "DraftWeight", "基礎抽選ウェイト");
            DrawProperty(serialized, "SynergyTags", "相性タグ");
            DrawProperty(serialized, "SuppressedTags", "抑制タグ");
            DrawProperty(serialized, "Icon", "アイコン");
            DrawProperty(serialized, "Category", "カテゴリ");
            EditorGUILayout.Space(5f);
            DrawProperty(serialized, "Modifiers", "ステータス変化");
            DrawProperty(serialized, "GameplayValue", "ゲーム効果値");
            DrawProperty(serialized, "CombatPressureRuleId", "圧力ルールID");
            DrawProperty(serialized, "CombatPressureOutputType", "ビルド出力モデル");
            DrawProperty(serialized, "RequiresCollectibleFocus", "取得時にモデル選択");
            DrawProperty(serialized, "Cost", "基礎コスト");
            DrawProperty(serialized, "CostMagni", "コスト倍率");
            serialized.ApplyModifiedProperties();

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Lv.1～10 表示確認", EditorStyles.boldLabel);
            int max = Mathf.Min(10, Mathf.Max(1, upgrade.MaxLevel));
            for (int level = 1; level <= max; level++)
                EditorGUILayout.LabelField($"Lv.{level}", upgrade.GetCardText(level), EditorStyles.helpBox);
        }

        private void DrawEffectsEditor(UpgradeData upgrade)
        {
            EditorGUILayout.LabelField($"{upgrade.DisplayName} — 特殊効果", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "遺物・契約・進化の挙動を部品として組み合わせます。追加降下は発動元の出力モデルをそのまま使います。",
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
                "特殊枠を先に確率指定し、100%から引いた残りをベース枠で均等分配します。実行中はビルドの出現補正も反映した実効確率を確認できます。",
                MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawSummaryCard("特殊枠 合計", $"{specialTotal:0.##}%", specialTotal > 100f ? "100%を超えています" : "直接指定の合計", specialTotal > 100f ? new Color(1f, 0.35f, 0.3f) : new Color(1f, 0.75f, 0.28f));
                DrawSummaryCard("ベース枠 残り", $"{baseBudget:0.##}%", baseItems.arraySize > 0 ? $"1種あたり約 {baseBudget / baseItems.arraySize:0.##}%" : "ベースモデル未登録", new Color(0.35f, 0.85f, 1f));
            }
            if (specialTotal > 100f)
                EditorGUILayout.HelpBox("特殊枠の合計が100%を超えています。ベース枠が抽選されません。", MessageType.Error);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("ベース枠（残り確率を均等分配）", EditorStyles.boldLabel);
            DrawCollectibleArray(baseItems, false, table, baseBudget);
            if (GUILayout.Button("+ ベース枠", GUILayout.Width(120f))) baseItems.InsertArrayElementAtIndex(baseItems.arraySize);

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("特殊枠（個別確率）", EditorStyles.boldLabel);
            int remove = -1;
            for (int index = 0; index < specialItems.arraySize; index++)
            {
                SerializedProperty row = specialItems.GetArrayElementAtIndex(index);
                SerializedProperty data = row.FindPropertyRelative("Data");
                SerializedProperty chance = row.FindPropertyRelative("DropChancePercent");
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(data, GUIContent.none, GUILayout.MinWidth(250f));
                    EditorGUILayout.PropertyField(chance, new GUIContent("設定%"), GUILayout.Width(125f));
                    CollectibleData item = data.objectReferenceValue as CollectibleData;
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

        private static void DrawCollectibleArray(SerializedProperty items, bool unused, CollectibleTable table, float baseBudget)
        {
            int remove = -1;
            float each = items.arraySize > 0 ? baseBudget / items.arraySize : 0f;
            for (int index = 0; index < items.arraySize; index++)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    SerializedProperty itemProperty = items.GetArrayElementAtIndex(index);
                    EditorGUILayout.PropertyField(itemProperty, GUIContent.none, GUILayout.MinWidth(250f));
                    GUILayout.Label($"設定目安 {each:0.00}%", CreateMetricStyle(13, new Color(1f, 0.78f, 0.3f), TextAnchor.MiddleRight), GUILayout.Width(125f));
                    CollectibleData item = itemProperty.objectReferenceValue as CollectibleData;
                    if (item != null && Application.isPlaying)
                        GUILayout.Label($"実効 {table.GetEffectiveDropPercent(item.Type):0.00}%", CreateMetricStyle(13, new Color(0.42f, 0.9f, 1f), TextAnchor.MiddleRight), GUILayout.Width(105f));
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                }
            }
            if (remove >= 0) items.DeleteArrayElementAtIndex(remove);
        }

        private void DrawCombatPressure()
        {
            CombatPressureRuleSet ruleSet = _config.CombatPressureRuleSet;
            if (ruleSet == null)
            {
                EditorGUILayout.HelpBox("コンボ・状態異常ルールが未設定です。", MessageType.Error);
                return;
            }

            SerializedObject serialized = new SerializedObject(ruleSet);
            serialized.Update();
            SerializedProperty rules = serialized.FindProperty("_rules");
            EditorGUILayout.LabelField("コンボ・状態異常ビルド", new GUIStyle(EditorStyles.boldLabel) { fontSize = 17 });
            EditorGUILayout.HelpBox(
                "雑魚1体の同時状態数ではなく、毒・凍結の付与累計を進行値として扱います。閾値到達時の降下数、間隔、一時バフ、通常出現ウェイトをルールごとに調整します。",
                MessageType.Info);

            SerializedObject configSerialized = new SerializedObject(_config);
            configSerialized.Update();
            EditorGUILayout.PropertyField(
                configSerialized.FindProperty("_combatPressureProgression"),
                new GUIContent("Lv進行の共通数値"),
                true);
            configSerialized.ApplyModifiedProperties();
            _config.CombatPressureProgression.Apply();
            EditorGUILayout.Space(8f);

            int remove = -1;
            for (int index = 0; index < rules.arraySize; index++)
            {
                SerializedProperty rule = rules.GetArrayElementAtIndex(index);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PropertyField(rule.FindPropertyRelative("_displayName"), GUIContent.none, GUILayout.MinWidth(220f));
                        EditorGUILayout.PropertyField(rule.FindPropertyRelative("_enabled"), new GUIContent("有効"), GUILayout.Width(55f));
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("削除", GUILayout.Width(52f))) remove = index;
                    }
                    EditorGUILayout.PropertyField(rule, GUIContent.none, true);
                }
            }
            if (remove >= 0) rules.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("+ ルールを追加", GUILayout.Width(140f), GUILayout.Height(28f)))
                rules.InsertArrayElementAtIndex(rules.arraySize);
            serialized.ApplyModifiedProperties();
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

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("候補抽選シミュレーション", EditorStyles.boldLabel);
            _simulationWave = EditorGUILayout.IntField("クリアWave", Mathf.Max(1, _simulationWave));
            _simulationTrials = EditorGUILayout.IntSlider("試行回数", _simulationTrials, 100, 10000);
            if (GUILayout.Button("抽選を実行", GUILayout.Width(140f), GUILayout.Height(28f)))
                RunSimulation();

            if (_simulationResult != null)
            {
                foreach (KeyValuePair<UpgradeData, int> pair in _simulationResult.OrderByDescending(pair => pair.Value))
                {
                    float percent = pair.Value / (float)_simulationTrials * 100f;
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.LabelField(pair.Key.DisplayName, EditorStyles.boldLabel);
                        GUILayout.Label($"{pair.Value}回  {percent:0.00}%", CreateMetricStyle(14, new Color(0.42f, 0.9f, 1f), TextAnchor.MiddleRight), GUILayout.Width(150f));
                    }
                }
            }
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
            if (_config.CombatPressureRuleSet == null) messages.Add((MessageType.Error, "コンボ・状態異常ルールが未設定です。"));

            foreach (IGrouping<int, WaveRewardDefinition> duplicate in _config.WaveRewards.Where(item => item != null).GroupBy(item => item.ClearedWave).Where(group => group.Count() > 1))
                messages.Add((MessageType.Error, $"Wave {duplicate.Key} の報酬が重複しています。"));

            if (_config.UpgradePool != null)
            {
                foreach (IGrouping<string, UpgradeData> duplicate in _config.UpgradePool.Upgrades.Where(item => item != null).GroupBy(item => item.Id).Where(group => string.IsNullOrWhiteSpace(group.Key) || group.Count() > 1))
                    messages.Add((MessageType.Error, $"強化ID '{duplicate.Key}' が未設定または重複しています。"));
                foreach (UpgradeData upgrade in _config.UpgradePool.Upgrades.Where(item => item != null && item.DraftWeight <= 0f))
                    messages.Add((MessageType.Warning, $"{upgrade.DisplayName}: 基礎抽選ウェイトが0以下です。"));
            }

            if (_config.CombatPressureRuleSet != null)
                foreach (string message in _config.CombatPressureRuleSet.ValidateRules())
                    messages.Add((MessageType.Error, message));

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

        private void RunSimulation()
        {
            _simulationResult = new Dictionary<UpgradeData, int>();
            if (_config.UpgradePool == null) return;
            WaveRewardDefinition reward = _config.GetRewardForWave(_simulationWave);
            WaveRewardKind kind = reward != null ? reward.RewardKind : WaveRewardKind.Standard;
            List<UpgradeData> candidates = _config.UpgradePool.Upgrades.Where(item => item != null && MatchesReward(item, kind)).ToList();
            float total = candidates.Sum(item => Mathf.Max(_config.Draft.MinimumWeight, item.DraftWeight));
            var random = new System.Random(7919 + _simulationWave);
            for (int trial = 0; trial < _simulationTrials && total > 0f; trial++)
            {
                float pick = (float)random.NextDouble() * total;
                foreach (UpgradeData candidate in candidates)
                {
                    pick -= Mathf.Max(_config.Draft.MinimumWeight, candidate.DraftWeight);
                    if (pick > 0f) continue;
                    _simulationResult.TryGetValue(candidate, out int count);
                    _simulationResult[candidate] = count + 1;
                    break;
                }
            }
        }

        private static bool MatchesReward(UpgradeData item, WaveRewardKind kind)
        {
            return kind switch
            {
                WaveRewardKind.Contract => item.OfferType == UpgradeOfferType.Contract,
                WaveRewardKind.Evolution => item.OfferType == UpgradeOfferType.Evolution,
                WaveRewardKind.None => false,
                _ => item.OfferType == UpgradeOfferType.Standard ||
                     item.OfferType == UpgradeOfferType.CombatPressureRule ||
                     item.OfferType == UpgradeOfferType.Relic,
            };
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
            upgrade.OfferType = _newUpgradeType;
            upgrade.MaxLevel = _newUpgradeType == UpgradeOfferType.Standard || _newUpgradeType == UpgradeOfferType.CombatPressureRule ? 10 : 1;
            AssetDatabase.CreateAsset(upgrade, path);
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
                    row.FindPropertyRelative("_evolutionCandidateCount").intValue = snapshot.EvolutionCandidateCount;
                    row.FindPropertyRelative("_allowDeepening").boolValue = snapshot.AllowDeepening;
                    row.FindPropertyRelative("_deepeningLevelGain").intValue = snapshot.DeepeningLevelGain;
                }
                else
                {
                    row.FindPropertyRelative("_rewardKind").enumValueIndex = (int)WaveRewardKind.Standard;
                    row.FindPropertyRelative("_candidateCount").intValue = _config.Draft.DefaultCandidateCount;
                    row.FindPropertyRelative("_evolutionCandidateCount").intValue = 2;
                    row.FindPropertyRelative("_allowDeepening").boolValue = false;
                    row.FindPropertyRelative("_deepeningLevelGain").intValue = 2;
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

        private static void DrawTuningValue(string label, SerializedProperty property, Color accent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MinWidth(190f), GUILayout.Height(70f)))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
                GUIStyle style = new GUIStyle(EditorStyles.numberField)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                };
                SetTextColor(style, accent);
                if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = EditorGUILayout.IntField(property.intValue, style, GUILayout.Height(30f));
                else
                    property.floatValue = EditorGUILayout.FloatField(property.floatValue, style, GUILayout.Height(30f));
            }
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
