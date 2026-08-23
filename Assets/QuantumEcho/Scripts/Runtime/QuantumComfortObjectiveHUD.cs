using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuantumEcho
{
    /// <summary>
    /// A deliberately small, camera-relative helper panel.  It restores the
    /// left-side objective readout without turning the game itself into a flat
    /// screen: puzzles, targets and the full task terminal remain world-space.
    /// </summary>
    public sealed class QuantumComfortObjectiveHUD : MonoBehaviour
    {
        TMP_Text _level;
        TMP_Text _objective;
        TMP_Text _detail;
        TMP_Text _prompt;

        public static QuantumComfortObjectiveHUD Create(Camera camera, TMP_FontAsset font)
        {
            var root = new GameObject("COMFORT OBJECTIVE HUD - left auxiliary", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(camera.transform, false);
            // Auxiliary-only panel: raised and moved toward the outer left corner
            // so it reads as a top-left HUD without taking the puzzle centre.
            rect.localPosition = new Vector3(-0.60f, 0.31f, 1.55f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * 0.0012f;
            rect.sizeDelta = new Vector2(600f, 290f);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 24;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 18f;

            var panel = root.AddComponent<Image>();
            panel.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Background.psd");
            panel.type = Image.Type.Sliced;
            panel.color = new Color(0.015f, 0.06f, 0.11f, 0.83f);

            var hud = root.AddComponent<QuantumComfortObjectiveHUD>();
            hud._level = AddText(rect, font, "Level", new Vector2(24f, -22f), new Vector2(512f, 34f), 24f, new Color(0.25f, 0.92f, 1f, 1f));
            hud._objective = AddText(rect, font, "Objective", new Vector2(24f, -67f), new Vector2(512f, 58f), 34f, Color.white);
            hud._detail = AddText(rect, font, "Detail", new Vector2(24f, -128f), new Vector2(512f, 76f), 22f, new Color(0.78f, 0.91f, 0.97f, 1f));
            hud._prompt = AddText(rect, font, "Prompt", new Vector2(24f, -220f), new Vector2(512f, 32f), 22f, new Color(0.32f, 1f, 0.8f, 1f));
            return hud;
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
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        public void SetContent(string level, string index, string objective, string detail, string key, string prompt)
        {
            if (_level != null) _level.text = string.IsNullOrWhiteSpace(index) ? level : $"{level}  ·  目标 {index}";
            if (_objective != null) _objective.text = objective;
            if (_detail != null) _detail.text = detail;
            if (_prompt != null) _prompt.text = string.IsNullOrWhiteSpace(key) ? prompt : $"{key}  {prompt}";
        }
    }
}
