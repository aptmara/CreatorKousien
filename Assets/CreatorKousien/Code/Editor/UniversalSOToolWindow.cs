#if UNITY_EDITOR
using Game.Gameplay.Enemy.Boss;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using EnemyData = Game.Core.Enemy.EnemyDefinition;
public class UniversalSOToolWindow : EditorWindow
{
    private const string savePath = "Assets/CreatorKousien/Content/Features/";

    //==== 新規作成 ======
    private string _newSOName = "NewSO";
    private string _typeName = "";
    private ScrollView _createFieldContainer;
    private MonoScript _selectedScript;
    private ScriptableObject _tempInstance;

    //===== 既存項目編集 =====
    private ScriptableObject _editingTarget;
    private ScrollView _editFieldContainer;

    //===== Preview/DemoSpawn =====


    //===== Prefab編集 =====
    private GameObject _editPrefabRoot;
    private GameObject _selectedHierarchyGo;
    private ScrollView _prefabHierarchyContainer;
    private ScrollView _prefabComponentContainer;
    private Editor _prefabEditor;

    //===== BossGimmickTimeline =====
    private BossBattleFlowController targetController;
    private float zoom = 20.0f;
    private Vector2 scrollPos;


    [MenuItem("Editor/SO Editor")]
    public static void ShowWindow()
    {
        UniversalSOToolWindow wnd = GetWindow<UniversalSOToolWindow>();
        wnd.titleContent = new GUIContent("SO Tool");
    }

    public void CreateGUI()
    {
        VisualElement root = rootVisualElement;

        var tabView = new TabView();
        tabView.style.flexGrow = 1;
        root.Add(tabView);

        var EditTab = new Tab("Edit");
        SetupEditTab(EditTab);
        tabView.Add(EditTab);

        var AppendTab = new Tab("Append");
        SetupAppendTab(AppendTab);
        tabView.Add(AppendTab);


        var PrefabTab = new Tab("Prefab");
        SetupPrefabTab(PrefabTab);
        tabView.Add(PrefabTab);

        var BossTimeLineTab = new Tab("BossTimeLine");
        SetupBossTimeline(BossTimeLineTab);
        tabView.Add(BossTimeLineTab);
    }

    //======= SO 追加 ==========

    private void SetupAppendTab(VisualElement tab)
    {
        tab.style.paddingTop = 10;
        tab.style.flexGrow = 1;

        var nameField = new TextField("作成するSOの名前") { value = _newSOName };
        nameField.RegisterValueChangedCallback(evt => _newSOName = evt.newValue);
        tab.Add(nameField);

        var typeField = new TextField("作成するSOの種類(例:Enemy)") { value = _typeName };
        typeField.RegisterValueChangedCallback(evt => _typeName = evt.newValue);
        tab.Add(typeField);

        _createFieldContainer = new ScrollView(ScrollViewMode.Vertical);
        _createFieldContainer.style.flexGrow = 1;
        _createFieldContainer.style.marginTop = 10;
        _createFieldContainer.style.marginBottom = 10;

        var scriptField = new ObjectField("対象スクリプト")
        {
            objectType = typeof(MonoScript)
        };
        
        MonoScript defaultScript = FindMonoScriptOf<EnemyData>();
        if(defaultScript != null)
        {
            scriptField.value = defaultScript;
            OnScriptSelected(defaultScript);
        }

        scriptField.RegisterValueChangedCallback(evt =>
        {
            OnScriptSelected(evt.newValue as MonoScript);
        });

        tab.Add(scriptField);
        tab.Add(_createFieldContainer);
        
        // Save
        var createButton = new Button(CreateSO)
        {
            text = "Create or Load SO",
            style = { height = 30, marginTop = 10, marginBottom = 10 }
        };
        tab.Add(createButton);
    }

