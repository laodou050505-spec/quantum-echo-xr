using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumEcho.Editor
{
    [InitializeOnLoad]
    public static class QuantumEchoPlaythroughQA
    {
        const string SessionKey = "QuantumEcho.RunFullPlaythroughQA";
        const string BatchKey = "QuantumEcho.BatchFullPlaythroughQA";

        static QuantumEchoPlaythroughQA()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Quantum Echo/Run Full Four-Level Playthrough QA")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(BatchKey, false))
            {
                SessionState.SetBool(BatchKey, false);
                var reportPath = Path.Combine(Application.dataPath, "../Logs/QuantumEchoPlaythroughQA.txt");
                var passed = File.Exists(reportPath) && !File.ReadAllText(reportPath).Contains("FAIL");
                EditorApplication.Exit(passed ? 0 : 1);
                return;
            }
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, false);
            var manager = Object.FindAnyObjectByType<QuantumEchoGameManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogError("QATEST could not start: QuantumEchoGameManager is missing.");
                EditorApplication.isPlaying = false;
                return;
            }
            manager.gameObject.AddComponent<QuantumEchoPlaythroughRunner>();
        }

        public static void RunBatch()
        {
            QuantumEchoTitleSceneBuilder.EnsureTitleScene();
            EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.TitleScenePath, OpenSceneMode.Single);
            if (!QuantumEchoTitleSceneBuilder.AuditOpenedTitleScene(out var titleIssue))
            {
                Debug.LogError("QATEST title isolation failed: " + titleIssue);
                EditorApplication.Exit(1);
                return;
            }
            EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath, OpenSceneMode.Single);
            QuantumEchoScenePolisher.PolishExistingSceneSafely();
            if (!QuantumEchoScenePolisher.WriteSceneAudit())
            {
                EditorApplication.Exit(1);
                return;
            }
            var reportPath = Path.Combine(Application.dataPath, "../Logs/QuantumEchoPlaythroughQA.txt");
            if (File.Exists(reportPath)) File.Delete(reportPath);
            SessionState.SetBool(BatchKey, true);
            // Batchmode cannot enter the interactive Editor play-mode loop on every
            // Unity version. Run the same deterministic four-level harness in the
            // scene's serialized state instead, while preserving editor Play Mode
            // for the menu command above.
            QuantumEchoPlaythroughRunner.RunDeterministicBatch();
            var passed = File.Exists(reportPath) && !File.ReadAllText(reportPath).Contains("FAIL");
            SessionState.SetBool(BatchKey, false);
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
