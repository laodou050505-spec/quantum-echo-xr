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
            if (progressPanel != null) progressPanel.SetActive(false);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (toastText != null) toastText.gameObject.SetActive(false);
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
            if (toastText != null && toastText.gameObject.activeSelf && Time.unscaledTime >= _toastUntil)
            {
                toastText.gameObject.SetActive(false);
            }
        }

        public void SetLevel(string code, int loop, string timerLabel)
        {
            levelText.text = code;
            loopText.text = $"当前回路  {loop:00}";
            timerLabelText.text = timerLabel;
        }

        public void SetLoop(int loop)
        {
            loopText.text = $"当前回路  {loop:00}";
        }

        public void SetObjective(int index, string title, string copy)
        {
            objectiveIndexText.text = $"{index + 1:00}";
            objectiveTitleText.text = title;
            objectiveCopyText.text = copy;
        }

        public void SetTimer(float remaining, float duration)
        {
            timerValueText.text = $"{Mathf.Max(0f, remaining):0.0}s";
            timerFill.fillAmount = duration <= 0f ? 0f : Mathf.Clamp01(remaining / duration);
        }

        public void SetEchoCount(int count, int maximum)
        {
            echoCountText.text = $"回声序列  {count} / {maximum}";
        }

        public void SetPrompt(string key, string message)
        {
            promptKeyText.text = key;
            promptText.text = message;
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
        }

        public void ShowResult(string title, int echoes, float totalSeconds, bool finalLevel)
        {
            resultTitleText.text = title;
            resultStatsText.text = $"使用回声  {echoes:00}    总用时  {Mathf.FloorToInt(totalSeconds / 60f):00}:{Mathf.FloorToInt(totalSeconds % 60f):00}\n\n{(finalLevel ? "按 Q 重玩第三关" : "按 Q 进入下一关")}";
            resultPanel.SetActive(true);
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }
    }

}