    private void OnScriptSelected(MonoScript script)
    {
        _selectedScript = script;
        _createFieldContainer.Clear();

        if(_tempInstance != null)
        {
            DestroyImmediate(_tempInstance);
        }

        if (script == null) return;

        System.Type scriptType = script.GetClass();

        if (scriptType != null && typeof(ScriptableObject).IsAssignableFrom(scriptType))
        {
            _tempInstance = CreateInstance(scriptType);

            SerializedObject serializedObject = new SerializedObject(_tempInstance);
            var inspectorElement = new InspectorElement(serializedObject);
            _createFieldContainer.Add(inspectorElement);


            if(typeof(EnemyData).IsAssignableFrom(scriptType))
            {
                var demoSpawnButton = new Button(DemoSpawn)
                {
                    text = "Demo Spawn",
                    style = {height = 30,marginTop = 10, marginBottom = 10}
                };
            }
        }
        else
        {
            _createFieldContainer.Add(new Label("WARN ScriptableObject を継承したスクリプトを選択してください。"));
        }


    }

    private void DemoSpawn()
    {

    }

    private void CreateSO()
    {
        if(_tempInstance == null)
        {
            Debug.LogError("[SOAppendEditor] 作成対象のスクリプトが正しく選択されていません");
        }

        if (string.IsNullOrEmpty(_typeName) || string.IsNullOrEmpty(_newSOName))
        {
            Debug.LogError("[SOAppendEditor] type or SOName is Null.");
            return;
        }

        string saveDirectory = savePath + _typeName + "/Data/";
        string saveAssetname = _typeName + "_" + _newSOName + ".asset";
        string saveFullPath = saveDirectory + saveAssetname;
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }
        ScriptableObject data = Instantiate(_tempInstance);
        AssetDatabase.CreateAsset(data, saveFullPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>新規SOを生成・保存しました:</color> {saveFullPath}");

        OnScriptSelected(_selectedScript);

    }

    //======= SO 編集 ==========

    private void SetupEditTab(VisualElement tab)
    {
        tab.style.paddingTop = 10;

        var targetField = new ObjectField("編集対象のSO")
        {
            objectType = typeof(ScriptableObject),
        };

        _editFieldContainer = new ScrollView(ScrollViewMode.Vertical);
        _editFieldContainer.style.flexGrow = 1;
        _editFieldContainer.style.marginTop = 10;
        _editFieldContainer.style.marginBottom = 10;

        targetField.RegisterValueChangedCallback(evt =>
        {
            _editingTarget = evt.newValue as ScriptableObject;
            showEditUI();
        });

        tab.Add(targetField);
        tab.Add(_editFieldContainer);

        var saveEditButton = new Button(SaveExistingSO)
        {
            text = "上書き保存",
            style = {height = 35,marginTop = 15,backgroundColor = new Color(0.2f,0.4f,0.7f)}
        };
        tab.Add(saveEditButton);
    }


    private void showEditUI()
    {
        _editFieldContainer.Clear();

        if (_editingTarget == null) return;

        SerializedObject serializedObject = new SerializedObject( _editingTarget );
        var inspectorElement = new InspectorElement( serializedObject );
        _editFieldContainer.Add(inspectorElement);
    }

    private void SaveExistingSO()
    {
        if (_editingTarget != null)
        {
            Debug.LogWarning("編集対象のSOを選択してください");
            return;
        }

        EditorUtility.SetDirty(_editingTarget);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=yellow>既存SOを上書き保存しました:</color> {AssetDatabase.GetAssetPath(_editingTarget)}");
    }

