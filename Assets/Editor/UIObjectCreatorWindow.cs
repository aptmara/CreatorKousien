//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file : UIObjectCreatorWindow.cs
// brief : アイコンとテキストを組み合わせた専用UIオブジェクトを、エディタ上から簡単に生成するためのEditorWindowです。
// author : 山本郁也
// data 2026/05/13
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using TMPro; // TextMeshProUGUIを生成するために必要です。
using UnityEditor; // EditorWindowやMenuItemなど、Unityエディタ拡張機能を使うために必要です。
using UnityEngine; // GameObject、Canvas、Vector2など、Unity基本機能を使うために必要です。
using UnityEngine.EventSystems; // EventSystemを生成するために必要です。
using UnityEngine.UI; // Image、CanvasScaler、GraphicRaycasterなど、UI機能を使うために必要です。

/// <summary>
/// アイコンとテキストの配置関係を表す列挙型です。
/// </summary>
public enum UITextLayoutType
{
    /// <summary>
    /// アイコンを左、テキストを右に配置します。
    /// </summary>
    IconLeftTextRight,

    /// <summary>
    /// アイコンを上、テキストを下に配置します。
    /// </summary>
    IconTopTextBottom,

    /// <summary>
    /// アイコンを右、テキストを左に配置します。
    /// </summary>
    IconRightTextLeft,

    /// <summary>
    /// アイコンを下、テキストを上に配置します。
    /// </summary>
    IconBottomTextTop
}

/// <summary>
/// 専用UIオブジェクトをエディタ上から生成するためのEditorWindowです。
/// </summary>
public class UIObjectCreatorWindow : EditorWindow
{
    /// <summary>
    /// 作成先のCanvasです。
    /// </summary>
    private Canvas targetCanvas = null;

    /// <summary>
    /// 生成するUIオブジェクトの名前です。
    /// </summary>
    private string objectName = "CustomUIObject";

    /// <summary>
    /// 名前テキストに入れる初期文字列です。
    /// </summary>
    private string titleText = "Name";

    /// <summary>
    /// 説明テキストに入れる初期文字列です。
    /// </summary>
    private string descriptionText = "Description";

    /// <summary>
    /// アイコン用Imageに設定するSpriteです。
    /// </summary>
    private Sprite iconSprite = null;

    /// <summary>
    /// フレーム用Imageに設定するSpriteです。
    /// </summary>
    private Sprite frameSprite = null;

    /// <summary>
    /// ルートUIオブジェクトのサイズです。
    /// </summary>
    private Vector2 rootSize = new Vector2(300f, 120f);

    /// <summary>
    /// アイコンのサイズです。
    /// </summary>
    private Vector2 iconSize = new Vector2(80f, 80f);

    /// <summary>
    /// 名前テキストのサイズです。
    /// </summary>
    private Vector2 nameTextSize = new Vector2(180f, 35f);

    /// <summary>
    /// 説明テキストのサイズです。
    /// </summary>
    private Vector2 descriptionTextSize = new Vector2(180f, 55f);

    /// <summary>
    /// アイコンとテキストの配置関係です。
    /// </summary>
    private UITextLayoutType textLayoutType = UITextLayoutType.IconLeftTextRight;

    /// <summary>
    /// フレームを生成するかどうかです。
    /// </summary>
    private bool useFrame = true;

    /// <summary>
    /// アイコンを生成するかどうかです。
    /// </summary>
    private bool useIcon = true;

    /// <summary>
    /// 名前テキストを生成するかどうかです。
    /// </summary>
    private bool useNameText = true;

    /// <summary>
    /// 説明テキストを生成するかどうかです。
    /// </summary>
    private bool useDescriptionText = true;

    /// <summary>
    /// 生成するCustomUIElementに設定するIDです。
    /// </summary>
    private int customElementId = 0;

    /// <summary>
    /// Unity上部メニューからこのウィンドウを開くための関数です。
    /// </summary>
    [MenuItem("Tools/UI/Custom UI Object Creator")]
    public static void Open()
    {
        // UIObjectCreatorWindowを開き、ウィンドウ名を設定します。
        GetWindow<UIObjectCreatorWindow>("UI Object Creator");
    }

