using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// A short non-interactive completion notification.  It may be camera-relative
    /// because it is only transient feedback; puzzles and all primary controls stay
    /// at their authored world-space terminal/targets.
    /// </summary>
    public sealed class QuantumLevelTransitionHUD : MonoBehaviour
    {
        TMP_Text _title;
        TMP_Text _detail;

        public static QuantumLevelTransitionHUD Create(Camera camera, TMP_FontAsset font)
        {
            var root = new GameObject("LEVEL COMPLETE TRANSITION HUD - brief feedback", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(camera.transform, false);
            rect.localPosition = new Vector3(0f, 0.06f, 1.7f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.00115f;
            rect.sizeDelta = new Vector2(660f, 210f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 30;
            root.GetComponent<CanvasScaler>().dynamicPixelsPerUnit = 20f;

            var panel = root.AddComponent<Image>();
            panel.color = new Color(0.012f, 0.07f, 0.12f, 0.94f);

            var hud = root.AddComponent<QuantumLevelTransitionHUD>();
            hud._title = AddText(rect, font, "Complete Title", new Vector2(30f, -35f), new Vector2(600f, 64f), 37f, new Color(0.3f, 0.96f, 1f, 1f));
            hud._detail = AddText(rect, font, "Complete Detail", new Vector2(30f, -112f), new Vector2(600f, 50f), 25f, Color.white);
            root.SetActive(false);
            return hud;
        }

        static TMP_Text AddText(RectTransform parent, TMP_FontAsset font, string name, Vector2 position, Vector2 size, float fontSize, Color color)
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
            if (_title != null) _title.text = $"关卡完成 · {completedTitle}";
            if (_detail != null) _detail.text = $"正在进入第 {nextLevelOneBased} 关  ·  {seconds:0.0} 秒";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
