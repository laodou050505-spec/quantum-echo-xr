using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using TMPro;

namespace QuantumEcho.Editor
{
    /// <summary>Creates the deliberately isolated, opaque startup phase.</summary>
    public static class QuantumEchoTitleSceneBuilder
    {
        public const string TitleScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Title.unity";
        public const string GameplayScenePath = "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity";

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
            CreateCube(cover.transform, "Opaque Cover Slab", new Vector3(0f, 1.7f, -3.42f), new Vector3(5.6f, 3.35f, 0.18f), Void, true);
            var cyanMaterial = LoadMaterial("Assets/QuantumEcho/Materials/CyanEmission.mat", Cyan);
            CreateAccentLine(cover.transform, "Cyan Cover Rim Top", new Vector3(0f, 3.0f, -3.30f), new Vector3(4.78f, 0.025f, 0.026f), cyanMaterial);
            CreateAccentLine(cover.transform, "Cyan Cover Rim Bottom", new Vector3(0f, 0.40f, -3.30f), new Vector3(4.78f, 0.025f, 0.026f), cyanMaterial);
            CreateAccentLine(cover.transform, "Cyan Cover Rim Left", new Vector3(-2.38f, 1.7f, -3.30f), new Vector3(0.025f, 2.62f, 0.026f), cyanMaterial);
            CreateAccentLine(cover.transform, "Cyan Cover Rim Right", new Vector3(2.38f, 1.7f, -3.30f), new Vector3(0.025f, 2.62f, 0.026f), cyanMaterial);

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
            terminalLandmark.transform.position = new Vector3(0f, 1.7f, -3.25f);
            titleStart.firstLandmark = terminalLandmark.transform;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset");
            CreateText(terminalLandmark.transform, "Title", "量 子 回 声", new Vector3(0f, 0.77f, 0f), 4.8f, Color.white, font);
            CreateText(terminalLandmark.transform, "Subtitle", "QUANTUM ECHO  /  空间解密实验", new Vector3(0f, 0.34f, 0f), 1.6f, Cyan, font);
            CreateText(terminalLandmark.transform, "Description", "以回声记录路径，穿越四间量子实验舱", new Vector3(0f, -0.08f, 0f), 1.3f, new Color(0.72f, 0.86f, 0.9f), font);
            CreateText(terminalLandmark.transform, "Start Label", "开始同步", new Vector3(0f, -0.65f, 0f), 2.8f, Amber, font);
            CreateText(terminalLandmark.transform, "Start Hint", "ENTER / E  或 右手扳机", new Vector3(0f, -0.98f, 0f), 1.05f, new Color(0.76f, 0.85f, 0.88f), font);
            CreateText(terminalLandmark.transform, "Exit Label", "ESC / EXIT", new Vector3(0f, -1.26f, 0f), 0.9f, new Color(0.45f, 0.56f, 0.6f), font);

            var startTarget = CreateCube(terminalLandmark.transform, "START TARGET - world-space trigger", new Vector3(0f, -0.66f, 0.035f), new Vector3(2.15f, 0.52f, 0.10f), Amber, true);
            startTarget.GetComponent<Renderer>().enabled = false;
            var exitTarget = CreateCube(terminalLandmark.transform, "EXIT TARGET - world-space trigger", new Vector3(0f, -1.26f, 0.035f), new Vector3(1.25f, 0.22f, 0.1f), Void, true);
            exitTarget.GetComponent<Renderer>().enabled = false;

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
            controller.startTarget = startTarget.GetComponent<Collider>();
            controller.exitTarget = exitTarget.GetComponent<Collider>();
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
                        title.xrOrigin != null && title.titleCamera != null && hasIsolatedTitleRoot &&
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
                               $"camera={(title != null && title.titleCamera != null)}, titleRoot={hasIsolatedTitleRoot}, " +
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
                Regex.IsMatch(controllerBlock.Groups["body"].Value, @"titleCamera: \{fileID: (?!0\})");
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
                        title.xrOrigin != null && title.titleCamera != null && hasIsolatedTitleRoot &&
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

        static void CreateText(Transform parent, string name, string content, Vector3 localPosition, float size, Color color, TMP_FontAsset font)
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
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta = new Vector2(6.5f, 0.8f);
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
