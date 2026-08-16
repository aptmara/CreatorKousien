using UnityEngine;
using UnityEngine.EventSystems;
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
        private Selectable _selectable;
        private Material _runtimeImageOutlineMaterial;
        private Material _originalGraphicMaterial;
        private Vector3 _baseScale;
        private Color _lastOutlineColor;
        private float _lastOutlineSize;
        private bool _navigationHighlighted;
        private bool _pointerInside;
        private bool _highlighted;
        private bool _initialized;

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

        private void Update()
        {
            InitializeVisuals();
            RefreshHighlightedState();
            ApplyVisualState();
            RefreshOutlineIfNeeded();
        }

        public static void SetPointerMode(bool pointerMode)
        {
            _pointerMode = pointerMode;
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
            return _pointerMode ? _pointerInside : _navigationHighlighted;
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
            if (_imageOutlineMaterial != null || !IsActive() || !_highlighted || graphic == null)
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
            }

            InitializeImageOutlineMaterial();
            _visualGraphics = _visualRoot.GetComponentsInChildren<Graphic>(true);
            _baseScale = _animationTarget.localScale;
            _initialized = true;
        }

        private void ApplyVisualState()
        {
            if (!_initialized)
            {
                return;
            }

            float scale;
            Color tint;
            if (_highlighted)
            {
                float maximumScale = Mathf.Max(_selectedMinScale, _selectedMaxScale);
                float curve = (Mathf.Sin(Time.unscaledTime * _animationSpeed) + 1f) * 0.5f;
                scale = Mathf.Lerp(_selectedMinScale, maximumScale, curve);
                tint = Color.white;
            }
            else
            {
                scale = _unselectedScale;
                tint = _unselectedTint;
            }

            _animationTarget.localScale = Vector3.Scale(_baseScale, new Vector3(scale, scale, scale));
            ApplyTint(tint);
        }

        private void ApplyTint(Color tint)
        {
            if (_visualGraphics == null)
            {
                return;
            }

            for (int i = 0; i < _visualGraphics.Length; i++)
            {
                _visualGraphics[i].canvasRenderer.SetColor(tint);
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
            if (_imageOutlineMaterial == null || graphic == null || _runtimeImageOutlineMaterial != null)
            {
                return;
            }

            _originalGraphicMaterial = graphic.material;
            _runtimeImageOutlineMaterial = new Material(_imageOutlineMaterial)
            {
                name = $"{_imageOutlineMaterial.name} ({name})",
                hideFlags = HideFlags.HideAndDontSave
            };
            graphic.material = _runtimeImageOutlineMaterial;
            UpdateImageOutlineMaterial();
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
        }

        private void ReleaseImageOutlineMaterial()
        {
            if (_runtimeImageOutlineMaterial == null)
            {
                return;
            }

            if (graphic != null && graphic.material == _runtimeImageOutlineMaterial)
            {
                graphic.material = _originalGraphicMaterial;
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
