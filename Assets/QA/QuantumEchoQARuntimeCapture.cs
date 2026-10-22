// QuantumEchoQARuntimeCapture — Play-mode QA screenshot capture.
// Injected automatically when QA_CAPTURE_REQUESTED session flag is set.
// All HUD canvases are WorldSpace → Camera.Render() into RenderTexture
// captures everything, including the comfort panel and world labels.
//
// Invoke via: QuantumEchoQALauncher.CaptureAndBuild (Editor menu item).

using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuantumEcho
{
    public sealed class QuantumEchoQARuntimeCapture : MonoBehaviour
    {
        const string OutDir  = "VisualQA/EN-20261022";
        const int W = 1920, H = 1080;

        // ── auto-inject when QA session flag is set ──────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoInject()
        {
#if UNITY_EDITOR
            if (!SessionState.GetBool("QA_CAPTURE_REQUESTED", false)) return;
            var go = new GameObject("_QACaptureRunner");
            DontDestroyOnLoad(go);
            go.AddComponent<QuantumEchoQARuntimeCapture>();
            Debug.Log("[QA Capture] Runner injected.");
#endif
        }

        void Start() => StartCoroutine(RunAll());

        // ── main coroutine ────────────────────────────────────────────────────
        IEnumerator RunAll()
        {
            Directory.CreateDirectory(OutDir);

            // 1. Title scene (already loaded)
            yield return new WaitForSeconds(1.5f); // let all Start()s fire
            yield return CaptureTitle();

            // 2. Load main scene
            SceneManager.LoadScene("QuantumEcho_Main");
            yield return new WaitUntil(() =>
                SceneManager.GetActiveScene().name == "QuantumEcho_Main");
            yield return new WaitForSeconds(2f); // Awake/Start/LateUpdate

            // 3. Main scene captures
            yield return CaptureMainScene();

            Debug.Log("[QA Capture] All screenshots saved to " + OutDir);

#if UNITY_EDITOR
            SessionState.SetBool("QA_CAPTURE_COMPLETE", true);
            EditorApplication.ExitPlaymode();
#endif
        }

        // ── Title scene ───────────────────────────────────────────────────────
        IEnumerator CaptureTitle()
        {
            // Find Title Camera
            var cam = FindCamera("Title Camera");
            if (cam == null) { Debug.LogWarning("[QA Capture] Title Camera not found."); yield break; }

            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "00_title_scene.png");
            Debug.Log("[QA Capture] 00_title_scene.png saved.");
        }

        // ── Main scene ────────────────────────────────────────────────────────
        IEnumerator CaptureMainScene()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                cam = FindCamera("Main Camera");
                if (cam == null) { Debug.LogError("[QA Capture] Main Camera not found."); yield break; }
            }

            // Silence game manager so it doesn't fight our HUD calls
            var gm = FindFirstObjectByType<QuantumEchoGameManager>();
            if (gm != null) gm.enabled = false;

            var hud = FindFirstObjectByType<QuantumHUD>();
            if (hud == null) { Debug.LogError("[QA Capture] QuantumHUD not found."); yield break; }

            var chud = hud.comfortObjectiveHud;
            if (chud == null) { Debug.LogError("[QA Capture] ComfortObjectiveHUD null."); yield break; }

            // ── Shots 01-04: Level openings ──────────────────────────────────
            var levels = new[]
            {
                (code: "QER-00", idx: 0, titleStr: "Let Your Echo Hold the Door",
                 objTitle: "Stand on the Holder",
                 objCopy: "Step onto the blue disc to keep the door open. Then seal it as an Echo."),
                (code: "QER-01", idx: 1, titleStr: "One Self, Three Time Slices",
                 objTitle: "Sync Stabilizer A",
                 objCopy: "Enter zone A to sync, then commit this loop."),
                (code: "QER-02", idx: 2, titleStr: "Set the Rhythm for the Phase Train",
                 objTitle: "Record the Departure Beat",
                 objCopy: "Sync at junction A and commit — this sends the Phase Train from the start."),
                (code: "QER-03", idx: 3, titleStr: "Don't Let the Hunters Find You",
                 objTitle: "Record Left Decoy",
                 objCopy: "Run to the upper-left Decoy Zone and seal — the first Hunter will follow your Echo.")
            };

            for (int i = 0; i < levels.Length; i++)
            {
                var lv = levels[i];
                hud.SetLevel(lv.code, 1, "Echo ends in");
                hud.SetObjective(0, lv.objTitle, lv.objCopy);
                hud.SetEchoCount(0, 3);
                hud.SetTimer(14f, 14f);
                chud.SetContent(lv.code, "01", lv.objTitle, lv.objCopy,
                    "[Grip / A / X]", "Seal this action and start the next loop");
                chud.SetCountdown(12.4f);
                chud.SetHelpExpanded(false);
                hud.SetProgress(false);
                hud.HideResult();

                yield return new WaitForEndOfFrame();
                RenderAndSave(cam, $"0{i+1}_level{i}_opening.png");
                Debug.Log($"[QA Capture] 0{i+1}_level{i}_opening.png saved.");
            }

            // ── Shot 05: Panel expanded (longest objective) ──────────────────
            hud.SetLevel("QER-03", 2, "Decoy Loop");
            hud.SetObjective(1, "Record Right Decoy",
                "In the next loop, run to the upper-right Decoy Zone and seal — both Hunters will be drawn away.");
            chud.SetContent("QER-03", "02", "Record Right Decoy",
                "In the next loop, run to the upper-right Decoy Zone and seal — both Hunters will be drawn away.",
                "[Grip / A / X]", "Seal this action and start the next loop");
            chud.SetHelpExpanded(true);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "05_panel_expanded.png");
            Debug.Log("[QA Capture] 05_panel_expanded.png saved.");

            // Collapse panel for remaining shots
            chud.SetHelpExpanded(false);

            // ── Shots 06-08: Toast variants ──────────────────────────────────
            hud.ShowToast("Echo 1 sealed");
            yield return new WaitForSeconds(0.4f);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "06_toast_echo_sealed.png");
            Debug.Log("[QA Capture] 06_toast_echo_sealed.png saved.");

            yield return new WaitForSeconds(2f); // let first toast expire

            hud.ShowToast("Echo slots full — undo an Echo first");
            yield return new WaitForSeconds(0.4f);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "07_toast_slots_full.png");
            Debug.Log("[QA Capture] 07_toast_slots_full.png saved.");

            yield return new WaitForSeconds(2f);

            hud.ShowToast("A Hunter has locked onto you…");
            yield return new WaitForSeconds(0.4f);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "08_toast_hunter.png");
            Debug.Log("[QA Capture] 08_toast_hunter.png saved.");

            yield return new WaitForSeconds(2f);

            // ── Shot 09: Progress bar ────────────────────────────────────────
            hud.SetProgress(true, "[R-Trigger · Hold] Extract Core", 0.42f);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "09_progress_extract.png");
            Debug.Log("[QA Capture] 09_progress_extract.png saved.");
            hud.SetProgress(false);

            // ── Shot 10: World labels close-up ────────────────────────────────
            var holderLabel = FindLabelGO("Label - Holder");
            if (holderLabel != null)
            {
                var pos = holderLabel.transform.position;
                var fwd = holderLabel.transform.forward;
                cam.transform.position = pos - fwd * 2.5f + Vector3.up * 0.8f;
                cam.transform.LookAt(pos + Vector3.up * 0.5f);
            }
            else
            {
                // fall back to a known position near level-0 core area
                cam.transform.position = new Vector3(-2.65f, 1.65f, 8.5f);
                cam.transform.rotation = Quaternion.Euler(0, 180f, 0);
            }
            // Also show a few additional labels in frame (if camera positioned near holder)
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "10_world_labels_close.png");
            Debug.Log("[QA Capture] 10_world_labels_close.png saved.");

            // Reset camera to spawn position
            cam.transform.position = new Vector3(0, 1.65f, 0);
            cam.transform.rotation = Quaternion.identity;

            // ── Shot 11: Level transition ─────────────────────────────────────
            hud.SetProgress(false);
            hud.HideResult();
            hud.ShowLevelTransition("Echo Relay Complete", 2, 4f);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "11_level_transition.png");
            Debug.Log("[QA Capture] 11_level_transition.png saved.");
            hud.HideLevelTransition();

            // ── Shot 12: Final results ────────────────────────────────────────
            hud.SetLevel("QER-03", 3, "Decoy Loop");
            hud.ShowResult("Lost Data Recovered", 3, 147f, true);
            yield return new WaitForEndOfFrame();
            RenderAndSave(cam, "12_final_results.png");
            Debug.Log("[QA Capture] 12_final_results.png saved.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        static Camera FindCamera(string name)
        {
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.gameObject.name == name) return c;
            return null;
        }

        static GameObject FindLabelGO(string name)
        {
            var all = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (var tmp in all)
                if (tmp.gameObject.name == name) return tmp.gameObject;
            return null;
        }

        static void RenderAndSave(Camera cam, string filename)
        {
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 2;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            DestroyImmediate(rt);
            var bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);
            var path = Path.Combine(OutDir, filename);
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[QA Capture] Saved {path} ({bytes.Length / 1024} KB)");
        }
    }
}