    //======= プレハブ編集 ==========
    private void SetupPrefabTab(VisualElement tab)
    {
        tab.style.paddingTop = 10;
        tab.style.flexGrow = 1;

        var prefabField = new ObjectField("編集対象プレハブ")
        {
            objectType = typeof(GameObject),
            allowSceneObjects = false,
        };
        tab.Add(prefabField);

        var btnGroup = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 5,marginBottom = 5 } };

        var openStageBtn = new Button(() =>
        {
            if (_editPrefabRoot == null) return;
            string path = AssetDatabase.GetAssetPath(_editPrefabRoot);
            PrefabStageUtility.OpenPrefab(path);
        })
        {
            text = "Sceneビュー(Prefab Stage)で直接調整)",
            style = { flexGrow = 1, height = 28 },
        };

        var savePrefabBtn = new Button(() =>
        {
            if(_editPrefabRoot == null) return;
            EditorUtility.SetDirty(_editPrefabRoot);
            AssetDatabase.SaveAssets();
            Debug.Log($"<color=yellow>プレハブを保存しました:</color> {AssetDatabase.GetAssetPath(_editPrefabRoot)}");
        })
        {
            text = "プレハブ上書き保存",
            style = {width = 140, height = 28,backgroundColor = new Color(0.2f,0.4f,0.7f)},
        };

        btnGroup.Add(openStageBtn);
        btnGroup.Add(savePrefabBtn);
        tab.Add(btnGroup);

        //=== Preview
        var previewLabel = new Label("3D Preview") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 5 } };
        tab.Add(previewLabel);

        var previewContainer = new IMGUIContainer(() =>
        {
            if(_editPrefabRoot != null)
            {
                if(_prefabEditor == null || _prefabEditor.target != _editPrefabRoot)
                {
                    if (_prefabEditor != null) DestroyImmediate(_prefabEditor);
                    _prefabEditor = Editor.CreateEditor(_editPrefabRoot);
                }

                Rect rect = GUILayoutUtility.GetRect(100, 180, GUILayout.ExpandWidth(true));
                _prefabEditor.OnInteractivePreviewGUI(rect, GUIStyle.none);
            }
            else
            {
                GUILayout.Box("プレハブが選択されていません", GUILayout.Height(100), GUILayout.ExpandWidth(true));
            }
        });

        previewContainer.style.height = 180;
        previewContainer.style.marginBottom = 10;
        tab.Add(previewContainer);

        var marginContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1, marginTop = 5 } };

        // Hierarchy Panel
        var leftPanel = new VisualElement { style = { width = 180, flexShrink = 0, marginRight = 5 } };
        leftPanel.Add(new Label("Hierarchy 構造") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });
        _prefabHierarchyContainer = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        leftPanel.Add(_prefabHierarchyContainer);

        // Inspector Panel
        var rightPanel = new VisualElement { style = { flexGrow = 1, marginRight = 5 } };
        rightPanel.Add(new Label("Component 構造") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });
        _prefabComponentContainer = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        rightPanel.Add(_prefabComponentContainer);

        marginContainer.Add(leftPanel);
        marginContainer.Add(rightPanel);
        tab.Add(marginContainer);

        prefabField.RegisterValueChangedCallback(evt =>
        {
            _editPrefabRoot = evt.newValue as GameObject;
            _selectedHierarchyGo = _editPrefabRoot;
            RebuildPrefabComponentUI();
            RebuildPrefabHierarchyUI();
        });
    }


    private void RebuildPrefabHierarchyUI()
    {
        _prefabHierarchyContainer.Clear();
        if (_editPrefabRoot == null) return;

        AddGameObjectToHierarchyTree(_editPrefabRoot.transform, 0);
    }

    private void AddGameObjectToHierarchyTree(Transform current,int indentLevel)
    {
        bool isSelected = (_selectedHierarchyGo == current.gameObject);

        var itemBtn = new Button(() =>
            {
                _selectedHierarchyGo = current.gameObject;
                RebuildPrefabHierarchyUI();
                RebuildPrefabComponentUI();
            })
        {
            text = (indentLevel > 0 ? new string(' ', indentLevel * 3) + "L " : "") + current.name,
            style =
            {
                unityTextAlign = TextAnchor.MiddleLeft,
                height = 24,
                marginBottom = 2,
                backgroundColor = isSelected ? new Color(0.2f,0.5f,0.8f) : new Color(0.22f,0.22f,0.22f),
            }
        };

        _prefabHierarchyContainer.Add(itemBtn);

        // 再帰呼び出しで子オブジェクトも反映
        foreach (Transform child in current)
        {
            AddGameObjectToHierarchyTree(child, indentLevel + 1);
        }
    }

    private void RebuildPrefabComponentUI()
    {
        _prefabComponentContainer.Clear();
        if (_selectedHierarchyGo == null) return;
        Component[] components = _selectedHierarchyGo.GetComponents<Component>();

        foreach (var comp in components)
        {
            if (comp == null) continue;

            var foldout = new Foldout
            {
                text = comp.GetType().Name,
                value = true,
            };
            foldout.style.marginBottom = 8;

            SerializedObject serializedComp = new SerializedObject(comp);
            SerializedProperty prop = serializedComp.GetIterator();
            bool enterChildren = true;

            while (prop.NextVisible(enterChildren))
            {
                enterChildren = false;
                if(prop.name != "m_Script")
                {
                    var field = new PropertyField(prop);
                    field.BindProperty(prop);
                    foldout.Add(field);
                }
            }

            foldout.Bind(serializedComp);
            _prefabComponentContainer.Add(foldout);
        }
    }


    //=========== BossTimeLine ===============
    private void SetupBossTimeline(VisualElement tab)
    {
        tab.style.paddingTop = 10;
        tab.style.flexGrow = 1;

        var controllerField = new ObjectField("対象ボスコントローラー")
        {
            objectType = typeof(BossBattleFlowController),
            allowSceneObjects = true,
            value = targetController,
        };

        controllerField.RegisterValueChangedCallback(evt =>
        {
            targetController = evt.newValue as BossBattleFlowController;
        });
        tab.Add(controllerField);

        var timelineContainer = new IMGUIContainer(() =>
        {
            if (targetController == null)
            {
                EditorGUILayout.HelpBox("シーン上のBossGimmickController（またはController）をセットしてください", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawTimeline();
        });

        timelineContainer.style.flexGrow = 1;
        timelineContainer.style.marginTop = 10;
        tab.Add(timelineContainer);
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        zoom = EditorGUILayout.Slider("Zoom Scale", zoom, 5.0f, 50.0f, GUILayout.Width(250));

        if(GUILayout.Button("+ Gimmick Slot 追加",EditorStyles.toolbarButton,GUILayout.Width(130)))
        {
            Undo.RecordObject(targetController, "Add Gimmick Slot");
            targetController.GimmickSlots.Add(new GimmickSlot());
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawTimeline()
    {
        if (targetController == null || targetController.GimmickSlots == null) return;

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        Rect trackRect = GUILayoutUtility.GetRect(2000,targetController.GimmickSlots.Count * 45 + 30);

        Handles.color = new Color(1.0f,1.0f,1.0f,0.15f);
        for (int i = 0; i < 200; i+=5)
        {
            float x = i + zoom;
            Handles.DrawLine(new Vector3(x, trackRect.y), new Vector3(x, trackRect.y));
            GUI.Label(new Rect(x + 2, trackRect.y, 40, 20), $"{i}s", EditorStyles.miniLabel);
        }
        /*
        for (int i = 0; i < targetController.GimmickSlots.Count; ++i)
        {
            var slot = targetController.GimmickSlots[i];
            if (slot.data == null) return;

            float y = trackRect.y + 25 + (i * 40);

            if (slot.data.ExecutionType == GimmickExecutionType.Timeline)
            {
                float x = slot.data.triggerTime * zoom;
                float width = slot.data.waitForCompletion ? 120.0f : 80.0f;
                Rect blockRect = new Rect(x, y, width, 32);

                GUI.backgroundColor = slot.data.editColor;
                string label = $"{slot.data.BossGimmickName}\n{(slot.data.waitForCompletion ? "[Wait]" : "[Parallel]")}";
                GUI.Box(blockRect, label,EditorStyles.miniButton);
                GUI.backgroundColor = Color.white;

                Event e = Event.current;
                if(e.type == EventType.MouseDrag && blockRect.Contains(e.mousePosition - e.delta))
                {
                    Undo.RecordObject(slot.data, "Move Keyframe Time");
                    slot.data.triggerTime = Mathf.Max(0, e.mousePosition.x / zoom);
                    Repaint();
                }
            }
            else
            {
                Rect blockRect = new Rect(10, y, 170, 32);
                GUI.backgroundColor = Color.gray;
                GUI.Box(blockRect, $"{slot.data.BossGimmickName}\n[Interval: {slot.data.minInterval}s]", EditorStyles.miniButton);
                GUI.backgroundColor = Color.white;
            }
        }*/

        EditorGUILayout.EndScrollView();
    }

    private MonoScript FindMonoScriptOf<T>() where T : ScriptableObject
    {
        string[] guids = AssetDatabase.FindAssets($"t:MonoScript {typeof(T).Name}");
        if(guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<MonoScript>(path);
        }

        return null;
    }

    private void OnDestroy()
    {
        if (_prefabEditor != null)
        {
            DestroyImmediate(_prefabEditor);
        }


        if(_tempInstance != null)
        {
            Destroy(_tempInstance);
        }
    }
}
#endif
