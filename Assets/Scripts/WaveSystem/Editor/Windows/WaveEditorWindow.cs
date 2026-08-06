// ------------------------------------------------------------
// File		: WaveEditorWindow.cs
// Summary	: StageとWaveを1画面で行き来しながら編集する最強ウィンドウつくるぞ！
//
// Author	: [浅野 勇生]
// Created	: 2026-08-06
//
// Notes	:
// - 右ペインはCustomEditorをそのまま呼ぶため、Inspectorと表示が一致するようにします！
// - 左ペインでStageの構成をツリー表示し、クリックで編集対象を切り替えられるようにします！
// ------------------------------------------------------------
using UnityEditor;
using UnityEngine;

namespace Game.WaveSystem.Editor
{
    /// <summary>
    /// StageとWaveをまとめて編集するための最強ウィンドウクラス！
    /// </summary>
    public sealed class WaveEditorWindow : EditorWindow
    {
        /// <summary>
        /// ツリーの1行の高さ
        /// </summary>
        private const float RowHeight = 18f;

        /// <summary>
        /// ツリーのインデント幅
        /// </summary>
        private const float IndentWidth = 14f;

        /// <summary>
        /// ツリーの左ペインの最小幅
        /// </summary>
        private const float MinLeftWidth = 160f;

        /// <summary>
        /// 境界線の幅
        /// </summary>
        private const float SplitterWidth = 4f;


        [SerializeField]
        [Tooltip("編集対象のStage")]
        private StageDataSO stageData;

        [SerializeField]
        [Tooltip("右ペインで編集中のアセット")]
        private Object selectedTarget;

        [SerializeField]
        [Tooltip("左ペインの幅")]
        private float leftWidth = 220f;

        [SerializeField]
        [Tooltip("Poolごとの折りたたみ状態")]
        private bool[] poolExpanded = { true, true, true };

        // キャッシュされたEditor
        private UnityEditor.Editor cachedEditor;

        // ツリーのスクロール位置
        private Vector2 treeScroll;

        // インスペクターのスクロール位置
        private Vector2 inspectorScroll;

        // 境界線をドラッグ中かどうか
        private bool isDraggingSplitter;


        /// <summary>
        /// WaveEditorWindowを開く
        /// </summary>
        [MenuItem("Window/Wave System/Wave Editor")]
        private static void Open()
        {
            WaveEditorWindow window = GetWindow<WaveEditorWindow>();

            window.titleContent = new GUIContent("Wave Editor");
            window.minSize = new Vector2(720f, 400f);

            window.Show();
        }


        /// <summary>
        /// ウィンドウが有効化されたときに呼ばれる
        /// </summary>
        private void OnEnable()
        {
            if (stageData == null)
            {
                stageData = Selection.activeObject as StageDataSO;
            }

            if (selectedTarget == null)
            {
                selectedTarget = stageData;
            }
        }


        /// <summary>
        /// ウィンドウが無効化されたときに呼ばれる
        /// </summary>
        private void OnDisable()
        {
            // CreateCachedEditorで作成したEditorは、明示的にDestroyする
            if (cachedEditor != null)
            {
                DestroyImmediate(cachedEditor);
                cachedEditor = null;
            }
        }


        /// <summary>
        /// 一定間隔で再描画し、検証結果とタイムラインを最新に保つ
        /// </summary>
        private void OnInspectorUpdate()
        {
            Repaint();
        }


        /// <summary>
        /// ウィンドウを描画
        /// </summary>
        private void OnGUI()
        {
            DrawToolbar();

            EditorGUILayout.BeginHorizontal();

            DrawTreePane();
            DrawSplitter();
            DrawInspectorPane();

            EditorGUILayout.EndHorizontal();
        }


        /// <summary>
        /// 上部のツールバーを描画します
        /// </summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            EditorGUILayout.LabelField("Stage", EditorStyles.miniLabel, GUILayout.Width(38f));

            // StageDataSOをObjectFieldで表示する
            StageDataSO nextStage = (StageDataSO)EditorGUILayout.ObjectField(stageData, typeof(StageDataSO), false, GUILayout.Width(220f));

