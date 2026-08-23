using System.Collections.Generic;
using System.IO;
using System.Linq;
using ByteDance.PICO.XR;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Management;
using TMPro;
using UnityEngine.TextCore.LowLevel;

namespace QuantumEcho.Editor
{
    public static class QuantumEchoSceneBuilder
    {
        const string Root = "Assets/QuantumEcho";
        const string ScenePath = Root + "/Scenes/QuantumEcho_Main.unity";
        const string MaterialPath = Root + "/Materials";

        static readonly Color Void = new Color(0.02f, 0.03f, 0.04f);
        static readonly Color Steel = new Color(0.12f, 0.17f, 0.19f);
        static readonly Color DarkSteel = new Color(0.045f, 0.075f, 0.085f);
        static readonly Color Cyan = new Color(0.26f, 0.9f, 0.82f);
        static readonly Color Amber = new Color(1f, 0.66f, 0.25f);
        static readonly Color Magenta = new Color(0.88f, 0.32f, 0.74f);
        static readonly Color Danger = new Color(1f, 0.24f, 0.29f);

        static Material _floor;
        static Material _metal;
        static Material _darkMetal;
        static Material _cyan;
        static Material _amber;
        static Material _magenta;
        static Material _danger;
        static Material _glass;
        static Material _echo;
        static TMP_FontAsset _font;
        static Mesh _octahedron;

        [MenuItem("Quantum Echo/Build Editable PICO Scene")]
        public static void BuildAll()
        {
            EnsureFolders();
            ConfigureProject();
            CreateMaterials();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "QuantumEcho_Main";

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.28f, 0.3f);
            RenderSettings.ambientEquatorColor = new Color(0.05f, 0.08f, 0.1f);
            RenderSettings.ambientGroundColor = Void;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Void;
            RenderSettings.fogDensity = 0.012f;

            var sceneRoot = new GameObject("QUANTUM ECHO - EDITABLE SCENE");
            var levelsRoot = new GameObject("LEVELS - move and replace models here");
            levelsRoot.transform.SetParent(sceneRoot.transform);
            var levels = new QuantumLevelScene[4];
            levels[0] = BuildLevel(levelsRoot.transform, 0, QuantumLevelMode.Tutorial, new Vector3(0f, 0f, 0f));
            levels[1] = BuildLevel(levelsRoot.transform, 1, QuantumLevelMode.Paired, new Vector3(22f, 0f, 0f));
            levels[2] = BuildLevel(levelsRoot.transform, 2, QuantumLevelMode.Train, new Vector3(44f, 0f, 0f));
            levels[3] = BuildLevel(levelsRoot.transform, 3, QuantumLevelMode.Lure, new Vector3(66f, 0f, 0f));

            var rig = BuildPlayerRig(sceneRoot.transform, levels[0].designPlayerStart);
            var hud = BuildHud(sceneRoot.transform);
            hud.SetWorldAnchor(levels[0].worldTaskTerminalAnchor);
            var templates = new GameObject("RUNTIME TEMPLATES - editable source models");
            templates.transform.SetParent(sceneRoot.transform);
            var echoTemplate = BuildEchoTemplate(templates.transform);

            var managerObject = new GameObject("GAMEPLAY - Quantum Echo Manager");
            managerObject.transform.SetParent(sceneRoot.transform);
            var manager = managerObject.AddComponent<QuantumEchoGameManager>();
            manager.levels = levels;
            manager.player = rig;
            manager.hud = hud;
            manager.echoTemplate = echoTemplate;
            manager.echoTrailMaterial = _echo;
            manager.startingLevel = 0;

            var ambient = managerObject.AddComponent<AudioSource>();
            ambient.loop = true;
            ambient.playOnAwake = false;
            ambient.volume = 0.24f;
            ambient.spatialBlend = 0f;
            ambient.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/space_ambient.mp3");
            var effects = managerObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.volume = 0.72f;
            manager.ambientSource = ambient;
            manager.effectsSource = effects;
            manager.confirmClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/confirm.mp3");
            manager.errorClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/error.mp3");
            manager.unlockClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/unlock.mp3");
            manager.interactClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/interact.mp3");

            CreateGlobalLighting(sceneRoot.transform);
            CreateEventSystem(sceneRoot.transform);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = levels[0].gameObject;
            Debug.Log($"Quantum Echo editable scene created: {ScenePath}");
        }

        static void EnsureFolders()
        {
            foreach (var path in new[] { Root, Root + "/Scenes", MaterialPath, Root + "/Audio", Root + "/Documentation" })
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
        }

