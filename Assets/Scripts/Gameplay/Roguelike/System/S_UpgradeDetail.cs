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



    public void SpawnDetail(UpgradeData upgrade, bool playAnime = true)
    {
        if (_spawnedInstance != null)
        {
            if (playAnime)
                PlaySpawnAnimation();
            ChangeDetail(upgrade);
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

        if (playAnime)
            PlaySpawnAnimation();
        ChangeDetail(upgrade);
    }

    /// <summary>
    /// 強化の詳細を描画する
    /// カード選択更新時に呼び出し
    /// </summary>
    /// <param name="upgrade"></param>
    public void ChangeDetail(UpgradeData upgrade)
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


        _descriptionText.text = upgrade.Description;
        _nameText.text = upgrade.DisplayName;

        int level = _upgradeRuntimeState.GetLevel(upgrade);
    

        if (level == upgrade.MaxLevel)
        {
            _levelText.text = "Level : Max";
            _costText.text = "Cost : None";
        }
        else
        {
            int cost = upgrade.GetCost(level);
            _levelText.text = $"Level : {level} / {upgrade.MaxLevel}";
            _costText.text = $"Cost : {cost}";
        }

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
