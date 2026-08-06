using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
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

    }

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
        }
        else
        {
            _createFieldContainer.Add(new Label("WARN ScriptableObject を継承したスクリプトを選択してください。"));
        }

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
        if(_tempInstance != null)
        {
            Destroy(_tempInstance);
        }
    }
}
