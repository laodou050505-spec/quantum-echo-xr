using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// Replaces a plain UI background with the generated Quantum hologram panel.
    /// v4: PIL 程序化绘制干净底板——深藏青 85% 不透明 + 1px 青色细边 + 顶部高光，真 alpha，无棋盘格。
    /// The generated image contains no text; TMP remains fully editable and crisp.
    /// </summary>
    public static class QuantumGeneratedPanelSkin
    {
        // v4: PIL 绘制干净面板（2026-10-23），替换含棋盘格假透明的 v3 生成图
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
            // v4: Simple 模式——新底板已有真 alpha，不需要 9-slice；Simple 直接拉伸不会暴露棋盘格
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            image.raycastTarget = false;
        }
    }
}
