//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UpgradeDetail.cs
// brief  : 強化項目の詳細を描画
//
// auther : Takitani Shohei
// date   : 2026/07/15
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using Game.Data.Player;
using System.Collections;
using TMPro;
using UnityEngine;
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


        _nameText.text = upgrade.DisplayName;

        int level = _upgradeRuntimeState.GetLevel(upgrade);
        int nextLevel = Mathf.Clamp(level + Mathf.Max(1, levelGain), 1, upgrade.MaxLevel);
        _descriptionText.text = upgrade.GetTransitionText(level, levelGain);
    

        if (level >= upgrade.MaxLevel)
        {
            _levelText.text = "Level : Max";
            _costText.text = "取得済み";
        }
        else
        {
            _levelText.text = upgrade.OfferType == UpgradeOfferType.Relic ||
                              upgrade.OfferType == UpgradeOfferType.Contract ||
                              upgrade.OfferType == UpgradeOfferType.Evolution
                ? $"NEW  {upgrade.GetOfferLabel()}"
                : isDeepening
                    ? $"深化  Lv.{level} → {nextLevel}"
                    : $"{upgrade.GetOfferLabel()}  Lv.{level} → {nextLevel}";
            _costText.text = BuildSelectionNote(upgrade, isDeepening);
        }

        if (upgrade.OfferType == UpgradeOfferType.CombatPressureRule)
        {
            string outputName = CollectibleTable.GetDisplayName(upgrade.CombatPressureOutputType);
            _costText.text = $"{upgrade.GetOfferLabel()} / 降下: {outputName}固定";
        }

    }

    private void ConfigureTextLayout()
    {
        ConfigureText(_nameText, new Vector2(0f, 106f), new Vector2(372f, 60f), 50f, FontStyles.Bold);
        ConfigureText(_descriptionText, new Vector2(0f, 14f), new Vector2(378f, 126f), 38f, FontStyles.Bold);
        ConfigureText(_levelText, new Vector2(0f, -78f), new Vector2(372f, 42f), 34f, FontStyles.Bold);
        ConfigureText(_costText, new Vector2(0f, -122f), new Vector2(378f, 40f), 29f, FontStyles.Bold);
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

    private static string BuildSelectionNote(UpgradeData upgrade, bool isDeepening)
    {
        if (isDeepening) return "所持強化を一気に2レベル進める";
        return upgrade.OfferType switch
        {
            UpgradeOfferType.Relic => "取得したルールはこのラン中ずっと有効",
            UpgradeOfferType.Contract => "以降の抽選と生成規則を固定する",
            UpgradeOfferType.Evolution => "所持ビルドの接続規則を変える",
            _ => "無料・1つ選択",
        };
    }

    public void SpawnFocusTargetDetail(UpgradeData upgrade, CollectibleData target)
    {
        if (upgrade == null || target == null) return;

        SpawnDetail(upgrade);
        string targetName = CollectibleTable.GetDisplayName(target.Type);
        _nameText.text = targetName + "特化";
        _descriptionText.text =
            $"{upgrade.DisplayName}で発生する連鎖・爆発生成を、{targetName}へ集中する。\n取得直後に生成して効果をプレビュー。";
        _levelText.text = "このラン中の生成先";
        _costText.text = "決定すると強化を取得";
    }

    public void ChangeReactionSoldOut()
    {
        string str = "全部売り切れだよ";

        _descriptionText.text = str;

        _nameText.text = "";
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
