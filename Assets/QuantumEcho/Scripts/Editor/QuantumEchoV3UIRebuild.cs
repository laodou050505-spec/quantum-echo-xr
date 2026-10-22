using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using TMPro;
using UnityEngine.UI;

namespace QuantumEcho.Editor
{
    /// <summary>
    /// v3 UI/UX精修一键脚本。
    /// 在 batchmode 中执行，替换 HUD 面板皮肤、世界标签、开始菜单按钮字体、音效和手柄震动。
    /// 入口方法：QuantumEcho.Editor.QuantumEchoV3UIRebuild.RebuildAll
    ///
    /// 调用方式（总控 batchmode）：
    ///   lockf /tmp/en-unity-A.lock /Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity \
    ///     -projectPath /Users/ld/Documents/Codex/EnglishEditions/QuantumEcho-EN \
    ///     -buildTarget Android -batchmode -nographics -logFile /dev/stdout \
    ///     -executeMethod QuantumEcho.Editor.QuantumEchoV3UIRebuild.RebuildAll -quit
    /// </summary>
    public static class QuantumEchoV3UIRebuild
    {
        // ── Font paths ──────────────────────────────────────────────────────
        const string OrbitronTtfPath   = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Regular.ttf";
        const string OrbitronBoldPath  = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Bold.ttf";
        const string MonoTtfPath       = "Assets/QuantumEcho/Fonts/SciFi/ShareTechMono-Regular.ttf";
        const string OrbitronAssetPath = "Assets/QuantumEcho/Fonts/SciFi/Orbitron-Dynamic.asset";
        const string MonoAssetPath     = "Assets/QuantumEcho/Fonts/SciFi/ShareTechMono-Dynamic.asset";
        const string FallbackFontPath  = "Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset";

        // ── Panel / button image paths ───────────────────────────────────────
        const string HoloPanelPng  = "Assets/Resources/QuantumEcho/QuantumHologramPanel.png";
        const string HoloBtnPng    = "Assets/QuantumEcho/UI/Generated/ButtonV3/QuantumHologramButton.png";

        // ── Audio paths ──────────────────────────────────────────────────────
        const string HoverClipPath   = "Assets/QuantumEcho/Audio/ui_hover_scifi.mp3";
        const string ConfirmClipPath = "Assets/QuantumEcho/Audio/ui_confirm_scifi.mp3";
        const string ConfirmHardPath = "Assets/QuantumEcho/Audio/ui_confirm_hard.mp3";

        [MenuItem("Quantum Echo/v3 UI Rebuild (batchmode safe)")]
        public static void RebuildAll()
        {
            Debug.Log("[QE-v3] Starting UI rebuild…");
            AssetDatabase.Refresh();

            Step1_GenerateFontAssets();
            Step2_ImportHologramSprites();
            Step1b_RewireAtlasTextures();   // re-wire after sprite reimports
            AssetDatabase.Refresh();        // flush rewire so LoadAssetAtPath sees the updated asset
            Step3_RebuildTitleScene();
            Step4_RebuildMainScene();
            Step5_SaveAll();

            Debug.Log("[QE-v3] UI rebuild complete.");
            PrintFontSizeReport();
        }

        /// <summary>
        /// Batchmode entry: rebuild + screenshots (run WITHOUT -nographics so rendering works).
        /// lockf /tmp/en-unity-A.lock Unity -batchmode -buildTarget Android -executeMethod QuantumEcho.Editor.QuantumEchoV3UIRebuild.RebuildAndScreenshot -quit
        /// </summary>
        [MenuItem("Quantum Echo/v3 Rebuild + Screenshots")]
        public static void RebuildAndScreenshot()
        {
            RebuildAll();
            CaptureScreenshots();
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 1: Generate TMP FontAsset from TTF files
        // Requires Unity to be launched WITHOUT -nographics so FreeType works.
        // Uses AtlasPopulationMode.Dynamic so atlas is populated at runtime —
        // clean asset, no residual Noto glyph tables.
        // ────────────────────────────────────────────────────────────────────
        static void Step1_GenerateFontAssets()
        {
            Debug.Log("[QE-v3] Step 1: Generating TMP font assets (Dynamic mode)…");

            GenerateDynamicFontAsset(OrbitronTtfPath, OrbitronAssetPath, samplingPointSize: 90, atlasWidth: 1024, atlasHeight: 1024);
            GenerateDynamicFontAsset(MonoTtfPath,    MonoAssetPath,    samplingPointSize: 72, atlasWidth: 1024, atlasHeight: 1024);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QE-v3] Font assets generated.");
        }

