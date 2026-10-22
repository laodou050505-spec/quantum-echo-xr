using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace QuantumEcho
{
    /// <summary>
    /// World-space title action with an authored four-state response:
    /// idle, ray-hover, trigger-press and confirmed.
    /// v3: Adds haptic feedback and sci-fi UI audio on hover and press.
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
        [Header("v3: Sci-fi Audio")]
        public AudioSource audioSource;
        public AudioClip hoverClip;
        public AudioClip confirmClip;

        Material _material;
        Vector3 _idleScale;
        Color _idleColor;
        Color _hoverColor = new Color(0.20f, 1f, 0.95f, 1f);
        Color _pressedColor = new Color(1f, 0.68f, 0.22f, 1f);
        bool _hovered;
        bool _previousHovered;
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
            var wasHovered = _hovered;
            _hovered = focused;
            if (_pressedUntil > 0f) return;
            transform.localScale = focused ? _idleScale * hoverScale : _idleScale;
            SetColor(focused ? _hoverColor : _idleColor);
            // 全息深色底：常态用淡青白（可见），悬停时用纯白（更亮）
            if (label != null) label.color = focused ? new Color(0.98f, 1f, 1f, 1f) : new Color(0.78f, 0.95f, 1f, 1f);
            if (focusLine != null) focusLine.enabled = focused;
            if (focusRule != null) focusRule.enabled = focused;
            // v3: play hover sound and gentle haptic on new hover entry
            if (focused && !wasHovered)
            {
                PlayClip(hoverClip, 0.55f);
                SendHaptic(0.15f, 0.020f);
            }
        }

        public void Press()
        {
            _pressedUntil = Time.unscaledTime + 0.12f;
            transform.localScale = _idleScale * 0.965f;
            SetColor(_pressedColor);
            if (label != null) label.color = new Color(1f, 0.82f, 0.30f, 1f);  // 琥珀色闪光（深底可见）
            // v3: play confirm sound and stronger haptic on press
            PlayClip(confirmClip, 0.85f);
            SendHaptic(0.40f, 0.040f);
        }

        // v3: play audio through shared AudioSource on parent canvas/rig
        void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (audioSource != null)
            {
                audioSource.PlayOneShot(clip, volume);
                return;
            }
            // Fallback: find or create an AudioSource anywhere in the scene
            var go = new GameObject("~TitleAudio");
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            src.PlayOneShot(clip, volume);
            Destroy(go, clip.length + 0.2f);
        }

        // v3: brief haptic on both controllers (PICO/OpenXR)
        static void SendHaptic(float amplitude, float duration)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (left.isValid) left.SendHapticImpulse(0, amplitude, duration);
            if (right.isValid) right.SendHapticImpulse(0, amplitude, duration);
#endif
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
