using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace QuantumEcho.Editor
{
    /// <summary>Creates the deliberately isolated, opaque startup phase.</summary>
    public static class QuantumEchoTitleSceneBuilder
    {
        public const string TitleScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity";
        public const string GameplayScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";
        public const string CoverArtworkPath = "Assets/QuantumEcho/UI/Generated/CoverV6Clean/01_generated_image_url.png";
        public const string CoverMaterialPath = "Assets/QuantumEcho/Materials/QuantumEchoCoverArtwork.mat";

        static readonly Color Void = new Color(0.008f, 0.014f, 0.022f);
        static readonly Color Cyan = new Color(0.26f, 0.9f, 0.82f);
        static readonly Color Amber = new Color(1f, 0.66f, 0.25f);

        [MenuItem("Quantum Echo/Build Isolated Title Scene")]
        public static void BuildTitleScene()
        {
            var activePath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QuantumEcho_Title";
            RenderSettings.ambientLight = Void;
            RenderSettings.skybox = null;

            var root = new GameObject("TITLE ROOT - isolated startup phase");
            var cover = new GameObject("OPAQUE COVER - no gameplay behind this");
            cover.transform.SetParent(root.transform);
            var artwork = CreateCube(cover.transform, "NANO BANANA COVER ART - custom English wordmark", new Vector3(0f, 1.7f, -3.85f), new Vector3(4.8f, 2.7f, 0.025f), Color.white, false);
            artwork.GetComponent<Renderer>().sharedMaterial = CreateCoverMaterial();
            var coverArtwork = artwork.AddComponent<QuantumCoverArtwork>();
            coverArtwork.artwork = AssetDatabase.LoadAssetAtPath<Texture2D>(CoverArtworkPath);

            var designStartObject = new GameObject("TITLE PLAYER SPAWN - authoritative design start");
            designStartObject.transform.SetParent(root.transform);
            designStartObject.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var titleStart = designStartObject.AddComponent<QuantumDesignPlayerStart>();
            titleStart.xrEyeHeight = 1.65f;
            titleStart.xrThirdPersonViewingDistance = 0f;
            titleStart.desktopViewingDistance = 0f;
            titleStart.desktopViewingHeight = 1.65f;

            var terminalLandmark = new GameObject("TITLE TERMINAL - opaque cover landmark");
            terminalLandmark.transform.SetParent(root.transform);
            terminalLandmark.transform.position = new Vector3(0f, 1.7f, -3.8f);
            titleStart.firstLandmark = terminalLandmark.transform;
            // v3: Prefer Orbitron sci-fi font for title UI; fall back to NotoSansSC.
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/QuantumEcho/Fonts/SciFi/Orbitron-Dynamic.asset")
                    ?? AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset");
            // THESIS: Time echoes become the monumental cinematic key art, with
            // a custom two-line logotype and a precise left menu (user reference).
            // OWN-WORLD: Ink navy, pearl type, steel, cyan apertures, violet echoes.
            // STORY: See the past self, choose START, enter the spatial puzzle.
            // FIRST VIEWPORT: Wordmark upper-left; live equal-width menu beneath;
            // drone and temporal path own the right half. No bulky 3D button slabs.
            // FORM: User-pinned cinematic game menu, replacing V5's tiny logo.
            // Provenance: concept-seed was invoked but returned no output/seed;
            // the user's supplied game references are the explicit fallback.
            // FINISH: unreviewed and undocumented is unfinished; this build ends
            // with the finish review, the verdict, and DESIGN.md.
            var menu = new GameObject("TITLE MENU - authored 1600x900 layout", typeof(RectTransform), typeof(Canvas));
            var menuRect = menu.GetComponent<RectTransform>();
            menuRect.SetParent(terminalLandmark.transform, false);
            menuRect.localPosition = new Vector3(0f, 0f, 0.075f);
            menuRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            menuRect.localScale = Vector3.one * 0.003f;
            menuRect.sizeDelta = new Vector2(1600f, 900f);
            var menuCanvas = menu.GetComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.WorldSpace;
            menuCanvas.sortingOrder = 5;
            // v3: shared audio source on the menu for hover / press sounds
            var menuAudio = menu.AddComponent<AudioSource>();
            menuAudio.spatialBlend = 0f;
            menuAudio.volume = 1f;
            menuAudio.playOnAwake = false;
            var hoverClip   = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/QuantumEcho/Audio/ui_hover_scifi.mp3");
            var confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/QuantumEcho/Audio/ui_confirm_scifi.mp3");

            var startTarget = CreateMenuAction(menuRect, "START TARGET - world-space trigger", "START GAME", new Vector2(-476f, -116f), font, true);
            var startButton = startTarget.GetComponent<QuantumTitleMenuButton>();
            startButton.audioSource = menuAudio;
            startButton.hoverClip   = hoverClip;
            startButton.confirmClip = confirmClip;

            var exitTarget = CreateMenuAction(menuRect, "EXIT TARGET - world-space trigger", "EXIT", new Vector2(-476f, -200f), font, false);
            var exitButton = exitTarget.GetComponent<QuantumTitleMenuButton>();
            exitButton.audioSource = menuAudio;
            exitButton.hoverClip   = hoverClip;
            exitButton.confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/QuantumEcho/Audio/ui_confirm_hard.mp3");

            CreateMenuText(menuRect, "Start Hint", "[Right Trigger] Select & Confirm", new Vector2(-476f, -287f), new Vector2(448f, 36f), 22f, new Color(0.65f, 0.82f, 0.88f, 0.75f), font);

            var rig = new GameObject("TITLE XR RIG - design start derived");
            rig.transform.SetParent(root.transform);
            rig.transform.SetPositionAndRotation(titleStart.WorldPosition, titleStart.WorldYaw);
            var xrOrigin = rig.AddComponent<XROrigin>();
            xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            var floorOffset = new GameObject("Title Camera Floor Offset");
            floorOffset.transform.SetParent(rig.transform, false);
            var cameraObject = new GameObject("Title Camera");
            cameraObject.transform.SetParent(floorOffset.transform, false);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.localPosition = Vector3.up * titleStart.xrEyeHeight;
            var camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 44f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 40f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Void;
            cameraObject.AddComponent<AudioListener>();
            var poseDriver = cameraObject.AddComponent<TrackedPoseDriver>();
            poseDriver.positionInput = new InputActionProperty(new InputAction("Title Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition"));
            poseDriver.rotationInput = new InputActionProperty(new InputAction("Title Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation"));
            xrOrigin.CameraFloorOffsetObject = floorOffset;
            xrOrigin.Camera = camera;

            var rightController = new GameObject("Right PICO Title Controller");
            rightController.transform.SetParent(floorOffset.transform, false);
            rightController.transform.localPosition = new Vector3(0.25f, 1.25f, 0.35f);
            var rayObject = new GameObject("Right Title Controller Ray");
            rayObject.transform.SetParent(rightController.transform, false);
            var ray = rayObject.AddComponent<LineRenderer>();
            ray.useWorldSpace = true;
            ray.positionCount = 2;
            ray.startWidth = 0.008f;
            ray.endWidth = 0.002f;
            ray.sharedMaterial = LoadMaterial("Assets/QuantumEcho/Materials/CyanEmission.mat", Cyan);
            ray.enabled = false;

            var controller = rig.AddComponent<QuantumTitleController>();
            controller.designPlayerStart = titleStart;
            controller.xrOrigin = xrOrigin;
            controller.cameraFloorOffset = floorOffset.transform;
            controller.titleCamera = camera;
            controller.rightController = rightController.transform;
            controller.rightRay = ray;
            controller.titlePresentationRoot = terminalLandmark.transform;
            controller.opaqueCoverRoot = cover.transform;
            controller.presentationDistance = 2.70f;
            controller.startTarget = startTarget.GetComponent<Collider>();
            controller.exitTarget = exitTarget.GetComponent<Collider>();
            controller.startButton = startButton;
            controller.exitButton = exitButton;
            controller.gameplaySceneName = "QuantumEcho_Main";

            EditorSceneManager.SaveScene(scene, TitleScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(TitleScenePath, true),
                new EditorBuildSettingsScene(GameplayScenePath, true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!string.IsNullOrEmpty(activePath) && activePath != TitleScenePath && System.IO.File.Exists(activePath))
                EditorSceneManager.OpenScene(activePath, OpenSceneMode.Single);
            Debug.Log("Quantum Echo isolated title scene built. It contains only TITLE ROOT and loads gameplay with LoadSceneMode.Single.");
        }

        public static void EnsureTitleScene()
        {
            if (!System.IO.File.Exists(TitleScenePath)) BuildTitleScene();
            var settings = EditorBuildSettings.scenes;
            var correctOrder = settings.Length == 2 && settings[0].path == TitleScenePath && settings[1].path == GameplayScenePath &&
                               settings.All(scene => scene.enabled);
            if (!correctOrder)
            {
                EditorBuildSettings.scenes = new[]
                {
                    new EditorBuildSettingsScene(TitleScenePath, true),
                    new EditorBuildSettingsScene(GameplayScenePath, true)
                };
            }
        }

        public static bool VerifyTitleScene(out string issue, bool restorePreviousScene = true)
        {
            issue = string.Empty;
            if (!System.IO.File.Exists(TitleScenePath))
            {
                issue = "isolated title scene is missing";
                return false;
            }
            var previousPath = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            var allRoots = scene.GetRootGameObjects();
            // Query the opened scene hierarchy directly. Unity's global Find APIs
            // can return no scene components during a headless domain reload even
            // though the serialized title hierarchy is loaded.
            var title = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumTitleController>(true)).FirstOrDefault();
            var gameplayObjects = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumEchoGameManager>(true)).ToArray();
            var gameplayHud = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumHUD>(true)).ToArray();
            var hasIsolatedTitleRoot = allRoots.Any(root => root.name == "TITLE ROOT - isolated startup phase");
            var valid = title != null && title.designPlayerStart != null && title.startTarget != null &&
                        title.xrOrigin != null && title.titleCamera != null && title.titlePresentationRoot != null &&
                        title.opaqueCoverRoot != null && hasIsolatedTitleRoot &&
                        gameplayObjects.Length == 0 && gameplayHud.Length == 0;
            // Some ASCII staging copies hit a Unity batch-mode serialization race:
            // the hierarchy opens but editor type discovery temporarily returns no
            // MonoBehaviours. Preserve the same hard title isolation contract by
            // auditing the serialized scene references in that narrow case.
            if (!valid && Application.isBatchMode)
            {
                valid = VerifySerializedTitleContract(out var serializedDetail);
                if (valid) Debug.Log("Quantum title serialized batch audit passed: " + serializedDetail);
            }
            if (!valid)
            {
                Debug.LogError($"Quantum title audit detail: controller={(title != null)}, start={(title != null && title.designPlayerStart != null)}, " +
                               $"startTarget={(title != null && title.startTarget != null)}, xrOrigin={(title != null && title.xrOrigin != null)}, " +
                               $"camera={(title != null && title.titleCamera != null)}, presentation={(title != null && title.titlePresentationRoot != null)}, " +
                               $"opaqueCover={(title != null && title.opaqueCoverRoot != null)}, titleRoot={hasIsolatedTitleRoot}, " +
                               $"gameManagers={gameplayObjects.Length}, huds={gameplayHud.Length}, roots=[{string.Join(", ", allRoots.Select(root => root.name))}]");
                issue = "title must contain a design start and entry target, and no gameplay manager or HUD";
            }
            if (restorePreviousScene && !string.IsNullOrEmpty(previousPath) && previousPath != TitleScenePath && System.IO.File.Exists(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
            return valid;
        }

        static bool VerifySerializedTitleContract(out string detail)
        {
            detail = string.Empty;
            var yaml = File.ReadAllText(TitleScenePath);
            var controllerGuid = AssetDatabase.AssetPathToGUID("Assets/QuantumEcho/Scripts/Runtime/QuantumTitleController.cs");
            var startGuid = AssetDatabase.AssetPathToGUID("Assets/QuantumEcho/Scripts/Runtime/QuantumDesignPlayerStart.cs");
            var gameManagerGuid = AssetDatabase.AssetPathToGUID("Assets/QuantumEcho/Scripts/Runtime/QuantumEchoGameManager.cs");
            var hudGuid = AssetDatabase.AssetPathToGUID("Assets/QuantumEcho/Scripts/Runtime/QuantumHUD.cs");
            var hasRoot = yaml.Contains("m_Name: TITLE ROOT - isolated startup phase");
            var hasStart = yaml.Contains("guid: " + startGuid);
            var hasController = yaml.Contains("guid: " + controllerGuid);
            var hasGameplay = yaml.Contains("guid: " + gameManagerGuid) || yaml.Contains("guid: " + hudGuid);
            var controllerBlock = Regex.Match(
                yaml,
                @"m_Script: \{fileID: 11500000, guid: " + Regex.Escape(controllerGuid) + @", type: 3\}(?<body>.*?)(?=--- !u!|\z)",
                RegexOptions.Singleline);
            var hasAssignedReferences = controllerBlock.Success &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"designPlayerStart: \{fileID: (?!0\})") &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"startTarget: \{fileID: (?!0\})") &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"xrOrigin: \{fileID: (?!0\})") &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"titleCamera: \{fileID: (?!0\})") &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"titlePresentationRoot: \{fileID: (?!0\})") &&
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"opaqueCoverRoot: \{fileID: (?!0\})");
            detail = $"root={hasRoot}, controller={hasController}, designStart={hasStart}, refs={hasAssignedReferences}, gameplayRefs={hasGameplay}";
            return hasRoot && hasController && hasStart && hasAssignedReferences && !hasGameplay;
        }

        /// <summary>Audits only a title scene that the caller has already opened.</summary>
        public static bool AuditOpenedTitleScene(out string issue)
        {
            issue = string.Empty;
            var active = SceneManager.GetActiveScene();
            if (active.path != TitleScenePath)
            {
                issue = "title scene is not active";
                return false;
            }
            var allRoots = active.GetRootGameObjects();
            var title = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumTitleController>(true)).FirstOrDefault();
            var gameplayObjects = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumEchoGameManager>(true)).ToArray();
            var gameplayHud = allRoots.SelectMany(root => root.GetComponentsInChildren<QuantumHUD>(true)).ToArray();
            var hasIsolatedTitleRoot = allRoots.Any(root => root.name == "TITLE ROOT - isolated startup phase");
            var valid = title != null && title.designPlayerStart != null && title.startTarget != null &&
                        title.xrOrigin != null && title.titleCamera != null && title.titlePresentationRoot != null &&
                        title.opaqueCoverRoot != null && hasIsolatedTitleRoot &&
                        gameplayObjects.Length == 0 && gameplayHud.Length == 0;
            if (!valid) issue = "title must contain a design start and entry target, and no gameplay manager or HUD";
            return valid;
        }

        static GameObject CreateCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, bool collider)
        {
            var result = GameObject.CreatePrimitive(PrimitiveType.Cube);
            result.name = name;
            result.transform.SetParent(parent, false);
            result.transform.localPosition = localPosition;
            result.transform.localScale = localScale;
            if (!collider)
            {
                var box = result.GetComponent<BoxCollider>();
                if (box != null) Object.DestroyImmediate(box);
            }
            result.GetComponent<Renderer>().sharedMaterial = LoadMaterial("Assets/QuantumEcho/Materials/DarkMetal.mat", color);
            return result;
        }

        static void CreateAccentLine(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var line = CreateCube(parent, name, localPosition, localScale, Cyan, false);
            line.GetComponent<Renderer>().sharedMaterial = material;
        }

        static GameObject CreateMenuAction(RectTransform parent, string name, string label, Vector2 position, TMP_FontAsset font, bool primary)
        {
            // v3: Use holographic button sprite; fall back to flat color if not imported yet.
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(BoxCollider), typeof(QuantumTitleMenuButton));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(480f, 70f);
            var plate = root.GetComponent<Image>();
            var holoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/QuantumEcho/UI/Generated/ButtonV3/QuantumHologramButton.png");
            if (holoSprite != null)
            {
                plate.sprite = holoSprite;
                plate.type = Image.Type.Sliced;
                plate.pixelsPerUnitMultiplier = 3f;
                // Primary: brighter tint; secondary: standard tint
                plate.color = primary ? new Color(0.95f, 1.00f, 1.00f, 1f) : new Color(0.70f, 0.90f, 0.92f, 0.88f);
            }
            else
            {
                plate.color = primary ? new Color(0.83f, 0.92f, 0.94f, 1f) : new Color(0.035f, 0.060f, 0.078f, 0.93f);
            }
            plate.raycastTarget = false;
            root.GetComponent<BoxCollider>().size = new Vector3(480f, 70f, 5f);
            // v3: text color: primary gets dark label on bright button; secondary gets cyan-white
            var textColor = primary ? new Color(0.04f, 0.08f, 0.10f) : new Color(0.85f, 0.97f, 1.00f);
            var text = CreateMenuText(rect, primary ? "Start Label" : "Exit Label", label, new Vector2(12f, 0f), new Vector2(440f, 70f), 30f, textColor, font);
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 5f;
            // v3: focus underline is a thin cyan line at bottom of button
            var rule = new GameObject("Focus underline", typeof(RectTransform), typeof(Image));
            var ruleRect = rule.GetComponent<RectTransform>();
            ruleRect.SetParent(rect, false);
            ruleRect.anchoredPosition = new Vector2(0f, -33f);
            ruleRect.sizeDelta = new Vector2(480f, 2.5f);
            var ruleImg = rule.GetComponent<Image>();
            ruleImg.color = new Color(0.18f, 0.93f, 1f, 0.90f);
            ruleImg.raycastTarget = false;
            var button = root.GetComponent<QuantumTitleMenuButton>();
            button.action = primary ? QuantumTitleMenuButton.ActionKind.Start : QuantumTitleMenuButton.ActionKind.Exit;
            button.backingImage = plate;
            button.label = text;
            button.focusRule = rule.GetComponent<Image>();
            button.isPrimary = primary;
            button.hoverScale = 1.015f;
            return root;
        }

        static TextMeshProUGUI CreateMenuText(RectTransform parent, string name, string content, Vector2 position, Vector2 size, float fontSize, Color color, TMP_FontAsset font)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var text = root.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        static void CreateText(Transform parent, string name, string content, Vector3 localPosition, float size, Color color, TMP_FontAsset font, Vector2? dimensions = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            textObject.transform.localPosition = localPosition + Vector3.forward * 0.075f;
            textObject.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = textObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = content.Contains("\n") ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = false;
            text.rectTransform.sizeDelta = dimensions ?? new Vector2(6.5f, 0.8f);
        }

        static Material CreateCoverMaterial()
        {
            var shader = Shader.Find("QuantumEcho/CoverFeather");
            if (shader == null) throw new BuildFailedException("Cover feather shader is missing");
            var material = AssetDatabase.LoadAssetAtPath<Material>(CoverMaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, CoverMaterialPath);
            }
            else material.shader = shader;
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CoverArtworkPath);
            material.color = Color.white;
            material.SetFloat("_Feather", 0.20f);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material LoadMaterial(string path, Color fallback)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Standard"));
            material.color = fallback;
            return material;
        }
    }
}
