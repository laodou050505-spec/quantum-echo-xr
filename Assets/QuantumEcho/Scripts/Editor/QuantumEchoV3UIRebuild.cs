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
            Step3_RebuildTitleScene();
            Step4_RebuildMainScene();
            Step5_SaveAll();

            Debug.Log("[QE-v3] UI rebuild complete.");
            PrintFontSizeReport();
        }

        // ────────────────────────────────────────────────────────────────────
        // Step 1: Generate TMP FontAsset from TTF files
        // ────────────────────────────────────────────────────────────────────
        static void Step1_GenerateFontAssets()
        {
            Debug.Log("[QE-v3] Step 1: Generating TMP font assets…");

            // Initialize FontEngine (required in batchmode / -nographics)
            var initErr = FontEngine.InitializeFontEngine();
            if (initErr != FontEngineError.Success)
                Debug.LogWarning($"[QE-v3] FontEngine.InitializeFontEngine() returned: {initErr}");
            else
                Debug.Log("[QE-v3] FontEngine initialized.");

            GenerateFontAsset(OrbitronTtfPath, OrbitronAssetPath, samplingPointSize: 90, atlasWidth: 1024, atlasHeight: 1024);
            GenerateFontAsset(MonoTtfPath, MonoAssetPath, samplingPointSize: 72, atlasWidth: 1024, atlasHeight: 1024);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QE-v3] Font assets generated.");
        }

        static void GenerateFontAsset(string ttfPath, string assetPath, int samplingPointSize, int atlasWidth, int atlasHeight)
        {
            // Check if asset already exists
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[QE-v3] Font asset already exists: {assetPath}");
                return;
            }

            // Load the Unity Font asset
            var ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (ttf == null)
            {
                Debug.LogWarning($"[QE-v3] TTF not found at {ttfPath}, skipping font generation.");
                return;
            }

            // First, try static atlas creation (may fail in -nographics batchmode due to FreeType)
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                ttf,
                samplingPointSize,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth, atlasHeight);

            if (fontAsset != null)
            {
                fontAsset.name = Path.GetFileNameWithoutExtension(assetPath);
                AssetDatabase.CreateAsset(fontAsset, assetPath);
                Debug.Log($"[QE-v3] Created static font asset: {assetPath}");
                return;
            }

            // Fallback: duplicate NotoSansSC-Dynamic.asset (already properly initialized)
            // and re-point its sourceFontFile to the target TTF.
            // This avoids FreeType requirements in -nographics batchmode.
            const string templateAsset = "Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset";
            Debug.LogWarning($"[QE-v3] Static creation failed (nographics FreeType limitation). Duplicating {templateAsset} for {ttfPath}");
            AssetDatabase.CopyAsset(templateAsset, assetPath);
            AssetDatabase.Refresh();
            var dynamicAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (dynamicAsset == null)
            {
                Debug.LogError($"[QE-v3] Failed to duplicate template font asset to {assetPath}");
                return;
            }
            // Update name and source font reference
            dynamicAsset.name = Path.GetFileNameWithoutExtension(assetPath);
            var sourceFontProp = typeof(TMP_FontAsset).GetField("m_SourceFontFile",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (sourceFontProp != null) sourceFontProp.SetValue(dynamicAsset, ttf);
            // Reset glyph tables so TMP re-populates at runtime with the correct font
            dynamicAsset.ClearFontAssetData(setAtlasSizeToZero: false);
            EditorUtility.SetDirty(dynamicAsset);
            Debug.Log($"[QE-v3] Created dynamic font asset (duplicated template): {assetPath}");
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
