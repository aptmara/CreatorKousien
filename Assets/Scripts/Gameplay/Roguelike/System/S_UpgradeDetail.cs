//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeDetail.cs
// brief  : 強化項目の詳細を描画
//
// auther : Takitani Shohei
// date   : 2026/07/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using Game.Data.Player;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Game.Core.Roguelike;
using Game.Data.Collectibles;

public class S_UpgradeDetail : MonoBehaviour
{
    [Header("生成するプレハブ")]
    [SerializeField] private GameObject _detailPrefab;
    [Tooltip("生成先の親")]
    [SerializeField] private Transform _spawnParent;

    [Header("Prefab内のオブジェクト名(検索用)")]
    [SerializeField] private string _levelTextName = "LevelText";
    [SerializeField] private string _descriptionTextName = "DescriptionText";
    [SerializeField] private string _nameTextName = "NameText";
    [SerializeField] private string _costTextName = "CostText";


    [Header("強化データ")]
    [SerializeField] private SO_UpgradeRuntimeState _upgradeRuntimeState;


    [Header("吹き出し")]
    [Tooltip("拡縮するデータ")]
    [SerializeField] private string _speechBubbleName;
    private RectTransform _rectSpeechBubble;
    
    [SerializeField] private Vector2 _targetPosition = new Vector2(0.0f, 50.0f);
    [SerializeField] private float _animationDuration = 0.2f;
    [SerializeField] private AnimationCurve _animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine _animationCoroutine;

    private TextMeshProUGUI _levelText;
    private TextMeshProUGUI _descriptionText;
    private TextMeshProUGUI _nameText;
    private TextMeshProUGUI _costText;
    private GameObject _spawnedInstance;
    private readonly Image[] _nameUnderlines = new Image[2];


    private int _soldOutTxtOutputCount;
    private int _notEnouthMoneyTextCount;

    private UpgradeData _oldUpgrade;


    public void SpawnDetail(
        UpgradeData upgrade,
        int levelGain = 1,
        bool isDeepening = false,
        bool playAnime = true)
    {
        if (_oldUpgrade != null && upgrade.Id != _oldUpgrade.Id)
        {
            _notEnouthMoneyTextCount = 0;
            _soldOutTxtOutputCount = 0;

        }


        if (_spawnedInstance != null)
        {
            if (playAnime)
                PlaySpawnAnimation();
            ChangeDetail(upgrade, levelGain, isDeepening);
            return;
        }

        if(_detailPrefab == null || _spawnParent == null)
        {
            Debug.LogError("[S_UpgradeDetail] Prefabまたは生成元が未設定です。");
            return;
        }

        _spawnedInstance = Instantiate(_detailPrefab, _spawnParent);

        Transform speechBubble = _spawnedInstance.transform.Find(_speechBubbleName);

        if(speechBubble == null)
        {
            Debug.LogError($"[S_UpgradeDetail] '{_speechBubbleName}'が見つかりません");
            return;
        }
        _rectSpeechBubble = speechBubble.GetComponent<RectTransform>();

        foreach (var text in _spawnedInstance.GetComponentsInChildren<TextMeshProUGUI>())
        {
            if(text.gameObject.name == _levelTextName)
            {
                _levelText = text;
            }
            else if(text.gameObject.name == _descriptionTextName)
            {
                _descriptionText = text;
            }
            else if(text.gameObject.name == _nameTextName)
            {
                _nameText = text;
            }
            else if(text.gameObject.name == _costTextName)
            {
                _costText = text;
            }
        }

        if(_levelText == null || _descriptionText == null)
        {
            Debug.LogError($"[S_UpgradeDetail] Prefab内に'{_levelTextName}'、'{_descriptionTextName}'、'{_nameTextName}'または'{_costTextName}'の名前は存在していません");
        }

        ConfigureTextLayout();

        if (playAnime)
            PlaySpawnAnimation();
        ChangeDetail(upgrade, levelGain, isDeepening);


        _oldUpgrade = upgrade;
    }

