using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// Replaces a plain UI background with the generated Quantum Phase panel.
    /// The generated image contains no text; TMP remains fully editable and crisp.
    /// </summary>
    public static class QuantumGeneratedPanelSkin
    {
        const string ResourcePath = "QuantumEcho/QuantumPhasePanel";
        static Sprite _panelSprite;

        public static Sprite PanelSprite
        {
            get
            {
                if (_panelSprite == null) _panelSprite = Resources.Load<Sprite>(ResourcePath);
                return _panelSprite;
            }
        }

        public static void Apply(Image image, float opacity = 1f)
        {
            if (image == null || PanelSprite == null) return;
            image.enabled = true;
            image.sprite = PanelSprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 6f;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            image.raycastTarget = false;
        }
    }
}
