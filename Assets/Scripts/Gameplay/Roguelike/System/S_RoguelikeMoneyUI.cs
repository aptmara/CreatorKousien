//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_RoguelikeMoneyUI.cs
// brief  : ローグライクシーンでのお金表示
//          お金の実データは保持しない
//
// auther : Takitani Shohei
// date   : 2026/07/14 - begin.
// update : 2026/09/07 - コインアイコン・カウントアニメーションを追加 - 浅野
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class S_RoguelikeMoneyUI : MonoBehaviour
{
    [Header("UIで表示するデータ")]
    [Tooltip("生成する実データ")]
    [SerializeField] private GameObject _moneyPrefab;
    [Tooltip("生成先の親")]
    [SerializeField] private Transform _spawnParent;

    [Header("Prefab内のオブジェクト名(検索用、未設定時は自動検出)")]
    [SerializeField] private string _coinIconName = "CoinIcon";

    [Header("カウントアニメーション")]
    [SerializeField] private float _countDuration = 0.35f;
    [SerializeField] private AnimationCurve _countCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float _bounceScale = 1.18f;
    [SerializeField] private float _shakeDistance = 8f;

    private TextMeshProUGUI _text;
    private Image _coinIcon;
    private RectTransform _rootRect;
    private GameObject _spawnedInstance;
    private Coroutine _countCoroutine;
    private Coroutine _punchCoroutine;
    private int _displayedMoney;


    //____________________________________
    // public funtion

    /// <summary>
    /// UIを丸ごと生成
    /// </summary>
    /// <param name="money">所持金</param>
    public void SpawnMoneyUI(int money)
    {
        if(_spawnedInstance != null)
        {
            // 既に生成済みであれば処理を更新するのみ
            ChangeMoneyUI(money);
            return;
        }

        if(_moneyPrefab == null || _spawnParent == null)
        {
            Debug.LogError("[S_RoguelikeMoneyUI] Prefabまたは生成先が未設定です");
            return;
        }

        _spawnedInstance = Instantiate(_moneyPrefab, _spawnParent);
        _text = _spawnedInstance.GetComponentInChildren<TextMeshProUGUI>();
        _rootRect = _spawnedInstance.transform as RectTransform;

        Transform iconTransform = _spawnedInstance.transform.Find(_coinIconName);
        _coinIcon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;

        if(_text == null)
        {
            Debug.LogError("[S_RoguelikeMoneyUI] Prefab内にTextMeshProUGUIが見つかりませんでした");
            return;
        }

        _displayedMoney = money;
        _text.text = FormatMoney(money);
    }

    /// <summary>
    /// 表示する残金を更新(アニメーション付き)
    /// 強化購入・リロール後に呼び出し
    /// </summary>
    /// <param name="money">変更後のお金</param>
    public void ChangeMoneyUI(int money)
    {
        if(_text == null)
        {
            Debug.LogWarning("[S_RoguelikeMoneyUI] まだSpawnMoneyUIが呼ばれていません");
            return;
        }

        bool increased = money >= _displayedMoney;

        if (_countCoroutine != null)
            StopCoroutine(_countCoroutine);
        _countCoroutine = StartCoroutine(CountTo(money));

        if (_punchCoroutine != null)
            StopCoroutine(_punchCoroutine);
        _punchCoroutine = StartCoroutine(increased ? PunchScale() : ShakeHorizontal());
    }

    public void SetVisible(bool isVisible)
    {
        if (_spawnedInstance != null)
            _spawnedInstance.SetActive(isVisible);
    }


    //____________________________________
    // private funtion

    private static string FormatMoney(int money) => money.ToString("N0");

    private IEnumerator CountTo(int targetMoney)
    {
        int startMoney = _displayedMoney;
        float elapsed = 0f;

        while (elapsed < _countDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = _countCurve.Evaluate(Mathf.Clamp01(elapsed / _countDuration));
            _displayedMoney = Mathf.RoundToInt(Mathf.Lerp(startMoney, targetMoney, t));
            _text.text = FormatMoney(_displayedMoney);
            yield return null;
        }

        _displayedMoney = targetMoney;
        _text.text = FormatMoney(_displayedMoney);
        _countCoroutine = null;
    }

    private IEnumerator PunchScale()
    {
        if (_rootRect == null)
            yield break;

        Vector3 baseScale = Vector3.one;
        float elapsed = 0f;
        const float duration = 0.25f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float scale = 1f + (_bounceScale - 1f) * (1f - t) * Mathf.Sin(t * Mathf.PI);
            _rootRect.localScale = baseScale * scale;
            yield return null;
        }

        _rootRect.localScale = baseScale;
        _punchCoroutine = null;
    }

    private IEnumerator ShakeHorizontal()
    {
        if (_rootRect == null)
            yield break;

        Vector2 basePos = _rootRect.anchoredPosition;
        float elapsed = 0f;
        const float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float offset = _shakeDistance * (1f - t) * Mathf.Sin(t * Mathf.PI * 6f);
            _rootRect.anchoredPosition = basePos + new Vector2(offset, 0f);
            yield return null;
        }

        _rootRect.anchoredPosition = basePos;
        _punchCoroutine = null;
    }
}