            // Stageが変更された場合は、SetStageを呼び出して右ペインの編集対象を切り替える
            if (nextStage != stageData)
            {
                SetStage(nextStage);
            }

            if (GUILayout.Button("Stage一覧 ▼", EditorStyles.toolbarDropDown, GUILayout.Width(100f)))
            {
                ShowStageMenu();
            }

            GUILayout.FlexibleSpace();

            using (new EditorGUI.DisabledScope(selectedTarget == null))
            {
                if (GUILayout.Button("Projectで表示", EditorStyles.toolbarButton, GUILayout.Width(100f)))
                {
                    // 選択中のアセットをProjectビューで表示する
                    EditorGUIUtility.PingObject(selectedTarget);
                }
            }

            EditorGUILayout.EndHorizontal();
        }


        /// <summary>
        /// プロジェクト内のStageDataSOを検索して、メニューとして表示します
        /// </summary>
        private void ShowStageMenu()
        {
            GenericMenu menu = new GenericMenu();

            // メニューを開いた時だけ検索する
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(StageDataSO)}");

            if (guids.Length == 0)
            {
                menu.AddDisabledItem(new GUIContent("StageDataSOが見つかりません"));
                menu.ShowAsContext();

                return;
            }

            // StageDataSOを検索してメニューに追加する
            for (int i = 0; i < guids.Length; i++)
            {
                // GUIDからアセットのパスを取得する
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                // アセットのパスからStageDataSOをロードする
                StageDataSO stage = AssetDatabase.LoadAssetAtPath<StageDataSO>(path);

                if (stage == null)
                {
                    continue;
                }

                // メニューに追加する
                menu.AddItem(new GUIContent(stage.name), stage == stageData, () =>
                {
                    SetStage(stage);
                });
            }