        // Characters pre-baked into the atlas so screenshots work in Edit mode
        // (Dynamic font Awake() is not called in Edit mode, so we bake glyphs upfront)
        const string BakeCharSet =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz" +
            "0123456789 !\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~" +
            "▶";   // U+25B6 — used as countdown prefix icon

        /// <summary>
        /// Creates a clean Dynamic TMP_FontAsset from a TTF and immediately pre-bakes
        /// BakeCharSet into the atlas so the font renders correctly in Edit-mode screenshots.
        ///
        /// Strategy A: TMP_FontAsset.CreateFontAsset (needs FreeType; may fail in batchmode).
        /// Strategy B: FontEngine.LoadFontFace(filePath) + manual asset init via reflection.
        ///
        /// Always recreates to guarantee a clean asset with no residual Noto glyph tables.
        /// </summary>
        static void GenerateDynamicFontAsset(string ttfPath, string assetPath,
            int samplingPointSize, int atlasWidth, int atlasHeight)
        {
            // Force-delete existing asset so we always start clean
            if (File.Exists(Path.Combine(Application.dataPath, "..",
                    assetPath.Replace('/', Path.DirectorySeparatorChar))))
            {
                AssetDatabase.DeleteAsset(assetPath);
                Debug.Log($"[QE-v3] Deleted old font asset: {assetPath}");
            }

            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null)
            {
                Debug.LogWarning($"[QE-v3] TTF not found at {ttfPath}, skipping.");
                return;
            }

            // ── Strategy A: standard TMP API (works when FreeType is available) ──
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf, samplingPointSize, atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth, atlasHeight,
                atlasPopulationMode: AtlasPopulationMode.Dynamic);

            if (fontAsset != null)
            {
                fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                // Pre-bake glyphs so Edit-mode screenshots render text
                bool allAdded = fontAsset.TryAddCharacters(BakeCharSet);
                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[QE-v3] [StrategyA] Dynamic font asset: {assetPath} — glyphs pre-baked: {allAdded}");
                return;
            }

            // ── Strategy B: FontEngine from file path + manual asset init ──
            // In batchmode, FontEngine.LoadFontFace(Font) fails because Unity's
            // Font.fontData is empty; loading from the raw file path works.
            Debug.LogWarning($"[QE-v3] Strategy A failed (batchmode FreeType limit). Trying Strategy B for {ttfPath}");

            FontEngine.InitializeFontEngine();
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            var fullPath    = Path.Combine(projectRoot, ttfPath.Replace('/', Path.DirectorySeparatorChar));
            var faceErr = FontEngine.LoadFontFace(fullPath, samplingPointSize);
            if (faceErr != FontEngineError.Success)
            {
                Debug.LogError($"[QE-v3] FontEngine.LoadFontFace({fullPath}) failed: {faceErr}. Cannot create font asset.");
                return;
            }

            var faceInfo = FontEngine.GetFaceInfo();
            Debug.Log($"[QE-v3] Loaded font face: {faceInfo.familyName} {faceInfo.styleName} pt{samplingPointSize}");

