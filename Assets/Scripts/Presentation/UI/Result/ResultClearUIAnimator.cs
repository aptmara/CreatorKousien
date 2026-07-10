// ------------------------------------------------------------
// File		: ResultClearUIAnimator.cs
// Summary	: クリア後のUIアニメーションを制御するクラス
//
// Author	: [浅野 勇生]
// Created	: 2026-07-09
//
// Notes	:
// - ベースのUIアニメーションを再生する
// ------------------------------------------------------------
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Result
{
    public class ResultClearUIAnimator : MonoBehaviour
    {
        [Header("UIアニメーション設定")]
        [Tooltip("左の木のRectTransform")]
        [SerializeField] private RectTransform _treeL;

        [Tooltip("右の木のRectTransform")]
        [SerializeField] private RectTransform _treeR;

        [Tooltip("メインボードのRectTransform")]
        [SerializeField] private RectTransform _mainBoard;

        [Tooltip("小さいボードのRectTransform")]
        [SerializeField] private RectTransform _smallBoard;

        [Tooltip("猫のRectTransform")]
        [SerializeField] private RectTransform[] _cats;

        [Tooltip("魂のRectTransformの配列")]
        [SerializeField] private RectTransform[] _souls;

        [Tooltip("下のアイテムのRectTransformの配列")]
        [SerializeField] private RectTransform[] _bottomItems;

        [Tooltip("タイトルボタン")]
        [SerializeField] private Button _titleButton;

        [Tooltip("クリアテキスト")]
        [SerializeField] private TextMeshProUGUI _clearText;


        [Header("アニメーションの速度設定")]
        [Header("--- Tree ---")]
        [Tooltip("木のアニメーションの時間")]
        [SerializeField] private float _treeInDuration = 0.35f;
        [Tooltip("木のアニメーションのオフセット")]
        [SerializeField] private Vector2 _treeOffset = new Vector2(520f, 0f);
        [Tooltip("木のアニメーションの開始スケール")]
        [SerializeField] private float _treeStartScale = 0.7f;
        [Tooltip("木のアニメーションのポップスケール")]
        [SerializeField] private float _treePopScale = 1.15f;


        [Header("--- Main Board ---")]
        [Tooltip("メインボードのアニメーションの時間")]
        [SerializeField] private float _mainBoardDropDuration = 0.65f;
        [Tooltip("メインボードのアニメーションのオフセットY")]
        [SerializeField] private float _mainBoardDropOffsetY = 900f;
        [Tooltip("メインボードのアニメーションのポップスケール")]
        [SerializeField] private float _mainBoardBounceScale = 1.08f;


        [Header("--- Small Board ---")]
        [Tooltip("小さいボードのアニメーションの時間")]
        [SerializeField] private float _smallBoardDuration = 0.45f;
        [Tooltip("小さいボードのアニメーションの開始オフセット")]
        [SerializeField] private Vector2 _smallBoardStartOffset = new Vector2(0f, 160f);
        [Tooltip("小さいボードのアニメーションの開始回転角度")]
        [SerializeField] private float _smallBoardStartRotation = 180f;
        [Tooltip("小さいボードのアニメーションの開始スケール")]
        [SerializeField] private float _smallBoardStartScale = 0.2f;
        [Tooltip("小さいボードのアニメーションのポップスケール")]
        [SerializeField] private float _smallBoardPopScale = 1.12f;


        [Header("--- Cat / Souls ---")]
        [Tooltip("猫のアニメーションの時間")]
        [SerializeField] private float _catDuration = 0.35f;
        [Tooltip("猫のアニメーションの開始オフセット")]
        [SerializeField] private Vector2 _catStartOffset = new Vector2(0f, -170f);
        [Tooltip("猫のアニメーションの開始スケール")]
        [SerializeField] private float _catStartScale = 0.75f;
        [Tooltip("猫のアニメーションのポップスケール")]
        [SerializeField] private float _catPopScale = 1.08f;
        [Tooltip("魂のアニメーションのポップスケール")]
        [SerializeField] private float _soulFadeDuration = 0.3f;
        [Tooltip("魂が順番に出る間隔")]
        [SerializeField] private float _soulFadeInterval = 0.08f;
        [Tooltip("魂のふわふわ移動量")]
        [SerializeField] private Vector2 _soulFloatAmplitude = new Vector2(18f, 24f);
        [Tooltip("魂のふわふわ速度")]
        [SerializeField] private Vector2 _soulFloatSpeed = new Vector2(1.4f, 1.1f);


        [Header("--- Bottom Items ---")]
        [Tooltip("下のアイテムのアニメーションの時間")]
        [SerializeField] private float _bottomItemDuration = 0.28f;
        [Tooltip("下のアイテムのアニメーションの開始オフセット")]
        [SerializeField] private Vector2 _bottomItemStartOffset = new Vector2(0f, -180f);
        [Tooltip("下のアイテムのアニメーションのポップスケール")]
        [SerializeField] private float _bottomItemPopScale = 1.15f;
        [Tooltip("下のアイテムのアニメーションの表示間隔")]
        [SerializeField] private float _bottomItemInterval = 0.08f;


        [Header("--- BackGround ---")]
        [Tooltip("背景のCanvasGroup")]
        [SerializeField] private CanvasGroup _backgroundBlur;
        [Tooltip("背景のフェードイン時間")]
        [SerializeField] private float _backgroundFadeDuration = 0.35f;
        [Tooltip("背景の最終アルファ値")]
        [SerializeField] private float _backgroundBlurAlpha = 0.75f;



        /// --- 内部変数 ---
        private Vector2 _treeLPos, _treeRPos, _mainBoardPos, _smallBoardPos;
        private Vector2[] _catsPos;
        private Vector2[] _soulsPos;
        private bool _floatSouls;



        /// <summary>
        /// タイトルに戻るボタンがクリックされたときの処理を設定して、UIアニメーションを再生する
        /// </summary>
        /// <param name="onTitleClicked">タイトルに戻るボタンをクリックしたときの処理</param>
        public void Play(Action onTitleClicked)
        {
            StopAllCoroutines();
            CaptureFinalPositions();
            ResetView();

            if (_clearText != null)
            {
                _clearText.text = "GAME CLEAR";
            }

            if (_titleButton != null)
            {
                _titleButton.interactable = false;
                _titleButton.onClick.RemoveAllListeners();
                _titleButton.onClick.AddListener(() => onTitleClicked?.Invoke());
            }

            StartCoroutine(PlayRoutine());
        }


        /// <summary>
        /// UIアニメーションの再生コルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayRoutine()
        {
            gameObject.SetActive(true);

            // --- 背景のフェードインアニメーション ---
            StartCoroutine(PlayBackgroundFadeInRoutine());


            // --- 左右の木を順番に表示するアニメーション ---
            StartCoroutine(PlayTreeInRoutine());

            // --- 魂をフェードで表示して、ふわふわ開始 ---
            StartCoroutine(PlaySoulsAppearRoutine());


            // --- 下のアイテムを順番に表示するアニメーション ---
            StartCoroutine(PlayBottomItemsAppearRoutine());


            yield return new WaitForSecondsRealtime(1.0f);

            // --- メインボードを表示するアニメーション ---
            yield return PlayMainBoardDropRoutine();


            // --- 小さいボードと猫を順番に表示するアニメーション ---
            StartCoroutine(PlayCatAppearRoutine());
            yield return PlaySmallBoardAppearRoutine();


            // --- タイトルに戻るボタンを有効化 ---
            if (_titleButton != null)
            {
                _titleButton.interactable = true;
            }
        }


        /// <summary>
        /// 魂を浮かせるアニメーションを更新する
        /// </summary>
        private void Update()
        {
            if (!_floatSouls || _souls == null || _souls.Length == 0)
            {
                return;
            }

            float time = Time.unscaledTime;
            for (int i = 0; i < _souls.Length; i++)
            {
                if (_souls[i] == null || !_souls[i].gameObject.activeSelf)
                {
                    continue;
                }

                // 魂の位置をふわふわさせる
                float phase = (float)i * 1.7f;
                _souls[i].anchoredPosition = _soulsPos[i] + new Vector2(
                    Mathf.Sin(time * _soulFloatSpeed.x + phase) * _soulFloatAmplitude.x,
                    Mathf.Cos(time * _soulFloatSpeed.y + phase) * _soulFloatAmplitude.y
                );
            }
        }


        /// <summary>
        /// UI要素の最終位置をキャプチャする
        /// </summary>
        private void CaptureFinalPositions()
        {
            _treeLPos = _treeL.anchoredPosition;
            _treeRPos = _treeR.anchoredPosition;
            _mainBoardPos = _mainBoard.anchoredPosition;
            _smallBoardPos = _smallBoard.anchoredPosition;

            _catsPos = new Vector2[_cats.Length];
            for (int i = 0; i < _cats.Length; i++)
            {
                if (_cats[i] != null)
                {
                    _catsPos[i] = _cats[i].anchoredPosition;
                }
            }

            _soulsPos = new Vector2[_souls.Length];
            for (int i = 0; i < _souls.Length; i++)
            {
                _soulsPos[i] = _souls[i].anchoredPosition;
            }
        }


        /// <summary>
        /// リセット前のUI要素の最終位置をキャプチャする
        /// </summary>
        private void ResetView()
        {
            if (_backgroundBlur != null)
            {
                _backgroundBlur.alpha = 0f;
                _backgroundBlur.gameObject.SetActive(false);
            }

            _floatSouls = false;
            _treeL.gameObject.SetActive(true);
            _treeR.gameObject.SetActive(true);
            _mainBoard.gameObject.SetActive(false);
            _smallBoard.gameObject.SetActive(false);

            foreach (var cat in _cats)
            {
                if (cat != null)
                {
                    cat.gameObject.SetActive(false);
                }
            }

            // 魂と下のアイテムを非表示にする
            foreach (var soul in _souls)
            {
                soul.gameObject.SetActive(false);
            }

            foreach (var item in _bottomItems)
            {
                item.gameObject.SetActive(false);
            }
        }




        // それぞれのUI要素のアニメーションを再生するコルーチン
        // ------------------------------------------------------------
        private IEnumerator PlayBackgroundFadeInRoutine()
        {
            if (_backgroundBlur == null)
            {
                yield break;
            }

            _backgroundBlur.gameObject.SetActive(true);
            _backgroundBlur.alpha = 0f;

            float duration = Mathf.Max(0.01f, _backgroundFadeDuration);
            float time = 0f;

            // 背景のフェードインアニメーションを再生
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float t = EaseInOut(Mathf.Clamp01(time / duration));
                _backgroundBlur.alpha = Mathf.Lerp(0f, _backgroundBlurAlpha, t);

                yield return null;
            }

            _backgroundBlur.alpha = _backgroundBlurAlpha;
        }


        /// <summary>
        /// 左右の木を順番に表示するアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayTreeInRoutine()
        {
            Vector2 treeLStart = _treeLPos + new Vector2(-_treeOffset.x, _treeOffset.y);
            Vector2 treeRStart = _treeRPos + new Vector2(_treeOffset.x, _treeOffset.y);

            _treeL.gameObject.SetActive(true);
            _treeR.gameObject.SetActive(true);

            // 初期位置とスケールを設定
            _treeL.anchoredPosition = treeLStart;
            _treeR.anchoredPosition = treeRStart;
            _treeL.localScale = Vector3.one * _treeStartScale;
            _treeR.localScale = Vector3.one * _treeStartScale;

            // 木のアニメーションを再生
            float time = 0f;
            while (time < _treeInDuration)
            {
                // 時間の経過に応じて位置とスケールを補間
                time += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / _treeInDuration));

                _treeL.anchoredPosition = Vector2.LerpUnclamped(treeLStart, _treeLPos, t);
                _treeR.anchoredPosition = Vector2.LerpUnclamped(treeRStart, _treeRPos, t);

                float scale = Mathf.LerpUnclamped(_treeStartScale, _treePopScale, t);
                _treeL.localScale = Vector3.one * scale;
                _treeR.localScale = Vector3.one * scale;

                yield return null;
            }

            yield return ScalePair(_treeL, _treeR, _treePopScale, 1f, 0.12f, EaseOutBack);
        }



        /// <summary>
        /// メインボードを落下させるアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayMainBoardDropRoutine()
        {
            Vector2 start = _mainBoardPos + Vector2.up * _mainBoardDropOffsetY;

            // メインボードを表示して初期位置とスケールを設定
            _mainBoard.anchoredPosition = start;
            _mainBoard.localScale = Vector3.one;
            _mainBoard.gameObject.SetActive(true);

            // メインボードのアニメーションを再生
            float time = 0f;
            while (time < _mainBoardDropDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOutBounce(Mathf.Clamp01(time / _mainBoardDropDuration));

                _mainBoard.anchoredPosition = Vector2.LerpUnclamped(start, _mainBoardPos, t);

                yield return null;
            }

            _mainBoard.anchoredPosition = _mainBoardPos;

            yield return PunchScale(_mainBoard, _mainBoardBounceScale, 0.12f);
        }


        /// <summary>
        /// 小さいボードを表示するアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlaySmallBoardAppearRoutine()
        {
            Vector2 start = _smallBoardPos + _smallBoardStartOffset;
            float overshootRotation = -Mathf.Sign(_smallBoardStartRotation) * 12f;

            // 小さいボードを表示して初期位置、スケール、回転を設定
            _smallBoard.gameObject.SetActive(true);
            _smallBoard.anchoredPosition = start;
            _smallBoard.localScale = Vector3.one;
            _smallBoard.localRotation = Quaternion.Euler(_smallBoardStartRotation, 0f, 0f);

            // 小さいボードのアニメーションを再生
            float duration = Mathf.Max(0.01f, _smallBoardDuration);
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                float rawT = Mathf.Clamp01(time / duration);

                float moveT = EaseOutBack(rawT);
                _smallBoard.anchoredPosition = Vector2.LerpUnclamped(start, _smallBoardPos, moveT);

                float rotateT = Mathf.Clamp01((rawT - 0.35f) / 0.65f);
                rotateT = EaseOutBack(rotateT);

                float rotationX = Mathf.LerpUnclamped(_smallBoardStartRotation, overshootRotation, rotateT);
                _smallBoard.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

                yield return null;
            }

            time = 0f;
            float settleDuration = 0.12f;

            // 小さいボードの回転を元に戻すアニメーションを再生
            while (time < settleDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseInOut(Mathf.Clamp01(time / settleDuration));

                float rotationX = Mathf.LerpUnclamped(overshootRotation, 0f, t);
                _smallBoard.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

                yield return null;
            }

            // 最終的な位置、スケール、回転を設定
            _smallBoard.anchoredPosition = _smallBoardPos;
            _smallBoard.localScale = Vector3.one;
            _smallBoard.localRotation = Quaternion.identity;
        }


        /// <summary>
        /// 猫を表示するアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayCatAppearRoutine()
        {
            if (_cats == null || _cats.Length == 0)
            {
                yield break;
            }

            // 猫を順番に表示するアニメーションを再生
            for (int i = 0; i < _cats.Length; i++)
            {
                if (_cats[i] == null)
                {
                    continue;
                }

                yield return PlaySingleCatAppearRoutine(_cats[i], _catsPos[i]);
            }
        }


        /// <summary>
        /// 猫を表示するアニメーションのコルーチン（単体）
        /// </summary>
        /// <param name="cat">猫のRectTransform</param>
        /// <param name="finalPos">最終位置</param>
        /// <returns></returns>
        private IEnumerator PlaySingleCatAppearRoutine(RectTransform cat, Vector2 finalPos)
        {
            Vector2 start = finalPos + _catStartOffset;

            // 猫を表示して初期位置とスケールを設定
            cat.gameObject.SetActive(true);
            cat.anchoredPosition = start;
            cat.localScale = Vector3.one * _catStartScale;

            // 猫のアニメーションを再生
            float duration = Mathf.Max(0.01f, _catDuration);
            float time = 0f;

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / duration));

                cat.anchoredPosition = Vector2.LerpUnclamped(start, finalPos, t);
                float scale = Mathf.LerpUnclamped(_catStartScale, _catPopScale, t);
                cat.localScale = Vector3.one * scale;

                yield return null;
            }

            cat.anchoredPosition = finalPos;
            cat.localScale = Vector3.one;

            yield return PunchScale(cat, _catPopScale, 0.1f);
        }



        /// <summary>
        /// 魂を順番にフェードインさせるアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlaySoulsAppearRoutine()
        {
            _floatSouls = true;

            // 魂を順番にフェードインさせるアニメーションを再生
            foreach (var soul in _souls)
            {
                if (soul == null)
                {
                    continue;
                }

                // CanvasGroupを取得または追加して、アルファ値を0に設定
                CanvasGroup canvasGroup = GetOrAddCanvasGroup(soul);
                canvasGroup.alpha = 0f;

                soul.gameObject.SetActive(true);
                soul.localScale = Vector3.one * 0.8f;

                StartCoroutine(FadeSoulIn(soul, canvasGroup));

                yield return new WaitForSecondsRealtime(_soulFadeInterval);
            }

            yield return new WaitForSecondsRealtime(_soulFadeDuration);

        }



        /// <summary>
        /// 下のアイテムを順番に表示するアニメーションのコルーチン
        /// </summary>
        /// <returns></returns>
        private IEnumerator PlayBottomItemsAppearRoutine()
        {
            foreach (var item in _bottomItems)
            {
                if (item == null)
                {
                    continue;
                }

                yield return PlayBottomItemAppearRoutine(item);
                yield return new WaitForSecondsRealtime(_bottomItemInterval);
            }
        }



        /// <summary>
        /// 下のアイテムを表示するアニメーションのコルーチン
        /// </summary>
        /// <param name="item">対象のアイテム</param>
        /// <returns></returns>
        private IEnumerator PlayBottomItemAppearRoutine(RectTransform item)
        {
            Vector2 end = item.anchoredPosition;
            Vector2 start = end + _bottomItemStartOffset;

            item.gameObject.SetActive(true);
            item.anchoredPosition = start;
            item.localScale = Vector3.zero;

            float time = 0f;
            while (time < _bottomItemDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / _bottomItemDuration));

                item.anchoredPosition = Vector2.LerpUnclamped(start, end, t);

                float scale = Mathf.LerpUnclamped(0f, _bottomItemPopScale, t);
                item.localScale = Vector3.one * scale;

                yield return null;
            }

            item.anchoredPosition = end;

            yield return PunchScale(item, _bottomItemPopScale, 0.1f);
        }




        // 移動関連のコルーチンとイージング関数
        // ------------------------------------------------------------

        /// <summary>
        /// UI要素を指定された位置に移動するコルーチン
        /// </summary>
        /// <param name="target">対象のRectTransform</param>
        /// <param name="from">移動開始位置</param>
        /// <param name="to">移動終了位置</param>
        /// <param name="duration">アニメーションの持続時間</param>
        /// <param name="ease">イージング値</param>
        /// <returns></returns>
        private static IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration, Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                // イージング関数を使用して、時間の進行に応じた補間値を計算
                float t = ease(Mathf.Clamp01(time / duration));
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                yield return null;
            }
            target.anchoredPosition = to;
        }


        /// <summary>
        /// UI要素を指定された位置に移動するコルーチン
        /// </summary>
        /// <param name="a">対象A</param>
        /// <param name="aFrom">Aの移動開始位置</param>
        /// <param name="aTo">Aの移動終了位置</param>
        /// <param name="b">対象B</param>
        /// <param name="bFrom">Bの移動開始位置</param>
        /// <param name="bTo">Bの移動終了位置</param>
        /// <param name="duration">アニメーションの持続時間</param>
        /// <param name="ease">イージング値</param>
        /// <returns></returns>
        private static IEnumerator MovePair(RectTransform a, Vector2 aFrom, Vector2 aTo, RectTransform b, Vector2 bFrom, Vector2 bTo, float duration, Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                // イージング関数を使用して、時間の進行に応じた補間値を計算
                float t = ease(Mathf.Clamp01(time / duration));
                a.anchoredPosition = Vector2.LerpUnclamped(aFrom, aTo, t);
                b.anchoredPosition = Vector2.LerpUnclamped(bFrom, bTo, t);
                yield return null;
            }
        }



        /// <summary>
        /// UI要素のスケールを指定された範囲で変更するコルーチン
        /// </summary>
        /// <param name="a">トランスフォームA</param>
        /// <param name="b">トランスフォームB</param>
        /// <param name="from">開始スケール</param>
        /// <param name="to">終了スケール</param>
        /// <param name="duration">アニメーションの持続時間</param>
        /// <param name="ease">イージング関数</param>
        /// <returns></returns>
        private static IEnumerator ScalePair(RectTransform a, RectTransform b, float from, float to, float duration, Func<float, float> ease)
        {
            float time = 0f;

            // スケールアニメーションを再生
            while (time < duration)
            {
                // 時間の経過に応じてスケールを補間
                time += Time.unscaledDeltaTime;
                float t = ease(Mathf.Clamp01(time / duration));
                float scale = Mathf.LerpUnclamped(from, to, t);

                a.localScale = Vector3.one * scale;
                b.localScale = Vector3.one * scale;

                yield return null;
            }

            // 最終的なスケールを設定
            a.localScale = Vector3.one * to;
            b.localScale = Vector3.one * to;
        }



        /// <summary>
        /// UI要素のスケールを一時的に拡大して元に戻すパンチアニメーションのコルーチン
        /// </summary>
        /// <param name="target">対象のRectTransform</param>
        /// <param name="popScale">ポップ時のスケール</param>
        /// <param name="duration">アニメーションの持続時間</param>
        /// <returns></returns>
        private static IEnumerator PunchScale(RectTransform target, float popScale, float duration)
        {
            float halfDuration = duration * 0.5f;

            // スケールアニメーションを再生
            float time = 0f;
            while (time < halfDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / halfDuration));
                float scale = Mathf.LerpUnclamped(1f, popScale, t);

                target.localScale = Vector3.one * scale;
                yield return null;
            }

            // スケールを元に戻すアニメーションを再生
            time = 0f;
            while (time < halfDuration)
            {
                time += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(time / halfDuration));
                float scale = Mathf.LerpUnclamped(popScale, 1f, t);

                target.localScale = Vector3.one * scale;
                yield return null;
            }

            target.localScale = Vector3.one;
        }


        /// <summary>
        /// 魂をフェードインさせるアニメーションのコルーチン
        /// </summary>
        /// <param name="soul">魂のRectTransform</param>
        /// <param name="canvasGroup">CanvasGroup</param>
        /// <returns></returns>
        private IEnumerator FadeSoulIn(RectTransform soul, CanvasGroup canvasGroup)
        {
            float time = 0f;

            // 魂のフェードインアニメーションを再生
            while (time < _soulFadeDuration)
            {
                // 時間の経過に応じてアルファ値とスケールを補間
                time += Time.unscaledDeltaTime;
                float rawT = Mathf.Clamp01(time / _soulFadeDuration);
                float t = EaseInOut(rawT);

                canvasGroup.alpha = Mathf.Clamp01(t);
                soul.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t);

                yield return null;
            }

            // 最終的なアルファ値とスケールを設定
            canvasGroup.alpha = 1f;
            soul.localScale = Vector3.one;
        }




        /// <summary>
        /// UI要素を指定された位置に移動し、スケールを変更するコルーチン
        /// </summary>
        /// <param name="target">対象のRectTransform</param>
        /// <param name="from">移動開始位置</param>
        /// <param name="to">移動終了位置</param>
        /// <param name="scaleFrom">スケール開始値</param>
        /// <param name="scaleTo">スケール終了値</param>
        /// <param name="duration">アニメーションの持続時間</param>
        /// <param name="ease">イージング値</param>
        /// <returns></returns>
        private static IEnumerator MoveScale(RectTransform target, Vector2 from, Vector2 to, float scaleFrom, float scaleTo, float duration, Func<float, float> ease)
        {
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;

                // イージング関数を使用して、時間の進行に応じた補間値を計算
                float t = ease(Mathf.Clamp01(time / duration));
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(scaleFrom, scaleTo, t);
                yield return null;
            }
        }



        /// <summary>
        /// バックスイングのイージング関数
        /// </summary>
        /// <param name="t">時間</param>
        /// <returns>イージングの値</returns>
        private static float EaseOutBack(float t) => 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);


        /// <summary>
        /// バウンスのイージング関数
        /// </summary>
        /// <param name="t">時間</param>
        /// <returns>イージングの値</returns>
        private static float EaseOutBounce(float t)
        {
            // バウンスのイージング関数の実装
            if (t < (1f / 2.75f))
            {
                return 7.5625f * t * t;
            }
            else if (t < (2f / 2.75f))
            {
                return 7.5625f * (t -= (1.5f / 2.75f)) * t + 0.75f;
            }
            else if (t < (2.5f / 2.75f))
            {
                return 7.5625f * (t -= (2.25f / 2.75f)) * t + 0.9375f;
            }
            else
            {
                return 7.5625f * (t -= (2.625f / 2.75f)) * t + 0.984375f;
            }
        }


        /// <summary>
        /// イーズインアウトのイージング関数
        /// </summary>
        /// <param name="t">時間</param>
        /// <returns></returns>
        private static float EaseInOut(float t)
        {
            return t * t * (3f - 2f * t);
        }


        /// <summary>
        /// CanvasGroupコンポーネントを取得するか、存在しない場合は追加する
        /// </summary>
        /// <param name="target">対象のRectTransform</param>
        /// <returns></returns>
        private static CanvasGroup GetOrAddCanvasGroup(RectTransform target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }
    }
}