    /// <summary>
    /// EditorWindowのGUIを描画します。
    /// </summary>
    private void OnGUI()
    {
        // このウィンドウが何をするためのものかを表示します。
        EditorGUILayout.LabelField("Create Custom UI Object", EditorStyles.boldLabel);

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // 作成先Canvasをエディタ上から指定できるようにします。
        targetCanvas = (Canvas)EditorGUILayout.ObjectField(
            "Target Canvas",
            targetCanvas,
            typeof(Canvas),
            true
        );

        // 生成するルートオブジェクト名を入力できるようにします。
        objectName = EditorGUILayout.TextField("Object Name", objectName);

        // CustomUIElementに設定するIDを入力できるようにします。
        customElementId = EditorGUILayout.IntField("Element ID", customElementId);

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // Sprite設定欄の見出しを表示します。
        EditorGUILayout.LabelField("Sprites", EditorStyles.boldLabel);

        // アイコンに使うSpriteを指定できるようにします。
        iconSprite = (Sprite)EditorGUILayout.ObjectField(
            "Icon Sprite",
            iconSprite,
            typeof(Sprite),
            false
        );

        // フレームに使うSpriteを指定できるようにします。
        frameSprite = (Sprite)EditorGUILayout.ObjectField(
            "Frame Sprite",
            frameSprite,
            typeof(Sprite),
            false
        );

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // テキスト設定欄の見出しを表示します。
        EditorGUILayout.LabelField("Texts", EditorStyles.boldLabel);

        // 名前テキストの初期文字列を入力できるようにします。
        titleText = EditorGUILayout.TextField("Name Text", titleText);

        // 説明テキストの初期文字列を入力できるようにします。
        descriptionText = EditorGUILayout.TextField("Description Text", descriptionText);

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // サイズ設定欄の見出しを表示します。
        EditorGUILayout.LabelField("Sizes", EditorStyles.boldLabel);

        // ルートUIオブジェクトのサイズを入力できるようにします。
        rootSize = EditorGUILayout.Vector2Field("Root Size", rootSize);

        // アイコンサイズを入力できるようにします。
        iconSize = EditorGUILayout.Vector2Field("Icon Size", iconSize);

        // 名前テキストサイズを入力できるようにします。
        nameTextSize = EditorGUILayout.Vector2Field("Name Text Size", nameTextSize);

        // 説明テキストサイズを入力できるようにします。
        descriptionTextSize = EditorGUILayout.Vector2Field("Description Text Size", descriptionTextSize);

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // レイアウト設定欄の見出しを表示します。
        EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);

        // アイコンとテキストの配置関係を選択できるようにします。
        textLayoutType = (UITextLayoutType)EditorGUILayout.EnumPopup(
            "Text Layout",
            textLayoutType
        );

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // 生成パーツ設定欄の見出しを表示します。
        EditorGUILayout.LabelField("Create Parts", EditorStyles.boldLabel);

        // フレームを生成するかどうかを選択できるようにします。
        useFrame = EditorGUILayout.Toggle("Use Frame", useFrame);

        // アイコンを生成するかどうかを選択できるようにします。
        useIcon = EditorGUILayout.Toggle("Use Icon", useIcon);

        // 名前テキストを生成するかどうかを選択できるようにします。
        useNameText = EditorGUILayout.Toggle("Use Name Text", useNameText);

        // 説明テキストを生成するかどうかを選択できるようにします。
        useDescriptionText = EditorGUILayout.Toggle("Use Description Text", useDescriptionText);

        // 見た目を整理するために余白を入れます。
        EditorGUILayout.Space();