            // Construct a clean Dynamic TMP_FontAsset via reflection.
            fontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);

            var bf = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            SetField(fontAsset, "m_SourceFontFile",       bf, ttf);
            // KEY FIX: set the file-path fallback so LoadFontFace() succeeds in batchmode.
            // TMP_FontAsset.LoadFontFace() tries FontEngine.LoadFontFace(Font, …) first; if
            // that fails (batchmode, empty fontData), it falls back to LoadFontFace(filePath).
            // Without this field, TryAddCharacters silently fails with all characters missing.
            SetField(fontAsset, "m_SourceFontFilePath",   bf, fullPath);
            SetField(fontAsset, "m_AtlasPopulationMode",  bf, (int)AtlasPopulationMode.Dynamic);
            SetField(fontAsset, "m_FaceInfo",             bf, faceInfo);
            SetField(fontAsset, "m_AtlasWidth",           bf, atlasWidth);
            SetField(fontAsset, "m_AtlasHeight",          bf, atlasHeight);
            SetField(fontAsset, "m_AtlasPadding",         bf, 9);
            SetField(fontAsset, "m_AtlasRenderMode",      bf, (int)GlyphRenderMode.SDFAA);
            // KEY FIX: set version so TMP.ReadFontAssetDefinition() does NOT run UpgradeFontAsset()
            // Condition is: material != null && m_Version == "" → UpgradeFontAsset → NRE (m_fontInfo null)
            // CreateFontAssetInstance sets this to "1.1.0"; our manual path must also set it.
            SetField(fontAsset, "m_Version",              bf, "1.1.0");
            SetField(fontAsset, "m_GlyphTable",             bf, new System.Collections.Generic.List<UnityEngine.TextCore.Glyph>());
            SetField(fontAsset, "m_CharacterTable",         bf, new System.Collections.Generic.List<TMP_Character>());
            SetField(fontAsset, "m_GlyphLookupDictionary",  bf, new System.Collections.Generic.Dictionary<uint, UnityEngine.TextCore.Glyph>());
            SetField(fontAsset, "m_CharacterLookupDictionary", bf, new System.Collections.Generic.Dictionary<uint, TMP_Character>());
            // Needed by TryAddGlyphsToTexture; freeGlyphRects/usedGlyphRects are internal so use reflection.
            // GlyphRect is in UnityEngine.TextCore (not LowLevel).
            SetField(fontAsset, "m_FreeGlyphRects", bf,
                new System.Collections.Generic.List<UnityEngine.TextCore.GlyphRect>
                    { new UnityEngine.TextCore.GlyphRect(0, 0, atlasWidth - 1, atlasHeight - 1) });
            SetField(fontAsset, "m_UsedGlyphRects", bf,
                new System.Collections.Generic.List<UnityEngine.TextCore.GlyphRect>());

            // CRITICAL: inject a valid atlas texture BEFORE calling TryAddCharacters.
            // TryAddCharacters calls FontEngine.RenderGlyphsToTexture(m_AtlasTextures[0], …).
            // If m_AtlasTextures is null, the call silently fails with all chars "missing".
            var atlasTex = new Texture2D(atlasWidth, atlasHeight, TextureFormat.Alpha8, false, true);
            atlasTex.name = fontAsset.name + " Atlas";
            atlasTex.Apply(false, false);   // upload blank pixels; keeps texture accessible from CPU
            SetField(fontAsset, "m_AtlasTextures", bf, new Texture2D[] { atlasTex });

            // Pre-bake glyphs while FontEngine still has the face loaded
            string missing;
            bool bakeOk = fontAsset.TryAddCharacters(BakeCharSet, out missing);
            Debug.Log($"[QE-v3] [StrategyB] TryAddCharacters: ok={bakeOk}, missing='{missing}', glyphs={fontAsset.glyphTable?.Count}");

            // Step 1: persist the main asset to disk.
            // At this point m_AtlasTextures[0] = atlasTex (in-memory); Unity will serialize
            // it as null until we AddObjectToAsset and re-wire below.
            AssetDatabase.CreateAsset(fontAsset, assetPath);

            // Step 2: add the atlas texture as a named sub-asset
            AssetDatabase.AddObjectToAsset(atlasTex, assetPath);
            AssetDatabase.SaveAssets();

            // Step 3: re-load the persisted asset AND the persisted sub-asset texture,
            // then wire atlasTextures[0] via SerializedObject for correct internal reference.
            var persistedAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            Texture2D persistedTex = null;
            var allObjs = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var obj in allObjs)
                if (obj is Texture2D t) { persistedTex = t; break; }

            if (persistedAsset != null && persistedTex != null)
            {
                var so = new SerializedObject(persistedAsset);
                var texArray = so.FindProperty("m_AtlasTextures");
                if (texArray != null)
                {
                    texArray.arraySize = 1;
                    texArray.GetArrayElementAtIndex(0).objectReferenceValue = persistedTex;
                    so.ApplyModifiedProperties();
                }
                else
                {
                    Debug.LogWarning("[QE-v3] [StrategyB] m_AtlasTextures SerializedProperty not found; using public setter.");
                    persistedAsset.atlasTextures = new Texture2D[] { persistedTex };
                    EditorUtility.SetDirty(persistedAsset);
                }

                // Explicitly create the atlas material so TMP_Text.fontSharedMaterial is non-null
                // when ForceMeshUpdate is called in Edit mode (where Awake() may not fully init it).
                // Without this, ForceMeshUpdate throws NullReferenceException and text is invisible.
                var sdfShader = Shader.Find("TextMeshPro/Distance Field");
                if (sdfShader == null) sdfShader = Shader.Find("TextMeshPro/Mobile/Distance Field");
                if (sdfShader != null)
                {
                    var atlasMat = new Material(sdfShader);
                    atlasMat.name = persistedAsset.name + " Atlas Material";
                    atlasMat.SetTexture(ShaderUtilities.ID_MainTex, persistedTex);
                    ShaderUtilities.GetShaderPropertyIDs();
                    AssetDatabase.AddObjectToAsset(atlasMat, assetPath);
                    persistedAsset.material = atlasMat;
                    Debug.Log($"[QE-v3] [StrategyB] Created atlas material: {atlasMat.name}");
                }
                else
                {
                    Debug.LogWarning("[QE-v3] [StrategyB] TextMeshPro/Distance Field shader not found; material NOT created.");
                }

                AssetDatabase.SaveAssets();
                var verify = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
                Debug.Log($"[QE-v3] [StrategyB] atlas wired: {verify?.atlasTexture?.name ?? "NULL"} | glyphs={verify?.glyphTable?.Count} | mat={verify?.material?.name ?? "NULL"}");
                Debug.Log($"[QE-v3] [StrategyB] Created dynamic font asset: {assetPath} ({faceInfo.familyName})");
            }
            else
            {
                Debug.LogError($"[QE-v3] [StrategyB] atlas wiring failed. persistedAsset={persistedAsset != null}, persistedTex={persistedTex != null}");
            }
        }

        static void SetField(object obj, string name, System.Reflection.BindingFlags bf, object value)
        {
            // Walk the inheritance chain so base-class private/internal fields are found.
            // e.g. m_Version is declared on TMP_Asset (base of TMP_FontAsset).
            var type = obj.GetType();
            System.Reflection.FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(name, bf);
                type = type.BaseType;
            }
            if (field != null) field.SetValue(obj, value);
            else Debug.LogWarning($"[QE-v3] SetField: field '{name}' not found on {obj.GetType().Name} or any base class");
        }

        // ────────────────────────────────────────────────────────────────────
        // Screenshots: render each scene's main camera to PNG files
        // Must be called in a batchmode session WITHOUT -nographics
        // Output directory: Builds/EN/Screenshots/
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("Quantum Echo/v3 Capture Screenshots")]
        public static void CaptureScreenshots()
        {
            Debug.Log("[QE-v3] Capturing screenshots…");
            var outDir = "Builds/EN/Screenshots";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            // ── Title menu: normal + hover state ──────────────────────────────
            // Camera uses the scene's XR Title Camera (tagged MainCamera) at (0,1.65,0)
            // looking toward the terminal at z=-3.85 with the 4.8×2.7 cover art.
            CaptureTitleScene(false, outDir + "/title_normal.png", 1920, 1080);
            CaptureTitleScene(true,  outDir + "/title_hover.png",  1920, 1080);

            // ── Gameplay HUD: collapsed + expanded ────────────────────────────
            // HUD canvas is at WorldTaskTerminalAnchor (-5.45, 1.62, 2.25).
            // Screenshot camera looks at that point from 1.8 m away.
            CaptureHUD(false, outDir + "/hud_collapsed.png", 1920, 1080);
            CaptureHUD(true,  outDir + "/hud_expanded.png",  1920, 1080);

            // ── World labels close-up ─────────────────────────────────────────
            // "Tutorial Field" cyan label at (0, 2.35, -0.72) and
            // "Q-00 // Core" at (0, 2.65, -4.15) — camera stands at (0, 2.5, 2.5)
            // and looks toward (0, 2.5, -2.0) to frame both labels in a wide shot.
            CaptureWorldLabels(outDir + "/world_labels.png", 1920, 1080);

            // ── Gameplay overview (unchanged) ─────────────────────────────────
            CaptureGameplayScene("Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity",
                outDir + "/gameplay_hud.png", 1920, 1080);

            Debug.Log($"[QE-v3] Screenshots saved to {outDir}/");
        }

        // Force every TMP_Text in the loaded scene to rebuild its mesh with the current
        // font asset. Needed in Edit-mode because TMP_FontAsset.Awake() never fires,
        // so glyphs are not added to the atlas automatically.
        static void ForceTMPMeshUpdates()
        {
            foreach (var tm in Object.FindObjectsByType<TMPro.TMP_Text>(FindObjectsSortMode.None))
            {
                try { tm.ForceMeshUpdate(true, true); }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[QE-v3] ForceMeshUpdate on '{tm.gameObject.name}': {ex.Message}");
                }
            }
            UnityEngine.Canvas.ForceUpdateCanvases();
        }

        // Title scene: normal or hover state.
        // Always creates a dedicated camera at player eye height (1.65 m) looking toward
        // the cover-art terminal at z ≈ -3.85. The XR Camera.main has a TrackedPoseDriver
        // that stays at (0,0,0) in batchmode (no HMD), so we bypass it entirely.
        static void CaptureTitleScene(bool hoverStartButton, string outPng, int w, int h)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            // Dedicated screenshot camera — do NOT use Camera.main here because the
            // TrackedPoseDriver doesn't run in batchmode.
            var camGO = new GameObject("~SS_Cam");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView = 44f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 40f;
            // Player spawn is at (0,0,0); eye height 1.65 m; face the terminal panel.
            camGO.transform.position = new Vector3(0f, 1.65f, 0f);
            camGO.transform.LookAt(new Vector3(0f, 1.65f, -3.85f));

            // Assign camera to WorldSpace canvases so they render
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace)
                    canvas.worldCamera = cam;

            // Optionally put the START GAME button into hover state
            if (hoverStartButton)
            {
                foreach (var btn in Object.FindObjectsByType<QuantumTitleMenuButton>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (btn.name.Contains("START") || btn.name.Contains("Start"))
                    {
                        btn.SetFocused(true);
                        break;
                    }
                }
            }

            ForceTMPMeshUpdates();
            RenderToCam(cam, outPng, w, h);
            Object.DestroyImmediate(camGO);
        }

        // Gameplay HUD: collapsed or expanded.
        // The HUD terminal is at (-5.45, 1.62, 2.25) facing the player spawn at ≈(0,1.25,0).
        // Camera sits at the player's eye position (0, 1.5, 0) and looks toward the terminal.
        static void CaptureHUD(bool expanded, string outPng, int w, int h)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            var camGO = new GameObject("~HUD_Cam");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;

            // Place the camera in front of the HUD's rendered face.
            // The HUD canvas faces toward the player spawn area; we determine the
            // correct position by reading the HUD's actual world transform, then
            // moving 3.5 m along its forward vector so we see the front, not the back.
            var hud = Object.FindFirstObjectByType<QuantumHUD>();
            Vector3 hudPos = new Vector3(-5.45f, 1.62f, 2.25f);
            Vector3 hudFwd = Vector3.forward;
            if (hud != null) { hudPos = hud.transform.position; hudFwd = hud.transform.forward; }
            var camPos = hudPos + hudFwd * 3.5f + new Vector3(0f, 0.15f, 0f);
            camGO.transform.position = camPos;
            camGO.transform.LookAt(hudPos + new Vector3(0f, 0.15f, 0f));

            // Wire the camera to WorldSpace canvases
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace)
                    canvas.worldCamera = cam;

            // Set sample content so panels are not all blank (hud already found above)
            if (hud != null)
            {
                Debug.Log($"[QE-v3] HUD found at world pos {hud.transform.position} rot {hud.transform.eulerAngles}");
                hud.SetLevel("Q-00", 1, "Tutorial Loop");
                hud.SetObjective(0, "Stand on the Holder",
                    "Step onto the blue disc and seal it as an Echo.");
                hud.SetEchoCount(0, 4);
                hud.SetTimer(14f, 30f);
                hud.SetPrompt("[Grip / A / X]", "Seal this action and start the next loop");
            }
            else { Debug.LogWarning("[QE-v3] QuantumHUD not found in scene"); }

            ForceTMPMeshUpdates();
            RenderToCam(cam, outPng, w, h);
            Object.DestroyImmediate(camGO);
        }

        // World labels close-up: frames "Tutorial Field" (0,2.35,-0.72 cyan) and
        // "Q-00 // Core" (0,2.65,-4.15 cyan) by placing camera at (0,2.5,2.0)
        // looking toward (0,2.5,-2.0) with a wide FOV.
        static void CaptureWorldLabels(string outPng, int w, int h)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity",
                UnityEditor.SceneManagement.OpenSceneMode.Single);

            // QuantumHologramWorldLabel.Build() is called from Awake() which does not
            // fire in Edit mode. Force-initialize every label via reflection.
            var buildMethod = typeof(QuantumHologramWorldLabel).GetMethod(
                "Build",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (var label in Object.FindObjectsByType<QuantumHologramWorldLabel>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                try { buildMethod?.Invoke(label, null); }
                catch (System.Exception ex)
                { Debug.LogWarning($"[QE-v3] WorldLabel Build failed on '{label.name}': {ex.Message}"); }
            }

            var camGO = new GameObject("~Label_Cam");
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.008f, 0.014f, 0.022f);
            cam.fieldOfView = 65f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 30f;
            // Stand a bit in front of the field, aim toward the label area
            camGO.transform.position = new Vector3(0f, 2.0f, 2.5f);
            camGO.transform.LookAt(new Vector3(0f, 2.5f, -2.0f));

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (canvas.renderMode == RenderMode.WorldSpace)
                    canvas.worldCamera = cam;

            ForceTMPMeshUpdates();
            RenderToCam(cam, outPng, w, h);
            Object.DestroyImmediate(camGO);
        }

        static void CaptureGameplayScene(string scenePath, string outPng, int w, int h)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

            // Use scene's main camera; if none, create one at player eye height (1.5 m)
            Camera cam = Camera.main;
            bool createdCam = false;
            if (cam == null)
            {
                var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                if (cams.Length > 0) cam = cams[0];
            }
            if (cam == null)
            {
                var camGO = new GameObject("~SS_Cam");
                cam = camGO.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 60f;
                camGO.transform.position = new Vector3(0f, 1.5f, 0f);
                createdCam = true;
            }
            // Assign camera to any un-assigned World Space canvases so they render
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    canvas.worldCamera = cam;
            }

            ForceTMPMeshUpdates();
            RenderToCam(cam, outPng, w, h);
            if (createdCam) Object.DestroyImmediate(cam.gameObject);
        }

        static void RenderToCam(Camera cam, string outPng, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.antiAliasing = 4;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.ARGB32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(rt);
            File.WriteAllBytes(outPng, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Debug.Log($"[QE-v3] Screenshot saved: {outPng}");
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 2: Ensure hologram sprites are imported as UI Sprites with correct borders
        // ────────────────────────────────────────────────────────────────────
        static void Step2_ImportHologramSprites()
        {
            Debug.Log("[QE-v3] Step 2: Importing hologram sprites…");
            ImportSprite(HoloPanelPng, border: new Vector4(64, 64, 64, 64));
            ImportSprite(HoloBtnPng,   border: new Vector4(80, 80, 80, 80));
            AssetDatabase.Refresh();
            Debug.Log("[QE-v3] Sprites imported.");
        }

        static void ImportSprite(string path, Vector4 border)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[QE-v3] No TextureImporter for {path}");
                return;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            var androidSettings = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 2048,
                format = TextureImporterFormat.ASTC_6x6,
            };
            importer.SetPlatformTextureSettings(androidSettings);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[QE-v3] Imported sprite: {path}");
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 1b: Re-wire atlas textures after sprite reimports may have
        //          invalidated in-memory references (sub-assets survive on
        //          disk but the m_AtlasTextures reference can become null
        //          after an AssetDatabase reimport pass).
        // ────────────────────────────────────────────────────────────────────
        static void Step1b_RewireAtlasTextures()
        {
            Debug.Log("[QE-v3] Step 1b: Re-wiring font atlas textures…");
            RewireAtlas(OrbitronAssetPath);
            RewireAtlas(MonoAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[QE-v3] Atlas textures re-wired.");
        }

        static void RewireAtlas(string assetPath)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset == null)
            {
                Debug.LogWarning($"[QE-v3] RewireAtlas: font asset not found at {assetPath}");
                return;
            }

            // Find the atlas sub-asset (it survives disk even if the reference is null)
            Texture2D persistedTex = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (obj is Texture2D t) { persistedTex = t; break; }
            }

            if (persistedTex == null)
            {
                Debug.LogWarning($"[QE-v3] RewireAtlas: no Texture2D sub-asset found in {assetPath}");
                return;
            }

            // Check if already wired correctly
            if (fontAsset.atlasTexture != null)
            {
                Debug.Log($"[QE-v3] RewireAtlas: {fontAsset.name} already has atlas '{fontAsset.atlasTexture.name}' — skip");
                return;
            }

            // Re-wire via SerializedObject
            var so = new SerializedObject(fontAsset);
            var texArray = so.FindProperty("m_AtlasTextures");
            if (texArray == null)
            {
                Debug.LogWarning($"[QE-v3] RewireAtlas: m_AtlasTextures not found on {fontAsset.name}");
                return;
            }
            texArray.arraySize = 1;
            texArray.GetArrayElementAtIndex(0).objectReferenceValue = persistedTex;
            so.ApplyModifiedProperties();
            Debug.Log($"[QE-v3] RewireAtlas: {fontAsset.name} → atlas '{persistedTex.name}' (verify: {fontAsset.atlasTexture?.name ?? "NULL"})");
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 3: Rebuild the title scene (button sprites, fonts, audio)
        // ────────────────────────────────────────────────────────────────────
        static void Step3_RebuildTitleScene()
        {
            Debug.Log("[QE-v3] Step 3: Rebuilding title scene…");
            QuantumEchoTitleSceneBuilder.BuildTitleScene();
            Debug.Log("[QE-v3] Title scene rebuilt.");
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 4: Rebuild the main gameplay scene (hologram labels, new HUD skin)
        // ────────────────────────────────────────────────────────────────────
        static void Step4_RebuildMainScene()
        {
            Debug.Log("[QE-v3] Step 4: Rebuilding main gameplay scene…");
            QuantumEchoSceneBuilder.BuildAll();
            Debug.Log("[QE-v3] Main scene rebuilt.");
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 5: Save all assets and scenes
        // ────────────────────────────────────────────────────────────────────
        static void Step5_SaveAll()
        {
            Debug.Log("[QE-v3] Step 5: Saving all assets…");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QE-v3] All assets saved.");
        }

        // ────────────────────────────────────────────────────────────────────
        // VR Readability Report (Rule C) - called standalone for verification
        // Viewing distance: comfort HUD panel = 2.1 m, world labels = 2.5 m
        // ────────────────────────────────────────────────────────────────────
        [MenuItem("Quantum Echo/v3 Print VR Font Size Report")]
        public static void PrintFontSizeReport()
        {
            // ── Comfort Objective HUD (QuantumComfortObjectiveHUD) ──────────
            // In TMP UGUI WorldSpace canvas: worldHeight = fontSize (local canvas units) × localScale
            // Canvas localScale = MaximumScale = 0.00175 (set in UpdatePlacement → scale = MaximumScale)
            // ViewingDistance = 2.1 m
            // Rule C min body  = d × 0.017 = 2.1 × 0.017 = 0.0357 m
            // Rule C min sec   = d × 0.012 = 2.1 × 0.012 = 0.0252 m
            // Rule C min title = d × 0.028 = 2.1 × 0.028 = 0.0588 m
            const float hudScale = 0.00175f;
            const float hudDist  = 2.1f;
            const float minBody  = hudDist * 0.017f;   // 0.0357 m
            const float minSec   = hudDist * 0.012f;   // 0.0252 m
            const float minTitle = hudDist * 0.028f;   // 0.0588 m

            // v3 font sizes from QuantumComfortObjectiveHUD.Create()
            float LevelSz    = 22f  * hudScale; // 0.0385 m
            float ObjectiveSz= 38f  * hudScale; // 0.0665 m
            float DetailSz   = 27f  * hudScale; // 0.0473 m
            float PromptSz   = 27f  * hudScale; // 0.0473 m
            float CountdownSz= 23f  * hudScale; // 0.0403 m
            float ControlsSz = 26f  * hudScale; // 0.0455 m

            Debug.Log("[QE-v3] ── VR Font Size Report (Rule C) ──");
            Debug.Log($"[QE-v3] HUD viewing dist: {hudDist} m | min body {minBody*100:F2} cm | min title {minTitle*100:F2} cm | min sec {minSec*100:F2} cm");
            Debug.Log($"[QE-v3] [HUD] Level label  : {LevelSz*100:F2} cm >= {minSec*100:F2} cm (min-sec)  → {(LevelSz>=minSec?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [HUD] Objective    : {ObjectiveSz*100:F2} cm >= {minTitle*100:F2} cm (min-title)→ {(ObjectiveSz>=minTitle?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [HUD] Detail       : {DetailSz*100:F2} cm >= {minBody*100:F2} cm (min-body) → {(DetailSz>=minBody?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [HUD] Prompt       : {PromptSz*100:F2} cm >= {minBody*100:F2} cm (min-body) → {(PromptSz>=minBody?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [HUD] Countdown    : {CountdownSz*100:F2} cm >= {minSec*100:F2} cm (min-sec)  → {(CountdownSz>=minSec?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [HUD] Controls     : {ControlsSz*100:F2} cm >= {minSec*100:F2} cm (min-sec)  → {(ControlsSz>=minSec?"PASS":"FAIL")}");

            // ── World labels (QuantumHologramWorldLabel) ─────────────────────
            // fontSize in TextMeshPro (3D) corresponds to world-space units directly
            // Typical approach distance: 2.5 m
            const float lblDist    = 2.5f;
            const float lblMinBody = lblDist * 0.017f; // 0.0425 m
            const float lblMinSec  = lblDist * 0.012f; // 0.0300 m
            float primaryLblSz   = 0.12f;  // world meters (PrimarySize constant)
            float secondaryLblSz = 0.085f; // world meters (SecondarySize constant)
            Debug.Log($"[QE-v3] World label dist: {lblDist} m | min-body {lblMinBody*100:F2} cm | min-sec {lblMinSec*100:F2} cm");
            Debug.Log($"[QE-v3] [LABEL] Primary   : {primaryLblSz*100:F2} cm >= {lblMinBody*100:F2} cm → {(primaryLblSz>=lblMinBody?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [LABEL] Secondary  : {secondaryLblSz*100:F2} cm >= {lblMinSec*100:F2} cm  → {(secondaryLblSz>=lblMinSec?"PASS":"FAIL")}");

            // ── Title menu buttons (1600×900 canvas, scale 0.003) ──────────
            // worldHeight = fontSize × scale = 30 × 0.003 = 0.09 m
            const float menuScale = 0.003f;
            const float menuDist  = 2.7f;
            const float menuMin   = menuDist * 0.017f; // 0.046 m
            const float menuMinSec= menuDist * 0.012f; // 0.032 m
            float startBtnSz = 30f * menuScale;        // 0.09 m
            float hintSz     = 22f * menuScale;        // 0.066 m
            Debug.Log($"[QE-v3] Menu button dist: {menuDist} m | min-body {menuMin*100:F2} cm | min-sec {menuMinSec*100:F2} cm");
            Debug.Log($"[QE-v3] [MENU] START GAME: {startBtnSz*100:F2} cm >= {menuMin*100:F2} cm → {(startBtnSz>=menuMin?"PASS":"FAIL")}");
            Debug.Log($"[QE-v3] [MENU] Hint text : {hintSz*100:F2} cm >= {menuMinSec*100:F2} cm → {(hintSz>=menuMinSec?"PASS":"FAIL")}");
        }
    }
}
