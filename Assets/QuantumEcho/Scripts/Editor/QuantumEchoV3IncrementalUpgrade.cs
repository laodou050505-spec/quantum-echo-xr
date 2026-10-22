using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using QuantumEcho;

namespace QuantumEcho.Editor
{
    /// <summary>
    /// v3 增量 UI 精修：打开 en-v2 场景，仅修改 UI 相关对象。
    /// 严禁重建场景（不调用 SceneBuilder.BuildAll / TitleSceneBuilder.BuildTitleScene）。
    ///
    /// 遍历方式使用 scene.GetRootGameObjects() + GetComponentsInChildren()，
    /// 避免 FindObjectsByType 在 Unity 6 batchmode 下对 TMP 包组件的注册表缺失问题。
    ///
    /// batchmode 入口（总控构建前调用）：
    ///   lockf /tmp/en-unity-A.lock \
    ///     /Applications/Unity/Hub/Editor/6000.5.5f1/Unity.app/Contents/MacOS/Unity \
    ///     -batchmode -buildTarget Android \
    ///     -projectPath /Users/ld/Documents/Codex/EnglishEditions/QuantumEcho-EN \
    ///     -executeMethod QuantumEcho.Editor.QuantumEchoV3IncrementalUpgrade.RunFullV3Upgrade \
    ///     -logFile Logs/v3-incremental.log -quit
    /// </summary>
    public static class QuantumEchoV3IncrementalUpgrade
    {
        // ── 资源路径 ─────────────────────────────────────────────────────────
        const string OrbitronPath   = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Dynamic.asset";
        const string MonoPath       = "Assets/QuantumEcho/Fonts/SciFi/ShareTechMono-Dynamic.asset";
        const string HoloBtnPath    = "Assets/QuantumEcho/UI/Generated/ButtonV3/QuantumHologramButton.png";
        const string HoverClipPath  = "Assets/QuantumEcho/Audio/ui_hover_scifi.mp3";
        const string ConfirmPath    = "Assets/QuantumEcho/Audio/ui_confirm_scifi.mp3";
        const string ConfirmHardPath= "Assets/QuantumEcho/Audio/ui_confirm_hard.mp3";

        // ── 场景路径 ─────────────────────────────────────────────────────────
        const string TitleScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity";
        const string MainScenePath  = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";

        // ── 截图输出目录 ─────────────────────────────────────────────────────
        const string ScreenDir    = "VisualQA/V3-20261022";
        const string BaselineDir  = "VisualQA/V3-20261022/en-v2-comparison";

        // ────────────────────────────────────────────────────────────────────
        // 辅助：从已加载场景收集所有 TMP 组件（不依赖 FindObjectsByType 注册表）
        // ────────────────────────────────────────────────────────────────────
        static List<TMP_Text> GetAllTMPInScene(Scene scene)
        {
            var list = new List<TMP_Text>();
            foreach (var root in scene.GetRootGameObjects())
            {
                list.AddRange(root.GetComponentsInChildren<TextMeshProUGUI>(true));
                list.AddRange(root.GetComponentsInChildren<TextMeshPro>(true));
            }
            return list;
        }

        static List<T> GetAllInScene<T>(Scene scene) where T : Component
        {
            var list = new List<T>();
            foreach (var root in scene.GetRootGameObjects())
                list.AddRange(root.GetComponentsInChildren<T>(true));
            return list;
        }

