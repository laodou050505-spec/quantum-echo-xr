// v4：量子回声目标面板截图——验证棋盘格已去除
// -executeMethod QEV4ObjectiveHUDCapture.Run
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class QEV4ObjectiveHUDCapture
{
    const string MainScenePath  = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";
    const string OrbitronPath   = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Dynamic.asset";
    const string OutDir         = "VisualQA/EN-v4";
    const int    W              = 1920;
    const int    H              = 1080;

    public static void Run()
    {
        Directory.CreateDirectory(OutDir);
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
        if (orbitron == null)
        {
            // fallback: find any TMP font
            var guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
            if (guids.Length > 0)
                orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // 创建临时相机
        var camGO = new GameObject("~V4_SS_Cam");
        var cam   = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
        cam.fieldOfView     = 60f;
        cam.nearClipPlane   = 0.01f;
        cam.farClipPlane    = 30f;
        cam.transform.position = new Vector3(0f, 1.6f, 0f);
        cam.transform.rotation = Quaternion.identity;

        // 创建目标 HUD（QuantumComfortObjectiveHUD）
        var hud = QuantumEcho.QuantumComfortObjectiveHUD.Create(cam, orbitron);
        hud.SetContent("QER-00", "01", "Stand on the Holder",
            "Step onto the blue disc to keep the door open. Then seal it as an Echo.",
            "[L-Stick]", "Step onto the blue Holder");
        hud.SetCountdown(5.8f);

        // 强制 TMP 更新
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include))
            t.ForceMeshUpdate(true, true);

        RenderToCam(cam, OutDir + "/objective_panel_v4.png");
        Debug.Log($"[v4 Capture] 已保存 {OutDir}/objective_panel_v4.png");

        hud.SetHelpExpanded(true);
        foreach (var t in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsInactive.Include))
            t.ForceMeshUpdate(true, true);
        RenderToCam(cam, OutDir + "/objective_panel_expanded_v4.png");
        Debug.Log($"[v4 Capture] 已保存 {OutDir}/objective_panel_expanded_v4.png");

        Object.DestroyImmediate(hud.gameObject);
        Object.DestroyImmediate(camGO);
    }

    static void RenderToCam(Camera cam, string outPng)
    {
        var rt  = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture   = null;
        File.WriteAllBytes(outPng, tex.EncodeToPNG());
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }
}