            // メニューを表示する
            menu.ShowAsContext();
        }


        /// <summary>
        /// 編集対象のStageを設定します
        /// </summary>
        /// <param name="nextStage"></param>
        private void SetStage(StageDataSO nextStage)
        {
            stageData = nextStage;

            SetSelectedTarget(nextStage);
        }


        /// <summary>
        /// 右ペインで編集する対象を切り替えます
        /// </summary>
        /// <param name="target">編集する対象</param>
        private void SetSelectedTarget(Object target)
        {
            selectedTarget = target;

            GUI.FocusControl(null);
        }


        /// <summary>
        /// 左側のツリーペインを描画します
        /// </summary>
        private void DrawTreePane()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(leftWidth));

            treeScroll = EditorGUILayout.BeginScrollView(treeScroll);

            if (stageData == null)
            {
                EditorGUILayout.HelpBox("上のStage欄にStageDataSOを設定してください", MessageType.Info);
            }
            else
            {
                // Stageのツリーを描画する
                DrawTreeRow(stageData, stageData.name, 0);

                DrawPoolNode(0, "序盤WavePool", stageData.EarlyWavePool);
                DrawPoolNode(1, "中盤WavePool", stageData.MiddleWavePool);
                DrawPoolNode(2, "終盤WavePool", stageData.LateWavePool);

                DrawBossNode(stageData);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }


        /// <summary>
        /// 1つのWavePoolとその候補Waveを描画します
        /// </summary>
        /// <param name="poolIndex">Poolの番号(0: 序盤, 1: 中盤, 2: 終盤)</param>
        /// <param name="poolLabel">Poolのラベル</param>
        /// <param name="pool">描画対象のWavePoolデータ</param>
        private void DrawPoolNode(int poolIndex, string poolLabel, WavePoolData pool)
        {
            if (pool == null)
            {
                return;
            }

            // Poolの折りたたみ状態を描画する
            Rect headerRect = EditorGUILayout.GetControlRect(false, RowHeight);

            headerRect.x += IndentWidth;
            headerRect.width -= IndentWidth;

            // Poolのラベルと選択数を表示する
            poolExpanded[poolIndex] = EditorGUI.Foldout(headerRect, poolExpanded[poolIndex], $"{poolLabel} ({pool.SelectionCount}個を選択", true);

            if (!poolExpanded[poolIndex] || pool.Candidates == null)
            {
                return;
            }

            // 候補Waveのリストを描画する
            for (int i = 0; i < pool.Candidates.Count; i++)
            {
                WeightedWaveEntry entry = pool.Candidates[i];

                if (entry == null || entry.WaveData == null)
                {
                    continue;
                }

                string label = entry.IsSelectable ? entry.WaveData.name : $"{entry.WaveData.name} (無効)";

                DrawTreeRow(entry.WaveData, label, 2);
            }
        }


        /// <summary>
        ///  最終Waveを描画します
        /// </summary>
        /// <param name="stage">描画対象のStage</param>
        private void DrawBossNode(StageDataSO stage)
        {
            Rect headerRect = EditorGUILayout.GetControlRect(false, RowHeight);

            headerRect.x += IndentWidth;
            headerRect.width -= IndentWidth;

            EditorGUI.LabelField(headerRect, "最終Wave");

            // ボスWaveが設定されていない場合は、(未設定)と表示する
            if (stage.BossWave == null)
            {
                Rect emptyRect = EditorGUILayout.GetControlRect(false, RowHeight);

                emptyRect.x += IndentWidth * 2f;

                EditorGUI.LabelField(emptyRect, "(未設定)", EditorStyles.miniLabel);

                return;
            }

            DrawTreeRow(stage.BossWave, stage.BossWave.name, 2);
        }


        /// <summary>
        /// ツリーの1行を描画します
        /// </summary>
        /// <param name="target">その行が指すアセット</param>
        /// <param name="label">表示するテキスト</param>
        /// <param name="indentLevel">インデントの段数</param>
        private void DrawTreeRow(Object target, string label, int indentLevel)
        {
            Rect rowRect = EditorGUILayout.GetControlRect(false, RowHeight);

            if (target == selectedTarget)
            {
                EditorGUI.DrawRect(rowRect, WaveEditorStyles.SelectionColor);
            }

            Rect labelRect = new Rect(rowRect.x + indentLevel * IndentWidth, rowRect.y, rowRect.width - indentLevel * IndentWidth, rowRect.height);

            // レベル風のボタンにして、行全体をクリックできるように
            if (GUI.Button(labelRect, new GUIContent(label, label), EditorStyles.label))
            {
                SetSelectedTarget(target);
            }
        }


        /// <summary>
        /// 左右のペインを分ける境界線を描画します
        /// </summary>
        private void DrawSplitter()
        {
            Rect splitterRect = GUILayoutUtility.GetRect(SplitterWidth, SplitterWidth, GUILayout.Width(SplitterWidth), GUILayout.ExpandHeight(true));

            EditorGUI.DrawRect(splitterRect, WaveEditorStyles.SelectionColor);

            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            Event current = Event.current;

            if (current.type == EventType.MouseDown && splitterRect.Contains(current.mousePosition))
            {
                isDraggingSplitter = true;
                current.Use();
            }

            if (isDraggingSplitter)
            {
                leftWidth = Mathf.Clamp(current.mousePosition.x, MinLeftWidth, position.width * 0.6f);
                Repaint();
            }

            if (current.type == EventType.MouseUp)
            {
                isDraggingSplitter = false;
            }
        }


        /// <summary>
        /// 右側の編集ペインを描画します
        /// </summary>
        private void DrawInspectorPane()
        {
            EditorGUILayout.BeginVertical();

            if (selectedTarget == null)
            {
                EditorGUILayout.HelpBox("左の一覧から、編集したいStageやWaveを選択してください。", MessageType.Info);

                EditorGUILayout.EndVertical();

                return;
            }

            // 対象に対応するCustomEditorを取得する
            UnityEditor.Editor.CreateCachedEditor(selectedTarget, null, ref cachedEditor);

            inspectorScroll = EditorGUILayout.BeginScrollView(inspectorScroll);

            EditorGUILayout.LabelField(selectedTarget.name, EditorStyles.largeLabel);

            if (cachedEditor != null)
            {
                cachedEditor.OnInspectorGUI();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }
    }
}

