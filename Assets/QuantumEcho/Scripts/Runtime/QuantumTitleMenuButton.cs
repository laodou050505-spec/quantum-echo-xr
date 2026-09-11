using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// World-space title action with an authored four-state response:
    /// idle, ray-hover, trigger-press and confirmed.  The artwork stays clean;
    /// only the live button treatment changes.
    /// </summary>
    public sealed class QuantumTitleMenuButton : MonoBehaviour
    {
        public enum ActionKind { Start, Exit }

        public ActionKind action;
        public Renderer backingRenderer;
        public Image backingImage;
        public Graphic focusRule;
        public bool isPrimary;
        public TMP_Text label;
        public LineRenderer focusLine;
        public float hoverScale = 1.035f;

        Material _material;
        Vector3 _idleScale;
        Color _idleColor;
        Color _hoverColor = new Color(0.20f, 1f, 0.95f, 1f);
        Color _pressedColor = new Color(1f, 0.68f, 0.22f, 1f);
        bool _hovered;
        float _pressedUntil;

        public bool IsHovered => _hovered;

        void Awake()
        {
            _idleScale = transform.localScale;
            if (backingImage != null) _idleColor = backingImage.color;
            if (backingRenderer != null)
            {
                _material = backingRenderer.material;
                _idleColor = ReadColor(_material, Color.white);
            }
            if (label != null) label.alpha = 1f;
            SetFocused(false);
        }

        void Update()
        {
            if (_pressedUntil > 0f && Time.unscaledTime >= _pressedUntil)
            {
                _pressedUntil = 0f;
                transform.localScale = _hovered ? _idleScale * hoverScale : _idleScale;
                SetColor(_hovered ? _hoverColor : _idleColor);
            }
        }

        public void SetFocused(bool focused)
        {
            _hovered = focused;
            if (_pressedUntil > 0f) return;
            transform.localScale = focused ? _idleScale * hoverScale : _idleScale;
            SetColor(focused ? _hoverColor : _idleColor);
            if (label != null) label.color = focused || isPrimary ? new Color(0.02f, 0.045f, 0.060f) : new Color(0.85f, 0.92f, 0.95f);
            if (focusLine != null) focusLine.enabled = focused;
            if (focusRule != null) focusRule.enabled = focused;
        }

        public void Press()
        {
            _pressedUntil = Time.unscaledTime + 0.12f;
            transform.localScale = _idleScale * 0.965f;
            SetColor(_pressedColor);
            if (label != null) label.color = new Color(0.02f, 0.045f, 0.060f);
        }

        void SetColor(Color color)
        {
            if (backingImage != null) backingImage.color = color;
            if (_material == null) return;
            if (_material.HasProperty("_Color")) _material.SetColor("_Color", color);
            if (_material.HasProperty("_EmissionColor")) _material.SetColor("_EmissionColor", color * 0.55f);
        }

        static Color ReadColor(Material material, Color fallback)
        {
            if (material == null || !material.HasProperty("_Color")) return fallback;
            return material.GetColor("_Color");
        }
    }
}
