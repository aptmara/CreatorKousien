// ================================================================================
// File         : GameUIController.cs
// Author       : Oti Haruhiko
//
// Description  : GameUiを管理するクラス
// Created      : 2026-08-18
// ================================================================================

using Game.Core.Events;
using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("キャンバス")]
    [SerializeField] CanvasGroup _canvas;
    [Header("WaveUI")]
    [SerializeField] TMPro.TextMeshProUGUI _waveCountUI;
    [Header("StageProgressUI")]
    [SerializeField] GameObject _gaugePoint;
    [SerializeField] GameObject _gauge;
    [SerializeField] float _gaugeSize;
    [SerializeField] TMPro.TextMeshProUGUI _groupText;
    [SerializeField] float progressGaugeMoveTime;

    // 透明度変更用 
    Coroutine alphaCoroutine = null;

    // ゲージ進行度
    private float _gaugeProgress = 0.0f;
    Coroutine progressCoroutine = null;

    void Awake()
    {
        _canvas.alpha = 0.0f;
    }

    private void OnEnable()
    {
        Game.Core.Events.EventBus.Subscribe<GameChangeEvent>(OnProgressGaugeRequest);
        Game.Core.Events.EventBus.Subscribe<GroupChangeEvent>(OnGroupChange);
    }

    private void OnDisable()
    {
        Game.Core.Events.EventBus.Unsubscribe<GameChangeEvent>(OnProgressGaugeRequest);
        Game.Core.Events.EventBus.Unsubscribe<GroupChangeEvent>(OnGroupChange);
    }

    private void Update()
    {
        UpdateProgressGauge();
    }

    // ウェーブ名を受け取りWave (ウェーブ名)のフォーマットで表示する
    public void SetWave(string waveName)
    {
        _waveCountUI.text = "Wave " + waveName;
    }

    private void OnGroupChange(GroupChangeEvent ev)
    {
        _groupText.text = ev.CurrentCount.ToString() + "/" + ev.MaxCount.ToString();
    }

    // GameUIを表示する
    public void UIVisible(float processingTime)
    {
        // 既に透明度を変更中だった場合止めて新たにLerpを開始する
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        alphaCoroutine = StartCoroutine(LerpUIAlpha(1.0f, processingTime));
    }

    // GameUIを非表示にする
    public void UIInvisible(float processingTime)
    {
        // 既に透明度を変更中だった場合止めて新たにLerpを開始する
        if (alphaCoroutine != null) StopCoroutine(alphaCoroutine);
        alphaCoroutine = StartCoroutine(LerpUIAlpha(0.0f, processingTime));
    }

    // GameUIの透明度をLerpで指定の値に変更
    IEnumerator LerpUIAlpha(float targetAlpha, float processingTime)
    {

        // 継続秒数が0以下だった場合即座に透明度を更新する
        if (processingTime <= 0.0f)
        {
            _canvas.alpha = targetAlpha;
            yield break;
        }

        // 数値を初期化
        float alphaProgress = 0.0f;
        float startAlpha = _canvas.alpha;
        // Lerpで透明度を更新
        while (alphaProgress < 1.0f)
        {
            alphaProgress += Time.deltaTime / processingTime;
            Mathf.Clamp(alphaProgress, 0.0f, 1.0f);
            _canvas.alpha = Mathf.Lerp(startAlpha, targetAlpha, alphaProgress);
            yield return null;
        }

        // 余計な数値が残らないよう削除
        alphaCoroutine = null;
    }

    private void OnProgressGaugeRequest(GameChangeEvent ev)
    {
        float target = (float)ev.SpawnedEnemy / (float)ev.MaxEnemy;
        if(progressCoroutine != null) StopCoroutine (progressCoroutine);
        progressCoroutine = StartCoroutine(LerpProgressGauge(target));

    }

    IEnumerator LerpProgressGauge(float targetProgress)
    {
        float startProgress = _gaugeProgress;
        float lerpProgress = 0.0f;

        while (lerpProgress < 1.0f)
        {
            // 進行度を更新
            lerpProgress += Time.deltaTime / progressGaugeMoveTime;
            Mathf.Clamp (lerpProgress, 0.0f, 1.0f);
            _gaugeProgress = Mathf.Lerp(startProgress, targetProgress, lerpProgress);
            // 進行度に応じてゲージを更新
            UpdateProgressGauge();
            yield return null;
        }

        progressCoroutine = null;
    }

    void UpdateProgressGauge()
    {
        // ゲージの進行ポイントの座標を更新
        RectTransform pointRect = _gaugePoint.GetComponent<RectTransform>();

        Vector3 newPos = pointRect.localPosition;
        newPos.y = _gaugeSize * _gaugeProgress - _gaugeSize / 2.0f;
  
        pointRect.localPosition = newPos;

        // ゲージ本体の大きさを更新
        RectTransform gaugeRect = _gauge.GetComponent<RectTransform>();

        Vector3 newScale = gaugeRect.localScale;
        newScale.y = _gaugeProgress;
        gaugeRect.localScale = newScale;
    }
}