    /// <summary>
    /// 強化の詳細を描画する
    /// カード選択更新時に呼び出し
    /// </summary>
    /// <param name="upgrade"></param>
    public void ChangeDetail(UpgradeData upgrade, int levelGain = 1, bool isDeepening = false)
    {
        if(upgrade == null)
        {
            Debug.LogWarning("[S_UpgradeDetail] upgradeのデータがnullです");
            return;
        }

        if(_descriptionText == null || _levelText == null ||
            _nameText == null || _costText == null)
        {
            Debug.LogWarning("[S_UpgradeDetail] まだSpawnDetailが呼ばれていません");
            return;
        }


        _nameText.text = FormatDisplayName(upgrade.DisplayName);
        RefreshNameUnderlines();

        int level = _upgradeRuntimeState.GetLevel(upgrade);
        int nextLevel = Mathf.Clamp(level + Mathf.Max(1, levelGain), 1, upgrade.MaxLevel);
        _descriptionText.text = FormatDescription(upgrade.GetTransitionText(level, levelGain));
    

        if (level >= upgrade.MaxLevel)
        {
            _levelText.text = "Level : Max";
            _costText.text = "取得済み";
        }
        else
        {
            _levelText.text = $"成長  Lv.{level} → {nextLevel}";
            _costText.text = BuildSelectionNote(upgrade, level);
        }
    }

    private void ConfigureTextLayout()
    {
        ConfigureText(
            _nameText,
            new Vector2(0f, 98f),
            new Vector2(372f, 76f),
            40f,
            FontStyles.Bold);
        ConfigureAutoSize(_nameText, 26f, 40f);
        ConfigureText(_descriptionText, new Vector2(0f, 0f), new Vector2(378f, 116f), 38f, FontStyles.Bold);
        ConfigureAutoSize(_descriptionText, 22f, 38f);
        ConfigureText(_levelText, new Vector2(0f, -78f), new Vector2(372f, 42f), 34f, FontStyles.Bold);
        ConfigureText(_costText, new Vector2(0f, -122f), new Vector2(378f, 40f), 29f, FontStyles.Bold);
    }

    private static void ConfigureAutoSize(TextMeshProUGUI text, float minimumSize, float maximumSize)
    {
        text.enableAutoSizing = true;
        text.fontSizeMin = minimumSize;
        text.fontSizeMax = maximumSize;
    }

    private static string FormatDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return displayName;

        string formattedName = displayName.Trim();
        if (formattedName.Length >= 2 &&
            formattedName[0] == '【' &&
            formattedName[formattedName.Length - 1] == '】')
        {
            formattedName = formattedName.Substring(1, formattedName.Length - 2).Trim();
        }

        if (formattedName.Length <= 7)
            return formattedName;

        int center = formattedName.Length / 2;
        int breakIndex = -1;
        int nearestDistance = int.MaxValue;

        TryUseBreakAfter(formattedName, "ごと", center, ref breakIndex, ref nearestDistance);
        TryUseBreakAfter(formattedName, "種類", center, ref breakIndex, ref nearestDistance);
        TryUseBreakAfter(formattedName, "時", center, ref breakIndex, ref nearestDistance);
        TryUseBreakAfter(formattedName, "分", center, ref breakIndex, ref nearestDistance);
        TryUseBreakBefore(formattedName, "生成", center, ref breakIndex, ref nearestDistance);

        if (breakIndex < 2 || breakIndex > formattedName.Length - 2)
            breakIndex = center;

