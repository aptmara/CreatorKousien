using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.UI.Loading
{
    public sealed class LoadingView : MonoBehaviour
    {
        private const string TextureRoot = "Textures/Title/UI_Title_Logo/";

        private readonly List<DecorationState> _decorations = new();
        private Camera _loadingCamera;
        private CanvasGroup _loadingGroup;
        private Image _blackOverlay;
        private RectTransform _logo;
        private CanvasGroup _gameStartGroup;
        private RectTransform[] _gameCharacters;
        private float _logoAngle;
        private bool _animateLoading = true;

        private sealed class DecorationState
        {
            public RectTransform Transform;
            public Vector2 BasePosition;
            public Vector3 BaseScale;
            public float Phase;
            public float FloatHeight;
            public float FloatSpeed;
        }

        public void Initialize()
        {
            CreateCamera();
            CreateCanvas();
        }

        private void Update()
        {
            if (!_animateLoading)
            {
                return;
            }

            float phase = Mathf.Repeat(_logoAngle, 360f) / 360f;
            float speed = Mathf.Lerp(45f, 230f, Mathf.Sin(phase * Mathf.PI) * Mathf.Sin(phase * Mathf.PI));
            _logoAngle = Mathf.Repeat(_logoAngle - speed * Time.unscaledDeltaTime, 360f);
            _logo.localRotation = Quaternion.Euler(0f, 0f, _logoAngle);

            float time = Time.unscaledTime;
            foreach (DecorationState decoration in _decorations)
            {
                float wave = Mathf.Sin(time * decoration.FloatSpeed + decoration.Phase);
                decoration.Transform.anchoredPosition = decoration.BasePosition + Vector2.up * wave * decoration.FloatHeight;
                decoration.Transform.localScale = decoration.BaseScale * (1f + wave * 0.035f);
            }
        }

        public IEnumerator PlayGameStartRoutine()
        {
            yield return StopLogoRoutine(0.45f);

            _blackOverlay.color = new Color(0.035f, 0.01f, 0.06f, 0f);
            yield return FadeImageRoutine(_blackOverlay, 0f, 1f, 0.25f);
            _loadingGroup.alpha = 0f;
            _loadingCamera.enabled = false;
            yield return FadeImageRoutine(_blackOverlay, 1f, 0f, 0.45f);

            _gameStartGroup.alpha = 1f;
            for (int i = 0; i < _gameCharacters.Length; i++)
            {
                StartCoroutine(PopCharacterRoutine(_gameCharacters[i], i));
                yield return new WaitForSecondsRealtime(0.1f);
            }

            yield return new WaitForSecondsRealtime(0.65f);

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

        private IEnumerator StopLogoRoutine(float duration)
        {
            _animateLoading = false;
            float startAngle = _logoAngle;
            float targetAngle = Mathf.Floor(startAngle / 360f) * 360f - 360f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                _logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(startAngle, targetAngle, 1f - Mathf.Pow(1f - t, 3f)));
                yield return null;
            }

            _logo.localRotation = Quaternion.identity;
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
            float startRotation = finalRotation + Mathf.Lerp(-10f, 10f, index / 3f);
            Image image = target.GetComponent<Image>();
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            target.anchoredPosition = startPosition;
            target.localScale = Vector3.zero;
            target.localRotation = Quaternion.Euler(0f, 0f, startRotation);

            float elapsed = 0f;
            const float duration = 0.42f;
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

            CreateDecorations(loadingRoot);

            Image logoImage = CreateImage("LoadingLogo", loadingRoot, LoadSprite("UI_Title_Logo"));
            _logo = logoImage.rectTransform;
            _logo.anchorMin = _logo.anchorMax = new Vector2(0.5f, 0.52f);
            _logo.sizeDelta = new Vector2(620f, 350f);
            _logo.anchoredPosition = Vector2.zero;
            logoImage.preserveAspect = true;

            _blackOverlay = CreateImage("TransitionBlack", canvasObject.transform, null);
            Stretch(_blackOverlay.rectTransform);
            _blackOverlay.color = new Color(0.035f, 0.01f, 0.06f, 0f);

            CreateGameStart(canvasObject.transform);
        }

        private void CreateDecorations(RectTransform parent)
        {
            CreateDecoration(parent, "UI_Title_Pumpkin", new Vector2(-760f, -330f), new Vector2(260f, 260f), -14f, 0.2f, 20f, 1.2f);
            CreateDecoration(parent, "UI_Title_Apple", new Vector2(760f, -350f), new Vector2(300f, 300f), 13f, 1.1f, 24f, 1.05f);
            CreateDecoration(parent, "UI_Title_Ghost", new Vector2(-770f, 340f), new Vector2(230f, 230f), 9f, 2.2f, 28f, 0.9f);
            CreateDecoration(parent, "UI_Title_Candy", new Vector2(780f, 330f), new Vector2(220f, 220f), -10f, 3.1f, 22f, 1.25f);
            CreateDecoration(parent, "UI_Title_Ice", new Vector2(-470f, -430f), new Vector2(180f, 180f), 16f, 4.2f, 18f, 1.4f);
            CreateDecoration(parent, "UI_Title_Soul1", new Vector2(500f, 390f), new Vector2(150f, 150f), -8f, 5.1f, 34f, 0.8f);
            CreateDecoration(parent, "UI_Title_Soul2", new Vector2(390f, -390f), new Vector2(130f, 130f), 7f, 0.8f, 30f, 1.0f);
        }

        private void CreateDecoration(
            RectTransform parent,
            string spriteName,
            Vector2 position,
            Vector2 size,
            float rotation,
            float phase,
            float floatHeight,
            float floatSpeed)
        {
            Image image = CreateImage(spriteName, parent, LoadSprite(spriteName));
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.72f);

            _decorations.Add(new DecorationState
            {
                Transform = rect,
                BasePosition = position,
                BaseScale = Vector3.one,
                Phase = phase,
                FloatHeight = floatHeight,
                FloatSpeed = floatSpeed
            });
        }

        private void CreateGameStart(Transform parent)
        {
            RectTransform root = CreateRect("GameStartRoot", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.72f);
            root.sizeDelta = new Vector2(920f, 330f);
            root.anchoredPosition = Vector2.zero;
            _gameStartGroup = root.gameObject.AddComponent<CanvasGroup>();
            _gameStartGroup.alpha = 0f;

            string[] names = { "G", "A", "M", "E" };
            string[] resources = { "UI_Clear_G", "UI_Clear_A", "UI_Clear_M", "UI_Clear_E" };
            Vector2[] positions =
            {
                new Vector2(-300f, -35f),
                new Vector2(-105f, 30f),
                new Vector2(105f, 30f),
                new Vector2(300f, -35f)
            };
            float[] rotations = { -12f, -4f, 4f, 12f };

            _gameCharacters = new RectTransform[4];
            for (int i = 0; i < _gameCharacters.Length; i++)
            {
                Image image = CreateImage(names[i], root, Resources.Load<Sprite>($"Textures/GAMECLEAR/char/{resources[i]}"));
                RectTransform rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(190f, 190f);
                rect.anchoredPosition = positions[i];
                rect.localRotation = Quaternion.Euler(0f, 0f, rotations[i]);
                image.preserveAspect = true;
                _gameCharacters[i] = rect;
            }
        }

        private static Sprite LoadSprite(string name)
        {
            return Resources.Load<Sprite>(TextureRoot + name);
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
