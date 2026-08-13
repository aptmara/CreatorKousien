using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Presentation.UI.Pause
{
    [DisallowMultipleComponent]
    public sealed class PauseSelectionOutline : BaseMeshEffect, ISelectHandler, IDeselectHandler
    {
        private const float OutlineThickness = 4f;

        [SerializeField] private Color _outlineColor = Color.white;

        private bool _highlighted;

        public void OnSelect(BaseEventData eventData)
        {
            SetHighlighted(true);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            SetHighlighted(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted)
            {
                return;
            }

            _highlighted = highlighted;
            graphic?.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vertexHelper)
        {
            if (!IsActive() || !_highlighted || graphic == null)
            {
                return;
            }

            Rect rect = graphic.rectTransform.rect;
            float left = rect.xMin - OutlineThickness;
            float right = rect.xMax + OutlineThickness;
            float bottom = rect.yMin - OutlineThickness;
            float top = rect.yMax + OutlineThickness;

            AddQuad(vertexHelper, new Vector2(left, top - OutlineThickness), new Vector2(right, top), _outlineColor);
            AddQuad(vertexHelper, new Vector2(left, bottom), new Vector2(right, bottom + OutlineThickness), _outlineColor);
            AddQuad(vertexHelper, new Vector2(left, bottom + OutlineThickness), new Vector2(left + OutlineThickness, top - OutlineThickness), _outlineColor);
            AddQuad(vertexHelper, new Vector2(right - OutlineThickness, bottom + OutlineThickness), new Vector2(right, top - OutlineThickness), _outlineColor);
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
