using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// A short non-interactive completion notification.  It may be camera-relative
    /// because it is only transient feedback; puzzles and all primary controls stay
    /// at their authored world-space terminal/targets.
    /// v2 polish: smooth fade-in / fade-out, larger panel, accent colour bar.
    /// </summary>
    public sealed class QuantumLevelTransitionHUD : MonoBehaviour
    {
        TMP_Text _title;
        TMP_Text _detail;
        CanvasGroup _group;
        // v2: fade state
        float _fadeTarget;
        float _fadeSpeed = 3.2f;

        public static QuantumLevelTransitionHUD Create(Camera camera, TMP_FontAsset font)
        {
            var root = new GameObject("LEVEL COMPLETE TRANSITION HUD - brief feedback",
                typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(camera.transform, false);
            // v2: slightly higher, same forward distance
            rect.localPosition = new Vector3(0f, 0.22f, 2.2f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.00105f;
            // v2: taller panel to breathe
            rect.sizeDelta = new Vector2(740f, 230f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 20f;

            // v2: start fully transparent; Update will animate to opaque
            var group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;

            var panel = root.AddComponent<Image>();
            QuantumGeneratedPanelSkin.Apply(panel, 0.97f);

            // v2: cyan accent line at the top of the panel
            var accentObj = new GameObject("Accent Bar", typeof(RectTransform), typeof(Image));
            var accentRect = accentObj.GetComponent<RectTransform>();
            accentRect.SetParent(rect, false);
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot    = new Vector2(0.5f, 1f);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0f, 4f);
            accentObj.GetComponent<Image>().color = new Color(0.18f, 0.93f, 1f, 0.90f);

            var hud = root.AddComponent<QuantumLevelTransitionHUD>();
            hud._group = group;
            // v2: bolder title colour (full cyan), larger font
            hud._title  = AddText(rect, font, "Complete Title",
                new Vector2(58f, -52f), new Vector2(624f, 66f), 40f, new Color(0.25f, 0.95f, 1f, 1f));
            hud._title.fontStyle = TMPro.FontStyles.Bold;
            hud._detail = AddText(rect, font, "Complete Detail",
                new Vector2(58f, -130f), new Vector2(624f, 52f), 26f, new Color(0.85f, 0.92f, 1f, 1f));
            root.SetActive(false);
            return hud;
        }

        void Update()
        {
            if (_group == null) return;
            _group.alpha = Mathf.MoveTowards(_group.alpha, _fadeTarget, Time.unscaledDeltaTime * _fadeSpeed);
            // Auto-hide once fully faded out
            if (_fadeTarget <= 0f && _group.alpha <= 0.01f && gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        static TMP_Text AddText(RectTransform parent, TMP_FontAsset font, string name,
            Vector2 position, Vector2 size, float fontSize, Color color)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = child.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = child.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.Top;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        public void Show(string completedTitle, int nextLevelOneBased, float seconds)
        {
            if (_title != null)  _title.text  = $"Level Complete  ·  {completedTitle}";
            if (_detail != null) _detail.text = $"Entering Level {nextLevelOneBased}  ·  {seconds:0.0}s";
            gameObject.SetActive(true);
            // v2: animate in from zero
            if (_group != null) _group.alpha = 0f;
            _fadeTarget = 1f;
        }

        public void Hide()
        {
            // v2: animate out rather than snap
            _fadeTarget = 0f;
            // If not yet shown, just deactivate immediately
            if (_group != null && _group.alpha <= 0.01f && gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }
}
