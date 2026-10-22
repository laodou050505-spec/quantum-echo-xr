using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuantumEcho
{
    public sealed class QuantumHUD : MonoBehaviour
    {
        public TMP_Text levelText;
        public TMP_Text loopText;
        public TMP_Text objectiveIndexText;
        public TMP_Text objectiveTitleText;
        public TMP_Text objectiveCopyText;
        public TMP_Text timerLabelText;
        public TMP_Text timerValueText;
        public Image timerFill;
        public TMP_Text echoCountText;
        public TMP_Text promptKeyText;
        public TMP_Text promptText;
        public TMP_Text toastText;
        public GameObject progressPanel;
        public TMP_Text progressLabelText;
        public TMP_Text progressValueText;
        public Image progressFill;
        public GameObject resultPanel;
        public TMP_Text resultTitleText;
        public TMP_Text resultStatsText;
        [Header("Comfort auxiliary HUD")]
        [Tooltip("A small left-side, camera-relative objective reminder. It contains only short status text; gameplay stays world-space.")]
        public QuantumComfortObjectiveHUD comfortObjectiveHud;
        [Tooltip("Brief camera-relative success feedback. It is notification-only; all objectives and interaction remain in world space.")]
        public QuantumLevelTransitionHUD levelTransitionHud;

        float _toastUntil;

        /// <summary>
        /// Attaches the full objective/result terminal to an authored point in the
        /// active room. It intentionally has no camera parent and never uses a
        /// screen-space canvas, so all rule text and core feedback stay walk-up,
        /// world-space XR content.
        /// </summary>
        public void SetWorldAnchor(Transform anchor)
        {
            if (anchor == null) return;
            transform.SetParent(anchor, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        void Awake()
        {
            HideNonEssentialEmptyPanels();
            ApplyControllerOnlyPresentation();
            if (progressPanel != null) progressPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (toastText != null) toastText.gameObject.SetActive(false);
        }

        void ApplyControllerOnlyPresentation()
        {
            // The desktop keyboard remains a QA fallback, but every player-facing
            // instruction is authored for the PICO controllers. This also replaces
            // any legacy serialized Q/E text before the first gameplay Update.
            if (promptKeyText != null)
            {
                promptKeyText.text = "[Grip / A / X]";
                promptKeyText.fontSize = 18f;
                promptKeyText.textWrappingMode = TextWrappingModes.NoWrap;
                var rect = promptKeyText.rectTransform;
                rect.sizeDelta = new Vector2(180f, rect.sizeDelta.y);
            }
            if (promptText != null)
            {
                promptText.text = "Seal this action and start the next loop";
                var rect = promptText.rectTransform;
                rect.anchoredPosition = new Vector2(206f, rect.anchoredPosition.y);
                rect.sizeDelta = new Vector2(430f, rect.sizeDelta.y);
            }
            if (progressLabelText != null)
                progressLabelText.text = "[R-Trigger · Hold] Extract";
        }

        void Start()
        {
            EnsureComfortObjectiveHud();
            RefreshComfortObjectiveHud();
        }

        void HideNonEssentialEmptyPanels()
        {
            SetPanelActive("Brand", false);
            SetPanelActive("Level Tabs", false);
            SetPanelActive("Loop", false);
            SetPanelActive("Echo Rail", false);
        }

        void SetPanelActive(string panelName, bool active)
        {
            foreach (var child in GetComponentsInChildren<Transform>(true))
            {
                if (child.name != panelName) continue;
                child.gameObject.SetActive(active);
                return;
            }
        }

        void Update()
        {
            EnsureComfortObjectiveHud();
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime >= _toastUntil)
            {
                toastText.gameObject.SetActive(false);
            }
        }

        public void SetLevel(string code, int loop, string timerLabel)
        {
            levelText.text = code;
            loopText.text = $"Loop  {loop:00}";
            timerLabelText.text = timerLabel;
            RefreshComfortObjectiveHud();
        }

        public void SetLoop(int loop)
        {
            loopText.text = $"Loop  {loop:00}";
        }

        public void SetObjective(int index, string title, string copy)
        {
            objectiveIndexText.text = $"{index + 1:00}";
            objectiveTitleText.text = title;
            objectiveCopyText.text = copy;
            RefreshComfortObjectiveHud();
        }

        public void SetTimer(float remaining, float duration)
        {
            timerValueText.text = $"{Mathf.Max(0f, remaining):0.0}s";
            timerFill.fillAmount = duration <= 0f ? 0f : Mathf.Clamp01(remaining / duration);
            EnsureComfortObjectiveHud();
            if (comfortObjectiveHud != null) comfortObjectiveHud.SetCountdown(remaining);
        }

        /// <summary>v2 polish: tints the timer amber/red when the loop is nearly full.</summary>
        public void SetLoopWarning(bool warning)
        {
            if (timerValueText != null)
                timerValueText.color = warning ? new Color(1f, 0.30f, 0.22f) : new Color(1f, 0.68f, 0.18f, 0.96f);
            if (timerFill != null)
                timerFill.color = warning ? new Color(1f, 0.28f, 0.20f, 0.88f) : new Color(1f, 0.68f, 0.18f, 0.88f);
        }

        public void SetEchoCount(int count, int maximum)
        {
            echoCountText.text = $"Echoes  {count} / {maximum}";
        }

        public void SetPrompt(string key, string message)
        {
            promptKeyText.text = FormatControllerKey(key);
            promptText.text = message;
            RefreshComfortObjectiveHud();
        }

        static string FormatControllerKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            if (key.StartsWith("[") && key.EndsWith("]")) return key;
            return $"[{key}]";
        }

        void EnsureComfortObjectiveHud()
        {
            if (comfortObjectiveHud != null) return;
            var camera = Camera.main;
            var font = objectiveTitleText != null ? objectiveTitleText.font : null;
            if (camera == null || font == null) return;
            comfortObjectiveHud = QuantumComfortObjectiveHUD.Create(camera, font);
        }

        void EnsureLevelTransitionHud()
        {
            if (levelTransitionHud != null) return;
            var camera = Camera.main;
            var font = resultTitleText != null ? resultTitleText.font : null;
            if (camera == null || font == null) return;
            levelTransitionHud = QuantumLevelTransitionHUD.Create(camera, font);
        }

        void RefreshComfortObjectiveHud()
        {
            if (comfortObjectiveHud == null) return;
            comfortObjectiveHud.SetContent(
                levelText != null ? levelText.text : string.Empty,
                objectiveIndexText != null ? objectiveIndexText.text : string.Empty,
                objectiveTitleText != null ? objectiveTitleText.text : string.Empty,
                objectiveCopyText != null ? objectiveCopyText.text : string.Empty,
                promptKeyText != null ? promptKeyText.text : string.Empty,
                promptText != null ? promptText.text : string.Empty);
        }

        public void SetProgress(bool visible, string label = "", float progress = 0f)
        {
            progressPanel.SetActive(visible);
            if (!visible) return;
            progress = Mathf.Clamp01(progress);
            progressLabelText.text = label;
            progressValueText.text = $"{Mathf.RoundToInt(progress * 100f)}%";
            progressFill.fillAmount = progress;
        }

        public void ShowToast(string message, float seconds = 1.9f)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
            _toastUntil = Time.unscaledTime + seconds;
            EnsureComfortObjectiveHud();
            if (comfortObjectiveHud != null) comfortObjectiveHud.ShowFeedback(message, seconds);
        }

        public void ShowResult(string title, int echoes, float totalSeconds, bool finalLevel)
        {
            resultTitleText.text = title;
            resultStatsText.text = $"Echoes  {echoes:00}    Time  {Mathf.FloorToInt(totalSeconds / 60f):02}:{Mathf.FloorToInt(totalSeconds % 60f):02}\n\n{(finalLevel ? "Press [Menu] to retry the final level" : "Loading next level…")}";
            resultPanel.SetActive(true);
        }

        /// <summary>Shows a readable completion beat before automatic level travel.</summary>
        public void ShowLevelTransition(string completedTitle, int nextLevelOneBased, float seconds)
        {
            EnsureLevelTransitionHud();
            if (levelTransitionHud != null)
                levelTransitionHud.Show(completedTitle, nextLevelOneBased, seconds);
        }

        public void HideLevelTransition()
        {
            if (levelTransitionHud != null) levelTransitionHud.Hide();
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
            HideLevelTransition();
        }
    }

}