        return formattedName.Insert(breakIndex, "\n");
    }

    private void RefreshNameUnderlines()
    {
        const float thickness = 4f;
        const float horizontalPadding = 8f;

        EnsureNameUnderlines();
        _nameText.ForceMeshUpdate();

        int visibleLineCount = string.IsNullOrEmpty(_nameText.text)
            ? 0
            : Mathf.Min(_nameText.textInfo.lineCount, _nameUnderlines.Length);

        for (int index = 0; index < _nameUnderlines.Length; index++)
        {
            Image underline = _nameUnderlines[index];
            bool isVisible = index < visibleLineCount;
            underline.gameObject.SetActive(isVisible);
            if (!isVisible)
                continue;

            TMP_LineInfo line = _nameText.textInfo.lineInfo[index];
            float lineWidth = line.lineExtents.max.x - line.lineExtents.min.x;
            float centerX = (line.lineExtents.min.x + line.lineExtents.max.x) * 0.5f;
            RectTransform rect = underline.rectTransform;
            rect.anchoredPosition = new Vector2(centerX, line.descender - thickness);
            rect.sizeDelta = new Vector2(lineWidth + horizontalPadding, thickness);
            underline.color = _nameText.color;
        }
    }

    private void EnsureNameUnderlines()
    {
        for (int index = 0; index < _nameUnderlines.Length; index++)
        {
            if (_nameUnderlines[index] != null)
                continue;

            var lineObject = new GameObject(
                $"NameUnderline_{index + 1}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            lineObject.layer = _nameText.gameObject.layer;
            lineObject.transform.SetParent(_nameText.rectTransform, false);

            Image underline = lineObject.GetComponent<Image>();
            underline.raycastTarget = false;
            RectTransform rect = underline.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            _nameUnderlines[index] = underline;
        }
    }

    private static void TryUseBreakAfter(
        string text,
        string separator,
        int center,
        ref int breakIndex,
        ref int nearestDistance)
    {
        int separatorIndex = text.IndexOf(separator, System.StringComparison.Ordinal);
        if (separatorIndex < 0)
            return;

        TryUseBreak(separatorIndex + separator.Length, text.Length, center, ref breakIndex, ref nearestDistance);
    }

    private static void TryUseBreakBefore(
        string text,
        string separator,
        int center,
        ref int breakIndex,
        ref int nearestDistance)
    {
        int separatorIndex = text.IndexOf(separator, System.StringComparison.Ordinal);
        if (separatorIndex < 0)
            return;

        TryUseBreak(separatorIndex, text.Length, center, ref breakIndex, ref nearestDistance);
    }

    private static void TryUseBreak(
        int candidate,
        int textLength,
        int center,
        ref int breakIndex,
        ref int nearestDistance)
    {
        if (candidate < 2 || candidate > textLength - 2)
            return;

        int distance = Mathf.Abs(candidate - center);
        if (distance >= nearestDistance)
            return;

        breakIndex = candidate;
        nearestDistance = distance;
    }

    private static string FormatDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return description;

        string[] sourceLines = description
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace("。", "。\n")
            .Replace("！", "！\n")
            .Replace("？", "？\n")
            .Split('\n');
        var formattedLines = new List<string>();

        foreach (string sourceLine in sourceLines)
        {
            string line = sourceLine.Trim();
            if (line.Length == 0)
                continue;

            int breakIndex = FindDescriptionBreak(line);
            if (breakIndex < 0)
            {
                formattedLines.Add(line);
                continue;
            }

            formattedLines.Add(line.Substring(0, breakIndex + 1).TrimEnd());
            formattedLines.Add(line.Substring(breakIndex + 1).TrimStart());
        }

        return string.Join("\n", formattedLines);
    }

    private static int FindDescriptionBreak(string line)
    {
        const int minimumLengthToBreak = 16;
        if (line.Length < minimumLengthToBreak)
            return -1;

        int center = line.Length / 2;
        int nearestIndex = -1;
        int nearestDistance = int.MaxValue;
        for (int index = 0; index < line.Length; index++)
        {
            if (line[index] != '、' || index < 4 || index > line.Length - 5)
                continue;

            int distance = Mathf.Abs(index - center);
            if (distance >= nearestDistance)
                continue;

            nearestIndex = index;
            nearestDistance = distance;
        }

        return nearestIndex;
    }

    private static void ConfigureText(
        TextMeshProUGUI text,
        Vector2 position,
        Vector2 size,
        float fontSize,
        FontStyles style)
    {
        if (text == null)
            return;

        RectTransform rect = text.rectTransform;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.enableAutoSizing = false;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Truncate;
    }

    private static string BuildSelectionNote(UpgradeData upgrade, int currentLevel)
    {
        return $"{upgrade.GetCost(currentLevel)} コイン";
    }

    public void ChangeReactionSoldOut()
    {
        string str = "全部売り切れだよ";

        _descriptionText.text = str;

        _nameText.text = "";
        RefreshNameUnderlines();
        _levelText.text = "";
        _costText.text = "";
    }

    public void ChangeReactionNotEnouthMoney()
    {
        _notEnouthMoneyTextCount++;
        int count = _notEnouthMoneyTextCount;

        string str;
        if (count >= 1 && count < 3) str = "お金が足りないよ";
        else if (count >= 4 && count < 7) str = "無料ではあげられないよ";
        else if (count >= 8 && count < 12) str = "話聞いてる？？";
        else str = "………………………………";

        _descriptionText.text = str;

        _nameText.text = "";
        RefreshNameUnderlines();
        _levelText.text = "";
        _costText.text = "";



    }

    private static readonly string[] MoveSpeedReactions = { "足が速くなるよ！", "風みたいに駆け抜けろ！", "俊足自慢だね" };
    private static readonly string[] BarrierReactions = { "これで守りも安心だね", "壊れにくくなるよ", "頼れる盾になるはずさ" };
    private static readonly string[] SpawnCountReactions = { "たくさん出てくるよ", "数が増えるのはお得だよ", "どんどん降ってくるよ" };
    private static readonly string[] ArmScaleReactions = { "手が大きくなるよ！", "リーチが伸びて便利だよ", "がっしりつかめるようになるね" };
    private static readonly string[] ConsumableReactions = { "これは甘くて美味しいよ", "お客さんに人気の一品だよ", "出会える確率が上がるよ" };
    private static readonly string[] DefaultReactions = { "いい選択だね", "気に入ってもらえて嬉しいよ" };

    private int _reactionRotationIndex;

    /// <summary>
    /// 強化IDに応じて店主の一言を切り替える(購入成功時に呼び出し。移動速度/バリア/出現数/腕拡大の判定に使用)
    /// </summary>
    public void ChangeReactionOnPurchase(UpgradeData upgrade)
    {
        if (upgrade == null) return;

        // ホバーが一度も発生しておらず詳細パネルが未生成の場合は何もしない
        if (_descriptionText == null || _nameText == null || _levelText == null || _costText == null)
            return;

        string[] pool = upgrade.Id switch
        {
            "2" => MoveSpeedReactions,
            "20" => BarrierReactions,
            "5" => SpawnCountReactions,
            "1" => ArmScaleReactions,
            _ => upgrade.Category == UpgradeCategory.Consumable
                ? ConsumableReactions
                : DefaultReactions,
        };

        _reactionRotationIndex = (_reactionRotationIndex + 1) % pool.Length;
        _descriptionText.text = pool[_reactionRotationIndex];
        _nameText.text = "";
        RefreshNameUnderlines();
        _levelText.text = "";
        _costText.text = "";
    }

    private void PlaySpawnAnimation()
    {
        if(_animationCoroutine != null)
        {
            StopCoroutine(_animationCoroutine);
        }

        _animationCoroutine = StartCoroutine(ScaleAnimation());
    }


    private IEnumerator ScaleAnimation()
    {
        _rectSpeechBubble.localScale = Vector3.zero;
        _rectSpeechBubble.anchoredPosition = Vector2.zero;
        // 前フレームの描画が残っている可能性があるため、処理を待つ
        yield return null;


        Vector2 startPos = Vector2.zero;
        Vector2 endPos = _targetPosition;

        float elapsed = 0.0f;

        while (elapsed < _animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / _animationDuration);
            t = _animationCurve.Evaluate(t);

            _rectSpeechBubble.localScale =
                Vector3.Lerp(Vector3.zero, Vector3.one, t);

            _rectSpeechBubble.anchoredPosition =
                Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        _rectSpeechBubble.localScale = Vector3.one;
        _rectSpeechBubble.anchoredPosition = endPos;

        _animationCoroutine = null;

    }


}
