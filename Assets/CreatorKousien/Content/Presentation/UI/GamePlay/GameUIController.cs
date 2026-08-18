// ================================================================================
// File         : GameUIController.cs
// Author       : Oti Haruhiko
//
// Description  : GameUiを管理するクラス
// Created      : 2026-08-18
// ================================================================================

using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    [Header("キャンバス")]
    [SerializeField] CanvasGroup _canvas;
    [Header("各UI")]
    [SerializeField] TMPro.TextMeshProUGUI _waveCountUI;

    // 透明度変更用
    float alphaProgress = 0.0f;
    Coroutine alphaCoroutine = null;

    void Awake()
    {
        _canvas.alpha = 0.0f;
    }

    // ウェーブ名を受け取りWave (ウェーブ名)のフォーマットで表示する
    public void SetWave(string waveName)
    {
        _waveCountUI.text = "Wave " + waveName;
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
        alphaProgress = 0.0f;
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
        alphaProgress = 0.0f;
        alphaCoroutine = null;
    }
}
