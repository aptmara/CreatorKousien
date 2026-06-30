//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// beief   : S_RoguelikeEffectApplier.cs
//
// auther : Shohei Takitani
// data   : 2026/06/30 - 作成(create)
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class S_RoguelikeEffectApplier : MonoBehaviour
{
    [Tooltip("ベースとなるキャンバス")]
    [SerializeField] private Canvas _canvas;

    [Header("見た目")]
    [Tooltip("背景")]
    [SerializeField] private Image _back;
    [Tooltip("テキスト")]
    [SerializeField] private TextMeshProUGUI _text;
    [Tooltip("画像を配列で取得")]
    [SerializeField] private Image[] _images;


    [Header("機能面")]
    [Tooltip("ボタン")]
    [SerializeField] private Button _button;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // キャンバスを有効化
        _canvas.gameObject.SetActive(true);

        // 背景
        Image back = Instantiate(_back, _canvas.transform);
        RectTransform rect = back.rectTransform;
        // 最大化
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;


        // グループを作成
        CreateGroup(new Vector2(   0.0f, 0.0f));
        CreateGroup(new Vector2( 400.0f, -50.0f));
        CreateGroup(new Vector2(-400.0f, -50.0f));


        // キャラクター描画
        CreateItem(_images[2], new Vector2(0.0f, -200.0f));
    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnButtonClick()
    {
        Debug.Log("クリック！");
    }


    private void CreateGroup(Vector2 basepos)
    {
        CreateItem(_images[0],
            new Vector2(basepos.x, basepos.y + 200));
        CreateItem(_images[1],
            new Vector2(basepos.x, basepos.y + 50));
        CreateItem("Hallo world",
            new Vector2(basepos.x, basepos.y + 75));
        CreateItem(
            new Vector2(basepos.x, basepos.y + 70));
    }

    private void CreateItem(Image img, Vector2 pos)
    {
        Image image = Instantiate(img, _canvas.transform);
        image.rectTransform.anchoredPosition = pos;
    }

    private void CreateItem(string message, Vector2 pos)
    {
        var text = Instantiate(_text, _canvas.transform);
        text.text = message;
        text.rectTransform.anchoredPosition = pos;
    }

    private void CreateItem(Vector2 pos)
    {
        Button btn = Instantiate(_button, _canvas.transform);
        RectTransform rect = btn.GetComponent<RectTransform>();
        rect.anchoredPosition = pos;

        btn.onClick.AddListener(OnButtonClick);
    }
}