        // Createボタンを表示し、押されたらUIオブジェクトを生成します。
        if (GUILayout.Button("Create"))
        {
            // UIオブジェクト生成処理を実行します。
            CreateUIObject();
        }
    }

    /// <summary>
    /// エディタで指定した設定を元に、専用UIオブジェクトを生成します。
    /// </summary>
    private void CreateUIObject()
    {
        // 作成先Canvasとして、エディタ上で指定されたCanvasを一旦使います。
        Canvas canvas = targetCanvas;

        // 作成先Canvasが指定されていない場合、シーン内からCanvasを探す処理に入ります。
        if (canvas == null)
        {
            // シーン内に存在する最初のCanvasを探します。
            canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();

            // 既存Canvasが見つからなかった場合、新しくCanvasを作成します。
            if (canvas == null)
            {
                // 新しいCanvasを生成し、その参照を受け取ります。
                canvas = CreateCanvas();
            }
        }

        // UI入力イベントに必要なEventSystemがない場合は作成します。
        EnsureEventSystemExists();

        // 専用UIのルートGameObjectを作成します。
        GameObject root = new GameObject(objectName, typeof(RectTransform));

        // Undo操作で生成を取り消せるようにします。
        Undo.RegisterCreatedObjectUndo(root, "Create Custom UI Object");

        // ルートUIオブジェクトをCanvasの子にします。
        root.transform.SetParent(canvas.transform, false);

        // ルートUIにCustomUIElementを追加します。
        CustomUIElement element = root.AddComponent<CustomUIElement>();

        // CustomUIElementにIDを設定します。
        element.SetId(customElementId);

        // ルートUIのRectTransformを取得します。
        RectTransform rootRect = root.GetComponent<RectTransform>();

        // ルートUIのサイズを設定します。
        rootRect.sizeDelta = rootSize;

        // ルートUIのアンカー最小値を中央にします。
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);

        // ルートUIのアンカー最大値を中央にします。
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);

        // ルートUIの基準点を中央にします。
        rootRect.pivot = new Vector2(0.5f, 0.5f);

        // ルートUIをCanvas中央に配置します。
        rootRect.anchoredPosition = Vector2.zero;

        // 選択されたレイアウトに応じて、各パーツの位置を計算します。
        CalcLayoutPositions(
            textLayoutType,
            out Vector2 iconPosition,
            out Vector2 namePosition,
            out Vector2 descriptionPosition
        );

        // フレームを使う設定なら、フレームImageを生成します。
        if (useFrame)
        {
            // フレームImageをルート全面に配置します。
            CreateImage(
                root.transform,
                "Frame",
                frameSprite,
                Vector2.zero,
                rootSize,
                true
            );
        }

        // アイコンを使う設定なら、アイコンImageを生成します。
        if (useIcon)
        {
            // アイコンImageをレイアウト計算された位置に配置します。
            CreateImage(
                root.transform,
                "Icon",
                iconSprite,
                iconPosition,
                iconSize,
                false
            );
        }

        // 名前テキストを使う設定なら、名前TextMeshProUGUIを生成します。
        if (useNameText)
        {
            // 名前テキストをレイアウト計算された位置に配置します。
            CreateText(
                root.transform,
                "NameText",
                titleText,
                namePosition,
                nameTextSize,
                24f
            );
        }

        // 説明テキストを使う設定なら、説明TextMeshProUGUIを生成します。
        if (useDescriptionText)
        {
            // 説明テキストをレイアウト計算された位置に配置します。
            CreateText(
                root.transform,
                "DescriptionText",
                descriptionText,
                descriptionPosition,
                descriptionTextSize,
                16f
            );
        }

        // 生成したUIオブジェクトをHierarchy上で選択状態にします。
        Selection.activeGameObject = root;
    }

    /// <summary>
    /// シーン内にCanvasが存在しない場合に、新しいCanvasを生成します。
    /// </summary>
    /// <returns>生成したCanvasを返します。</returns>
    private Canvas CreateCanvas()
    {
        // Canvas用GameObjectを作成し、必要なUIコンポーネントも同時に追加します。
        GameObject canvasObject = new GameObject(
            "Canvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        // Undo操作でCanvas生成を取り消せるようにします。
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Canvas");

        // Canvasコンポーネントを取得します。
        Canvas canvas = canvasObject.GetComponent<Canvas>();

        // Canvasを画面全体に直接描画するOverlayモードにします。
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // CanvasScalerを取得します。
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();

        // 画面サイズに応じてUIをスケールさせる設定にします。
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 基準解像度を1920x1080にします。
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 作成したCanvasを返します。
        return canvas;
    }

    /// <summary>
    /// UI入力イベントに必要なEventSystemがシーン内に存在するか確認し、なければ作成します。
    /// </summary>
    private void EnsureEventSystemExists()
    {
        // シーン内のEventSystemを探します。
        EventSystem eventSystem = UnityEngine.Object.FindFirstObjectByType<EventSystem>();

        // EventSystemがすでに存在する場合は、何もせず終了します。
        if (eventSystem != null)
        {
            // 既存のEventSystemで十分なので処理を抜けます。
            return;
        }

        // EventSystem用GameObjectを作成します。
        GameObject eventSystemObject = new GameObject(
            "EventSystem",
            typeof(EventSystem),
            typeof(StandaloneInputModule)
        );

        // Undo操作でEventSystem生成を取り消せるようにします。
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
    }

    /// <summary>
    /// 指定されたレイアウト種類に応じて、アイコン、名前テキスト、説明テキストの位置を計算します。
    /// </summary>
    /// <param name="layoutType">選択されたレイアウト種類です。</param>
    /// <param name="iconPosition">計算後のアイコン位置です。</param>
    /// <param name="namePosition">計算後の名前テキスト位置です。</param>
    /// <param name="descriptionPosition">計算後の説明テキスト位置です。</param>
    private void CalcLayoutPositions(
        UITextLayoutType layoutType,
        out Vector2 iconPosition,
        out Vector2 namePosition,
        out Vector2 descriptionPosition
    )
    {
        // アイコンとテキストの間隔として使う値です。
        float spacing = 20f;

        // アイコンを左、テキストを右に配置する場合の位置を計算します。
        if (layoutType == UITextLayoutType.IconLeftTextRight)
        {
            // アイコンを左側に配置します。
            iconPosition = new Vector2(-rootSize.x * 0.25f, 0f);

            // 名前テキストを右上側に配置します。
            namePosition = new Vector2(iconSize.x * 0.5f + spacing, 20f);

            // 説明テキストを右下側に配置します。
            descriptionPosition = new Vector2(iconSize.x * 0.5f + spacing, -25f);

            // このレイアウトの計算が完了したので処理を抜けます。
            return;
        }

        // アイコンを上、テキストを下に配置する場合の位置を計算します。
        if (layoutType == UITextLayoutType.IconTopTextBottom)
        {
            // アイコンを上側に配置します。
            iconPosition = new Vector2(0f, rootSize.y * 0.2f);

            // 名前テキストをアイコンの下に配置します。
            namePosition = new Vector2(0f, -iconSize.y * 0.35f - spacing);

            // 説明テキストを名前テキストの下に配置します。
            descriptionPosition = new Vector2(0f, -iconSize.y * 0.35f - spacing - 40f);

            // このレイアウトの計算が完了したので処理を抜けます。
            return;
        }

        // アイコンを右、テキストを左に配置する場合の位置を計算します。
        if (layoutType == UITextLayoutType.IconRightTextLeft)
        {
            // アイコンを右側に配置します。
            iconPosition = new Vector2(rootSize.x * 0.25f, 0f);

            // 名前テキストを左上側に配置します。
            namePosition = new Vector2(-iconSize.x * 0.5f - spacing, 20f);

            // 説明テキストを左下側に配置します。
            descriptionPosition = new Vector2(-iconSize.x * 0.5f - spacing, -25f);

            // このレイアウトの計算が完了したので処理を抜けます。
            return;
        }

        // アイコンを下、テキストを上に配置する場合の位置を計算します。
        if (layoutType == UITextLayoutType.IconBottomTextTop)
        {
            // アイコンを下側に配置します。
            iconPosition = new Vector2(0f, -rootSize.y * 0.2f);

            // 名前テキストを上側に配置します。
            namePosition = new Vector2(0f, iconSize.y * 0.35f + spacing);

            // 説明テキストを名前テキストの下側に配置します。
            descriptionPosition = new Vector2(0f, iconSize.y * 0.35f + spacing - 40f);

            // このレイアウトの計算が完了したので処理を抜けます。
            return;
        }

        // 想定外の値が来た場合は、左アイコン右テキストの初期配置にします。
        iconPosition = new Vector2(-rootSize.x * 0.25f, 0f);

        // 想定外の値が来た場合の名前テキスト位置です。
        namePosition = new Vector2(iconSize.x * 0.5f + spacing, 20f);

        // 想定外の値が来た場合の説明テキスト位置です。
        descriptionPosition = new Vector2(iconSize.x * 0.5f + spacing, -25f);
    }

    /// <summary>
    /// Imageを持つUI子オブジェクトを作成します。
    /// </summary>
    /// <param name="parent">作成したImageオブジェクトの親です。</param>
    /// <param name="name">作成するGameObject名です。</param>
    /// <param name="sprite">Imageに設定するSpriteです。</param>
    /// <param name="position">RectTransformのanchoredPositionです。</param>
    /// <param name="size">RectTransformのsizeDeltaです。</param>
    /// <param name="raycastTarget">UI入力判定の対象にするかどうかです。</param>
    /// <returns>作成したImageコンポーネントを返します。</returns>
    private Image CreateImage(
        Transform parent,
        string name,
        Sprite sprite,
        Vector2 position,
        Vector2 size,
        bool raycastTarget
    )
    {
        // Image用GameObjectを作成し、RectTransformとImageを追加します。
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));

        // 作成したImageオブジェクトを指定された親の子にします。
        imageObject.transform.SetParent(parent, false);

        // ImageオブジェクトのRectTransformを取得します。
        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();

        // Imageオブジェクトのサイズを設定します。
        rectTransform.sizeDelta = size;

        // Imageオブジェクトの位置を設定します。
        rectTransform.anchoredPosition = position;

        // Imageコンポーネントを取得します。
        Image image = imageObject.GetComponent<Image>();

        // ImageにSpriteを設定します。
        image.sprite = sprite;

        // このImageをクリックやHover判定の対象にするかどうかを設定します。
        image.raycastTarget = raycastTarget;

        // 作成したImageコンポーネントを返します。
        return image;
    }

    /// <summary>
    /// TextMeshProUGUIを持つUI子オブジェクトを作成します。
    /// </summary>
    /// <param name="parent">作成したTextオブジェクトの親です。</param>
    /// <param name="name">作成するGameObject名です。</param>
    /// <param name="text">初期表示する文字列です。</param>
    /// <param name="position">RectTransformのanchoredPositionです。</param>
    /// <param name="size">RectTransformのsizeDeltaです。</param>
    /// <param name="fontSize">TextMeshProUGUIのフォントサイズです。</param>
    /// <returns>作成したTextMeshProUGUIコンポーネントを返します。</returns>
    private TextMeshProUGUI CreateText(
        Transform parent,
        string name,
        string text,
        Vector2 position,
        Vector2 size,
        float fontSize
    )
    {
        // Text用GameObjectを作成し、RectTransformとTextMeshProUGUIを追加します。
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));

        // 作成したTextオブジェクトを指定された親の子にします。
        textObject.transform.SetParent(parent, false);

        // TextオブジェクトのRectTransformを取得します。
        RectTransform rectTransform = textObject.GetComponent<RectTransform>();

        // Textオブジェクトのサイズを設定します。
        rectTransform.sizeDelta = size;

        // Textオブジェクトの位置を設定します。
        rectTransform.anchoredPosition = position;

        // TextMeshProUGUIコンポーネントを取得します。
        TextMeshProUGUI textMeshPro = textObject.GetComponent<TextMeshProUGUI>();

        // 表示する文字列を設定します。
        textMeshPro.text = text;

        // フォントサイズを設定します。
        textMeshPro.fontSize = fontSize;

        // テキストの配置を左寄せにします。
        textMeshPro.alignment = TextAlignmentOptions.Left;

        // テキスト自体はクリック判定の対象にしないようにします。
        textMeshPro.raycastTarget = false;

        // 作成したTextMeshProUGUIコンポーネントを返します。
        return textMeshPro;
    }
}
