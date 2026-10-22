using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuantumEcho
{
    /// <summary>
    /// v3: A world-space label with a holographic plaque backing.
    /// - Bottom plate: dark glass + cyan border drawn via QuantumHudPanelGraphic
    /// - Two text rows supported; auto-stacks with no overlap
    /// - Always faces the player camera (billboard)
    /// - Satisfies VR readability rule C: text cap-height >= d * 0.017 at 2.5m
    /// </summary>
    [ExecuteAlways]
    public sealed class QuantumHologramWorldLabel : MonoBehaviour
    {
        [Tooltip("Primary (larger) label line.")]
        public string primaryText;
        [Tooltip("Secondary label line. Leave blank for single-line plaque.")]
        public string secondaryText;
        [Tooltip("Accent color for the plaque border and primary text.")]
        public Color accentColor = new Color(0.18f, 0.93f, 1f, 0.92f);
        public TMP_FontAsset font;
        public bool billboard = true;

        // World-space font sizes chosen for VR rule C at 2.5 m:
        // cap height must be >= 2.5 * 0.017 = 0.043 m
        // TMP fontSize in world-units = physical height in meters at scale 1
        // Primary 0.12 world units => ~0.12 m cap height (well above threshold)
        // Secondary 0.085 => ~0.085 m (above threshold for secondary: 2.5 * 0.012 = 0.030 m)
        const float PrimarySize   = 0.12f;
        const float SecondarySize = 0.085f;
        const float PlatePaddingX = 0.18f;
        const float PlatePaddingY = 0.12f;

        TextMeshPro _primary;
        TextMeshPro _secondary;
        GameObject _plate;
        bool _built;

        void Awake() => Build();
        void OnEnable() { if (!_built) Build(); }

#if UNITY_EDITOR
        void OnValidate() { _built = false; Rebuild(); }
#endif

        void Build()
        {
            if (_built) return;
            _built = true;

            // 1. Plaque backing (Canvas in World Space)
            var plateGO = new GameObject("Plaque Backing", typeof(RectTransform),
                typeof(Canvas), typeof(QuantumHudPanelGraphic));
            plateGO.transform.SetParent(transform, false);
            plateGO.transform.localPosition = Vector3.zero;
            var plateCanvas = plateGO.GetComponent<Canvas>();
            plateCanvas.renderMode = RenderMode.WorldSpace;
            plateCanvas.sortingOrder = 10;
            var panel = plateGO.GetComponent<QuantumHudPanelGraphic>();
            panel.borderColor = accentColor;
            panel.accentColor = accentColor;
            panel.color = new Color(0.02f, 0.06f, 0.08f, 0.82f);
            panel.cutSize   = 8f;
            panel.borderWidth = 1.5f;
            _plate = plateGO;

            // 2. Primary text
            var primaryGO = new GameObject("Primary Text", typeof(RectTransform));
            primaryGO.transform.SetParent(transform, false);
            _primary = primaryGO.AddComponent<TextMeshPro>();
            _primary.font = font;
            _primary.fontSize = PrimarySize;
            _primary.color = accentColor;
            _primary.alignment = TextAlignmentOptions.Center;
            _primary.textWrappingMode = TextWrappingModes.NoWrap;
            _primary.overflowMode = TextOverflowModes.Overflow;
            _primary.characterSpacing = 3f;
            _primary.text = primaryText;

            // 3. Secondary text (optional)
            if (!string.IsNullOrWhiteSpace(secondaryText))
            {
                var secGO = new GameObject("Secondary Text", typeof(RectTransform));
                secGO.transform.SetParent(transform, false);
                _secondary = secGO.AddComponent<TextMeshPro>();
                _secondary.font = font;
                _secondary.fontSize = SecondarySize;
                _secondary.color = new Color(accentColor.r, accentColor.g, accentColor.b, accentColor.a * 0.70f);
                _secondary.alignment = TextAlignmentOptions.Center;
                _secondary.textWrappingMode = TextWrappingModes.NoWrap;
                _secondary.overflowMode = TextOverflowModes.Overflow;
                _secondary.characterSpacing = 2f;
                _secondary.text = secondaryText;
            }

            Rebuild();
        }

        void Rebuild()
        {
            if (_primary == null) { _built = false; Build(); return; }
            _primary.text = primaryText;
            if (_secondary != null) _secondary.text = secondaryText ?? string.Empty;

            bool hasSec = _secondary != null && !string.IsNullOrWhiteSpace(secondaryText);
            float totalHeight = hasSec
                ? PrimarySize + SecondarySize * 1.35f
                : PrimarySize;

            // Stack: primary on top, secondary below
            _primary.rectTransform.localPosition = new Vector3(0f, hasSec ? SecondarySize * 0.7f : 0f, 0.01f);
            if (_secondary != null)
                _secondary.rectTransform.localPosition = new Vector3(0f, -(PrimarySize * 0.55f), 0.01f);

            // Fit plate around text
            if (_plate != null)
            {
                // Estimate text width from longest line (approximate at 0.065 m per char @ 0.12 size)
                float maxLen = Mathf.Max(
                    (primaryText ?? string.Empty).Length,
                    hasSec ? (secondaryText ?? string.Empty).Length * (SecondarySize / PrimarySize) : 0f);
                float plateW = Mathf.Max(0.5f, maxLen * PrimarySize * 0.60f) + PlatePaddingX * 2f;
                float plateH = totalHeight + PlatePaddingY * 2f;
                var plateRect = _plate.GetComponent<RectTransform>();
                plateRect.sizeDelta = new Vector2(plateW, plateH);
                plateRect.localScale = Vector3.one;
                plateRect.localPosition = new Vector3(0f, 0f, -0.005f);
            }
        }

        void LateUpdate()
        {
            if (!billboard) return;
            var cam = Camera.main;
            if (cam == null) return;
            // Billboard: face camera, keep up-right
            var dir = transform.position - cam.transform.position;
            if (dir.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }
}
