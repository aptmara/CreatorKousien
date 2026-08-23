using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseSelectionOutline : BaseMeshEffect, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private static bool _pointerMode;

        [SerializeField] private Color _outlineColor = Color.white;
        [SerializeField, Min(0f)] private float _outlineSize = 4f;
        [SerializeField] private Color _unselectedTint = new(0.62f, 0.62f, 0.62f, 1f);
        [SerializeField, Min(0f)] private float _unselectedScale = 0.9f;
        [SerializeField, Min(0f)] private float _selectedMinScale = 1f;
        [SerializeField, Min(0f)] private float _selectedMaxScale = 1.06f;
        [SerializeField, Min(0f)] private float _animationSpeed = 4f;
        [SerializeField] private RectTransform _animationTarget;
        [SerializeField] private Transform _visualRoot;
        [SerializeField] private Material _imageOutlineMaterial;

        private Graphic[] _visualGraphics;
        private Color[] _visualBaseColors;
        private Selectable _selectable;
        private Image _sourceImage;
        private Image _outlineImage;
        private RectTransform _outlineRectTransform;
        private Canvas _canvas;
        private Material _runtimeImageOutlineMaterial;
        private readonly Vector3[] _worldCorners = new Vector3[4];
        private Vector3 _baseScale;
        private Color _lastOutlineColor;
        private float _lastOutlineSize;
        private bool _navigationHighlighted;
        private bool _pointerInside;
        private bool _highlighted;
        private bool _initialized;
        private bool _scalingEnabled = true;
        private bool _useInstancePointerMode;
        private bool _instancePointerMode;
        private bool _followExternalScaleWhileNotInteractable;
        private bool _wasInteractable = true;

        public bool IsHighlighted => _highlighted;

        protected override void OnEnable()
        {
            base.OnEnable();
            InitializeVisuals();
            _navigationHighlighted = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            _pointerInside = false;
            _highlighted = ShouldHighlight();
            ApplyVisualState();
            CacheOutlineSettings();
            UpdateImageOutlineMaterial();
            graphic?.SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            if (_initialized && _animationTarget != null)
            {
                _animationTarget.localScale = _baseScale;
                ApplyTint(Color.white);
            }

            _navigationHighlighted = false;
            _pointerInside = false;
            _highlighted = false;
            UpdateImageOutlineMaterial();
            base.OnDisable();
        }

        protected override void OnDestroy()
        {
            ReleaseImageOutlineMaterial();
            base.OnDestroy();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            _outlineSize = Mathf.Max(0f, _outlineSize);
            UpdateImageOutlineMaterial();
            graphic?.SetVerticesDirty();
            graphic?.SetMaterialDirty();
        }

        private void Update()
        {
            InitializeVisuals();
            SyncImageOutlineGraphic();
            RefreshHighlightedState();
            ApplyVisualState();
            RefreshOutlineIfNeeded();
        }

        public static void SetPointerMode(bool pointerMode)
        {
            _pointerMode = pointerMode;
        }

        public void Configure(
            Color outlineColor,
            float outlineSize,
            Color unselectedTint,
            float unselectedScale,
            float selectedMinScale,
            float selectedMaxScale,
            float animationSpeed,
            Material imageOutlineMaterial,
            bool followExternalScaleWhileNotInteractable)
        {
            if (_initialized && _animationTarget != null)
            {
                _animationTarget.localScale = _baseScale;
                ApplyTint(Color.white);
            }

            ReleaseImageOutlineMaterial();
            _outlineColor = outlineColor;
            _outlineSize = Mathf.Max(0f, outlineSize);
            _unselectedTint = unselectedTint;
            _unselectedScale = Mathf.Max(0f, unselectedScale);
            _selectedMinScale = Mathf.Max(0f, selectedMinScale);
            _selectedMaxScale = Mathf.Max(0f, selectedMaxScale);
            _animationSpeed = Mathf.Max(0f, animationSpeed);
            _imageOutlineMaterial = imageOutlineMaterial;
            _followExternalScaleWhileNotInteractable = followExternalScaleWhileNotInteractable;
            _useInstancePointerMode = true;
            _initialized = false;
            InitializeVisuals();
            ApplyVisualState();
            CacheOutlineSettings();
            UpdateImageOutlineMaterial();
            graphic?.SetVerticesDirty();
        }

        public void SetInstancePointerMode(bool pointerMode)
        {
            _useInstancePointerMode = true;
            _instancePointerMode = pointerMode;
            RefreshHighlightedState();
        }

        public void OnSelect(BaseEventData eventData)
        {
            SetHighlighted(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetHighlighted(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _pointerInside = true;
            RefreshHighlightedState();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerInside = false;
            RefreshHighlightedState();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_navigationHighlighted == highlighted)
            {
                return;
            }

            _navigationHighlighted = highlighted;
            RefreshHighlightedState();
        }

        internal void SetScalingEnabled(bool enabled)
        {
            _scalingEnabled = enabled;
            if (_initialized && _animationTarget != null)
            {
                _animationTarget.localScale = _baseScale;
                ApplyVisualState();
            }
        }

        private void RefreshHighlightedState()
        {
            bool highlighted = ShouldHighlight();
            if (_highlighted == highlighted)
            {
                return;
            }

            _highlighted = highlighted;
            ApplyVisualState();
            UpdateImageOutlineMaterial();
            graphic?.SetVerticesDirty();
        }

        private bool ShouldHighlight()
        {
            bool pointerMode = _useInstancePointerMode ? _instancePointerMode : _pointerMode;
            return pointerMode ? _pointerInside : _navigationHighlighted;
        }

        public void SetVisualTargets(RectTransform animationTarget, Transform visualRoot)
        {
            if (_initialized && _animationTarget != null)
            {
                _animationTarget.localScale = _baseScale;
                ApplyTint(Color.white);
            }

            _animationTarget = animationTarget;
            _visualRoot = visualRoot;
            _initialized = false;
            InitializeVisuals();
            ApplyVisualState();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (_outlineImage != null || !IsActive() || !_highlighted || graphic == null)
            {
                return;
            }

            float outlineSize = Mathf.Max(0f, _outlineSize);
            if (outlineSize <= 0f)
            {
                return;
            }

            Rect rect = graphic.rectTransform.rect;
            float left = rect.xMin - outlineSize;
            float right = rect.xMax + outlineSize;
            float bottom = rect.yMin - outlineSize;
            float top = rect.yMax + outlineSize;

            AddQuad(vertexHelper, new Vector2(left, top - outlineSize), new Vector2(right, top), _outlineColor);
            AddQuad(vertexHelper, new Vector2(left, bottom), new Vector2(right, bottom + outlineSize), _outlineColor);
            AddQuad(vertexHelper, new Vector2(left, bottom + outlineSize), new Vector2(left + outlineSize, top - outlineSize), _outlineColor);
            AddQuad(vertexHelper, new Vector2(right - outlineSize, bottom + outlineSize), new Vector2(right, top - outlineSize), _outlineColor);
        }

        private void InitializeVisuals()
        {
            if (_initialized)
            {
                return;
            }

            if (_animationTarget == null)
            {
                _animationTarget = (RectTransform)transform;
            }

            if (_visualRoot == null)
            {
                _visualRoot = transform;
            }

            _selectable = GetComponent<Selectable>();
            if (_selectable != null)
            {
                _selectable.transition = Selectable.Transition.None;
                _wasInteractable = _selectable.interactable;
            }

            InitializeImageOutlineMaterial();
            _visualGraphics = _visualRoot.GetComponentsInChildren<Graphic>(true);
            _visualBaseColors = new Color[_visualGraphics.Length];
            for (int i = 0; i < _visualGraphics.Length; i++)
            {
                _visualBaseColors[i] = _visualGraphics[i].canvasRenderer.GetColor();
            }

            _baseScale = _animationTarget.localScale;
            _initialized = true;
        }

        private void ApplyVisualState()
        {
            if (!_initialized)
            {
                return;
            }

            if (_followExternalScaleWhileNotInteractable && _selectable != null)
            {
                if (!_selectable.interactable)
                {
                    _baseScale = _animationTarget.localScale;
                    _wasInteractable = false;
                    ApplyTint(Color.white);
                    return;
                }

                if (!_wasInteractable)
                {
                    _baseScale = _animationTarget.localScale;
                    _wasInteractable = true;
                }
            }

            float scale = 1f;
            Color tint;
            if (_highlighted)
            {
                if (_scalingEnabled)
                {
                    float maximumScale = Mathf.Max(_selectedMinScale, _selectedMaxScale);
                    float curve = (Mathf.Sin(Time.unscaledTime * _animationSpeed) + 1f) * 0.5f;
                    scale = Mathf.Lerp(_selectedMinScale, maximumScale, curve);
                }

                tint = Color.white;
            }
            else
            {
                if (_scalingEnabled)
                {
                    scale = _unselectedScale;
                }

                tint = _unselectedTint;
            }

            _animationTarget.localScale = Vector3.Scale(_baseScale, new Vector3(scale, scale, scale));
            ApplyTint(tint);
        }

        private void ApplyTint(Color tint)
        {
            if (_visualGraphics == null || _visualBaseColors == null)
            {
                return;
            }

            for (int i = 0; i < _visualGraphics.Length; i++)
            {
                Color baseColor = _visualBaseColors[i];
                _visualGraphics[i].canvasRenderer.SetColor(new Color(
                    baseColor.r * tint.r,
                    baseColor.g * tint.g,
                    baseColor.b * tint.b,
                    baseColor.a * tint.a));
            }
        }

        private void RefreshOutlineIfNeeded()
        {
            if (Mathf.Approximately(_lastOutlineSize, _outlineSize) && _lastOutlineColor == _outlineColor)
            {
                return;
            }

            CacheOutlineSettings();
            UpdateImageOutlineMaterial();
            graphic?.SetVerticesDirty();
        }

        private void CacheOutlineSettings()
        {
            _lastOutlineSize = _outlineSize;
            _lastOutlineColor = _outlineColor;
        }

        private void InitializeImageOutlineMaterial()
        {
            if (_imageOutlineMaterial == null || _runtimeImageOutlineMaterial != null)
            {
                return;
            }

            Image sourceImage = _selectable != null ? _selectable.targetGraphic as Image : null;
            if (sourceImage == null)
            {
                sourceImage = graphic as Image;
            }

            if (sourceImage == null)
            {
                return;
            }

            _sourceImage = sourceImage;
            _canvas = sourceImage.canvas;
            _runtimeImageOutlineMaterial = new Material(_imageOutlineMaterial)
            {
                name = $"{_imageOutlineMaterial.name} ({name})",
                hideFlags = HideFlags.HideAndDontSave
            };

            GameObject outlineObject = new("SelectionOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            outlineObject.hideFlags = HideFlags.HideAndDontSave;
            outlineObject.layer = gameObject.layer;
            outlineObject.transform.SetParent(sourceImage.transform, false);
            outlineObject.transform.SetAsFirstSibling();

            _outlineRectTransform = (RectTransform)outlineObject.transform;
            _outlineRectTransform.anchorMin = Vector2.zero;
            _outlineRectTransform.anchorMax = Vector2.one;
            _outlineRectTransform.pivot = sourceImage.rectTransform.pivot;
            _outlineRectTransform.localScale = Vector3.one;

            _outlineImage = outlineObject.GetComponent<Image>();
            _outlineImage.raycastTarget = false;
            _outlineImage.maskable = sourceImage.maskable;
            _outlineImage.useSpriteMesh = false;
            _outlineImage.color = Color.white;
            _outlineImage.material = _runtimeImageOutlineMaterial;
            SyncImageOutlineGraphic();
            UpdateImageOutlineMaterial();
        }

        private void SyncImageOutlineGraphic()
        {
            if (_sourceImage == null || _outlineImage == null || _outlineRectTransform == null)
            {
                return;
            }

            Sprite sprite = _sourceImage.overrideSprite;
            _outlineImage.enabled = _sourceImage.enabled && sprite != null;
            if (sprite == null)
            {
                return;
            }

            if (_outlineImage.sprite != sprite)
            {
                _outlineImage.sprite = sprite;
            }

            _outlineImage.type = Image.Type.Simple;
            _outlineImage.preserveAspect = false;

            Vector2 padding = CalculateLocalOutlinePadding();
            _outlineRectTransform.offsetMin = new Vector2(-padding.x, -padding.y);
            _outlineRectTransform.offsetMax = new Vector2(padding.x, padding.y);

            Rect sourceRect = _sourceImage.rectTransform.rect;
            Vector4 spriteUV = DataUtility.GetOuterUV(sprite);
            _runtimeImageOutlineMaterial.SetVector("_SpriteUVRect", spriteUV);
            _runtimeImageOutlineMaterial.SetVector("_OriginalSize", new Vector4(sourceRect.width, sourceRect.height, 0f, 0f));
            _runtimeImageOutlineMaterial.SetVector("_OutlinePadding", new Vector4(padding.x, padding.y, 0f, 0f));
        }

        private Vector2 CalculateLocalOutlinePadding()
        {
            float screenPadding = Mathf.Max(0f, _outlineSize) + 1f;
            RectTransform sourceRectTransform = _sourceImage.rectTransform;
            Rect sourceRect = sourceRectTransform.rect;
            sourceRectTransform.GetWorldCorners(_worldCorners);

            Camera canvasCamera = _canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera
                : null;
            Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, _worldCorners[0]);
            Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, _worldCorners[1]);
            Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, _worldCorners[3]);

            float pixelsPerLocalX = sourceRect.width > 0f
                ? Vector2.Distance(bottomLeft, bottomRight) / sourceRect.width
                : 1f;
            float pixelsPerLocalY = sourceRect.height > 0f
                ? Vector2.Distance(bottomLeft, topLeft) / sourceRect.height
                : 1f;

            return new Vector2(
                screenPadding / Mathf.Max(pixelsPerLocalX, 0.0001f),
                screenPadding / Mathf.Max(pixelsPerLocalY, 0.0001f));
        }

        private void UpdateImageOutlineMaterial()
        {
            if (_runtimeImageOutlineMaterial == null)
            {
                return;
            }

            _runtimeImageOutlineMaterial.SetColor("_OutlineColor", _outlineColor);
            _runtimeImageOutlineMaterial.SetFloat("_OutlineSize", Mathf.Max(0f, _outlineSize));
            _runtimeImageOutlineMaterial.SetFloat("_OutlineEnabled", _highlighted ? 1f : 0f);
            SyncImageOutlineGraphic();
            _outlineImage?.SetMaterialDirty();
        }

        private void ReleaseImageOutlineMaterial()
        {
            if (_runtimeImageOutlineMaterial == null)
            {
                return;
            }

            if (_outlineImage != null)
            {
                _outlineImage.material = null;
            }

            if (Application.isPlaying)
            {
                Destroy(_runtimeImageOutlineMaterial);
            }
            else
            {
                DestroyImmediate(_runtimeImageOutlineMaterial);
            }

            _runtimeImageOutlineMaterial = null;

            if (_outlineImage != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(_outlineImage.gameObject);
                }
                else
                {
                    DestroyImmediate(_outlineImage.gameObject);
                }
            }

            _outlineImage = null;
            _outlineRectTransform = null;
            _sourceImage = null;
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 min, Vector2 max, Color color)
        {
            int startIndex = vertexHelper.currentVertCount;
            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;
            vertex.uv0 = new Vector4(0.5f, 0.5f, 0f, 0f);

            vertex.position = new Vector3(min.x, min.y);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(min.x, max.y);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(max.x, max.y);
            vertexHelper.AddVert(vertex);
            vertex.position = new Vector3(max.x, min.y);
            vertexHelper.AddVert(vertex);

            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
    }
}
