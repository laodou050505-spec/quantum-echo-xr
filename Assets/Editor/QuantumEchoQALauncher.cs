// QuantumEchoQALauncher — Editor entry point for Play-mode QA screenshot
// capture followed by APK build.
//
// Usage (interactive):  QUANTUM ECHO EN / QA Capture + Build APK
// Usage (batchmode):    -executeMethod QuantumEchoQALauncher.CaptureAndBuild
//   (do NOT use -nographics — rendering is required for screenshots)
//
// Flow:
//   1. Sets QA_CAPTURE_REQUESTED in SessionState.
//   2. Enters Play mode — QuantumEchoQARuntimeCapture auto-injects.
//   3. Runtime capture coroutine runs, saves PNGs, sets QA_CAPTURE_COMPLETE.
//   4. EditorApplication.ExitPlaymode() is called from runtime script.
//   5. playModeStateChanged fires EnteredEditMode → BuildENAndroid().

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class QuantumEchoQALauncher
{
    static QuantumEchoQALauncher()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    [MenuItem("QUANTUM ECHO EN/QA Capture + Build APK", priority = 10)]
    public static void CaptureAndBuild()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[QA Launcher] Already in Play mode. Stop first.");
            return;
        }
        SessionState.SetBool("QA_CAPTURE_REQUESTED", true);
        SessionState.SetBool("QA_CAPTURE_COMPLETE", false);
        Debug.Log("[QA Launcher] Entering Play mode for screenshot capture...");
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("QUANTUM ECHO EN/Build APK Only", priority = 11)]
    public static void BuildOnly() => QuantumEchoENBuilder.BuildENAndroid();

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        if (!SessionState.GetBool("QA_CAPTURE_COMPLETE", false)) return;

        // Clear flags
        SessionState.SetBool("QA_CAPTURE_REQUESTED", false);
        SessionState.SetBool("QA_CAPTURE_COMPLETE", false);

        Debug.Log("[QA Launcher] Screenshots complete. Starting APK build...");
        try
        {
            QuantumEchoENBuilder.BuildENAndroid();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[QA Launcher] APK build failed: " + e.Message);
        }
    }
}
