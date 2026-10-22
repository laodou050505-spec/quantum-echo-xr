using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR;

namespace QuantumEcho
{
    /// <summary>
    /// A top-left, camera-relative objective panel with a stable safe margin.
    /// Help expands downwards; puzzle targets remain world-space.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class QuantumComfortObjectiveHUD : MonoBehaviour
    {
        TMP_Text _level;
        TMP_Text _objective;
        TMP_Text _detail;
        TMP_Text _prompt;
        TMP_Text _controls;
        TMP_Text _countdown;
        TMP_Text _feedback;
        float _feedbackUntil;
        RectTransform _panel;
        TMP_Text _helpHint;
        bool _helpExpanded;
        bool _previousHelp;
        Material _panelMaterial;
        Material _textMaterial;
        Camera _camera;
        // v3 sci-fi: scanline overlay graphic
        UnityEngine.UI.RawImage _scanlineOverlay;
        // v3 sci-fi: divider line between header and body
        UnityEngine.UI.Image _divider;
        // v3 sci-fi: accent icon for countdown row
        TMP_Text _countdownIcon;
        // v2 polish: countdown colour state
        float _countdownRemaining;
        // v3: scanline animation
        float _scanlineOffset;
        static readonly Color CountdownNormal = new Color(1f, 0.80f, 0.43f);
        static readonly Color CountdownWarn   = new Color(1f, 0.52f, 0.15f);
        static readonly Color CountdownAlert  = new Color(1f, 0.20f, 0.18f);
        // v3 color palette
        static readonly Color CyanBright  = new Color(0.11f, 0.91f, 1.00f, 0.95f);
        static readonly Color CyanDim     = new Color(0.11f, 0.91f, 1.00f, 0.45f);
        static readonly Color AmberBright = new Color(1.00f, 0.72f, 0.20f, 0.96f);
        const float PanelWidth = 840f;
        const float CompactHeight = 400f;
        const float ExpandedHeight = 600f;
        const float ViewingDistance = 2.1f;
        const float MaximumScale = 0.00175f;
        const float DesktopLeftMargin = 0.06f;
        const float DesktopTopMargin = 0.07f;
        const float XRSideInset = 0.06f;
        const float XRTopInset = 0.06f;
        const float XRSafeNdc = 0.88f;

        public static QuantumComfortObjectiveHUD Create(Camera camera, TMP_FontAsset font)
        {
            var root = new GameObject("COMFORT OBJECTIVE HUD - top left", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(camera.transform, false);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * MaximumScale;
            rect.sizeDelta = new Vector2(PanelWidth, CompactHeight);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 24;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 18f;

            var panel = root.AddComponent<Image>();
            QuantumGeneratedPanelSkin.Apply(panel, 1f);

            var hud = root.AddComponent<QuantumComfortObjectiveHUD>();
            // Hologram markers deliberately use queue 4000. This auxiliary
            // readout must render after them, with its own type above its opaque
            // backing. Keep the regular world-space terminals depth-aware.
            hud._panelMaterial = new Material(Shader.Find("UI/Default"))
            {
                name = "Quantum comfort opaque panel",
                renderQueue = 4100
            };
            hud._textMaterial = new Material(font.material)
            {
                name = "Quantum comfort readable type",
                renderQueue = 4110
            };
            panel.material = hud._panelMaterial;
            hud._panel = rect;
            hud._camera = camera;
            hud.UpdatePlacement();

            // v3: divider line between level header and objective body
            var dividerObj = new GameObject("Header Divider", typeof(RectTransform), typeof(Image));
            var dividerRect = dividerObj.GetComponent<RectTransform>();
            dividerRect.SetParent(rect, false);
            dividerRect.anchorMin = new Vector2(0f, 1f);
            dividerRect.anchorMax = new Vector2(0f, 1f);
            dividerRect.pivot = new Vector2(0f, 1f);
            dividerRect.anchoredPosition = new Vector2(24f, -62f);
            dividerRect.sizeDelta = new Vector2(792f, 1.5f);
            var dividerImg = dividerObj.GetComponent<Image>();
            dividerImg.color = CyanDim;
            dividerImg.raycastTarget = false;
            hud._divider = dividerImg;

            // v3: scanline overlay (subtle animated lines over whole panel)
            var scanObj = new GameObject("Scanline Overlay", typeof(RectTransform), typeof(UnityEngine.UI.RawImage));
            var scanRect = scanObj.GetComponent<RectTransform>();
            scanRect.SetParent(rect, false);
            scanRect.anchorMin = Vector2.zero;
            scanRect.anchorMax = Vector2.one;
            scanRect.offsetMin = Vector2.zero;
            scanRect.offsetMax = Vector2.zero;
            var scanImg = scanObj.GetComponent<UnityEngine.UI.RawImage>();
            scanImg.color = new Color(0.12f, 0.92f, 0.96f, 0.045f);
            scanImg.raycastTarget = false;
            hud._scanlineOverlay = scanImg;

            // v3: level label uses smaller Orbitron-style monospaced look (track letter-spacing)
            hud._level = AddText(rect, font, "Level", new Vector2(32f, -18f), new Vector2(420f, 38f), 22f, CyanBright);
            hud._level.characterSpacing = 3.5f;

            // v3: help hint right-aligned in header row (anchor top-right of panel)
            hud._helpHint = AddText(rect, font, "Help Hint", new Vector2(390f, -18f), new Vector2(418f, 38f), 19f, new Color(0.60f, 0.80f, 0.86f, 0.80f));
            hud._helpHint.alignment = TextAlignmentOptions.TopRight;
            hud._helpHint.text = "[Press L-Stick] Controls";

            // v3: objective title - larger, bold, white
            hud._objective = AddText(rect, font, "Objective", new Vector2(32f, -72f), new Vector2(776f, 62f), 38f, Color.white);
            hud._objective.fontStyle = FontStyles.Bold;
            hud._objective.characterSpacing = 1f;

            // v3: detail text with slightly tinted color
            hud._detail = AddText(rect, font, "Detail", new Vector2(32f, -144f), new Vector2(776f, 96f), 27f, new Color(0.80f, 0.91f, 0.94f));
            hud._detail.lineSpacing = 6f;

            // v3: prompt with cyan-green tint and controller key prefix icon
            hud._prompt = AddText(rect, font, "Prompt", new Vector2(32f, -248f), new Vector2(776f, 48f), 27f, new Color(0.55f, 0.96f, 0.90f));

            // v3: countdown row - amber icon + text
            hud._countdownIcon = AddText(rect, font, "Echo Icon", new Vector2(32f, -308f), new Vector2(32f, 38f), 22f, AmberBright);
            hud._countdownIcon.text = "⬡";
            hud._countdown = AddText(rect, font, "Echo Countdown", new Vector2(70f, -308f), new Vector2(720f, 38f), 23f, AmberBright);
            hud._countdown.text = "Echo ends in 0.0s";
            hud._countdown.characterSpacing = 1.5f;

            hud._controls = AddText(rect, font, "PICO Controls", new Vector2(32f, -368f), new Vector2(776f, 172f), 26f, Color.white);
            hud._controls.text = "[L-Stick] Move\n[R-Trigger] Interact / Hold to Extract\n[Grip / A / X] Seal Echo\n[B / Y] Undo   [Menu] Reset";
            hud._controls.gameObject.SetActive(false);
            hud._feedback = AddText(rect, font, "Action Feedback", new Vector2(32f, -256f), new Vector2(776f, 70f), 27f, AmberBright);
            hud._feedback.gameObject.SetActive(false);
            return hud;
        }

        void Update()
        {
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick, out var helpHeld);
            foreach (var device in InputSystem.devices)
            {
                var isLeft = false;
                foreach (var usage in device.usages)
                    if (usage.ToString() == "LeftHand") isLeft = true;
                if (!isLeft) continue;
                var click = device.TryGetChildControl<ButtonControl>("thumbstickClicked");
                helpHeld |= click != null && click.isPressed;
            }
            var desktopHelp = Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
            if (desktopHelp || helpHeld && !_previousHelp)
                SetHelpExpanded(!_helpExpanded);
            _previousHelp = helpHeld;
            if (_feedback != null && _feedback.gameObject.activeSelf && Time.unscaledTime >= _feedbackUntil)
            {
                _feedback.gameObject.SetActive(false);
                _prompt.gameObject.SetActive(true);
            }
            // v2 polish: pulse the countdown text alpha when time is critical (< 2 s)
            if (_countdown != null && _countdownRemaining > 0f && _countdownRemaining <= 2f)
            {
                var pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 3.2f));
                var c = _countdown.color;
                _countdown.color = new Color(c.r, c.g, c.b, pulse);
                if (_countdownIcon != null) _countdownIcon.color = new Color(c.r, c.g, c.b, pulse);
            }
            // v3: animate scanline UV offset for a subtle moving-scanline effect
            if (_scanlineOverlay != null)
            {
                _scanlineOffset = (_scanlineOffset + Time.unscaledDeltaTime * 0.08f) % 1f;
                _scanlineOverlay.uvRect = new Rect(0f, _scanlineOffset, 1f, 1f);
            }
        }

        public void SetHelpExpanded(bool expanded)
        {
            _helpExpanded = expanded;
            // Grow downwards without moving the familiar objective/header.
            _panel.sizeDelta = new Vector2(PanelWidth, expanded ? ExpandedHeight : CompactHeight);
            UpdatePlacement();
            _controls.gameObject.SetActive(expanded);
            _helpHint.text = expanded ? "[Press L-Stick] Hide Controls" : "[Press L-Stick] Controls";
        }

        void OnEnable() => Application.onBeforeRender += BeforeRenderPlacement;
        void OnDisable() => Application.onBeforeRender -= BeforeRenderPlacement;
        [BeforeRenderOrder(100)]
        void BeforeRenderPlacement() => UpdatePlacement();
        void LateUpdate() => UpdatePlacement();

        void UpdatePlacement()
        {
            if (_camera == null || _panel == null) return;
            if (Application.platform == RuntimePlatform.Android || _camera.stereoEnabled)
            {
                // XR has asymmetric per-eye projections. Anchor against the
                // shared per-eye image edge rather than a centred comfort box,
                // so the panel appears in the actual upper-left of the screen.
                var safe = Rect.MinMaxRect(-1.55f, -1.05f, 1.55f, 1.5f);
                if (_camera.stereoEnabled)
                {
                    var left = EyeSafeRect(_camera, Camera.StereoscopicEye.Left);
                    var right = EyeSafeRect(_camera, Camera.StereoscopicEye.Right);
                    var intersection = Rect.MinMaxRect(Mathf.Max(safe.xMin, left.xMin, right.xMin), Mathf.Max(safe.yMin, left.yMin, right.yMin),
                        Mathf.Min(safe.xMax, left.xMax, right.xMax), Mathf.Min(safe.yMax, left.yMax, right.yMax));
                    if (intersection.width > 0.2f && intersection.height > 0.2f) safe = intersection;
                }
                PlaceInsideXRRect(safe);
                return;
            }
            var topLeft = _camera.transform.InverseTransformPoint(
                _camera.ViewportToWorldPoint(new Vector3(DesktopLeftMargin, 1f - DesktopTopMargin, ViewingDistance)));
            var leftPoint = _camera.ViewportToWorldPoint(new Vector3(DesktopLeftMargin, 0.5f, ViewingDistance));
            var rightPoint = _camera.ViewportToWorldPoint(new Vector3(1f - DesktopLeftMargin, 0.5f, ViewingDistance));
            var scale = Mathf.Min(MaximumScale, Vector3.Distance(leftPoint, rightPoint) / PanelWidth);
            _panel.localScale = Vector3.one * scale;
            _panel.localPosition = new Vector3(topLeft.x + PanelWidth * scale * 0.5f, topLeft.y - _panel.sizeDelta.y * scale * 0.5f, ViewingDistance);
        }

        void PlaceInsideXRRect(Rect safe)
        {
            var left = safe.xMin + XRSideInset;
            var right = safe.xMax - XRSideInset;
            var top = safe.yMax - XRTopInset;
            var scale = Mathf.Min(MaximumScale, (right-left) / PanelWidth, (top-safe.yMin) / ExpandedHeight);
            _panel.localScale = Vector3.one * scale;
            var halfWidth = PanelWidth * scale * 0.5f;
            _panel.localPosition = new Vector3(left+halfWidth, top-_panel.sizeDelta.y*scale*0.5f, ViewingDistance);
        }

        static Rect EyeSafeRect(Camera camera, Camera.StereoscopicEye eye)
        {
            return ProjectEyeSafeRect(camera.GetStereoProjectionMatrix(eye),
                camera.transform.worldToLocalMatrix * camera.GetStereoViewMatrix(eye).inverse);
        }

        public static Rect ProjectEyeSafeRect(Matrix4x4 projection, Matrix4x4 eyeToCamera)
        {
            var inverse = projection.inverse;
            var origin = eyeToCamera.MultiplyPoint3x4(Vector3.zero);
            var min = Vector2.positiveInfinity;
            var max = Vector2.negativeInfinity;
            foreach (var x in new[] { -XRSafeNdc, XRSafeNdc })
            foreach (var y in new[] { -XRSafeNdc, XRSafeNdc })
            {
                var direction = eyeToCamera.MultiplyVector(inverse.MultiplyPoint(new Vector3(x,y,0f)));
                if (Mathf.Abs(direction.z) < 0.00001f) return Rect.zero;
                var point = origin + direction * ((ViewingDistance-origin.z) / direction.z);
                if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.x) || float.IsInfinity(point.y)) return Rect.zero;
                min = Vector2.Min(min, point); max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        void OnDestroy()
        {
            if (_panelMaterial != null) { if (Application.isPlaying) Destroy(_panelMaterial); else DestroyImmediate(_panelMaterial); }
            if (_textMaterial != null) { if (Application.isPlaying) Destroy(_textMaterial); else DestroyImmediate(_textMaterial); }
        }

        static TMP_Text AddText(RectTransform parent, TMP_FontAsset font, string name, Vector2 anchoredPosition, Vector2 size, float fontSize, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            var text = child.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = parent.GetComponent<QuantumComfortObjectiveHUD>()._textMaterial;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public void SetContent(string level, string index, string objective, string detail, string key, string prompt)
        {
            if (_level != null) _level.text = string.IsNullOrWhiteSpace(index) ? level : $"{level}  ·  Objective {index}";
            if (_objective != null) _objective.text = objective;
            if (_detail != null) _detail.text = detail;
            if (_prompt != null) _prompt.text = string.IsNullOrWhiteSpace(key) ? prompt : $"{key}  {prompt}";
        }

        public void SetCountdown(float remaining)
        {
            _countdownRemaining = remaining;
            if (_countdown == null) return;
            _countdown.text = $"Echo ends in {Mathf.Max(0f, remaining):0.0}s";
            // v2 polish: colour transitions: normal → amber warn → red alert
            Color target;
            if (remaining > 5f)      target = CountdownNormal;
            else if (remaining > 2f) target = CountdownWarn;
            else                     target = CountdownAlert;
            _countdown.color = target;
        }

        public void ShowFeedback(string message, float seconds)
        {
            if (_feedback == null || string.IsNullOrWhiteSpace(message)) return;
            _feedback.text = message;
            _prompt.gameObject.SetActive(false);
            _feedback.gameObject.SetActive(true);
            _feedbackUntil = Time.unscaledTime + Mathf.Max(1f, seconds);
        }
    }
}
