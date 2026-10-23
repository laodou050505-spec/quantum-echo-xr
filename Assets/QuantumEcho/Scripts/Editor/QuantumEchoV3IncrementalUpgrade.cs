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
    /// 字体在 OpenScene 之后重新加载（跨场景切换后引用可能失效）。
    ///
    /// batchmode 入口：
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
        const string OrbitronPath    = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Dynamic.asset";
        const string MonoPath        = "Assets/QuantumEcho/Fonts/SciFi/ShareTechMono-Dynamic.asset";
        const string HoloBtnPath     = "Assets/QuantumEcho/UI/Generated/ButtonV3/QuantumHologramButton.png";
        const string HoverClipPath   = "Assets/QuantumEcho/Audio/ui_hover_scifi.mp3";
        const string ConfirmPath     = "Assets/QuantumEcho/Audio/ui_confirm_scifi.mp3";
        const string ConfirmHardPath = "Assets/QuantumEcho/Audio/ui_confirm_hard.mp3";

        // ── 场景路径 ─────────────────────────────────────────────────────────
        const string TitleScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity";
        const string MainScenePath  = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";

        // ── 截图输出目录 ─────────────────────────────────────────────────────
        const string ScreenDir   = "VisualQA/V3-20261022";
        const string BaselineDir = "VisualQA/V3-20261022/en-v2-comparison";

        // ────────────────────────────────────────────────────────────────────
        // 辅助：场景内组件收集（不依赖 FindObjectsByType 注册表）
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

            // ── 步骤 1: 基线碰撞 QA ────────────────────────────────────────
            Debug.Log("[INC] 步骤 1: 基线碰撞/HUD QA…");
            RunQA("Logs/QuantumEcho-CollisionAndHudQA-baseline.txt");

            // ── 步骤 2: 基线截图 ────────────────────────────────────────────
            Debug.Log("[INC] 步骤 2: en-v2 基线截图…");
            CaptureTitleShot(BaselineDir + "/title_normal_env2.png", idle: true);
            CaptureMainShot(BaselineDir + "/gameplay_env2.png");

            // ── 步骤 3: Sprite 导入 ─────────────────────────────────────────
            Debug.Log("[INC] 步骤 3: 确认全息按钮 Sprite…");
            EnsureHologramButtonSprite();
            AssetDatabase.Refresh();

            // ── 步骤 5: 升级标题场景 ────────────────────────────────────────
            Debug.Log("[INC] 步骤 5: 升级标题场景…");
            UpgradeTitleScene();

            // ── 步骤 6: 升级主场景 ──────────────────────────────────────────
            Debug.Log("[INC] 步骤 6: 升级主场景…");
            UpgradeMainScene();

            // ── 步骤 7: 升级后截图 ──────────────────────────────────────────
            Debug.Log("[INC] 步骤 7: 拍摄升级后截图…");
            CaptureTitleShot(ScreenDir + "/title_normal.png",  idle: true);
            CaptureTitleShot(ScreenDir + "/title_hover.png",   idle: false, hoverBtn: true);
            CaptureTitleShot(ScreenDir + "/title_pressed.png", idle: false, pressBtn: true);
            CaptureHUDShot(ScreenDir + "/hud_collapsed.png",   expanded: false);
            CaptureHUDShot(ScreenDir + "/hud_expanded.png",    expanded: true);
            CaptureMainShot(ScreenDir + "/gameplay_hud.png");

            // ── 步骤 8: 改动后 QA ────────────────────────────────────────────
            Debug.Log("[INC] 步骤 8: 改动后碰撞/HUD QA…");
            RunQA("Logs/QuantumEcho-CollisionAndHudQA.txt");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[INC] ══════════════════════════════════════════════════════");
            Debug.Log("[INC]  升级完成！截图目录: " + ScreenDir);
            Debug.Log("[INC] ══════════════════════════════════════════════════════");
        }

        // ────────────────────────────────────────────────────────────────────
        // 碰撞/HUD QA 包装
        // ────────────────────────────────────────────────────────────────────
        static void RunQA(string destPath)
        {
            const string src = "Logs/QuantumEcho-CollisionAndHudQA.txt";
            bool needsCopy = !string.Equals(Path.GetFullPath(src), Path.GetFullPath(destPath),
                                            System.StringComparison.OrdinalIgnoreCase);
            try
            {
                QuantumEchoCollisionQA.RunBatch();
                if (needsCopy && File.Exists(src))
                    File.Copy(src, destPath, overwrite: true);
                Debug.Log("[INC] QA 结果: " + destPath);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[INC] QA 异常（继续）: " + ex.Message);
                if (needsCopy && File.Exists(src))
                    try { File.Copy(src, destPath, overwrite: true); } catch { /* 忽略二次拷贝失败 */ }
            }
        }

        // ────────────────────────────────────────────────────────────────────
        // 全息按钮 Sprite 导入（960×180，border=40）
        // ────────────────────────────────────────────────────────────────────
        static void EnsureHologramButtonSprite()
        {
            var importer = AssetImporter.GetAtPath(HoloBtnPath) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[INC] 全息按钮图未找到: " + HoloBtnPath); return; }
            importer.textureType      = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder     = new Vector4(40, 40, 40, 40);  // 960×180 图的 40px 边界
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled    = false;
            importer.maxTextureSize   = 1024;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            { name = "Android", overridden = true, maxTextureSize = 1024,
              format = TextureImporterFormat.ASTC_6x6 });
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log("[INC] 全息按钮 Sprite 导入完成（960×180，border=40）。");
        }

        // ────────────────────────────────────────────────────────────────────
        // 标题场景升级：字体 + 全息底图 + 音效
        // ────────────────────────────────────────────────────────────────────
        static void UpgradeTitleScene()
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            // 场景切换后重新加载（避免引用失效）
            var orbitron    = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            var holoSprite  = AssetDatabase.LoadAssetAtPath<Sprite>(HoloBtnPath);
            var hoverClip   = AssetDatabase.LoadAssetAtPath<AudioClip>(HoverClipPath);
            var confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(ConfirmPath);
            var confirmHard = AssetDatabase.LoadAssetAtPath<AudioClip>(ConfirmHardPath);
            Debug.Log($"[INC] 标题场景资产: orbitron={(orbitron!=null?"OK":"NULL")} sprite={(holoSprite!=null?"OK":"NULL")}");

            PrintStats("TITLE-BEFORE", scene);

            // ── 字体替换
            var tmpList = GetAllTMPInScene(scene);
            int fontPatched = 0;
            foreach (var t in tmpList)
                if (orbitron != null) { t.font = orbitron; EditorUtility.SetDirty(t); fontPatched++; }
            Debug.Log($"[INC] 标题：字体 {fontPatched}/{tmpList.Count} 个 → Orbitron");

            // ── 按钮：全息底图 + 音效
            var btns = GetAllInScene<QuantumTitleMenuButton>(scene);
            foreach (var btn in btns)
            {
                if (btn.audioSource == null)
                    foreach (var root in scene.GetRootGameObjects())
                    { btn.audioSource = root.GetComponentInChildren<AudioSource>(true); if (btn.audioSource != null) break; }
                btn.hoverClip   = hoverClip;
                btn.confirmClip = btn.action == QuantumTitleMenuButton.ActionKind.Exit ? confirmHard : confirmClip;
                if (btn.backingImage != null && holoSprite != null)
                {
                    btn.backingImage.sprite = holoSprite;
                    btn.backingImage.type   = Image.Type.Sliced;
                    btn.backingImage.pixelsPerUnitMultiplier = 4f;
                    btn.backingImage.color  = new Color(0.85f, 1f, 1f, 1f);  // 淡青白色调，不压暗全息图
                    EditorUtility.SetDirty(btn.backingImage);
                }
                EditorUtility.SetDirty(btn);
            }
            Debug.Log($"[INC] 标题：按钮 {btns.Count} 个（底图 + 音效）");

            // ── UI 安全验证
            foreach (var t in tmpList)
                if (t.GetComponent<Collider>() != null)
                    Debug.LogError("[INC] ❌ UI 含 Collider: " + t.gameObject.name);

            PrintStats("TITLE-AFTER", scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[INC] 标题场景已保存。");
        }

        // ────────────────────────────────────────────────────────────────────
        // 主场景升级：TMP 字体
        // ────────────────────────────────────────────────────────────────────
        static void UpgradeMainScene()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            var mono     = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MonoPath);
            Debug.Log($"[INC] 主场景资产: orbitron={(orbitron!=null?"OK":"NULL")} mono={(mono!=null?"OK":"NULL")}");

            PrintStats("MAIN-BEFORE", scene);

            var tmpList = GetAllTMPInScene(scene);
            int orbitronCount = 0, monoCount = 0;
            foreach (var t in tmpList)
            {
                bool isMono = t.name.Contains("Countdown") || t.name.Contains("Value") ||
                              t.name == "Echo Countdown" || t.name == "Timer Value" || t.name == "Progress Value";
                var target = (isMono && mono != null) ? mono : orbitron;
                if (target != null) { t.font = target; EditorUtility.SetDirty(t); if (isMono) monoCount++; else orbitronCount++; }
            }
            Debug.Log($"[INC] 主场景字体：Orbitron×{orbitronCount}  ShareTechMono×{monoCount}  共{tmpList.Count}");

            var labels = GetAllInScene<QuantumHologramWorldLabel>(scene);
            foreach (var lbl in labels) { if (orbitron != null) { lbl.font = orbitron; EditorUtility.SetDirty(lbl); } }
            Debug.Log($"[INC] QuantumHologramWorldLabel：{labels.Count} 个");

            // ── HUD 排版修复（v3 UI 精修：解决溢出 / 叠字）
            var allHUDs = GetAllInScene<QuantumHUD>(scene);
            if (allHUDs.Count > 0)
            {
                var hud = allHUDs[0];
                // 1. 目标标题：启用自动缩放防止"Stand on the Holder"顶到右边框
                if (hud.objectiveTitleText != null)
                {
                    hud.objectiveTitleText.enableAutoSizing = true;
                    hud.objectiveTitleText.fontSizeMin = 18f;
                    hud.objectiveTitleText.fontSizeMax = 30f;
                    EditorUtility.SetDirty(hud.objectiveTitleText);
                }
                // 2. 计时数值：右侧补 16px 内边距，"14.0s" 不贴边框
                if (hud.timerValueText != null)
                {
                    hud.timerValueText.margin = new Vector4(0f, 0f, 16f, 0f);
                    EditorUtility.SetDirty(hud.timerValueText);
                }
                // 3. Toast（反馈文字）：移入计时面板内部，24px 左右内边距，字号缩小；默认隐藏
                if (hud.toastText != null)
                {
                    var toastRT = hud.toastText.rectTransform;
                    // 新位置：计时面板内，进度条下方；anchoredPosition 相对 canvas 左上角 anchor
                    toastRT.anchoredPosition = new Vector2(176f, -109f);
                    toastRT.sizeDelta        = new Vector2(282f, 24f);
                    hud.toastText.fontSize   = 16f;
                    hud.toastText.margin     = new Vector4(0f, 0f, 0f, 0f);
                    hud.toastText.alignment  = TMPro.TextAlignmentOptions.Left;
                    hud.toastText.gameObject.SetActive(false);   // 默认不可见
                    EditorUtility.SetDirty(hud.toastText);
                    EditorUtility.SetDirty(toastRT);
                    EditorUtility.SetDirty(hud.toastText.gameObject);
                }
                // 4. 计时面板高度：98 → 120，为 Toast 腾出空间
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var tr in root.GetComponentsInChildren<Transform>(true))
                        if (tr.name == "Timer" && tr.GetComponent<RectTransform>() is RectTransform timerRT
                            && Mathf.Approximately(timerRT.sizeDelta.x, 330f)
                            && Mathf.Approximately(timerRT.sizeDelta.y, 98f))
                        {
                            timerRT.sizeDelta = new Vector2(330f, 120f);
                            EditorUtility.SetDirty(timerRT);
                        }
                // 5. 结算面板：截图时 Awake() 不运行，需在保存场景前显式隐藏
                if (hud.resultPanel != null)
                {
                    hud.resultPanel.SetActive(false);
                    EditorUtility.SetDirty(hud.resultPanel);
                }
                Debug.Log("[INC] HUD 排版修复完成（标题自动缩放 / 计时内边距 / Toast 重定位 / 结算面板隐藏）");
            }
            else
            {
                Debug.LogWarning("[INC] 未找到 QuantumHUD，跳过 HUD 排版修复");
            }

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
            Debug.Log($"[INC][{tag}] GOs={gos}  Box={box}  Sphere={sph}  Cap={cap}  Mesh={mesh}  RB={rb}");
            foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    var n = t.name;
                    if (n.StartsWith("Echo Lure Beacon") || n.Contains("Hunter Sentinel") || n.StartsWith("SENTINEL"))
                    {
                        int cols = t.GetComponentsInChildren<Collider>(true).Length;
                        Debug.Log($"[INC][{tag}]   {n}: colliders={cols} {(cols == 0 ? "OK" : "❌VIOLATION")}");
                    }
                }
        }

        // ────────────────────────────────────────────────────────────────────
        // 标题场景截图
        // idle=true: 常态（两按钮都显示）
        // hoverBtn=true: START 按钮悬停态（放大+高亮边框）
        // pressBtn=true: START 按钮按压态（缩小+琥珀色）
        // ────────────────────────────────────────────────────────────────────
        static void CaptureTitleShot(string outPng, bool idle, bool hoverBtn = false, bool pressBtn = false)
        {
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            // 每次截图后重加载字体，保证 TMP 字体已附
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);

            // 确保标题场景字体已设置
            foreach (var t in GetAllTMPInScene(scene))
                if (orbitron != null && (t.font == null || t.font.name != orbitron.name))
                { t.font = orbitron; EditorUtility.SetDirty(t); }

            var camGO = new GameObject("~SS_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.010f, 0.018f);
            cam.fieldOfView     = 44f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 40f;
            // 玩家眼位朝向标题终端
            camGO.transform.position = new Vector3(0f, 1.65f, 0f);
            camGO.transform.LookAt(new Vector3(0f, 1.65f, -3.85f));

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            // 设置按钮状态
            var btns = GetAllInScene<QuantumTitleMenuButton>(scene);
            foreach (var btn in btns)
            {
                // 重置缩放（避免上次截图的残留状态）
                btn.transform.localScale = Vector3.one;
                if (hoverBtn && btn.action == QuantumTitleMenuButton.ActionKind.Start)
                {
                    // 悬停：放大 3.5%，边框颜色提亮
                    btn.transform.localScale = Vector3.one * 1.035f;
                    if (btn.backingImage != null)
                        btn.backingImage.color = new Color(0.85f, 1f, 1f, 1f);
                    if (btn.label != null) btn.label.color = new Color(0.98f, 1f, 1f, 1f);
                }
                else if (pressBtn && btn.action == QuantumTitleMenuButton.ActionKind.Start)
                {
                    // 按压：缩小，琥珀色
                    btn.transform.localScale = Vector3.one * 0.965f;
                    if (btn.backingImage != null)
                        btn.backingImage.color = new Color(1f, 0.70f, 0.22f, 1f);
                    if (btn.label != null) btn.label.color = new Color(1f, 0.82f, 0.30f, 1f);
                }
                else
                {
                    // 常态：浅青白文字
                    if (btn.label != null) btn.label.color = new Color(0.78f, 0.95f, 1f, 1f);
                    if (btn.backingImage != null) btn.backingImage.color = new Color(0.85f, 1f, 1f, 1f);
                }
            }

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        // ────────────────────────────────────────────────────────────────────
        // HUD 截图（折叠/展开）
        // 关键：相机应在 HUD 正面（-forward 方向）
        // ────────────────────────────────────────────────────────────────────
        static void CaptureHUDShot(string outPng, bool expanded)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            foreach (var t in GetAllTMPInScene(scene))
                if (orbitron != null && (t.font == null || t.font.name != orbitron.name))
                { t.font = orbitron; EditorUtility.SetDirty(t); }

            var hudList = GetAllInScene<QuantumHUD>(scene);
            var hud     = hudList.Count > 0 ? hudList[0] : null;

            var camGO = new GameObject("~HUD_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.010f, 0.018f);
            cam.fieldOfView     = expanded ? 36f : 45f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 30f;

            // HUD 正面在 -forward 方向（Unity UI Canvas 可读面朝 -Z local）
            Vector3 hudPos = hud != null ? hud.transform.position : new Vector3(-5.45f, 1.62f, 2.25f);
            Vector3 hudFwd = hud != null ? hud.transform.forward  : Vector3.forward;
            float dist = expanded ? 1.5f : 2.5f;
            // 相机在 hudPos - hudFwd * dist（即 HUD 正面方向）
            camGO.transform.position = hudPos - hudFwd * dist + Vector3.up * 0.10f;
            camGO.transform.LookAt(hudPos + Vector3.up * 0.10f);

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            // 填充不同内容区分折叠/展开
            if (hud != null)
            {
                hud.SetLevel("Q-00", 1, "Tutorial Loop");
                hud.SetObjective(0, "Stand on the Holder",
                    expanded ? "Step onto the blue disc and seal it as an Echo. Wait for the timer to count down." : "…");
                hud.SetEchoCount(0, 4);
                hud.SetTimer(14f, 30f);
                hud.SetPrompt("[Grip / A / X]", expanded ? "Seal this action and start the next loop" : "…");

                // 展开态：显示进度面板；折叠态：隐藏
                if (hud.progressPanel != null)
                {
                    hud.progressPanel.SetActive(expanded);
                    EditorUtility.SetDirty(hud.progressPanel);
                }
                // 结算面板：截图时始终隐藏（Awake 在 batchmode 中不运行）
                if (hud.resultPanel != null)
                    hud.resultPanel.SetActive(false);
                // Toast：展开态显示"Loop Started"演示新排版；折叠态隐藏
                if (hud.toastText != null)
                {
                    hud.toastText.gameObject.SetActive(expanded);
                    if (expanded)
                        hud.toastText.text = "Loop Started";
                }
            }

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        // ────────────────────────────────────────────────────────────────────
        // 主场景总览截图（HUD + 世界铭牌）
        // ────────────────────────────────────────────────────────────────────
        static void CaptureMainShot(string outPng)
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var orbitron = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitronPath);
            foreach (var t in GetAllTMPInScene(scene))
                if (orbitron != null && (t.font == null || t.font.name != orbitron.name))
                { t.font = orbitron; EditorUtility.SetDirty(t); }

            var hudList = GetAllInScene<QuantumHUD>(scene);
            var hud     = hudList.Count > 0 ? hudList[0] : null;

            // 填充 HUD 内容
            if (hud != null)
            {
                hud.SetLevel("Q-00", 1, "Tutorial Loop");
                hud.SetObjective(0, "Stand on the Holder", "Step onto the blue disc.");
                hud.SetEchoCount(2, 4);
                hud.SetTimer(22f, 30f);
                hud.SetPrompt("[Grip / A / X]", "Seal and echo");
                // 截图时始终隐藏结算面板和 Toast（Awake 在 batchmode 中不运行）
                if (hud.resultPanel != null) hud.resultPanel.SetActive(false);
                if (hud.toastText != null) hud.toastText.gameObject.SetActive(false);
            }

            // 相机：从玩家出生点朝向 HUD + 世界铭牌区域
            var camGO = new GameObject("~Main_Cam");
            var cam   = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.005f, 0.010f, 0.018f);
            cam.fieldOfView     = 65f;
            cam.nearClipPlane   = 0.05f;
            cam.farClipPlane    = 30f;

            // 根据 HUD 位置决定相机放置：站在玩家出生点旁边，朝向同时包含 HUD 和场景
            Vector3 hudPos = hud != null ? hud.transform.position : new Vector3(-5.45f, 1.62f, 2.25f);
            Vector3 hudFwd = hud != null ? hud.transform.forward  : Vector3.forward;
            // 站在 HUD 正面稍偏侧，向内看
            Vector3 camPos = hudPos - hudFwd * 3.0f + Vector3.right * 2.0f + Vector3.up * 0.2f;
            camGO.transform.position = camPos;
            camGO.transform.LookAt(hudPos + Vector3.up * 0.2f);

            foreach (var canvas in GetAllInScene<Canvas>(scene))
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;

            ForceTMPUpdate(scene);
            RenderToCam(cam, outPng, 1920, 1080);
            Object.DestroyImmediate(camGO);
        }

        // ────────────────────────────────────────────────────────────────────
        // 辅助
        // ────────────────────────────────────────────────────────────────────
        static void ForceTMPUpdate(Scene scene)
        {
            foreach (var t in GetAllTMPInScene(scene))
                try { t.ForceMeshUpdate(true, true); }
                catch (System.Exception ex)
                { Debug.LogWarning("[INC] ForceMeshUpdate: " + t.gameObject.name + " " + ex.Message); }
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
