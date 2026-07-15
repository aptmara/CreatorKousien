//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
// file   : S_UIScaleAnimator.cs
// brief  : ローグライクシーンでのUIアニメーション
//
// auther : Takitani Shohei
// date   : 2026/07/14 - begin.
//_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/_/
using System;
using System.Collections;
using UnityEngine;

public class S_UIScaleAnimator : MonoBehaviour
{
    [Header("対象")]
    [SerializeField] private RectTransform _target;

    [Header("フォーカス中(継続的な拡縮)")]
    [Tooltip("フォーカス中の拡大倍率")]
    [SerializeField] private float _hoverScale = 1.1f;
    [Tooltip("フォーカスOn/Off切り替え時の遷移時間")]
    [SerializeField] private float _hoverTransitionDuration = 0.15f;
    [SerializeField] private AnimationCurve _hoverEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("選択確定時")]
    [Tooltip("最初に縮む倍率")]
    [SerializeField] private float _shrinkScale = 0.9f;
    [Tooltip("縮むのにかける時間")]
    [SerializeField] private float _shrinkDuration = 0.08f;
    [Tooltip("元より大きく膨らむ時の倍率")]
    [SerializeField] private float _overshootScale = 1.3f;
    [Tooltip("膨らむのにかける時間")]
    [SerializeField] private float _growDuration = 0.15f;
    [Tooltip("元のサイズへ戻るときの時間")]
    [SerializeField] private float _settleDuration = 0.15f;
    [SerializeField] private AnimationCurve _selectEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 _baseScale;
    private Coroutine _hoverRoutine;
    private Coroutine _selectRoutine;
    private bool _isHighlighted;



    //____________________________________
    // basic function

    private void Awake()
    {
        if(_target == null)
        {
            _target = GetComponent<RectTransform>();
        }
        _baseScale = _target.localScale;
    }



    //____________________________________
    // public function

    /// <summary>
    /// フォーカス状態を切り替える関数
    /// 選択確定アニメーション中は無視する
    /// </summary>
    /// <param name="isHighlighted"></param>
    public void SetHighlighted(bool isHighlighted)
    {
        _isHighlighted = isHighlighted;

        if (_selectRoutine != null) return;

        if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);

        float targetMultiplier = isHighlighted ? _hoverScale : 1.0f;
        _hoverRoutine = StartCoroutine(HoverRoutine(targetMultiplier));
    }

    /// <summary>
    /// 選択買う提示に呼ぶアニメーション
    /// 縮む　→　膨らむ　→　拡大、等倍へ
    /// </summary>
    /// <param name="onComplete"></param>
    public void PlaySelectedAnimation(Action onComplete = null)
    {
        if (_hoverRoutine != null) StopCoroutine(_hoverRoutine);
        if (_selectRoutine != null) StopCoroutine(_selectRoutine);

        _selectRoutine = StartCoroutine(SelectedAnimationRoutine(onComplete));
    }

    private IEnumerator HoverRoutine(float targetMultiplier)
    {
        yield return ScaleRoutine(CurrentMultiplier(), targetMultiplier, _hoverTransitionDuration, _hoverEase);
        _hoverRoutine = null;
    }

    private IEnumerator SelectedAnimationRoutine(Action onComplete)
    {
        float startMultiplier = CurrentMultiplier();

        yield return ScaleRoutine(startMultiplier, _shrinkScale, _shrinkDuration, _selectEase);
        yield return ScaleRoutine(_shrinkScale, _overshootScale, _growDuration, _selectEase);

        float endMultiplier = _isHighlighted ? _hoverScale : 1.0f;
        yield return ScaleRoutine(_overshootScale, endMultiplier, _settleDuration, _selectEase);

        _selectRoutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator ScaleRoutine(float fromMultiplier, float toMultiplier, float duration, AnimationCurve ease)
    {
        if(duration <= 0.0f)
        {
            ApplyMultiplier(toMultiplier);
            yield break;
        }

        float elapsed = 0.0f;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = ease.Evaluate(Mathf.Clamp01(elapsed / duration));

            ApplyMultiplier(Mathf.Lerp(fromMultiplier, toMultiplier, t));
            yield return null;
        }

        ApplyMultiplier(toMultiplier);
    }

    private float CurrentMultiplier()
    {
        if (_baseScale.x == 0.0f) return 1.0f;
        return _target.localScale.x / _baseScale.x;
    }

    private void ApplyMultiplier(float multiplier)
    {
        _target.localScale = _baseScale * multiplier;
    }

}