        // ────────────────────────────────────────────────────────────────────
        // batchmode 主入口
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("Quantum Echo/v3 Full Incremental Upgrade (safe)")]
        public static void RunFullV3Upgrade()
        {
            Debug.Log("[INC] ══════════════════════════════════════════════════════");
            Debug.Log("[INC]  QUANTUM ECHO v3 INCREMENTAL UI UPGRADE (SAFE MODE)");
            Debug.Log("[INC] ══════════════════════════════════════════════════════");

            Directory.CreateDirectory("Logs");
            Directory.CreateDirectory(ScreenDir);
            Directory.CreateDirectory(BaselineDir);

            AssetDatabase.Refresh();

            // ── 步骤 1: 基线碰撞 QA ───────────────────────────────────────
            Debug.Log("[INC] 步骤 1: 运行 en-v2 基线碰撞/HUD QA…");
            try
            {
                QuantumEchoCollisionQA.RunBatch();
                if (File.Exists("Logs/QuantumEcho-CollisionAndHudQA.txt"))
                    File.Copy("Logs/QuantumEcho-CollisionAndHudQA.txt",
                              "Logs/QuantumEcho-CollisionAndHudQA-baseline.txt", true);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[INC] QA 异常（继续）: {ex.Message}");
                if (File.Exists("Logs/QuantumEcho-CollisionAndHudQA.txt"))
                    File.Copy("Logs/QuantumEcho-CollisionAndHudQA.txt",
                              "Logs/QuantumEcho-CollisionAndHudQA-baseline.txt", true);
            }

            // ── 步骤 2: 基线截图 ──────────────────────────────────────────
            Debug.Log("[INC] 步骤 2: 拍摄 en-v2 基线截图…");
            CaptureTitleShot(BaselineDir + "/title_normal_env2.png", false);
            CaptureMainShot(BaselineDir + "/gameplay_env2.png");

            // ── 步骤 3: Sprite 导入 ────────────────────────────────────────
            Debug.Log("[INC] 步骤 3: 确认全息按钮 Sprite…");
            EnsureHologramButtonSprite();
            AssetDatabase.Refresh();

            // ── 步骤 4: 加载字体 ───────────────────────────────────────────
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            var mono     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoPath);
            Debug.Log($"[INC] 字体: Orbitron={(orbitron != null ? "OK" : "MISSING")}  ShareTechMono={(mono != null ? "OK" : "MISSING")}");

            // ── 步骤 5: 升级标题场景 ───────────────────────────────────────
            Debug.Log("[INC] 步骤 5: 升级标题场景…");
            UpgradeTitleScene(orbitron, mono);

            // ── 步骤 6: 升级主场景 ─────────────────────────────────────────
            Debug.Log("[INC] 步骤 6: 升级主场景…");
            UpgradeMainScene(orbitron, mono);

            // ── 步骤 7: 升级后截图 ─────────────────────────────────────────
            Debug.Log("[INC] 步骤 7: 拍摄升级后截图…");
            CaptureTitleShot(ScreenDir + "/title_normal.png", false);
            CaptureTitleShot(ScreenDir + "/title_hover.png", true);
            CaptureHUDShot(ScreenDir + "/hud_collapsed.png", false);
            CaptureHUDShot(ScreenDir + "/hud_expanded.png", true);
            CaptureMainShot(ScreenDir + "/gameplay_hud.png");

