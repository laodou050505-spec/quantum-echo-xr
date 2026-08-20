using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>Editable cut-corner glass panel with two phase-channel indicator rails.</summary>
    [ExecuteAlways]
    public sealed class QuantumHudPanelGraphic : MaskableGraphic
    {
        public Color borderColor = new Color(0.18f, 0.93f, 1f, 0.9f);
        public Color accentColor = new Color(0.18f, 0.93f, 1f, 1f);
        [Min(0f)] public float cutSize = 14f;
        [Min(0.5f)] public float borderWidth = 2f;
        [Min(8f)] public float accentLength = 46f;
        [Min(1f)] public float accentWidth = 4f;

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            var rect = GetPixelAdjustedRect();
            var cut = Mathf.Min(cutSize, Mathf.Min(rect.width, rect.height) * 0.3f);
            var inset = Mathf.Min(borderWidth, Mathf.Min(rect.width, rect.height) * 0.15f);
            AddQuad(vertexHelper,
                new Rect(rect.xMin + inset, rect.yMin + inset, rect.width - inset * 2f, rect.height - inset * 2f),
                color);
            AddQuad(vertexHelper, new Rect(rect.xMin + cut, rect.yMax - borderWidth, rect.width - cut * 2f, borderWidth), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMin, rect.yMin + cut, borderWidth, rect.height - cut * 2f), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMin + cut, rect.yMin, rect.width - cut * 2f, borderWidth), borderColor);
            AddQuad(vertexHelper, new Rect(rect.xMax - borderWidth, rect.yMin + cut, borderWidth, rect.height - cut * 2f), borderColor);

            var railLength = Mathf.Min(accentLength, Mathf.Max(8f, rect.width * 0.32f));
            AddQuad(vertexHelper, new Rect(rect.xMin + cut + 8f, rect.yMax - accentWidth - 5f, railLength, accentWidth), accentColor);
            AddQuad(vertexHelper,
                new Rect(rect.xMax - cut - railLength - 8f, rect.yMin + 5f, railLength * 0.56f, accentWidth * 0.55f),
                new Color(accentColor.r, accentColor.g, accentColor.b, accentColor.a * 0.62f));
        }

        static void AddVertex(VertexHelper helper, Vector2 position, Color vertexColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            helper.AddVert(vertex);
        }

        static void AddQuad(VertexHelper helper, Rect rect, Color quadColor)
        {
            var start = helper.currentVertCount;
            AddVertex(helper, new Vector2(rect.xMin, rect.yMin), quadColor);
            AddVertex(helper, new Vector2(rect.xMin, rect.yMax), quadColor);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMax), quadColor);
            AddVertex(helper, new Vector2(rect.xMax, rect.yMin), quadColor);
            helper.AddTriangle(start, start + 1, start + 2);
            helper.AddTriangle(start, start + 2, start + 3);
        }
    }
}
