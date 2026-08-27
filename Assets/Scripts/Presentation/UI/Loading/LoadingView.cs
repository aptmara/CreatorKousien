using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Loading
{
    public sealed class LoadingView : MonoBehaviour
    {
        private const float OpenFrameDuration = 1.8f;
        private const float HalfClosedFrameDuration = 0.08f;
        private const float ClosedFrameDuration = 0.12f;
        private const float CharacterEdgePadding = 24f;
        private const float LoadingTextHeight = 64f;
        private const string GameStartTextureRoot = "Textures/GAMECLEAR/";

        private Camera _loadingCamera;
        private CanvasGroup _loadingGroup;
        private Image _blackOverlay;
        private Image _characterImage;
        private CanvasGroup _gameStartGroup;
        private RectTransform[] _gameCharacters;
        private Sprite[] _blinkFrames;
        private int _blinkFrameIndex;
        private float _nextBlinkFrameAt;
        private bool _animateLoading = true;

        public void Initialize()
        {
            CreateCamera();
            CreateCanvas();
            ResetBlinkAnimation();
        }

        private void Update()
        {
            if (!_animateLoading || Time.unscaledTime < _nextBlinkFrameAt)
            {
                return;
            }

            _blinkFrameIndex = (_blinkFrameIndex + 1) % _blinkFrames.Length;
            _characterImage.sprite = _blinkFrames[_blinkFrameIndex];
            _nextBlinkFrameAt = Time.unscaledTime + GetFrameDuration(_blinkFrameIndex);
        }

        public IEnumerator PlayGameStartRoutine()
        {
            _animateLoading = false;
            _blackOverlay.color = new Color(0.035f, 0.01f, 0.06f, 0f);
            yield return FadeImageRoutine(_blackOverlay, 0f, 1f, 0.25f);
            _loadingGroup.alpha = 0f;
            _loadingCamera.enabled = false;
            yield return FadeImageRoutine(_blackOverlay, 1f, 0f, 0.45f);

            _gameStartGroup.alpha = 1f;
            for (int i = 0; i < _gameCharacters.Length; i++)
            {
                StartCoroutine(PopCharacterRoutine(_gameCharacters[i], i));
                yield return new WaitForSecondsRealtime(0.18f);
            }

            yield return new WaitForSecondsRealtime(0.72f);

            Vector3 exitStartPosition = _gameStartGroup.transform.localPosition;
            Vector3 dipPosition = exitStartPosition + Vector3.down * 22f;
            Vector3 exitPosition = exitStartPosition + Vector3.up * 58f;
            float elapsed = 0f;
            const float dipDuration = 0.12f;
            while (elapsed < dipDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = SmoothStep(Mathf.Clamp01(elapsed / dipDuration));
                _gameStartGroup.transform.localPosition = Vector3.Lerp(exitStartPosition, dipPosition, t);
                _gameStartGroup.transform.localScale = new Vector3(
                    Mathf.Lerp(1f, 1.04f, t),
                    Mathf.Lerp(1f, 0.94f, t),
                    1f);
                yield return null;
            }

            elapsed = 0f;
            const float fadeDuration = 0.32f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                _gameStartGroup.alpha = 1f - SmoothStep(t);
                _gameStartGroup.transform.localPosition = Vector3.Lerp(dipPosition, exitPosition, eased);
                _gameStartGroup.transform.localScale = Vector3.Lerp(
                    new Vector3(1.04f, 0.94f, 1f),
                    Vector3.one,
                    eased);
                yield return null;
            }

            _gameStartGroup.alpha = 0f;
            _gameStartGroup.transform.localPosition = exitStartPosition;
            _gameStartGroup.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// ローディング表示の表示・非表示を即座に切り替える処理
        /// 8/15 - Stage2で使えるようにするために追加 Asano
        /// </summary>
        /// <param name="visible"></param>
        public void SetLoadingVisible(bool visible)
        {
            _animateLoading = visible;

            if (_loadingGroup != null)
            {
                _loadingGroup.alpha = visible ? 1f : 0f;
            }

            if (_loadingCamera != null)
            {
                _loadingCamera.enabled = visible;
            }

            if (visible)
            {
                ResetBlinkAnimation();
            }
        }

        /// <summary>
        /// 黒フェードを挟んでローディング画面を表示します
        /// </summary>
        /// <param name="fadeDuration">フェードにかける時間(秒)</param>
        /// <returns></returns>
        public IEnumerator PlayEnterRoutine(float fadeDuration = 0.35f)
        {
            // ゲーム画面の上を黒で覆う
            SetLoadingVisible(false);

            _blackOverlay.color = new Color(0.035f, 0.01f, 0.06f, 0f);
            yield return FadeImageRoutine(_blackOverlay, 0f, 1f, fadeDuration);

            // 黒の裏でローディング画面を差し替えてから黒を明ける
            SetLoadingVisible(true);
            yield return FadeImageRoutine(_blackOverlay, 1f, 0f, fadeDuration);
        }

        private IEnumerator PopCharacterRoutine(RectTransform target, int index)
        {
            Vector2 finalPosition = target.anchoredPosition;
            float finalRotation = target.localEulerAngles.z;
            if (finalRotation > 180f)
            {
                finalRotation -= 360f;
            }

            Vector2 startPosition = finalPosition + Vector2.down * 85f;
            float normalizedIndex = _gameCharacters.Length > 1
                ? index / (float)(_gameCharacters.Length - 1)
                : 0.5f;
            float startRotation = finalRotation + Mathf.Lerp(-10f, 10f, normalizedIndex);
            Image image = target.GetComponent<Image>();
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            target.anchoredPosition = startPosition;
            target.localScale = Vector3.zero;
            target.localRotation = Quaternion.Euler(0f, 0f, startRotation);

            float elapsed = 0f;
            const float duration = 0.58f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseOutBack(t);
                target.anchoredPosition = Vector2.LerpUnclamped(startPosition, finalPosition, eased);
                target.localScale = Vector3.one * Mathf.LerpUnclamped(0f, 1f, eased);
                target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startRotation, finalRotation, eased));
                color.a = Mathf.Clamp01(t * 2.5f);
                image.color = color;
                yield return null;
            }

            target.anchoredPosition = finalPosition;
            target.localScale = Vector3.one;
            target.localRotation = Quaternion.Euler(0f, 0f, finalRotation);
            color.a = 1f;
            image.color = color;
        }

        private void CreateCamera()
        {
            GameObject cameraObject = new GameObject("LoadingCamera");
            cameraObject.transform.SetParent(transform, false);
            _loadingCamera = cameraObject.AddComponent<Camera>();
            _loadingCamera.clearFlags = CameraClearFlags.SolidColor;
            _loadingCamera.backgroundColor = new Color(0.035f, 0.01f, 0.06f, 1f);
            _loadingCamera.cullingMask = 0;
            _loadingCamera.depth = 100f;
        }

        private void CreateCanvas()
        {
            GameObject canvasObject = new GameObject("LoadingCanvas");
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform loadingRoot = CreateRect("LoadingRoot", canvasObject.transform);
            Stretch(loadingRoot);
            _loadingGroup = loadingRoot.gameObject.AddComponent<CanvasGroup>();

            Image background = CreateImage("Background", loadingRoot, null);
            Stretch(background.rectTransform);
            background.color = new Color(0.09f, 0.015f, 0.14f, 1f);

            LoadingBlinkFrames frameSettings = Resources.Load<LoadingBlinkFrames>("Loading/LoadingBlinkFrames");
            _blinkFrames = new[]
            {
                frameSettings.Open,
                frameSettings.HalfClosed,
                frameSettings.Closed,
                frameSettings.HalfClosed
            };

            _characterImage = CreateImage("BlinkingCharacter", loadingRoot, _blinkFrames[0]);
            RectTransform character = _characterImage.rectTransform;
            character.anchorMin = character.anchorMax = Vector2.zero;
            character.pivot = Vector2.zero;
            character.sizeDelta = new Vector2(400f, 400f);
            character.anchoredPosition = new Vector2(
                CharacterEdgePadding,
                CharacterEdgePadding + LoadingTextHeight);
            _characterImage.preserveAspect = true;

            GameObject loadingTextObject = new GameObject(
                "LoadingText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            loadingTextObject.transform.SetParent(loadingRoot, false);
            RectTransform loadingTextRect = (RectTransform)loadingTextObject.transform;
            loadingTextRect.anchorMin = loadingTextRect.anchorMax = Vector2.zero;
            loadingTextRect.pivot = Vector2.zero;
            loadingTextRect.sizeDelta = new Vector2(400f, LoadingTextHeight);
            loadingTextRect.anchoredPosition = new Vector2(CharacterEdgePadding, CharacterEdgePadding);

            TextMeshProUGUI loadingText = loadingTextObject.GetComponent<TextMeshProUGUI>();
            loadingText.font = frameSettings.Font;
            loadingText.text = "ロード中･･･";
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingText.fontStyle = FontStyles.Bold;
            loadingText.fontSize = 40f;
            loadingText.color = Color.white;
            loadingText.raycastTarget = false;

            _blackOverlay = CreateImage("TransitionBlack", canvasObject.transform, null);
            Stretch(_blackOverlay.rectTransform);
            _blackOverlay.color = new Color(0.035f, 0.01f, 0.06f, 0f);

            CreateGameStart(canvasObject.transform);
        }

        private void CreateGameStart(Transform parent)
        {
            RectTransform root = CreateRect("GameStartRoot", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.72f);
            root.sizeDelta = new Vector2(1100f, 330f);
            root.anchoredPosition = Vector2.zero;
            _gameStartGroup = root.gameObject.AddComponent<CanvasGroup>();
            _gameStartGroup.alpha = 0f;

            string[] names = { "S", "T", "A", "R", "T", "!" };
            string[] resources = { "UI_InGame_S", "UI_InGame_T", "UI_InGame_A", "UI_InGame_R", "UI_InGame_T", "UI_InGame_Exclamation" };

            _gameCharacters = new RectTransform[names.Length];
            float halfCharacterCount = (_gameCharacters.Length - 1) * 0.5f;
            const float arcRadius = 340f;
            const float arcAngleStep = 28f;
            const float centerGap = 36f;
            for (int i = 0; i < _gameCharacters.Length; i++)
            {
                float centeredIndex = i - halfCharacterCount;
                float angle = centeredIndex * arcAngleStep;
                float angleRadians = angle * Mathf.Deg2Rad;
                Vector2 position = new Vector2(
                    Mathf.Sin(angleRadians) * arcRadius + Mathf.Sign(centeredIndex) * centerGap * 0.5f,
                    (Mathf.Cos(angleRadians) - 1f) * arcRadius + 95f);

                Image image = CreateImage(names[i], root, Resources.Load<Sprite>(GameStartTextureRoot + resources[i]));
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(160f, 190f);
                rect.anchoredPosition = position;
                rect.localRotation = Quaternion.Euler(0f, 0f, -angle);
                image.preserveAspect = true;
                _gameCharacters[i] = rect;
            }
        }

        private void ResetBlinkAnimation()
        {
            _blinkFrameIndex = 0;
            _characterImage.sprite = _blinkFrames[_blinkFrameIndex];
            _nextBlinkFrameAt = Time.unscaledTime + OpenFrameDuration;
        }

        private static float GetFrameDuration(int frameIndex)
        {
            return frameIndex switch
            {
                0 => OpenFrameDuration,
                2 => ClosedFrameDuration,
                _ => HalfClosedFrameDuration
            };
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return (RectTransform)gameObject.transform;
        }

        private static Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static IEnumerator FadeImageRoutine(Image image, float from, float to, float duration)
        {
            Color color = image.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(from, to, SmoothStep(Mathf.Clamp01(elapsed / duration)));
                image.color = color;
                yield return null;
            }

            color.a = to;
            image.color = color;
        }

        private static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float value = t - 1f;
            return 1f + c3 * value * value * value + c1 * value * value;
        }
    }
}