            // ── 步骤 8: 改动后 QA ───────────────────────────────────────────
            Debug.Log("[INC] 步骤 8: 改动后碰撞 QA…");
            try
            {
                QuantumEchoCollisionQA.RunBatch();
                Debug.Log($"[INC] 改动后 QA 完成，结果: Logs/QuantumEcho-CollisionAndHudQA.txt");
            }
            catch (System.Exception ex)
            { Debug.LogWarning($"[INC] 改动后 QA 异常（继续）: {ex.Message}"); }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[INC] ══════════════════════════════════════════════════════");
            Debug.Log("[INC]  升级完成！截图目录: " + ScreenDir);
            Debug.Log("[INC] ══════════════════════════════════════════════════════");
        }

        // ────────────────────────────────────────────────────────────────────
        // 全息按钮 Sprite 导入
        // ────────────────────────────────────────────────────────────────────
        static void EnsureHologramButtonSprite()
        {
            var importer = AssetImporter.GetAtPath(HoloBtnPath) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[INC] 全息按钮图未找到: " + HoloBtnPath); return; }
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spriteBorder == new Vector4(80, 80, 80, 80))
            { Debug.Log("[INC] 全息按钮 Sprite 已正确，跳过。"); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(80, 80, 80, 80);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            { name = "Android", overridden = true, maxTextureSize = 2048,
              format = TextureImporterFormat.ASTC_6x6 });
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("[INC] 全息按钮 Sprite 导入完成。");
        }

        // ────────────────────────────────────────────────────────────────────
        // 标题场景升级
        // ────────────────────────────────────────────────────────────────────
        static void UpgradeTitleScene(TMP_FontAsset _orbitron, TMP_FontAsset _mono)
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);

            // 场景切换可能使场景外资产引用失效，在打开场景后重新加载
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            var mono     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoPath);
            Debug.Log($"[INC] 标题场景内重载字体: Orbitron={(orbitron != null ? "OK" : "NULL")} Mono={(mono != null ? "OK" : "NULL")}");

            Debug.Log("[INC] ── 标题场景改前统计 ──────────────────────────────");
            PrintStats("TITLE-BEFORE", scene);

            var holoSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(HoloBtnPath);
            var hoverClip   = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverClipPath);
            var confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ConfirmPath);
            var confirmHard = AssetDatabase.LoadAssetAtPath<AudioClip>(ConfirmHardPath);

            // ── 字体替换 ─────────────────────────────────────────────────
            var tmpList = GetAllTMPInScene(scene);
            int fontPatched = 0;
            foreach (var t in tmpList)
            {
                if (orbitron != null) { t.font = orbitron; EditorUtility.SetDirty(t); fontPatched++; }
            }
            Debug.Log($"[INC] 标题：替换 {fontPatched} 个 TMP_Text → Orbitron（共 {tmpList.Count} 个）");

            // ── 按钮：全息底图 + 音效 ─────────────────────────────────────
            var btns = GetAllInScene<QuantumTitleMenuButton>(scene);
            foreach (var btn in btns)
            {
                if (btn.audioSource == null)
                    btn.audioSource = btn.GetComponentInParent<AudioSource>(true);
                if (btn.audioSource == null)
                    foreach (var root in scene.GetRootGameObjects())
                    { btn.audioSource = root.GetComponentInChildren<AudioSource>(true); if (btn.audioSource != null) break; }
                btn.hoverClip   = hoverClip;
                btn.confirmClip = btn.action == QuantumTitleMenuButton.ActionKind.Exit
                    ? confirmHard : confirmClip;
                if (btn.backingImage != null && holoSprite != null)
                {
                    btn.backingImage.sprite = holoSprite;
                    btn.backingImage.type   = Image.Type.Sliced;
                    btn.backingImage.pixelsPerUnitMultiplier = 3f;
                    btn.backingImage.color  = btn.isPrimary
                        ? new Color(0.95f, 1f, 1f, 1f)
                        : new Color(0.70f, 0.90f, 0.92f, 0.88f);
                    EditorUtility.SetDirty(btn.backingImage);
                }
                EditorUtility.SetDirty(btn);
            }
            Debug.Log($"[INC] 标题：修改 {btns.Count} 个按钮（全息底图 + 音效）");

            // ── 隐藏桌面操作提示 ──────────────────────────────────────────
            foreach (var t in tmpList)
            {
                var txt = t.text ?? string.Empty;
                if (txt.Contains("MOUSE") || txt.Contains("ARROWS") ||
                    (txt.Contains("ENTER") && !txt.Contains("Trigger")))
                {
                    t.gameObject.SetActive(false);
                    EditorUtility.SetDirty(t.gameObject);
                    Debug.Log($"[INC]   隐藏桌面提示: '{txt.Substring(0, System.Math.Min(txt.Length, 40))}'");
                }
            }

            // ── UI 子对象安全验证：不能有 Collider ──────────────────────
            foreach (var t in tmpList)
                if (t.GetComponent<Collider>() != null)
                    Debug.LogError($"[INC] ❌ UI 子对象含 Collider: {t.gameObject.name}");

            Debug.Log("[INC] ── 标题场景改后统计 ──────────────────────────────");
            PrintStats("TITLE-AFTER", scene);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[INC] 标题场景已保存。");
        }

        // ────────────────────────────────────────────────────────────────────
        // 主场景升级
        // ────────────────────────────────────────────────────────────────────
        static void UpgradeMainScene(TMP_FontAsset _orbitron, TMP_FontAsset _mono)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);

            // 场景切换可能使场景外资产引用失效，在打开场景后重新加载
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            var mono     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoPath);
            Debug.Log($"[INC] 主场景内重载字体: Orbitron={(orbitron != null ? "OK" : "NULL")} Mono={(mono != null ? "OK" : "NULL")}");

            Debug.Log("[INC] ── 主场景改前统计 ────────────────────────────────");
            PrintStats("MAIN-BEFORE", scene);

            var tmpList = GetAllTMPInScene(scene);
            int orbitronCount = 0, monoCount = 0;
            foreach (var t in tmpList)
            {
                bool isMono = t.name.Contains("Countdown") || t.name.Contains("Value") ||
                              t.name == "Echo Countdown" || t.name == "Timer Value" ||
                              t.name == "Progress Value";
                var target = (isMono && mono != null) ? mono : orbitron;
                if (target != null)
                {
                    t.font = target;
                    EditorUtility.SetDirty(t);
                    if (isMono) monoCount++; else orbitronCount++;
                }
            }
            Debug.Log($"[INC] 主场景字体：Orbitron×{orbitronCount}  ShareTechMono×{monoCount}  共 {tmpList.Count} 个");

            // ── QuantumHologramWorldLabel 字体 ────────────────────────────
            var labels = GetAllInScene<QuantumHologramWorldLabel>(scene);
            foreach (var lbl in labels)
            { if (orbitron != null) { lbl.font = orbitron; EditorUtility.SetDirty(lbl); } }
            Debug.Log($"[INC] 主场景 QuantumHologramWorldLabel：更新 {labels.Count} 个");

            Debug.Log("[INC] ── 主场景改后统计 ────────────────────────────────");
            PrintStats("MAIN-AFTER", scene);

            EditorSceneManager.SaveScene(scene);
            Debug.Log("[INC] 主场景已保存。");
        }

        // ────────────────────────────────────────────────────────────────────
        // 场景统计
        // ────────────────────────────────────────────────────────────────────
        static void PrintStats(string tag, Scene scene)
        {
            int gos = 0, box = 0, sph = 0, cap = 0, mesh = 0, rb = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                gos  += root.GetComponentsInChildren<Transform>(true).Length;
                box  += root.GetComponentsInChildren<BoxCollider>(true).Length;
                sph  += root.GetComponentsInChildren<SphereCollider>(true).Length;
                cap  += root.GetComponentsInChildren<CapsuleCollider>(true).Length;
                mesh += root.GetComponentsInChildren<MeshCollider>(true).Length;
                rb   += root.GetComponentsInChildren<Rigidbody>(true).Length;
            }
            Debug.Log($"[INC][{tag}] GOs={gos}  Box={box}  Sphere={sph}  Capsule={cap}  Mesh={mesh}  RB={rb}");

            // 诱饵/追猎体碰撞体（必须为 0）
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var n = t.name;
                    if (n.StartsWith("Echo Lure Beacon") || n.Contains("Hunter Sentinel") || n.StartsWith("SENTINEL"))
                    {
                        int cols = t.GetComponentsInChildren<Collider>(true).Length;
                        Debug.Log($"[INC][{tag}]   {n}: colliders={cols} {(cols == 0 ? "OK" : "❌ VIOLATION")}");
                    }
                }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 截图辅助
        // ────────────────────────────────────────────────────────────────────
        static void CaptureTitleShot(string outPng, bool hoverStart)
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            var camGO = new GameObject("~SS_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView     = 44f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 40f;
            camGO.transform.position = new Vector3(0f, 1.65f, 0f);
            camGO.transform.LookAt(new Vector3(0f, 1.65f, -3.85f));

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            if (hoverStart)
                foreach (var btn in GetAllInScene<QuantumTitleMenuButton>(scene))
                    if (btn.action == QuantumTitleMenuButton.ActionKind.Start)
                    { btn.SetFocused(true); break; }

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        static void CaptureHUDShot(string outPng, bool expanded)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var hud   = GetAllInScene<QuantumHUD>(scene) is var hudList && hudList.Count > 0 ? hudList[0] : null;
            var camGO = new GameObject("~HUD_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView     = 40f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 30f;
            var hudPos = hud != null ? hud.transform.position : new Vector3(-5.45f, 1.62f, 2.25f);
            var hudFwd = hud != null ? hud.transform.forward  : Vector3.forward;
            camGO.transform.position = hudPos + hudFwd * 3.5f + Vector3.up * 0.15f;
            camGO.transform.LookAt(hudPos + Vector3.up * 0.15f);

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            if (hud != null)
            {
                hud.SetLevel("Q-00", 1, "Tutorial Loop");
                hud.SetObjective(0, "Stand on the Holder", "Step onto the blue disc.");
                hud.SetEchoCount(0, 4);
                hud.SetTimer(14f, 30f);
                hud.SetPrompt("[Grip / A / X]", "Seal this action and start the next loop");
                if (hud.comfortObjectiveHud != null)
                    hud.comfortObjectiveHud.SetHelpExpanded(expanded);
            }

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        static void CaptureMainShot(string outPng)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var camGO = new GameObject("~Main_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView     = 60f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 30f;
            camGO.transform.position = new Vector3(0f, 1.65f, 1.5f);
            camGO.transform.LookAt(new Vector3(0f, 1.5f, -2f));

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        static void ForceTMPUpdate(Scene scene)
        {
            foreach (var t in GetAllTMPInScene(scene))
            {
                try { t.ForceMeshUpdate(true, true); }
                catch (System.Exception ex)
                { Debug.LogWarning($"[INC] ForceMeshUpdate: {t.gameObject.name} {ex.Message}"); }
            }
            Canvas.ForceUpdateCanvases();
        }

        static void RenderToCam(Camera cam, string outPng, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            Object.DestroyImmediate(rt);
            Directory.CreateDirectory(Path.GetDirectoryName(outPng)!);
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log("[INC] 截图: " + outPng);
        }
    }
}
