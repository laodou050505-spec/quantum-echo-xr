using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuantumEcho
{
    /// <summary>Applies the editable instrument-panel skin to an existing HUD hierarchy.</summary>
    public sealed class QuantumHudSkin : MonoBehaviour
    {
        static readonly Color Cyan = new Color(0.18f, 0.93f, 1f, 0.92f);
        static readonly Color Magenta = new Color(0.77f, 0.32f, 1f, 0.92f);
        static readonly Color Amber = new Color(1f, 0.68f, 0.18f, 0.96f);

        public void ApplySkin()
        {
            Configure("Brand", Cyan, 14f, 0.72f);
            Configure("Level Tabs", Cyan, 12f, 0.78f);
            Configure("Loop", Magenta, 10f, 0.74f);
            Configure("Objective", Cyan, 18f, 0.82f);
            Configure("Timer", Amber, 14f, 0.82f);
            Configure("Echo Rail", Magenta, 12f, 0.78f);
            Configure("Context Prompt", Cyan, 16f, 0.86f);
            Configure("Hold Progress", Amber, 14f, 0.9f);
            Configure("Result Overlay", Cyan, 22f, 0.94f);

            foreach (var text in GetComponentsInChildren<TMP_Text>(true))
            {
                text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.Normal;
                text.overflowMode = TextOverflowModes.Truncate;
                if (text.name.Contains("Code") || text.name.Contains("Index") || text.name.Contains("Value")) text.color = Cyan;
                if (text.name.Contains("Count")) text.color = Magenta;
            }
        }

        void Configure(string objectName, Color accent, float cut, float opacity)
        {
            var target = FindChild(transform, objectName);
            if (target == null) return;
            var oldImage = target.GetComponent<Image>();
            if (oldImage != null)
            {
                QuantumGeneratedPanelSkin.Apply(oldImage, opacity);
            }

            var visual = target.Find("Instrument Panel Visual");
            if (visual == null)
            {
                var visualObject = new GameObject("Instrument Panel Visual", typeof(RectTransform));
                visualObject.transform.SetParent(target, false);
                visual = visualObject.transform;
            }
            visual.SetAsFirstSibling();
            var visualRect = visual as RectTransform;
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = Vector2.zero;
            visualRect.offsetMax = Vector2.zero;

            var panel = visual.GetComponent<QuantumHudPanelGraphic>();
            if (panel != null) panel.enabled = false;
        }

        static Transform FindChild(Transform root, string objectName)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName) return child;
            }
            return null;
        }
    }

}