        static void ConfigureProject()
        {
            PlayerSettings.companyName = "Quantum Echo Lab";
            PlayerSettings.productName = "量子回声";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.quantumecho.decrypt1");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
            // Full-space PICO VR must not be emitted as a freeform Android window.
            // Keeping this explicit prevents Unity 6 defaults from adding its
            // WindowManagerPreference metadata to the final manifest.
            PlayerSettings.Android.resizeableActivity = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });

            var playerSettingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettingsObjects.Length > 0)
            {
                var serialized = new SerializedObject(playerSettingsObjects[0]);
                var activeInput = serialized.FindProperty("activeInputHandler");
                if (activeInput != null)
                {
                    activeInput.intValue = 1;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var pxrSettings = PXR_Settings.GetSettings();
            if (pxrSettings == null)
            {
                pxrSettings = AssetDatabase.LoadAssetAtPath<PXR_Settings>("Assets/XR/Settings/PXR_Settings.asset");
                if (pxrSettings == null)
                {
                    Directory.CreateDirectory("Assets/XR/Settings");
                    pxrSettings = ScriptableObject.CreateInstance<PXR_Settings>();
                    AssetDatabase.CreateAsset(pxrSettings, "Assets/XR/Settings/PXR_Settings.asset");
                }
                EditorBuildSettings.AddConfigObject("ByteDance.PICO.XR.Settings", pxrSettings, true);
            }
            pxrSettings.stereoRenderingModeAndroid = PXR_Settings.StereoRenderingModeAndroid.Multiview;
            EditorUtility.SetDirty(pxrSettings);
            ConfigurePicoLoader();
        }

        static void ConfigurePicoLoader()
        {
            var buildTargetSettings = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
                .Select(guid => AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();
            if (buildTargetSettings == null)
            {
                buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings == null)
            {
                generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                AssetDatabase.AddObjectToAsset(generalSettings, buildTargetSettings);
                buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Android, generalSettings);
                var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                AssetDatabase.AddObjectToAsset(managerSettings, buildTargetSettings);
                generalSettings.Manager = managerSettings;
            }

            while (generalSettings.Manager.activeLoaders.Count > 0)
            {
                var loaderName = generalSettings.Manager.activeLoaders[0].GetType().FullName;
                XRPackageMetadataStore.RemoveLoader(generalSettings.Manager, loaderName, BuildTargetGroup.Android);
            }
            if (!XRPackageMetadataStore.AssignLoader(generalSettings.Manager, "ByteDance.PICO.XR.PXR_Loader", BuildTargetGroup.Android))
            {
                XRPackageMetadataStore.AssignLoader(generalSettings.Manager, nameof(PXR_Loader), BuildTargetGroup.Android);
            }
            EditorUtility.SetDirty(generalSettings.Manager);
            EditorUtility.SetDirty(generalSettings);
            EditorUtility.SetDirty(buildTargetSettings);
        }

        static void CreateMaterials()
        {
            _floor = Material("Floor", new Color(0.055f, 0.08f, 0.09f), 0.72f, 0.36f);
            _metal = Material("Metal", Steel, 0.3f, 0.78f);
            _darkMetal = Material("DarkMetal", DarkSteel, 0.46f, 0.72f);
            _cyan = Material("CyanEmission", new Color(0.05f, 0.2f, 0.2f), 0.22f, 0.45f, Cyan, 1.4f);
            _amber = Material("AmberEmission", new Color(0.22f, 0.14f, 0.05f), 0.3f, 0.4f, Amber, 0.8f);
            _magenta = Material("MagentaEmission", new Color(0.22f, 0.08f, 0.19f), 0.24f, 0.5f, Magenta, 1.35f);
            _danger = Material("DangerEmission", new Color(0.25f, 0.06f, 0.09f), 0.25f, 0.5f, Danger, 1.5f);
            _glass = Material("Glass", new Color(0.08f, 0.24f, 0.25f, 0.3f), 0.18f, 0.2f, Cyan, 0.32f, true);
            _echo = Material("EchoTrail", new Color(Magenta.r, Magenta.g, Magenta.b, 0.48f), 0.2f, 0.2f, Magenta, 1.1f, true);
            _font = GetOrCreateHudFont();
            _octahedron = CreateOctahedronMesh();
        }

        static TMP_FontAsset GetOrCreateHudFont()
        {
            const string fontPath = Root + "/Fonts/NotoSansSC-Variable.ttf";
            const string assetPath = Root + "/Fonts/NotoSansSC-Dynamic.asset";
            var settings = GetOrCreateTmpSettings();
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (fontAsset != null)
            {
                if (settings != null && TMP_Settings.defaultFontAsset != fontAsset)
                {
                    TMP_Settings.defaultFontAsset = fontAsset;
                    EditorUtility.SetDirty(settings);
                }
                return fontAsset;
            }
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"HUD font is missing: {fontPath}");
                return null;
            }
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"Unable to create TMP font asset from {fontPath}. Verify Include Font Data is enabled.");
                return null;
            }
            fontAsset.name = "Noto Sans SC Dynamic";
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            PersistFontAssetSubAssets(fontAsset);
            if (settings != null)
            {
                TMP_Settings.defaultFontAsset = fontAsset;
                EditorUtility.SetDirty(settings);
            }
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        static TMP_Settings GetOrCreateTmpSettings()
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings != null && Shader.Find("TextMeshPro/Distance Field") != null) return settings;
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Settings).Assembly);
            var packagePath = package != null
                ? Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage")
                : string.Empty;
            if (!File.Exists(packagePath))
            {
                Debug.LogError("TMP Essential Resources package was not found.");
                return settings;
            }
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh();
            settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null || Shader.Find("TextMeshPro/Distance Field") == null)
                Debug.LogError("TMP Essential Resources could not be imported.");
            return settings;
        }

        static void PersistFontAssetSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null && !EditorUtility.IsPersistent(texture)) AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }
            if (fontAsset.material != null && !EditorUtility.IsPersistent(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        }

        static Material Material(string name, Color color, float smoothness, float metallic, Color? emission = null, float emissionIntensity = 0f, bool transparent = false)
        {
            var path = $"{MaterialPath}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value * emissionIntensity);
            }
            if (transparent)
            {
                material.SetFloat("_Mode", 3f);
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.renderQueue = 3000;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static Mesh CreateOctahedronMesh()
        {
            const string path = Root + "/Materials/Octahedron.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Octahedron" };
            mesh.vertices = new[]
            {
                new Vector3(0, 1, 0), new Vector3(1, 0, 0), new Vector3(0, 0, 1), new Vector3(-1, 0, 0),
                new Vector3(0, 0, -1), new Vector3(0, -1, 0)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2, 0, 4, 3, 0, 1, 4, 5, 1, 2, 5, 2, 3, 5, 3, 4, 5, 4, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static QuantumLevelScene BuildLevel(Transform parent, int index, QuantumLevelMode mode, Vector3 position)
        {
            var root = new GameObject($"LEVEL_{index:00}_{mode} - EDITABLE");
            root.transform.SetParent(parent);
            root.transform.position = position;
            var level = root.AddComponent<QuantumLevelScene>();
            level.levelIndex = index;
            level.mode = mode;
            level.levelCode = $"QER-{index:00}";
            level.loopDuration = index switch { 0 => 14f, 1 => 12f, 2 => 16f, _ => 18f };
            level.title = index switch
            {
                0 => "让回声替你守住门",
                1 => "一个人，三个时间切片",
                2 => "给量子列车编一段节拍",
                _ => "别让追猎体认出你"
            };

            var geometry = Child(root.transform, "ENVIRONMENT - walls floor lights");
            var puzzle = Child(root.transform, "PUZZLE OBJECTS - move these freely");
            var labels = Child(root.transform, "WORLD LABELS");
            BuildRoom(geometry.transform, index);
            BuildRoomLights(geometry.transform);

            level.spawnPoint = Child(root.transform, "PLAYER SPAWN").transform;
            // Keep the entry point safely inside the deck instead of directly against
            // the back edge of the room.  This makes both desktop testing and headset
            // locomotion comfortable without changing the playable puzzle layout.
            level.spawnPoint.localPosition = new Vector3(0f, 0.05f, 4f);
            // All rooms are entered from +Z and face the first puzzle landmark at
            // world -Z. This marker is the single source for both desktop and PICO.
            level.spawnPoint.localRotation = Quaternion.Euler(0f, 180f, 0f);
            level.designPlayerStart = level.spawnPoint.gameObject.AddComponent<QuantumDesignPlayerStart>();
            level.designPlayerStart.xrEyeHeight = 1.65f;
            level.designPlayerStart.xrThirdPersonViewingDistance = 3.6f;
            level.designPlayerStart.desktopViewingDistance = 3.6f;
            level.designPlayerStart.xrThirdPersonObservationHeight = 1.05f;
            level.designPlayerStart.desktopViewingHeight = 2.7f;
            level.designPlayerStart.desktopFocusHeight = 0.72f;

            level.worldTaskTerminalAnchor = Child(root.transform, "WORLD TASK TERMINAL ANCHOR - fixed room space").transform;
            // The terminal sits inside the left entry wall, not in front of the
            // camera or over the gate. Players can walk around it and see its side.
            level.worldTaskTerminalAnchor.localPosition = new Vector3(-5.45f, 1.62f, 2.25f);

            var padAPosition = index switch
            {
                0 => new Vector3(-3.4f, 0.08f, 2.1f),
                1 => new Vector3(-3.65f, 0.08f, 2.05f),
                2 => new Vector3(-3.85f, 0.08f, 2.35f),
                _ => Vector3.zero
            };
            var padBPosition = index switch
            {
                1 => new Vector3(3.65f, 0.08f, 2.05f),
                2 => new Vector3(3.55f, 0.08f, 1.15f),
                _ => Vector3.zero
            };
            if (mode != QuantumLevelMode.Lure)
            {
                level.padA = CreatePad(puzzle.transform, index == 0 ? "A - HOLD PAD" : "A - SYNC PAD", padAPosition, out level.padARenderer);
                CreateWorldLabel(labels.transform, index == 0 ? "保持器 // 持续踩住" : index == 2 ? "A // 发车枢纽" : "A // 稳定器", padAPosition + Vector3.up * 1.25f, Amber);
            }
            if (mode == QuantumLevelMode.Paired || mode == QuantumLevelMode.Train)
            {
                level.padB = CreatePad(puzzle.transform, mode == QuantumLevelMode.Train ? "B - SWITCH PAD" : "B - SYNC PAD", padBPosition, out level.padBRenderer);
                CreateWorldLabel(labels.transform, mode == QuantumLevelMode.Train ? "B // 换轨枢纽" : "B // 稳定器", padBPosition + Vector3.up * 1.25f, Amber);
            }

            CreateGate(puzzle.transform, out level.gateLeft, out level.gateRight, out level.gateLeftRenderer, out level.gateRightRenderer);
            level.gateLeftClosedLocal = level.gateLeft.localPosition;
            level.gateRightClosedLocal = level.gateRight.localPosition;
            CreateWorldLabel(labels.transform, index switch { 0 => "教学力场", 1 => "双相位力场", 2 => "量子列车泊位", _ => "数据收容区" }, new Vector3(0f, 2.35f, -0.72f), Cyan);
            level.core = CreateCore(puzzle.transform, index, out var coreTarget);
            level.coreTarget = coreTarget;
            var terminalTarget = level.spawnPoint.position + Vector3.up * 1.25f;
            level.worldTaskTerminalAnchor.rotation = Quaternion.LookRotation(terminalTarget - level.worldTaskTerminalAnchor.position, Vector3.up);
            CreateWorldLabel(labels.transform, $"Q-{index:00} // 核心", new Vector3(0f, 2.65f, -4.15f), Cyan);

            if (mode == QuantumLevelMode.Train) BuildTrain(level, puzzle.transform, labels.transform);
            if (mode == QuantumLevelMode.Lure) BuildLure(level, puzzle.transform, labels.transform);
            level.designPlayerStart.firstLandmark = level.padA != null
                ? level.padA
                : mode == QuantumLevelMode.Lure && level.lureZones != null && level.lureZones.Length > 0
                    ? level.lureZones[0]
                    : level.core;
            return level;
        }

        static void BuildRoom(Transform parent, int index)
        {
            // The deck deliberately extends behind the spawn as well as around the
            // puzzle.  It is an ordinary editable Cube in the Scene hierarchy, so a
            // desktop tester or a headset user can take a few steps back safely.
            CreatePrimitive(parent, "Floor", PrimitiveType.Cube, new Vector3(0f, -0.2f, 2.45f), new Vector3(14.6f, 0.32f, 17.3f), _floor);
            // An intentionally visible, selectable extension behind the player spawn.
            // It gives a few metres of safe retreat room while preserving the compact
            // puzzle chamber in front of the player.
            CreatePrimitive(parent, "Spawn Safety Deck - editable", PrimitiveType.Cube, new Vector3(0f, -0.2f, 8.8f), new Vector3(14.6f, 0.32f, 5.4f), _floor);
            CreatePrimitive(parent, "Left Wall", PrimitiveType.Cube, new Vector3(-7.12f, 1.45f, 0f), new Vector3(0.32f, 3.1f, 12.2f), _darkMetal);
            CreatePrimitive(parent, "Right Wall", PrimitiveType.Cube, new Vector3(7.12f, 1.45f, 0f), new Vector3(0.32f, 3.1f, 12.2f), _darkMetal);
            CreatePrimitive(parent, "Rear Wall", PrimitiveType.Cube, new Vector3(0f, 1.45f, -5.85f), new Vector3(14.5f, 3.1f, 0.34f), _darkMetal);
            CreatePrimitive(parent, "Ceiling - hide for Scene editing", PrimitiveType.Cube, new Vector3(0f, 3.1f, 0f), new Vector3(14.6f, 0.18f, 12.4f), _darkMetal);
            CreatePrimitive(parent, "Rear Window", PrimitiveType.Cube, new Vector3(0f, 1.65f, -5.63f), new Vector3(6.4f, 1.3f, 0.08f), _glass, false);
            CreatePrimitive(parent, "Gate Left Barrier", PrimitiveType.Cube, new Vector3(-4.35f, 0.65f, -0.72f), new Vector3(5.25f, 1.3f, 0.48f), _darkMetal);
            CreatePrimitive(parent, "Gate Right Barrier", PrimitiveType.Cube, new Vector3(4.35f, 0.65f, -0.72f), new Vector3(5.25f, 1.3f, 0.48f), _darkMetal);
            for (var x = -6; x <= 6; x += 1)
            {
                var strip = CreatePrimitive(parent, $"Floor Grid X {x + 7:00}", PrimitiveType.Cube, new Vector3(x, 0.015f, 0f), new Vector3(0.018f, 0.012f, 11.2f), _cyan, false);
                strip.GetComponent<Renderer>().sharedMaterial = _cyan;
            }
            for (var z = -5; z <= 5; z += 1)
            {
                CreatePrimitive(parent, $"Floor Grid Z {z + 6:00}", PrimitiveType.Cube, new Vector3(0f, 0.016f, z), new Vector3(13.5f, 0.012f, 0.018f), _cyan, false);
            }
            CreateRing(parent, "Arena Ring Outer", Vector3.up * 0.035f, 5.15f, Cyan, 0.018f);
            CreateRing(parent, "Arena Ring Mid", new Vector3(0f, 0.036f, 0.35f), 3.45f, Cyan, 0.014f);
            CreateWorldLabel(parent, $"实验舱 {index:00} // 场景模型可直接编辑", new Vector3(0f, 2.72f, -5.58f), Cyan);
        }

        static void BuildRoomLights(Transform parent)
        {
            var cyanLight = new GameObject("Cyan Core Light").AddComponent<Light>();
            cyanLight.transform.SetParent(parent);
            cyanLight.transform.localPosition = new Vector3(0f, 2.2f, -3.8f);
            cyanLight.type = LightType.Point;
            cyanLight.color = Cyan;
            cyanLight.range = 9f;
            cyanLight.intensity = 5f;
            var magentaLight = new GameObject("Echo Light").AddComponent<Light>();
            magentaLight.transform.SetParent(parent);
            magentaLight.transform.localPosition = new Vector3(0f, 1.5f, 4.6f);
            magentaLight.type = LightType.Point;
            magentaLight.color = Magenta;
            magentaLight.range = 7f;
            magentaLight.intensity = 2.2f;
        }

        static Transform CreatePad(Transform parent, string name, Vector3 position, out Renderer discRenderer)
        {
            var group = Child(parent, name);
            group.transform.localPosition = position;
            // Primitive cylinders have tall capsule colliders even when their visual
            // height is tiny.  They made the pads behave like invisible walls, so the
            // visuals deliberately have no colliders.  Pad activation is a
            // range-check, so the player remains on the room floor and cannot be
            // blocked by an invisible collider or a high step.
            CreatePrimitive(group.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, new Vector3(2.3f, 0.14f, 2.3f), _metal, false);
            var disc = CreatePrimitive(group.transform, "Emission Disc", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), new Vector3(1.76f, 0.08f, 1.76f), _amber, false);
            discRenderer = disc.GetComponent<Renderer>();
            CreateRing(group.transform, "Activation Ring", new Vector3(0f, 0.18f, 0f), 1.03f, Amber, 0.04f);
            return group.transform;
        }

        static void CreateGate(Transform parent, out Transform left, out Transform right, out Renderer leftRenderer, out Renderer rightRenderer)
        {
            var group = Child(parent, "ENERGY GATE - panels animate locally");
            var leftObject = CreatePrimitive(group.transform, "Gate Left Panel", PrimitiveType.Cube, new Vector3(-0.82f, 0.86f, -0.72f), new Vector3(1.62f, 1.64f, 0.23f), _cyan);
            var rightObject = CreatePrimitive(group.transform, "Gate Right Panel", PrimitiveType.Cube, new Vector3(0.82f, 0.86f, -0.72f), new Vector3(1.62f, 1.64f, 0.23f), _cyan);
            left = leftObject.transform;
            right = rightObject.transform;
            leftRenderer = leftObject.GetComponent<Renderer>();
            rightRenderer = rightObject.GetComponent<Renderer>();
        }

        static Transform CreateCore(Transform parent, int index, out QuantumSceneTarget target)
        {
            var group = Child(parent, $"QUANTUM CORE Q-{index:00} - replace model here");
            group.transform.localPosition = new Vector3(0f, 1.12f, -4.15f);
            CreatePrimitive(group.transform, "Pedestal", PrimitiveType.Cylinder, new Vector3(0f, -0.75f, 0f), new Vector3(2.25f, 0.7f, 2.25f), _darkMetal);
            var orb = CreatePrimitive(group.transform, "Core Orb", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.72f, _cyan);
            var ring = CreateRing(group.transform, "Core Halo", Vector3.zero, 0.82f, Cyan, 0.03f);
            var ringB = CreateRing(group.transform, "Core Cage Vertical", Vector3.zero, 0.68f, Cyan, 0.018f);
            ringB.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            target = group.AddComponent<QuantumSceneTarget>();
            target.mainRenderer = orb.GetComponent<Renderer>();
            target.halo = ring;
            target.homeLocalPosition = group.transform.localPosition;
            target.haloBaseScale = Vector3.one;
            return group.transform;
        }

        static void BuildTrain(QuantumLevelScene level, Transform parent, Transform labels)
        {
            var railGroup = Child(parent, "PHASE TRAIN PUZZLE");
            CreatePrimitive(railGroup.transform, "Rail A", PrimitiveType.Cube, new Vector3(-1.45f, 0.08f, -2.55f), new Vector3(7.6f, 0.075f, 0.12f), _cyan, false);
            CreatePrimitive(railGroup.transform, "Rail B", PrimitiveType.Cube, new Vector3(2.3f, 0.08f, -3.65f), new Vector3(0.12f, 0.075f, 3.9f), _cyan, false);
            var train = Child(railGroup.transform, "PHASE TRAIN - replace model here");
            level.trainStartLocal = new Vector3(-5.25f, 0f, -2.55f);
            level.trainJunctionLocal = new Vector3(2.3f, 0f, -2.55f);
            level.trainDockLocal = new Vector3(2.3f, 0f, -4.15f);
            train.transform.localPosition = level.trainStartLocal;
            train.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var body = CreatePrimitive(train.transform, "Train Body", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1.35f, 0.45f, 0.78f), _cyan);
            CreatePrimitive(train.transform, "Head Light", PrimitiveType.Cube, new Vector3(0.72f, 0.37f, 0f), new Vector3(0.12f, 0.12f, 0.48f), _amber, false);
            level.train = train.transform;
            level.trainRenderer = body.GetComponent<Renderer>();
            CreateWorldLabel(labels, "相位列车 // 待命", new Vector3(0f, 1.4f, -2.55f), Amber);
        }

        static void BuildLure(QuantumLevelScene level, Transform parent, Transform labels)
        {
            var group = Child(parent, "LURE AND DATA PUZZLE");
            level.lureZones = new Transform[2];
            var lurePositions = new[] { new Vector3(-4.85f, 0.05f, 3.45f), new Vector3(4.85f, 0.05f, 3.45f) };
            for (var i = 0; i < 2; i++)
            {
                var zone = Child(group.transform, $"LURE ZONE {i + 1} - echo destination");
                zone.transform.localPosition = lurePositions[i];
                CreateRing(zone.transform, "Purple Residual Ring", Vector3.zero, 0.92f, Magenta, 0.07f);
                level.lureZones[i] = zone.transform;
                CreateWorldLabel(labels, $"诱饵残响 {i + 1}", lurePositions[i] + Vector3.up, Magenta);
            }

            level.fragments = new QuantumSceneTarget[2];
            // Keep both blue fragments on the player's side of the gate and offset
            // from the opening so they stay visible and reachable from the start.
            var fragmentPositions = new[] { new Vector3(-1.15f, 1.2f, 0.55f), new Vector3(1.15f, 1.2f, 0.55f) };
            for (var i = 0; i < 2; i++)
            {
                var fragment = CreateMeshObject(group.transform, $"DATA FRAGMENT {i + 1} - replace model here", _octahedron, _cyan, fragmentPositions[i], Vector3.one * 0.28f);
                var collider = fragment.AddComponent<BoxCollider>();
                collider.size = Vector3.one * 2f;
                var halo = CreateRing(fragment.transform, "Extraction Halo", Vector3.zero, 0.48f, Cyan, 0.025f);
                var target = fragment.AddComponent<QuantumSceneTarget>();
                target.mainRenderer = fragment.GetComponent<Renderer>();
                target.halo = halo;
                target.homeLocalPosition = fragment.transform.localPosition;
                target.haloBaseScale = Vector3.one;
                level.fragments[i] = target;
                CreateWorldLabel(labels, $"失落数据 {i + 1} // 长按提取", fragmentPositions[i] + Vector3.up * 0.72f, Cyan);
            }

            level.sentinels = new QuantumSceneTarget[2];
            for (var i = 0; i < 2; i++)
            {
                // Hunters begin low in the player's view and advance from the
                // foreground toward the avatar before being lured away.
                var home = new Vector3(i == 0 ? -2.65f : 2.65f, 0.95f, 7.8f);
                var sentinel = Child(group.transform, $"SENTINEL {i + 1} - replace model here");
                sentinel.transform.localPosition = home;
                var body = CreatePrimitive(sentinel.transform, "Drone Body", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.88f, _danger);
                CreatePrimitive(sentinel.transform, "Tracking Eye", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.42f), Vector3.one * 0.22f, _danger, false);
                var target = sentinel.AddComponent<QuantumSceneTarget>();
                target.mainRenderer = body.GetComponent<Renderer>();
                target.homeLocalPosition = home;
                level.sentinels[i] = target;
            }
            CreateWorldLabel(labels, "追猎矩阵 // 录制两条反向诱饵路线", new Vector3(0f, 1.4f, -0.28f), Danger);
        }

        static QuantumPlayerRig BuildPlayerRig(Transform parent, QuantumDesignPlayerStart designStart)
        {
            var rigObject = new GameObject("PLAYER RIG - PICO XR + Desktop Third Person");
            rigObject.transform.SetParent(parent);
            rigObject.transform.SetPositionAndRotation(designStart.WorldPosition, designStart.WorldYaw);
            var controller = rigObject.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.stepOffset = 0.25f;

            var origin = rigObject.AddComponent<XROrigin>();
            var floorOffset = Child(rigObject.transform, "Camera Floor Offset");
            var cameraObject = Child(floorOffset.transform, "Main Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = Void;
            cameraObject.AddComponent<AudioListener>();
            cameraObject.transform.localPosition = Vector3.up * designStart.xrEyeHeight;
            origin.CameraFloorOffsetObject = floorOffset;
            origin.Camera = camera;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var left = Child(floorOffset.transform, "Left PICO Controller");
            var right = Child(floorOffset.transform, "Right PICO Controller");
            left.transform.localPosition = new Vector3(-0.25f, 1.25f, 0.35f);
            right.transform.localPosition = new Vector3(0.25f, 1.25f, 0.35f);
            var leftModelLoader = left.AddComponent<PicoControllerModelLoader>();
            leftModelLoader.resourcePath = "Prefabs/LeftControllerModel";
            var rightModelLoader = right.AddComponent<PicoControllerModelLoader>();
            rightModelLoader.resourcePath = "Prefabs/RightControllerModel";
            CreatePrimitive(left.transform, "Editor Controller Placeholder", PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.12f, 0.18f), _cyan, false);
            CreatePrimitive(right.transform, "Editor Controller Placeholder", PrimitiveType.Cube, Vector3.zero, new Vector3(0.06f, 0.12f, 0.18f), _cyan, false);
            var leftLine = AddRay(left.transform, "Left Controller Ray", _cyan);
            var rightLine = AddRay(right.transform, "Right Controller Ray", _amber);

            var rig = rigObject.AddComponent<QuantumPlayerRig>();
            rig.xrOrigin = origin;
            rig.cameraFloorOffset = floorOffset.transform;
            rig.xrCamera = camera;
            rig.leftController = left.transform;
            rig.rightController = right.transform;
            rig.leftRay = leftLine;
            rig.rightRay = rightLine;
            rig.designPlayerStart = designStart;
            rig.xrAvatarOnlyLocomotion = true;
            rig.comfortMovementUsesDesignAxes = true;
            // The avatar controller is created on the visible character by the
            // runtime once the model is attached. It prevents PICO stick movement
            // from translating the tracked HMD / world camera.
            return rig;
        }

        static LineRenderer AddRay(Transform parent, string name, Material material)
        {
            var rayObject = Child(parent, name);
            var line = rayObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.008f;
            line.endWidth = 0.002f;
            line.sharedMaterial = material;
            line.enabled = false;
            return line;
        }

        static GameObject BuildEchoTemplate(Transform parent)
        {
            var template = Child(parent, "Echo Avatar Template - edit this model");
            CreatePrimitive(template.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.52f, 0f), new Vector3(0.55f, 0.72f, 0.55f), _echo, false);
            CreateRing(template.transform, "Echo Halo", new Vector3(0f, 0.12f, 0f), 0.48f, Magenta, 0.03f);
            template.SetActive(false);
            return template;
        }

        static QuantumHUD BuildHud(Transform parent)
        {
            var canvasObject = new GameObject("HUD - editable sci-fi panels");
            canvasObject.transform.SetParent(parent);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = null;
            canvas.sortingOrder = 0;
            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(1024f, 640f);
            canvasRect.localScale = Vector3.one * 0.00155f;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1024f, 640f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var hud = canvasObject.AddComponent<QuantumHUD>();
            var brand = Panel(canvasObject.transform, "Brand", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -28f), new Vector2(330f, 96f));
            Text(brand.transform, "Brand Text", "量子回声\nQ-RELAY / PICO XR", 28, TextAnchor.MiddleLeft, Color.white);

            var levelPanel = Panel(canvasObject.transform, "Level Tabs", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-210f, -28f), new Vector2(420f, 64f));
            hud.levelText = Text(levelPanel.transform, "Level Code", "QER-00", 22, TextAnchor.MiddleCenter, Cyan);
            var tabs = new GameObject("Editable Level Buttons");
            tabs.transform.SetParent(levelPanel.transform, false);
            var tabsRect = tabs.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.5f, 0f);
            tabsRect.anchorMax = new Vector2(0.5f, 0f);
            tabsRect.sizeDelta = new Vector2(300f, 34f);
            tabsRect.anchoredPosition = new Vector2(0f, -38f);
            var layout = tabs.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            for (var i = 0; i < 4; i++) CreateLevelButton(tabs.transform, i);

            var loopPanel = Panel(canvasObject.transform, "Loop", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-135f, -112f), new Vector2(270f, 48f));
            hud.loopText = Text(loopPanel.transform, "Loop Text", "当前回路  01", 20, TextAnchor.MiddleCenter, Cyan);

            var objective = Panel(canvasObject.transform, "Objective", new Vector2(0f, 0.72f), new Vector2(0f, 0.72f), new Vector2(28f, -10f), new Vector2(500f, 230f));
            hud.objectiveIndexText = Text(objective.transform, "Objective Index", "01", 40, TextAnchor.UpperLeft, Cyan, new Vector2(20f, -18f), new Vector2(72f, 58f));
            hud.objectiveTitleText = Text(objective.transform, "Objective Title", "踩住保持器", 28, TextAnchor.UpperLeft, Color.white, new Vector2(96f, -22f), new Vector2(370f, 52f));
            hud.objectiveCopyText = Text(objective.transform, "Objective Copy", "走进蓝色圆盘并封存为回声。", 19, TextAnchor.UpperLeft, new Color(0.78f, 0.86f, 0.86f), new Vector2(24f, -86f), new Vector2(445f, 120f));

            var timer = Panel(canvasObject.transform, "Timer", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-378f, -28f), new Vector2(350f, 108f));
            hud.timerLabelText = Text(timer.transform, "Timer Label", "教学回路长度", 17, TextAnchor.UpperLeft, new Color(0.7f, 0.8f, 0.8f), new Vector2(20f, -17f), new Vector2(205f, 30f));
            hud.timerValueText = Text(timer.transform, "Timer Value", "14.0s", 28, TextAnchor.UpperRight, Color.white, new Vector2(225f, -14f), new Vector2(105f, 36f));
            hud.timerFill = Bar(timer.transform, "Timer Bar", new Vector2(20f, -69f), new Vector2(310f, 8f), Cyan);

            var echoPanel = Panel(canvasObject.transform, "Echo Rail", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-378f, 92f), new Vector2(350f, 62f));
            hud.echoCountText = Text(echoPanel.transform, "Echo Count", "回声序列  0 / 4", 18, TextAnchor.MiddleCenter, Magenta);

            var promptPanel = Panel(canvasObject.transform, "Context Prompt", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-330f, 84f), new Vector2(660f, 66f));
            hud.promptKeyText = Text(promptPanel.transform, "Prompt Key", "Q / A", 20, TextAnchor.MiddleCenter, Cyan, new Vector2(16f, -13f), new Vector2(110f, 40f));
            hud.promptText = Text(promptPanel.transform, "Prompt Text", "封存当前动作并开始下一回路", 21, TextAnchor.MiddleLeft, Color.white, new Vector2(136f, -13f), new Vector2(500f, 40f));

            var progress = Panel(canvasObject.transform, "Hold Progress", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-250f, 168f), new Vector2(500f, 92f));
            hud.progressPanel = progress;
            hud.progressLabelText = Text(progress.transform, "Progress Label", "长按 E 回收", 18, TextAnchor.UpperLeft, Color.white, new Vector2(18f, -13f), new Vector2(360f, 30f));
            hud.progressValueText = Text(progress.transform, "Progress Value", "0%", 20, TextAnchor.UpperRight, Cyan, new Vector2(390f, -11f), new Vector2(90f, 32f));
            hud.progressFill = Bar(progress.transform, "Progress Bar", new Vector2(18f, -57f), new Vector2(464f, 9f), Cyan);

            hud.toastText = Text(canvasObject.transform, "Toast", "回路已启动", 20, TextAnchor.MiddleCenter, Amber, new Vector2(710f, -92f), new Vector2(500f, 54f));
            var result = Panel(canvasObject.transform, "Result Overlay", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-330f, -180f), new Vector2(660f, 360f));
            hud.resultPanel = result;
            hud.resultTitleText = Text(result.transform, "Result Title", "核心回收完成", 40, TextAnchor.MiddleCenter, Cyan, new Vector2(30f, -45f), new Vector2(600f, 80f));
            hud.resultStatsText = Text(result.transform, "Result Stats", "使用回声  02    总用时  00:00", 22, TextAnchor.MiddleCenter, Color.white, new Vector2(40f, -145f), new Vector2(580f, 150f));
            return hud;
        }

        static void CreateLevelButton(Transform parent, int index)
        {
            var buttonObject = new GameObject($"Level {index:00} Button");
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.04f, 0.1f, 0.11f, 0.92f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            var levelButton = buttonObject.AddComponent<QuantumLevelButton>();
            levelButton.levelIndex = index;
            Text(buttonObject.transform, "Label", index.ToString("00"), 16, TextAnchor.MiddleCenter, Cyan);
        }

        static void CreateGlobalLighting(Transform parent)
        {
            var lightObject = new GameObject("Global Directional Light");
            lightObject.transform.SetParent(parent);
            lightObject.transform.rotation = Quaternion.Euler(52f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.94f, 1f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }

        static void CreateEventSystem(Transform parent)
        {
            var eventObject = new GameObject("EventSystem - Input System Only");
            eventObject.transform.SetParent(parent);
            eventObject.AddComponent<EventSystem>();
            eventObject.AddComponent<InputSystemUIInputModule>();
        }

        static GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = panel.AddComponent<Image>();
            image.color = new Color(0.018f, 0.045f, 0.052f, 0.92f);
            image.raycastTarget = false;
            return panel;
        }

        static TMP_Text Text(Transform parent, string name, string value, int size, TextAnchor alignment, Color color, Vector2? position = null, Vector2? dimensions = null)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position ?? Vector2.zero;
            rect.sizeDelta = dimensions ?? Vector2.zero;
            if (!dimensions.HasValue)
            {
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.alignment = ConvertAlignment(alignment);
            text.color = color;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Truncate;
            text.raycastTarget = false;
            return text;
        }

        static TextAlignmentOptions ConvertAlignment(TextAnchor alignment)
        {
            return alignment switch
            {
                TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
                TextAnchor.UpperCenter => TextAlignmentOptions.Top,
                TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
                TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
                TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
                TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
                TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
                TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
                _ => TextAlignmentOptions.Center
            };
        }

        static Image Bar(Transform parent, string name, Vector2 position, Vector2 size, Color color)
        {
            var track = new GameObject(name + " Track");
            track.transform.SetParent(parent, false);
            var trackRect = track.AddComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0f, 1f);
            trackRect.anchorMax = new Vector2(0f, 1f);
            trackRect.pivot = new Vector2(0f, 1f);
            trackRect.anchoredPosition = position;
            trackRect.sizeDelta = size;
            var trackImage = track.AddComponent<Image>();
            trackImage.color = new Color(0.08f, 0.16f, 0.17f, 1f);
            var fillObject = new GameObject(name + " Fill");
            fillObject.transform.SetParent(track.transform, false);
            var fillRect = fillObject.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            var fill = fillObject.AddComponent<Image>();
            fill.color = color;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 1f;
            return fill;
        }

        static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material, bool collider = true)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider)
            {
                var primitiveCollider = gameObject.GetComponent<Collider>();
                if (primitiveCollider != null) Object.DestroyImmediate(primitiveCollider);
            }
            return gameObject;
        }

        static GameObject CreateMeshObject(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition, Vector3 localScale)
        {
            var gameObject = Child(parent, name);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
            return gameObject;
        }

        static Transform CreateRing(Transform parent, string name, Vector3 localPosition, float radius, Color color, float width)
        {
            var ringObject = Child(parent, name);
            ringObject.transform.localPosition = localPosition;
            ringObject.AddComponent<MeshFilter>().sharedMesh = GetAnnulusMesh(radius, width);
            ringObject.AddComponent<MeshRenderer>().sharedMaterial = color == Magenta ? _magenta : color == Amber ? _amber : color == Danger ? _danger : _cyan;
            return ringObject.transform;
        }

        static Mesh GetAnnulusMesh(float radius, float width)
        {
            var key = $"Ring_{Mathf.RoundToInt(radius * 1000f)}_{Mathf.RoundToInt(width * 1000f)}";
            var path = $"{MaterialPath}/{key}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            const int segments = 64;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            var outer = radius + width * 0.5f;
            var inner = Mathf.Max(0.001f, radius - width * 0.5f);
            for (var i = 0; i < segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * inner;
                vertices[i * 2 + 1] = direction * outer;
                var next = (i + 1) % segments;
                var triangle = i * 6;
                triangles[triangle] = i * 2;
                triangles[triangle + 1] = next * 2 + 1;
                triangles[triangle + 2] = i * 2 + 1;
                triangles[triangle + 3] = i * 2;
                triangles[triangle + 4] = next * 2;
                triangles[triangle + 5] = next * 2 + 1;
            }
            var mesh = new Mesh { name = key, vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static TextMeshPro CreateWorldLabel(Transform parent, string value, Vector3 localPosition, Color color)
        {
            var label = Child(parent, "Label - " + value);
            label.transform.localPosition = localPosition;
            // The player starts on the +Z side and looks toward -Z.  TextMesh fronts
            // otherwise face the far wall and read mirrored from the gameplay view.
            label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var text = label.AddComponent<TextMeshPro>();
            text.font = _font;
            text.text = value;
            text.fontSize = 1.55f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.color = color;
            return text;
        }
    }

    public static class QuantumEchoBuildPipeline
    {
        [MenuItem("Quantum Echo/Verify PICO Project")]
        public static void Verify()
        {
            var issues = new List<string>();
            EnsureAndroidPresentationMetadata();
            QuantumEchoTitleSceneBuilder.EnsureTitleScene();
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP) issues.Add("Android scripting backend is not IL2CPP");
            if ((PlayerSettings.Android.targetArchitectures & AndroidArchitecture.ARM64) == 0) issues.Add("ARM64 is not enabled");
            if (PlayerSettings.Android.applicationEntry != AndroidApplicationEntry.Activity) issues.Add("Android Activity entry is not selected");
            if (PlayerSettings.Android.resizeableActivity) issues.Add("Android Activity must not be resizeable for full-space PICO VR");
            if (!PlayerSettings.runInBackground) issues.Add("Run In Background is not enabled for PICO XR");
            if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel29) issues.Add("Android minimum API level is below 29");
            var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
            if (!graphicsApis.Contains(GraphicsDeviceType.OpenGLES3)) issues.Add("OpenGLES3 is not enabled for Android");
            var playerSettingsObjects = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (playerSettingsObjects.Length == 0) issues.Add("ProjectSettings asset could not be inspected");
            else
            {
                var serialized = new SerializedObject(playerSettingsObjects[0]);
                var activeInput = serialized.FindProperty("activeInputHandler");
                if (activeInput == null || activeInput.intValue != 1) issues.Add("Input System-only is not enabled");
            }
            if (PXR_Settings.GetSettings().stereoRenderingModeAndroid != PXR_Settings.StereoRenderingModeAndroid.Multiview) issues.Add("PICO Multiview is not enabled");
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings == null || generalSettings.AssignedSettings == null || !generalSettings.AssignedSettings.activeLoaders.Any(loader => loader is PXR_Loader)) issues.Add("PXR_Loader is not assigned for Android");
            if (!File.Exists("Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity")) issues.Add("Main scene is missing");
            if (!QuantumEchoTitleSceneBuilder.VerifyTitleScene(out var titleIssue)) issues.Add("isolated title scene: " + titleIssue);
            var buildScenes = EditorBuildSettings.scenes;
            if (buildScenes.Length != 2 || buildScenes[0].path != QuantumEchoTitleSceneBuilder.TitleScenePath ||
                buildScenes[1].path != QuantumEchoTitleSceneBuilder.GameplayScenePath ||
                !buildScenes.All(scene => scene.enabled))
                issues.Add("build settings must list isolated title then gameplay scene");
            if (issues.Count > 0) throw new BuildFailedException("PICO verification failed:\n" + string.Join("\n", issues));
            Debug.Log("PICO verification passed: PXR_Loader, Multiview, Input System-only, OpenGLES3, API 29+, ARM64, IL2CPP, Activity and editable scene are configured.");
        }

        /// <summary>
        /// Keeps the player-facing Android identity deterministic for every build.
        /// Unity otherwise leaves the Android icon list empty when a project is
        /// copied to the ASCII build staging directory.
        /// </summary>
        static void EnsureAndroidPresentationMetadata()
        {
            PlayerSettings.productName = "量子回声";
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon/AppIcon.png");
            if (icon == null) throw new BuildFailedException("Android icon is missing: Assets/AppIcon/AppIcon.png");
            PlayerSettings.SetIcons(NamedBuildTarget.Android, new[] { icon }, IconKind.Application);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Quantum Echo/Build macOS Desktop Preview")]
        public static void BuildMac()
        {
            Verify();
            Directory.CreateDirectory("Builds/macOS");
            const string outputPath = "Builds/macOS/QuantumEcho.app";
            if (Directory.Exists(outputPath)) FileUtil.DeleteFileOrDirectory(outputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { QuantumEchoTitleSceneBuilder.TitleScenePath, QuantumEchoTitleSceneBuilder.GameplayScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("macOS build failed");
        }

        [MenuItem("Quantum Echo/Build PICO Android APK")]
        public static void BuildAndroid()
        {
            Verify();
            Directory.CreateDirectory("Builds/Android");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            // Android's build toolchain rejects non-ASCII paths.  Keep its actual
            // output name ASCII; the delivered file may be renamed afterwards.
            const string outputPath = "Builds/Android/QuantumEcho-pico.apk";
            if (File.Exists(outputPath)) FileUtil.DeleteFileOrDirectory(outputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { QuantumEchoTitleSceneBuilder.TitleScenePath, QuantumEchoTitleSceneBuilder.GameplayScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                options = BuildOptions.CleanBuildCache
            });
            if (report.summary.result != BuildResult.Succeeded) throw new BuildFailedException("Android build failed");
        }

    }
}
