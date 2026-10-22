using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// Replaces a plain UI background with the generated Quantum hologram panel.
    /// v3: Updated to holographic cyan-border design with corner ornaments and scan lines.
    /// The generated image contains no text; TMP remains fully editable and crisp.
    /// </summary>
    public static class QuantumGeneratedPanelSkin
    {
        // v3: PanelV3 hologram panel generated 2026-10-22 (Tripo banana2)
        const string ResourcePath = "QuantumEcho/QuantumHologramPanel";
        // Fallback to v1 panel if new asset not yet imported
        const string FallbackPath = "QuantumEcho/QuantumPhasePanel";
        static Sprite _panelSprite;

        public static Sprite PanelSprite
        {
            get
            {
                if (_panelSprite == null)
                {
                    _panelSprite = Resources.Load<Sprite>(ResourcePath);
                    if (_panelSprite == null)
                        _panelSprite = Resources.Load<Sprite>(FallbackPath);
                }
                return _panelSprite;
            }
        }

        public static void Apply(Image image, float opacity = 1f)
        {
            if (image == null || PanelSprite == null) return;
            image.enabled = true;
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            // v3: tighter PPU so the border ornaments scale correctly on the narrow HUD panel
            image.pixelsPerUnitMultiplier = 4f;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            image.raycastTarget = false;
        }
    }
}
