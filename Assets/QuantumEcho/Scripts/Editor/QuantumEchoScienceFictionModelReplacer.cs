using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumEcho.Editor
{
    /// <summary>
    /// Places the user's numbered sci-fi FBX library in the already-authored scene.
    /// It deliberately preserves QuantumLevelScene references and puzzle root
    /// transforms, so gameplay keeps working while every visual stays editable in
    /// the Scene hierarchy.
    /// </summary>
    public static class QuantumEchoScienceFictionModelReplacer
    {
        const string ModelRoot = "Assets/QuantumEcho/Models/ScienceFictionSpace";
        const string AssetRootName = "SCI-FI ASSET REPLACEMENTS - editable";
        const string VisibilityRootName = "PLAYER VISUAL - model 10 - editor visible";
        // The visible 14-floor surface finishes at y=0.153.  Props that are meant
        // to stand on the deck share this value, while gameplay collisions remain
        // on the original hidden continuous floor.
        const float DeckSurfaceY = 0.155f;

        static readonly Dictionary<int, GameObject> Models = new Dictionary<int, GameObject>();
        static readonly Quaternion SourceZUpToUnityYUp = Quaternion.Euler(-90f, 0f, 0f);
        static Material _echoMaterial;
        static Material _gateForceFieldMaterial;
        static Material _fragmentMaterial;

        [MenuItem("Quantum Echo/Replace Scene With Science Fiction Models")]
        public static void ReplaceCurrentScene()
        {
            LoadModels();
            if (Models.Count != 26)
            {
                Debug.LogError("Sci-fi model replacement stopped: one or more numbered FBX assets are missing.");
                return;
            }

            _echoMaterial = GetOrCreateEchoMaterial();
            _gateForceFieldMaterial = GetOrCreateGateForceFieldMaterial();
            _fragmentMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuantumEcho/Materials/CyanEmission.mat");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity")
            {
                var opened = EditorSceneManager.OpenScene("Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity", OpenSceneMode.Single);
                if (!opened.IsValid())
                {
                    Debug.LogError("Sci-fi model replacement stopped: could not open QuantumEcho_Main.");
                    return;
                }
            }

            var levels = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include)
                .OrderBy(level => level.levelIndex).ToArray();
            if (levels.Length == 0)
            {
                Debug.LogError("Sci-fi model replacement stopped: no QuantumLevelScene was found.");
                return;
            }

            var sceneRoot = FindRoot("QUANTUM ECHO - EDITABLE SCENE");
            if (sceneRoot == null)
            {
                Debug.LogError("Sci-fi model replacement stopped: editable scene root is missing.");
                return;
            }

            ReplacePlayerAndEcho(sceneRoot);
            foreach (var level in levels)
            {
                ReplaceLevel(level);
                EditorUtility.SetDirty(level);
            }
            // Only the tutorial deck is visible in the authored scene.  Runtime level
            // selection still activates the requested root, while entering Play mode
            // can no longer reveal the other three puzzle decks for a frame.
            foreach (var level in levels) level.gameObject.SetActive(level.levelIndex == 0);
            BuildSharedSpaceDecor(sceneRoot);
            QuantumEchoNebulaSkybox.EnsureForActiveScene();

            scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Sci-fi model replacement complete: models 1-26 are placed in editable Scene objects; open-space layout and gameplay references are preserved.");
        }

        [MenuItem("Quantum Echo/Redistribute Deep Space Decor Only")]
        public static void RedistributeDeepSpaceDecorOnly()
        {
            LoadModels();
            if (Enumerable.Range(1, 9).Any(number => !Models.ContainsKey(number)))
            {
                Debug.LogError("Deep-space redistribution stopped: one or more models 01-09 are missing.");
                return;
            }

            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity", OpenSceneMode.Single);
            }

            var sceneRoot = FindRoot("QUANTUM ECHO - EDITABLE SCENE");
            if (sceneRoot == null)
            {
                Debug.LogError("Deep-space redistribution stopped: editable scene root is missing.");
                return;
            }

            BuildSharedSpaceDecor(sceneRoot);
            QuantumEchoNebulaSkybox.EnsureForActiveScene();
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                camera.farClipPlane = Mathf.Max(camera.farClipPlane, 260f);
                EditorUtility.SetDirty(camera);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Deep-space decor redistributed around the playable area; starfield restored and camera range extended.");
        }

        static void LoadModels()
        {
            Models.Clear();
            for (var number = 1; number <= 26; number++)
            {
                var folder = $"{ModelRoot}/{number}";
                var guid = AssetDatabase.FindAssets("t:Model", new[] { folder }).FirstOrDefault();
                if (string.IsNullOrEmpty(guid)) continue;
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) Models[number] = asset;
            }
        }

        static Transform FindRoot(string name)
        {
            return Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(transform => transform.name == name);
        }

        static void ReplaceLevel(QuantumLevelScene level)
        {
            var assets = GetOrCreateChild(level.transform, AssetRootName);
            ClearChildren(assets);

            ConvertToOpenSpace(level);
            BuildFloorAndArchitecture(level, assets);
            BuildStationSetDressing(level, assets);

            if (level.mode != QuantumLevelMode.Lure)
            {
                ReplacePad(level, level.padA, 0, "Pressure Stabilizer A - model 11");
                if (level.padB != null) ReplacePad(level, level.padB, 1, "Pressure Stabilizer B - model 11");
            }

            ReplaceGate(level);
            ReplaceCore(level);
            if (level.mode == QuantumLevelMode.Train) ReplaceTrainSet(level, assets);
            if (level.mode == QuantumLevelMode.Lure) ReplaceLureSet(level, assets);
        }

        static void ConvertToOpenSpace(QuantumLevelScene level)
        {
            var environment = level.transform.Find("ENVIRONMENT - walls floor lights");
            if (environment == null) return;
            foreach (var child in environment.GetComponentsInChildren<Transform>(true))
            {
                if (child == environment) continue;
                if (child.name == "Floor" || child.name == "Spawn Safety Deck - editable")
                {
                    var renderer = child.GetComponent<Renderer>();
                    if (renderer != null) renderer.enabled = false;
                    continue;
                }
                if (child.name.Contains("Wall") || child.name.Contains("Ceiling") || child.name.Contains("Rear Window") || child.name.Contains("Gate Left Barrier") || child.name.Contains("Gate Right Barrier") || child.name.Contains("Floor Grid") || child.name.Contains("Arena Ring") || child.name.StartsWith("Label -"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        static void BuildFloorAndArchitecture(QuantumLevelScene level, Transform assets)
        {
            var floor = Child(assets, "Floor Modules - model 14 - editable");
            // 14 is a 1x1 panel lying in source XY, with thickness in source Z.
            // Place() already converts every source model from Z-up to Unity Y-up,
            // so no additional rotation is needed here.  Source assets are never
            // stretched: each instance keeps a uniform scale, while the deck grows
            // by adding more tiles.  Keep the hidden floor cubes for continuous
            // controller collision.
            for (var x = -6; x <= 6; x += 2)
            {
                for (var z = -6; z <= 10; z += 2)
                {
                    var panel = Place(floor, 14, $"Deck Panel {x:00} {z:00}", new Vector3(x, 0.018f, z), 2.04f, Vector3.zero);
                    DisableColliders(panel);
                }
            }

            var walls = Child(assets, "Partial Station Walls - model 15 - editable");
            // No enclosing walls: these four pieces are visual framing and partial
            // cover only.  The z=8 pieces sit behind the player-facing edge.
            Place(walls, 15, "Left Frame", new Vector3(-6.45f, DeckSurfaceY, -3.7f), 2.15f, Vector3.zero);
            Place(walls, 15, "Right Frame", new Vector3(6.45f, DeckSurfaceY, -3.7f), 2.15f, new Vector3(0f, 180f, 0f));
            Place(walls, 15, "Rear Tech Partition Left", new Vector3(-4.9f, DeckSurfaceY, 7.6f), 2.05f, new Vector3(0f, 55f, 0f));
            Place(walls, 15, "Rear Tech Partition Right", new Vector3(4.9f, DeckSurfaceY, 7.6f), 2.05f, new Vector3(0f, -55f, 0f));

            // Build a continuous bulkhead from both sides of the gate all the way to
            // the deck edges.  The modular panels are tiled instead of stretched, so
            // the wall keeps the proportions of model 15 and the player cannot bypass
            // the puzzle by walking around the animated door.
            var gateWalls = Child(assets, "Force Gate Surround - model 15 - editable");
            var leftWallCenters = new[] { -6.15f, -4.18f, -2.21f };
            var rightWallCenters = new[] { 2.21f, 4.18f, 6.15f };
            for (var index = 0; index < leftWallCenters.Length; index++)
            {
                Place(gateWalls, 15, $"Gate Bulkhead Left {index + 1:00}", new Vector3(leftWallCenters[index], DeckSurfaceY, -0.72f), 2.05f, new Vector3(0f, 90f, 0f));
                Place(gateWalls, 15, $"Gate Bulkhead Right {index + 1:00}", new Vector3(rightWallCenters[index], DeckSurfaceY, -0.72f), 2.05f, new Vector3(0f, -90f, 0f));
            }
            AddStaticBlockingVolume(gateWalls, "Left Bulkhead Collision - gameplay", new Vector3(-4.2f, 1.02f, -0.72f), new Vector3(6.0f, 2.04f, 0.52f));
            AddStaticBlockingVolume(gateWalls, "Right Bulkhead Collision - gameplay", new Vector3(4.2f, 1.02f, -0.72f), new Vector3(6.0f, 2.04f, 0.52f));

            // The overhead model-26 service beams were visual dressing only and
            // obscured the gate approach in all four rooms, so they are omitted.
        }

        static void BuildStationSetDressing(QuantumLevelScene level, Transform assets)
        {
            var lights = Child(assets, "Guide Light Posts - model 16 - editable");
            Place(lights, 16, "Guide Light Left", new Vector3(-5.55f, DeckSurfaceY, 1.2f), 1.6f, Vector3.zero);
            Place(lights, 16, "Guide Light Right", new Vector3(5.55f, DeckSurfaceY, 1.2f), 1.6f, new Vector3(0f, 180f, 0f));
            Place(lights, 16, "Gate Camera Post Left", new Vector3(-2.15f, DeckSurfaceY, 0.65f), 1.25f, new Vector3(0f, 90f, 0f));
            Place(lights, 16, "Gate Camera Post Right", new Vector3(2.15f, DeckSurfaceY, 0.65f), 1.25f, new Vector3(0f, -90f, 0f));

            var terminals = Child(assets, "Research Terminals - model 24 - editable");
            Place(terminals, 24, "Research Terminal Left", new Vector3(-5.78f, DeckSurfaceY, -3.45f), 1.5f, new Vector3(0f, 90f, 0f));
            Place(terminals, 24, "Research Terminal Right", new Vector3(5.78f, DeckSurfaceY, -3.45f), 1.5f, new Vector3(0f, -90f, 0f));

            var crates = Child(assets, "Equipment Cases - model 25 - editable");
            Place(crates, 25, "Case A", new Vector3(-5.15f, DeckSurfaceY, 4.85f), 1.25f, new Vector3(0f, 20f, 0f));
            Place(crates, 25, "Case B", new Vector3(5.1f, DeckSurfaceY, 5.05f), 1.18f, new Vector3(0f, -25f, 0f));
        }

        static void ReplacePad(QuantumLevelScene level, Transform padRoot, int padIndex, string displayName)
        {
            if (padRoot == null) return;
            ClearChildren(padRoot);
            var model = Place(padRoot, 11, displayName, new Vector3(0f, 0.075f, 0f), 1.55f, Vector3.zero);
            DisableColliders(model);
            var renderer = FirstRenderer(model);
            if (padIndex == 0) level.padARenderer = renderer;
            else level.padBRenderer = renderer;
        }

        static void ReplaceGate(QuantumLevelScene level)
        {
            if (level.gateLeft == null || level.gateRight == null) return;
            ClearChildren(level.gateLeft);
            ClearChildren(level.gateRight);
            DisableRootVisualAndColliders(level.gateLeft);
            DisableRootVisualAndColliders(level.gateRight);
            // #12 is a single upright half-panel in its source axes.  Keep the
            // actual mechanical model equal-scale and lower than before.  A thin,
            // transparent force-field child carries the readable gameplay state.
            var left = Place(level.gateLeft, 12, "Dual Phase Gate Left - model 12", new Vector3(0f, -0.49f, 0f), 1.42f, new Vector3(0f, 90f, 0f));
            var right = Place(level.gateRight, 12, "Dual Phase Gate Right - model 12", new Vector3(0f, -0.49f, 0f), 1.42f, new Vector3(0f, -90f, 0f));
            DisableColliders(left);
            DisableColliders(right);
            AddGateBlockingVolume(level.gateLeft, "Gate Blocking Volume Left - gameplay", new Vector3(1.42f, 1.3f, 0.42f));
            AddGateBlockingVolume(level.gateRight, "Gate Blocking Volume Right - gameplay", new Vector3(1.42f, 1.3f, 0.42f));
            var leftField = AddGateForceField(level.gateLeft, "Transparent Phase Field Left");
            var rightField = AddGateForceField(level.gateRight, "Transparent Phase Field Right");
            level.gateLeftRenderer = leftField;
            level.gateRightRenderer = rightField;
        }

        static void ReplaceCore(QuantumLevelScene level)
        {
            if (level.core == null) return;
            ClearChildren(level.core);
            var core = Place(level.core, 13, "Quantum Core Containment Podium - model 13", new Vector3(0f, -0.965f, 0f), 1.7f, Vector3.zero);
            DisableColliders(core);
            var renderer = FirstRenderer(core);
            if (level.coreTarget != null)
            {
                level.coreTarget.mainRenderer = renderer;
                level.coreTarget.halo = null;
            }
        }

        static void ReplaceTrainSet(QuantumLevelScene level, Transform assets)
        {
            if (level.train == null) return;
            ClearChildren(level.train);
            // Model 17's source forward axis becomes Unity -Z after Z-up conversion.
            // The train root is still free to rotate at the junction; orient the
            // visible carriage itself so it is legible in level 02 from the player.
            var train = Place(level.train, 17, "Phase Train - model 17", new Vector3(0f, 0.32f, 0f), 2.05f, new Vector3(0f, -90f, 0f));
            DisableColliders(train);
            level.trainRenderer = FirstRenderer(train);

            var trainSet = Child(assets, "Phase Train Rail and Controls - models 18 19 20 - editable");
            Place(trainSet, 18, "Maglev Rail A - model 18", new Vector3(-1.45f, 0.15f, -2.55f), 2.1f, Vector3.zero);
            Place(trainSet, 18, "Maglev Rail B - model 18", new Vector3(2.3f, 0.15f, -3.65f), 2.1f, new Vector3(0f, 90f, 0f));
            Place(trainSet, 18, "Maglev Dock - model 18", new Vector3(2.3f, 0.15f, -4.15f), 2.1f, new Vector3(0f, 90f, 0f));
            Place(trainSet, 19, "Launch Control Console A - model 19", level.padA.localPosition + new Vector3(0f, 0.075f, -0.82f), 1.18f, Vector3.zero);
            Place(trainSet, 19, "Switch Control Console B - model 19", level.padB.localPosition + new Vector3(0f, 0.075f, -0.82f), 1.18f, Vector3.zero);
            Place(trainSet, 20, "Central Timing Prism - model 20", new Vector3(0f, DeckSurfaceY, -3.85f), 1.8f, new Vector3(0f, 20f, 0f));
        }

        static void ReplaceLureSet(QuantumLevelScene level, Transform assets)
        {
            var lureSet = Child(assets, "Lure and Data Visuals - models 21 22 23 - editable");
            if (level.lureZones != null)
            {
                for (var index = 0; index < level.lureZones.Length; index++)
                {
                    var zone = level.lureZones[index];
                    if (zone == null) continue;
                    var beacon = Place(lureSet, 21, $"Echo Lure Beacon {index + 1:00} - model 21", zone.localPosition + new Vector3(0f, DeckSurfaceY - zone.localPosition.y, 0f), 1.35f, Vector3.zero);
                    RemoveColliders(beacon);
                }
            }

            if (level.sentinels != null)
            {
                for (var index = 0; index < level.sentinels.Length; index++)
                {
                    var sentinel = level.sentinels[index];
                    if (sentinel == null) continue;
                    ClearChildren(sentinel.transform);
                    var drone = Place(sentinel.transform, 22, $"Hunter Sentinel {index + 1:00} - model 22", new Vector3(0f, -0.38f, 0f), 1.1f, Vector3.zero);
                    DisableColliders(drone);
                    sentinel.mainRenderer = FirstRenderer(drone);
                }
            }

            if (level.fragments != null)
            {
                // The blue data sits on the player's side of the containment door so
                // it remains visible before the gate opens and cannot be hidden behind
                // the panels or the relocated camera-side guide posts.
                var outerDataPositions = new[]
                {
                    new Vector3(-1.15f, 1.2f, 0.55f),
                    new Vector3(1.15f, 1.2f, 0.55f)
                };
                for (var index = 0; index < level.fragments.Length; index++)
                {
                    var fragment = level.fragments[index];
                    if (fragment == null) continue;
                    if (index < outerDataPositions.Length) fragment.transform.localPosition = outerDataPositions[index];
                    ClearChildrenExcept(fragment.transform, fragment.halo);
                    DisableRootVisualAndColliders(fragment.transform);
                    var data = Place(fragment.transform, 23, $"Lost Data Fragment {index + 1:00} - model 23", Vector3.zero, 0.86f, Vector3.zero);
                    if (_fragmentMaterial != null) ApplyMaterial(data, _fragmentMaterial);
                    DisableColliders(data);
                    var box = fragment.GetComponent<BoxCollider>();
                    if (box == null) box = fragment.gameObject.AddComponent<BoxCollider>();
                    box.center = Vector3.zero;
                    box.size = Vector3.one * 1.1f;
                    fragment.mainRenderer = FirstRenderer(data);
                }
                MoveLureDataLabels(level, outerDataPositions);
                QuantumEchoScenePolisher.RepairLureLayout(level);
            }
        }

        static void ReplacePlayerAndEcho(Transform sceneRoot)
        {
            var rig = Object.FindAnyObjectByType<QuantumPlayerRig>(FindObjectsInactive.Include);
            if (rig != null)
            {
                rig.desktopThirdPersonOffset = new Vector3(0f, 4.8f, 8.8f);
                // Runtime derives the PICO offset from the active room's single
                // QuantumDesignPlayerStart rather than retaining a second spawn.
                rig.xrThirdPersonOffset = Vector3.zero;
                rig.thirdPersonLookDownDegrees = 25f;
                // The reviewed transform defines the cyan lens as the front. Movement
                // yaw remains on the editable visual root while this child keeps the
                // exact imported-model orientation shown in the Unity Inspector.
                rig.visibleModelForwardYawOffset = 0f;
                EditorUtility.SetDirty(rig);
                var existing = rig.transform.Find(VisibilityRootName);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
                var playerVisualRoot = Child(rig.transform, VisibilityRootName);
                // The player model occupies the same standing origin as the
                // CharacterController.  A runtime component hides it for first
                // person, but it is intentionally visible and selectable in Scene.
                // User-defined scale: X/Y/Z are all 1.8.  Do not deform the source
                // drone to fake a different silhouette.
                var playerBody = Place(playerVisualRoot, 10, "Player Operation Body - model 10", new Vector3(0f, 0.24f, 0f), 1.8f, Vector3.zero);
                playerBody.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                playerVisualRoot.gameObject.AddComponent<QuantumFirstPersonVisual>();
                rig.operationBodyVisual = playerVisualRoot;
                var thirdPersonCamera = rig.xrCamera != null
                    ? rig.xrCamera.GetComponent<QuantumThirdPersonCamera>()
                    : null;
                if (thirdPersonCamera == null && rig.xrCamera != null)
                {
                    thirdPersonCamera = rig.xrCamera.gameObject.AddComponent<QuantumThirdPersonCamera>();
                }
                if (thirdPersonCamera != null)
                {
                    thirdPersonCamera.player = rig;
                    thirdPersonCamera.distance = 9.2f;
                    thirdPersonCamera.height = 4.6f;
                    thirdPersonCamera.targetHeight = 0.85f;
                    EditorUtility.SetDirty(thirdPersonCamera);
                }
                EditorUtility.SetDirty(rig);
            }

            var templateRoot = FindRoot("RUNTIME TEMPLATES - editable source models");
            if (templateRoot == null) return;
            var template = templateRoot.Find("Echo Avatar Template - edit this model");
            if (template == null) return;
            ClearChildren(template);
            // Echoes match the player's reviewed equal-scale transform and facing.
            var echo = Place(template, 10, "Echo Body - model 10 - translucent purple cyan", new Vector3(0f, 0.24f, 0f), 1.8f, Vector3.zero);
            echo.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
            ApplyMaterial(echo, _echoMaterial);
            template.gameObject.SetActive(false);
        }

        static void BuildSharedSpaceDecor(Transform sceneRoot)
        {
            var old = sceneRoot.Find("DEEP SPACE DECOR - models 01-09 - editable");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            var space = Child(sceneRoot, "DEEP SPACE DECOR - models 01-09 - editable");
            // The four playable rooms span the X axis.  Place the nine distant
            // objects around that whole span, including both Z hemispheres and
            // varied heights, so turning around in XR reveals a balanced sky rather
            // than a row of planets gathered in the initial camera direction.
            var planetPlacements = new[]
            {
                new { id = 1, pos = new Vector3(-32f, 10f, -36f), scale = 7f, rot = new Vector3(15f, 32f, 0f) },
                new { id = 2, pos = new Vector3(18f, -6f, -62f), scale = 10f, rot = new Vector3(0f, 42f, 0f) },
                new { id = 3, pos = new Vector3(88f, 18f, -44f), scale = 6f, rot = new Vector3(5f, -30f, 0f) },
                new { id = 4, pos = new Vector3(110f, 8f, 8f), scale = 9f, rot = new Vector3(-12f, 20f, 0f) },
                new { id = 5, pos = new Vector3(88f, -8f, 46f), scale = 5f, rot = new Vector3(0f, -45f, 0f) },
                new { id = 6, pos = new Vector3(36f, 24f, 70f), scale = 12f, rot = new Vector3(20f, 60f, 0f) },
                new { id = 7, pos = new Vector3(-25f, 5f, 50f), scale = 7f, rot = new Vector3(0f, 0f, 0f) },
                new { id = 8, pos = new Vector3(-55f, 17f, 5f), scale = 11f, rot = new Vector3(8f, 28f, 0f) },
                new { id = 9, pos = new Vector3(42f, 48f, -8f), scale = 4.5f, rot = new Vector3(0f, -15f, 0f) }
            };
            foreach (var item in planetPlacements)
            {
                var planet = Place(space, item.id, $"Space Decor {item.id:00} - model {item.id}", item.pos, item.scale, item.rot);
                DisableColliders(planet);
            }
        }

        static GameObject Place(Transform parent, int modelNumber, string name, Vector3 localPosition, float uniformScale, Vector3 localEuler)
        {
            var instance = PrefabUtility.InstantiatePrefab(Models[modelNumber], parent) as GameObject;
            if (instance == null) instance = Object.Instantiate(Models[modelNumber], parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            // Every Tripo source model is Z-up; convert to Unity's Y-up coordinate
            // system first, then apply the authored Unity-world yaw/tilt.
            instance.transform.localRotation = Quaternion.Euler(localEuler) * SourceZUpToUnityYUp;
            instance.transform.localScale = Vector3.one * uniformScale;
            return instance;
        }

        static void AddGateBlockingVolume(Transform gateRoot, string name, Vector3 size)
        {
            var volume = new GameObject(name);
            volume.transform.SetParent(gateRoot, false);
            // The visual panels slide with their gameplay roots.  This simple hidden
            // volume does the same, closing the central opening when shut and leaving
            // a generous clear passage as the two halves separate.
            var collider = volume.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;
        }

        static Renderer AddGateForceField(Transform gateRoot, string name)
        {
            var field = GameObject.CreatePrimitive(PrimitiveType.Cube);
            field.name = name;
            field.transform.SetParent(gateRoot, false);
            field.transform.localPosition = new Vector3(0f, 0.07f, -0.08f);
            field.transform.localScale = new Vector3(1.38f, 1.26f, 0.055f);
            var collider = field.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            var renderer = field.GetComponent<Renderer>();
            renderer.sharedMaterial = _gateForceFieldMaterial;
            return renderer;
        }

        static void AddStaticBlockingVolume(Transform parent, string name, Vector3 localPosition, Vector3 size)
        {
            var volume = new GameObject(name);
            volume.transform.SetParent(parent, false);
            volume.transform.localPosition = localPosition;
            var collider = volume.AddComponent<BoxCollider>();
            collider.center = Vector3.zero;
            collider.size = size;
        }

        static void MoveLureDataLabels(QuantumLevelScene level, Vector3[] positions)
        {
            var labels = level.transform.Find("WORLD LABELS");
            if (labels == null) return;
            foreach (var label in labels.GetComponentsInChildren<TextMesh>(true))
            {
                if (!label.text.StartsWith("失落数据")) continue;
                var isSecond = label.text.Contains("2");
                var index = isSecond ? 1 : 0;
                if (index >= positions.Length) continue;
                label.transform.localPosition = positions[index] + Vector3.up * 0.72f;
            }
        }

        static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        static Transform GetOrCreateChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            return child != null ? child : Child(parent, name);
        }

        static void ClearChildren(Transform parent)
        {
            var children = new List<GameObject>();
            foreach (Transform child in parent) children.Add(child.gameObject);
            foreach (var child in children) Object.DestroyImmediate(child);
        }

        static void ClearChildrenExcept(Transform parent, Transform keep)
        {
            var children = new List<GameObject>();
            foreach (Transform child in parent)
            {
                if (child != keep) children.Add(child.gameObject);
            }
            foreach (var child in children) Object.DestroyImmediate(child);
        }

        static Renderer FirstRenderer(GameObject root)
        {
            return root.GetComponentsInChildren<Renderer>(true).FirstOrDefault();
        }

        static void DisableColliders(GameObject root)
        {
            RemoveColliders(root);
        }

        static void RemoveColliders(GameObject root)
        {
            if (root == null) return;
            foreach (var collider in root.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider, true);
        }

        static void DisableRootVisualAndColliders(Transform root)
        {
            var renderer = root.GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            foreach (var collider in root.GetComponents<Collider>()) collider.enabled = false;
        }

        static void ApplyMaterial(GameObject root, Material material)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
            }
        }

        static Material GetOrCreateEchoMaterial()
        {
            const string path = "Assets/QuantumEcho/Materials/EchoBody_TransparentPurpleCyan.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = "Echo Body - Transparent Purple Cyan";
            material.color = new Color(0.55f, 0.18f, 0.85f, 0.46f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 0.8f, 1f) * 1.8f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetOrCreateGateForceFieldMaterial()
        {
            const string path = "Assets/QuantumEcho/Materials/GateForceField_TransparentCyan.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }
            material.name = "Gate Force Field - Transparent Cyan";
            material.color = new Color(0.12f, 0.82f, 1f, 0.46f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.08f, 0.55f, 0.9f) * 1.35f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
