using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Headless screenshot capture for QUANTUM ECHO English Edition visual QA.
/// Run via: -executeMethod QuantumEchoScreenshots.CaptureAll
/// Renders to VisualQA/EN-20261022/ using RenderTexture (no XR needed).
/// </summary>
public static class QuantumEchoScreenshots
{
    const string OutDir = "VisualQA/EN-20261022";
    const int W = 1920, H = 1080;

    public static void CaptureAll()
    {
        Directory.CreateDirectory(OutDir);
        // Title scene
        CaptureTitleScene();
        // Main scene (each of the 4 levels)
        CaptureMainScene();
        Debug.Log("[QA Screenshot] All captures complete → " + OutDir);
    }

    static void CaptureTitleScene()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity",
            OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);
        // Find camera (or create fallback)
        var cam = FindOrCreateCamera("Title");
        RenderAndSave(cam, "00_title_scene.png");

        // Find TMP texts to log for verification
        foreach (var tmp in Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (!string.IsNullOrWhiteSpace(tmp.text))
                Debug.Log($"[QA] TitleScene TMP '{tmp.name}': {tmp.text.Replace("\n","↵")}");
        }
    }

    static void CaptureMainScene()
    {
        var scene = EditorSceneManager.OpenScene(
            "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity",
            OpenSceneMode.Single);
        EditorSceneManager.SetActiveScene(scene);

        var cam = FindOrCreateCamera("Main");

        // Capture full default view (level 0 active)
        RenderAndSave(cam, "01_main_default.png");

        // Log all TMP texts for verification
        int count = 0;
        foreach (var tmp in Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (!string.IsNullOrWhiteSpace(tmp.text))
            {
                Debug.Log($"[QA] MainScene TMP '{tmp.name}': {tmp.text.Replace("\n","↵")}");
                count++;
            }
        }
        Debug.Log($"[QA] Total TMP texts in main scene: {count}");

        // Look for world labels (QuantumWorldLabel or similar GameObjects)
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            if (go.name.StartsWith("Label -") || go.name.Contains("World Label") || go.name.Contains("Decoy") || go.name.Contains("Holder"))
            {
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    Debug.Log($"[QA] WorldLabel '{go.name}': '{tmp.text}'");
            }
        }

        // Capture from different angles to show different level zones
        // Position camera to see level 0 (Tutorial area)
        PlaceCameraAt(cam, new Vector3(0, 4, -8), new Vector3(25, 0, 0));
        RenderAndSave(cam, "02_level0_tutorial_overview.png");

        // Level 1 area
        PlaceCameraAt(cam, new Vector3(10, 4, -8), new Vector3(25, -15, 0));
        RenderAndSave(cam, "03_level1_paired_overview.png");

        // Level 2 area  
        PlaceCameraAt(cam, new Vector3(20, 4, -8), new Vector3(25, -30, 0));
        RenderAndSave(cam, "04_level2_train_overview.png");

        // Level 3 area (Lure/Decoy)
        PlaceCameraAt(cam, new Vector3(30, 4, -8), new Vector3(25, -45, 0));
        RenderAndSave(cam, "05_level3_lure_overview.png");
    }

    static Camera FindOrCreateCamera(string label)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var cameras = Object.FindObjectsOfType<Camera>(true);
            if (cameras.Length > 0) cam = cameras[0];
        }
        if (cam == null)
        {
            var go = new GameObject($"QA Camera ({label})");
            cam = go.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 3, -10);
            cam.transform.rotation = Quaternion.Euler(15, 0, 0);
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.12f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
        return cam;
    }

    static void PlaceCameraAt(Camera cam, Vector3 pos, Vector3 eulerAngles)
    {
        cam.transform.position = pos;
        cam.transform.rotation = Quaternion.Euler(eulerAngles);
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
        Object.DestroyImmediate(rt);
        var bytes = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);
        var path = Path.Combine(OutDir, filename);
        File.WriteAllBytes(path, bytes);
        Debug.Log($"[QA Screenshot] Saved {path} ({bytes.Length / 1024} KB)");
    }
}
