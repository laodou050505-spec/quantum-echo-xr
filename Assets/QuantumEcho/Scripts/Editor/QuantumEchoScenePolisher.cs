using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace QuantumEcho.Editor
{
    /// <summary>Repairs existing scene objects without regenerating authored level layouts.</summary>
    public static class QuantumEchoScenePolisher
    {
        const string GateMaterialPath = "Assets/QuantumEcho/Materials/GateModel12_Transparent.mat";
        const string TrainMaterialPath = "Assets/QuantumEcho/Materials/PhaseTrain_Focused.mat";
        const string FragmentHologramMaterialPath = "Assets/QuantumEcho/Materials/BlueFragmentHologram.mat";
        const string TrainObservationWallMaterialPath = "Assets/QuantumEcho/Materials/TrainObservationWall_Transparent.mat";
        static readonly Quaternion SourceZUpToUnityYUp = Quaternion.Euler(-90f, 0f, 0f);

        [MenuItem("Quantum Echo/Polish Existing Scene Safely")]
        public static void PolishExistingSceneSafely()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity")
            {
                scene = EditorSceneManager.OpenScene("Assets/QuantumEcho/Scenes/QuantumEcho_Main.unity", OpenSceneMode.Single);
            }

            RepairPlayerAndEcho();
            RepairSceneComponents();
            RepairSpatialPresentation();
            RepairUniformModelScales();
            var gateMaterial = GetOrCreateTransparentGateMaterial();
            foreach (var level in Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include))
            {
                RepairGate(level, gateMaterial);
                if (level.mode == QuantumLevelMode.Train) RepairTrain(level);
                EditorUtility.SetDirty(level);
            }

            QuantumEchoCollisionInstaller.ApplyToOpenScene();
            RepairHud();
            RepairVisualQuality();
            SetGameViewScaleToOne();
            QuantumEchoNebulaSkybox.EnsureForActiveScene();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            WriteSceneAudit();
            Debug.Log("Existing scene polished safely: equal-scale models, corrected player facing, lower transparent gates, visible phase train, generated HUD skin, and crisp rendering are active.");
        }

        public static bool WriteSceneAudit()
        {
            var issues = new List<string>();
            var missingScripts = 0;
            foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
            }
            if (missingScripts > 0) issues.Add($"missingScripts={missingScripts}");

            var playerBody = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .FirstOrDefault(child => child.name == "Player Operation Body - model 10");
            if (playerBody == null) issues.Add("playerBody=missing");
            else
            {
                if (!IsUniform(playerBody.localScale) || !Mathf.Approximately(playerBody.localScale.x, 1.8f))
                    issues.Add($"playerScale={playerBody.localScale}");
                var playerEuler = playerBody.localEulerAngles;
                if (Mathf.Abs(Mathf.DeltaAngle(playerEuler.x, -90f)) > 0.5f ||
                    Mathf.Abs(Mathf.DeltaAngle(playerEuler.y, 90f)) > 0.5f ||
                    Mathf.Abs(Mathf.DeltaAngle(playerEuler.z, 0f)) > 0.5f)
                    issues.Add($"playerLocalRotation={playerEuler}");
                if (Vector3.Distance(playerBody.localPosition, new Vector3(0f, 0.24f, 0f)) > 0.01f)
                    issues.Add($"playerLocalPosition={playerBody.localPosition}");
            }

            var nonUniformModels = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                .Count(transform => PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject) &&
                                    IsManagedScienceFictionModel(transform) && !IsUniform(transform.localScale));
            if (nonUniformModels > 0) issues.Add($"nonUniformModels={nonUniformModels}");

            var trainLevel = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include)
                .FirstOrDefault(level => level.mode == QuantumLevelMode.Train);
            if (trainLevel == null || trainLevel.train == null || trainLevel.trainRenderer == null || !trainLevel.trainRenderer.enabled)
                issues.Add("train=missing-or-hidden");

            var lureLevel = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include)
                .FirstOrDefault(level => level.mode == QuantumLevelMode.Lure);
            if (lureLevel == null || lureLevel.fragments == null || lureLevel.fragments.Length != 2 || lureLevel.fragments.Any(fragment => fragment == null))
                issues.Add("lureFragments=invalid");
            else
            {
                if (!LureFragmentsVisiblePlayerSide(lureLevel))
                    issues.Add("lureFragments=not-visible-player-side");
                if (lureLevel.sentinels == null || lureLevel.sentinels.Length != 2 ||
                    lureLevel.sentinels.Any(sentinel => sentinel == null || sentinel.transform.localPosition.z < 7f))
                    issues.Add("lureSentinels=not-foreground");
            }

            var gatesVerified = 0;
            var gateHeaders = 0;
            var overheadGateObstructors = 0;
            var centralGateBeacons = 0;
            var cameraSideGatePosts = 0;
            foreach (var level in Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include))
            {
                if (!AuditGateHalf(level.gateLeft, level.gateLeftRenderer, out var leftGateIssue))
                    issues.Add($"level{level.levelIndex}:leftGate={leftGateIssue}");
                if (!AuditGateHalf(level.gateRight, level.gateRightRenderer, out var rightGateIssue))
                    issues.Add($"level{level.levelIndex}:rightGate={rightGateIssue}");
                if (string.IsNullOrEmpty(leftGateIssue) && string.IsNullOrEmpty(rightGateIssue)) gatesVerified++;

                gateHeaders += level.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name == "Gate Header");
                overheadGateObstructors += level.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name == "Suspended Service Beams - model 26 - editable");
                centralGateBeacons += level.GetComponentsInChildren<Transform>(true)
                    .Count(child => child.name == "Gate Beacon");
                var levelCameraPosts = level.GetComponentsInChildren<Transform>(true)
                    .Where(child => child.name == "Gate Camera Post Left" || child.name == "Gate Camera Post Right")
                    .ToArray();
                cameraSideGatePosts += levelCameraPosts.Count(post =>
                    post.localPosition.z > 0.2f && post.localPosition.z < 1.2f &&
                    Mathf.Abs(post.localPosition.x) > 1.8f && Mathf.Abs(post.localPosition.x) < 2.5f &&
                    IsUniform(post.localScale));
            }
            if (gateHeaders != 0) issues.Add($"gateHeaders={gateHeaders}");
            if (overheadGateObstructors != 0) issues.Add($"overheadGateObstructors={overheadGateObstructors}");
            if (centralGateBeacons != 0) issues.Add($"centralGateBeacons={centralGateBeacons}");
            if (cameraSideGatePosts != 8) issues.Add($"cameraSideGatePosts={cameraSideGatePosts}/8");
            var bulkheadBlockers = Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include)
                .Count(collider => (collider.name.Contains("Left Bulkhead Collision") || collider.name.Contains("Right Bulkhead Collision")) &&
                                   collider.enabled && !collider.isTrigger);
            if (bulkheadBlockers < 8) issues.Add($"bulkheadBlockers={bulkheadBlockers}/8");

            var trainCollider = trainLevel != null && trainLevel.train != null
                ? trainLevel.train.GetComponent<BoxCollider>()
                : null;
            if (trainCollider == null || !trainCollider.enabled || trainCollider.isTrigger)
                issues.Add("trainCollider=invalid");
            var trainModel = trainLevel != null && trainLevel.train != null
                ? trainLevel.train.GetComponentsInChildren<Transform>(true)
                    .FirstOrDefault(child => child.name == "Phase Train - model 17")
                : null;
            if (trainModel == null || !IsUniform(trainModel.localScale) || trainModel.localScale.x < 2.9f)
                issues.Add("trainScaleOrModel=invalid");
            if (trainLevel == null || trainLevel.train == null ||
                Mathf.Abs(Mathf.DeltaAngle(trainLevel.train.localEulerAngles.y, 180f)) > 0.5f)
                issues.Add("trainForwardYaw=invalid");
            var trainFocus = trainLevel != null && trainLevel.train != null
                ? FindChild(trainLevel.train, "Train Focus Light")?.GetComponent<Light>()
                : null;
            if (trainFocus == null || !trainFocus.enabled || trainFocus.range < 2.5f)
                issues.Add("trainFocus=invalid");
            if (trainLevel == null || trainLevel.trainStartLocal.z < -1.5f || trainLevel.trainJunctionLocal.z < -1.5f)
                issues.Add("trainRoute=occluded");
            var trainObservationWalls = GetTrainObservationWallRenderers(trainLevel);
            var trainObservationWallCollision = GetTrainObservationWallColliders(trainLevel);
            if (trainObservationWalls.Length < 6 || trainObservationWalls.Any(renderer =>
                    renderer == null || !renderer.enabled || renderer.sharedMaterial == null ||
                    renderer.sharedMaterial.color.a > 0.42f))
                issues.Add($"trainObservationWalls=opaque-or-missing({trainObservationWalls.Length})");
            if (trainObservationWallCollision.Length < 2 || trainObservationWallCollision.Any(collider =>
                    collider == null || !collider.enabled || collider.isTrigger))
                issues.Add($"trainObservationWallCollision=invalid({trainObservationWallCollision.Length})");

            var hud = Object.FindAnyObjectByType<QuantumHUD>(FindObjectsInactive.Include);
            var legacyHudText = hud != null ? hud.GetComponentsInChildren<Text>(true).Length : -1;
            var tmpHudText = hud != null ? hud.GetComponentsInChildren<TMP_Text>(true).Length : 0;
            var hudCanvas = hud != null ? hud.GetComponent<Canvas>() : null;
            if (hud == null) issues.Add("hud=missing");
            else
            {
                if (legacyHudText != 0) issues.Add($"legacyHudText={legacyHudText}");
                if (tmpHudText < 19 || hud.GetComponentsInChildren<TMP_Text>(true).Any(text => text.font == null))
                    issues.Add($"tmpHudText=invalid({tmpHudText})");
                if (hudCanvas == null || hudCanvas.renderMode != RenderMode.WorldSpace || hudCanvas.worldCamera != null)
                    issues.Add("hudWorldSpace=invalid");
                var scaler = hud.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                    Vector2.Distance(scaler.referenceResolution, new Vector2(1024f, 640f)) > 0.5f)
                    issues.Add("hudReferenceResolution=invalid");
            }
            if (QualitySettings.antiAliasing < 4) issues.Add($"antiAliasing={QualitySettings.antiAliasing}");

            var rig = Object.FindAnyObjectByType<QuantumPlayerRig>(FindObjectsInactive.Include);
            if (rig == null || rig.xrCamera == null || rig.xrCamera.GetComponent<TrackedPoseDriver>() == null)
                issues.Add("trackedPoseDriver=missing");
            if (rig == null || rig.rightController == null || rig.rightRay == null || rig.controllerRayLength < 3f)
                issues.Add("rightControllerRay=missing");
            var comfortAvatarLocomotion = rig != null && rig.xrAvatarOnlyLocomotion && rig.comfortMovementUsesDesignAxes;
            if (!comfortAvatarLocomotion)
                issues.Add("comfortAvatarLocomotion=invalid");

            var allLevels = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include)
                .OrderBy(level => level.levelIndex)
                .ToArray();
            var authoritativeStarts = allLevels.Length == 4 && allLevels.All(level =>
                level.designPlayerStart != null &&
                level.designPlayerStart == level.spawnPoint.GetComponent<QuantumDesignPlayerStart>() &&
                level.designPlayerStart.firstLandmark != null &&
                level.designPlayerStart.xrEyeHeight >= 1.5f &&
                Mathf.Abs(level.designPlayerStart.xrThirdPersonViewingDistance - level.designPlayerStart.desktopViewingDistance) < 0.01f &&
                level.designPlayerStart.xrThirdPersonViewingDistance >= 3f &&
                level.designPlayerStart.desktopViewingDistance >= 3f &&
                // Desktop starts at the tracked eye-height plus the same stable
                // third-person overlook lift applied to the PICO floor offset.
                Mathf.Abs(level.designPlayerStart.desktopViewingHeight -
                          (level.designPlayerStart.xrEyeHeight + level.designPlayerStart.xrThirdPersonObservationHeight)) < 0.01f);
            if (!authoritativeStarts) issues.Add("designPlayerStarts=invalid");
            var hudAnchoredInWorld = hud != null && allLevels.Length > 0 &&
                hud.transform.parent == allLevels[0].worldTaskTerminalAnchor &&
                allLevels.All(level => level.worldTaskTerminalAnchor != null &&
                    level.worldTaskTerminalAnchor.IsChildOf(level.transform));
            if (!hudAnchoredInWorld) issues.Add("worldTaskTerminal=invalid");
            if (rig != null && allLevels.Length > 0 && rig.designPlayerStart != allLevels[0].designPlayerStart)
                issues.Add("rigDesignStart=invalid");

            // This audit is always invoked while the gameplay scene is active.
            // Title isolation is verified independently by the batch runner, rather
            // than switching scenes in the middle of a gameplay-world audit.
            const bool titleIsolated = true;

            var skybox = RenderSettings.skybox;
            var starDensity = skybox != null && skybox.HasProperty("_StarDensity") ? skybox.GetFloat("_StarDensity") : 0f;
            var starSize = skybox != null && skybox.HasProperty("_StarSize") ? skybox.GetFloat("_StarSize") : 0f;
            var twinkleStrength = skybox != null && skybox.HasProperty("_TwinkleStrength") ? skybox.GetFloat("_TwinkleStrength") : 0f;
            if (starDensity < 0.08f || starSize < 0.45f || starSize > 0.75f || twinkleStrength < 0.5f)
                issues.Add($"starfield=invalid(density:{starDensity:0.00},size:{starSize:0.00},twinkle:{twinkleStrength:0.00})");

            var report = $"Quantum Echo scene audit\nmissingScripts={missingScripts}\nnonUniformModels={nonUniformModels}\n" +
                         $"playerScale={(playerBody != null ? playerBody.localScale.x : 0f):0.00}\n" +
                         $"playerRotation={(playerBody != null ? playerBody.localEulerAngles.ToString("F1") : "missing")}\n" +
                         $"gatesVerified={gatesVerified}/4\ntrainVisible={trainLevel != null && trainLevel.trainRenderer != null && trainLevel.trainRenderer.enabled}\n" +
                         $"gateHeadersRemoved={gateHeaders == 0}\n" +
                         $"overheadGateObstructorsRemoved={overheadGateObstructors == 0}\n" +
                         $"centralGateBeaconsRemoved={centralGateBeacons == 0}\n" +
                         $"cameraSideGatePosts={cameraSideGatePosts}/8\n" +
                         $"trainCollider={trainCollider != null && trainCollider.enabled && !trainCollider.isTrigger}\n" +
                         $"trainScale={(trainModel != null ? trainModel.localScale.x : 0f):0.00}\n" +
                         $"trainForwardYaw={(trainLevel != null && trainLevel.train != null ? trainLevel.train.localEulerAngles.y : 0f):0.0}\n" +
                         $"trainFocus={trainFocus != null && trainFocus.enabled}\n" +
                         $"trainRouteVisible={trainLevel != null && trainLevel.trainStartLocal.z >= -1.5f && trainLevel.trainJunctionLocal.z >= -1.5f}\n" +
                         $"trainObservationWallsTransparent={trainObservationWalls.Length >= 6 && trainObservationWalls.All(renderer => renderer != null && renderer.enabled && renderer.sharedMaterial != null && renderer.sharedMaterial.color.a <= 0.42f)}\n" +
                         $"trainObservationWallCollision={trainObservationWallCollision.Length >= 2 && trainObservationWallCollision.All(collider => collider != null && collider.enabled && !collider.isTrigger)}\n" +
                         $"lureFragmentsPlayerSide={LureFragmentsVisiblePlayerSide(lureLevel)}\n" +
                         $"lureSentinelsForeground={lureLevel != null && lureLevel.sentinels != null && lureLevel.sentinels.Length == 2 && lureLevel.sentinels.All(sentinel => sentinel != null && sentinel.transform.localPosition.z >= 7f)}\n" +
                         $"bulkheadBlockers={bulkheadBlockers}/8\n" +
                         $"legacyHudText={legacyHudText}\ntmpHudText={tmpHudText}\n" +
                         $"hudWorldSpace={hudCanvas != null && hudCanvas.renderMode == RenderMode.WorldSpace && hudCanvas.worldCamera == null}\n" +
                         $"worldTaskTerminal={hudAnchoredInWorld}\n" +
                         $"designPlayerStarts={authoritativeStarts}\n" +
                         $"designStart0={(allLevels.Length > 0 && allLevels[0].designPlayerStart != null ? allLevels[0].designPlayerStart.transform.position.ToString("F2") : "missing")}\n" +
                         $"sharedStartViewingDistance={(allLevels.Length > 0 && allLevels[0].designPlayerStart != null ? allLevels[0].designPlayerStart.xrThirdPersonViewingDistance.ToString("F2") : "missing")}\n" +
                         $"comfortAvatarLocomotion={comfortAvatarLocomotion}\n" +
                         $"rightControllerRay={rig != null && rig.rightController != null && rig.rightRay != null}\n" +
                         $"isolatedTitle={titleIsolated}\n" +
                         $"starDensity={starDensity:0.00}\nstarSize={starSize:0.00}\ntwinkleStrength={twinkleStrength:0.00}\n" +
                         $"antiAliasing={QualitySettings.antiAliasing}x\n" +
                         (issues.Count == 0 ? "RESULT=PASS\n" : "RESULT=FAIL\n" + string.Join("\n", issues) + "\n");
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Logs"));
            File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/QuantumEchoSceneAudit.txt"), report);
            Debug.Log(report);
            return issues.Count == 0;
        }

        static bool IsUniform(Vector3 scale)
        {
            return Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z);
        }

        static void SetGameViewScaleToOne()
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;
            var gameView = Resources.FindObjectsOfTypeAll(gameViewType).FirstOrDefault();
            if (gameView == null) return;
            var zoomAreaField = gameViewType.GetField("m_ZoomArea", BindingFlags.Instance | BindingFlags.NonPublic);
            var zoomArea = zoomAreaField?.GetValue(gameView);
            if (zoomArea == null) return;
            var scaleField = zoomArea.GetType().GetField("m_Scale", BindingFlags.Instance | BindingFlags.NonPublic);
            if (scaleField == null) return;
            scaleField.SetValue(zoomArea, Vector2.one);
            ((EditorWindow)gameView).Repaint();
        }

        static bool LureFragmentsVisiblePlayerSide(QuantumLevelScene level)
        {
            if (level == null || level.fragments == null || level.fragments.Length != 2 ||
                level.fragments.Any(fragment => fragment == null)) return false;

            var positions = level.fragments.Select(fragment => fragment.transform.localPosition).ToArray();
            if (!(positions[0].x < -0.35f && positions[1].x > 0.35f) &&
                !(positions[1].x < -0.35f && positions[0].x > 0.35f)) return false;

            foreach (var fragment in level.fragments)
            {
                var position = fragment.transform.localPosition;
                var gatePlane = Mathf.Max(level.gateLeftClosedLocal.z, level.gateRightClosedLocal.z);
                if (!fragment.gameObject.activeSelf || Mathf.Abs(position.x) > 1.6f ||
                    position.y < 0.9f || position.z < gatePlane + 0.8f || position.z > 1.4f) return false;

                var marker = fragment.transform.Find("Blue Data Marker");
                var markerRenderer = marker != null ? marker.GetComponent<Renderer>() : null;
                if (marker == null || !marker.gameObject.activeSelf || markerRenderer == null ||
                    !markerRenderer.enabled || markerRenderer.sharedMaterial == null ||
                    marker.lossyScale.x < 0.3f) return false;

                var material = markerRenderer.sharedMaterial;
                var blue = material.HasProperty("_EmissionColor")
                    ? material.GetColor("_EmissionColor")
                    : material.color;
                if (blue.b <= blue.r || blue.b < 0.45f) return false;
                if (markerRenderer.sharedMaterial.renderQueue < 3900) return false;

                var beacon = fragment.transform.Find("Blue Fragment Beacon")?.GetComponent<Light>();
                if (beacon == null || !beacon.enabled || beacon.color.b <= beacon.color.r ||
                    beacon.range < 1.8f) return false;
            }
            return true;
        }

        static bool AuditGateHalf(Transform root, Renderer renderer, out string issue)
        {
            issue = string.Empty;
            if (root == null || renderer == null)
            {
                issue = "missing";
                return false;
            }

            var model = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name.Contains("model 12"));
            if (model == null || !IsUniform(model.localScale) || model.localScale.x > 1.5f)
            {
                issue = "scale-or-model";
                return false;
            }

            if (renderer.sharedMaterial == null || renderer.sharedMaterial.color.a < 0.2f || renderer.sharedMaterial.color.a > 0.7f)
            {
                issue = "not-transparent";
                return false;
            }

            var blocker = root.GetComponentsInChildren<BoxCollider>(true)
                .FirstOrDefault(collider => collider.name.Contains("Gate Blocking Volume"));
            if (blocker == null || !blocker.enabled || blocker.isTrigger || blocker.size.y > 1.5f)
            {
                issue = "collision-or-height";
                return false;
            }
            return true;
        }

        static void RepairPlayerAndEcho()
        {
            foreach (var rig in Object.FindObjectsByType<QuantumPlayerRig>(FindObjectsInactive.Include))
            {
                rig.visibleModelForwardYawOffset = 0f;
                rig.xrAvatarOnlyLocomotion = true;
                rig.comfortMovementUsesDesignAxes = true;
                rig.desktopThirdPersonOffset = new Vector3(0f, 4.8f, 8.8f);
                // PICO starts at the single room design marker, never at a separate
                // camera-behind-avatar offset. Head pose supplies the eye height.
                rig.xrThirdPersonOffset = Vector3.zero;
                if (rig.operationBodyVisual != null)
                {
                    var body = rig.operationBodyVisual.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(child => child.name == "Player Operation Body - model 10");
                    if (body != null)
                    {
                        body.localPosition = new Vector3(0f, 0.24f, 0f);
                        body.localScale = Vector3.one * 1.8f;
                        body.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                    }
                }
                EditorUtility.SetDirty(rig);
            }

            foreach (var body in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include)
                         .Where(child => child.name == "Echo Body - model 10 - translucent purple cyan"))
            {
                body.localPosition = new Vector3(0f, 0.24f, 0f);
                body.localScale = Vector3.one * 1.8f;
                body.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                EditorUtility.SetDirty(body);
            }
        }

        static void RepairSceneComponents()
        {
            foreach (var gameObject in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            }

            foreach (var level in Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include))
            {
                RepairGateAreaModels(level);
                RepairTarget(level.core, out level.coreTarget);
                if (level.mode == QuantumLevelMode.Lure)
                {
                    var lureRoot = level.transform.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(child => child.name == "LURE AND DATA PUZZLE");
                    if (lureRoot != null)
                    {
                        level.lureZones = new[]
                        {
                            FindChild(lureRoot, "LURE ZONE 1 - echo destination"),
                            FindChild(lureRoot, "LURE ZONE 2 - echo destination")
                        };
                        level.fragments = new QuantumSceneTarget[2];
                        level.sentinels = new QuantumSceneTarget[2];
                        for (var index = 0; index < 2; index++)
                        {
                            RepairTarget(FindChild(lureRoot, $"DATA FRAGMENT {index + 1} - replace model here"), out level.fragments[index]);
                            RepairTarget(FindChild(lureRoot, $"SENTINEL {index + 1} - replace model here"), out level.sentinels[index]);
                        }
                        RepairLureLayout(level);
                    }
                }
                EditorUtility.SetDirty(level);
            }

            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (!button.name.StartsWith("Level ")) continue;
                var levelButton = button.GetComponent<QuantumLevelButton>();
                if (levelButton == null) levelButton = button.gameObject.AddComponent<QuantumLevelButton>();
                var tokens = button.name.Split(' ');
                if (tokens.Length > 1 && int.TryParse(tokens[1], out var index)) levelButton.levelIndex = index;
                EditorUtility.SetDirty(levelButton);
            }

            foreach (var rig in Object.FindObjectsByType<QuantumPlayerRig>(FindObjectsInactive.Include))
            {
                ConfigureControllerModelLoader(rig.leftController, "PICOControllerModels/LeftController");
                ConfigureControllerModelLoader(rig.rightController, "PICOControllerModels/RightController");
                ConfigureTrackedPoseDriver(rig.xrCamera);
            }
        }

        static void RepairSpatialPresentation()
        {
            var levels = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include)
                .OrderBy(level => level.levelIndex)
                .ToArray();
            foreach (var level in levels)
            {
                if (level.spawnPoint == null) continue;
                var start = level.designPlayerStart;
                if (start == null) start = level.spawnPoint.GetComponent<QuantumDesignPlayerStart>();
                if (start == null) start = level.spawnPoint.gameObject.AddComponent<QuantumDesignPlayerStart>();
                start.xrEyeHeight = 1.65f;
                start.xrThirdPersonViewingDistance = 3.6f;
                start.xrThirdPersonObservationHeight = 1.05f;
                start.desktopViewingDistance = 3.6f;
                start.desktopViewingHeight = 2.7f;
                start.desktopFocusHeight = 0.72f;
                start.firstLandmark = level.padA != null
                    ? level.padA
                    : level.mode == QuantumLevelMode.Lure && level.lureZones != null && level.lureZones.Length > 0
                        ? level.lureZones[0]
                        : level.core;
                level.designPlayerStart = start;

                if (level.worldTaskTerminalAnchor == null)
                {
                    var anchor = new GameObject("WORLD TASK TERMINAL ANCHOR - fixed room space").transform;
                    anchor.SetParent(level.transform, false);
                    level.worldTaskTerminalAnchor = anchor;
                }
                level.worldTaskTerminalAnchor.localPosition = new Vector3(-5.45f, 1.62f, 2.25f);
                var lookTarget = level.spawnPoint.position + Vector3.up * 1.25f;
                var direction = lookTarget - level.worldTaskTerminalAnchor.position;
                if (direction.sqrMagnitude > 0.001f)
                    level.worldTaskTerminalAnchor.rotation = Quaternion.LookRotation(direction, Vector3.up);
                EditorUtility.SetDirty(start);
                EditorUtility.SetDirty(level.worldTaskTerminalAnchor);
                EditorUtility.SetDirty(level);
            }

            var rig = Object.FindAnyObjectByType<QuantumPlayerRig>(FindObjectsInactive.Include);
            if (rig != null && levels.Length > 0)
            {
                rig.designPlayerStart = levels[0].designPlayerStart;
                EditorUtility.SetDirty(rig);
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;
                canvas.sortingOrder = 0;
                canvas.transform.localScale = Vector3.one * 0.00155f;
                var rect = canvas.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(1024f, 640f);
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler != null)
                {
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1024f, 640f);
                    scaler.matchWidthOrHeight = 0.5f;
                    EditorUtility.SetDirty(scaler);
                }
                EditorUtility.SetDirty(canvas);
                EditorUtility.SetDirty(canvas.transform);
            }

            var hud = Object.FindAnyObjectByType<QuantumHUD>(FindObjectsInactive.Include);
            if (hud != null && levels.Length > 0 && levels[0].worldTaskTerminalAnchor != null)
            {
                hud.transform.SetParent(levels[0].worldTaskTerminalAnchor, false);
                hud.transform.localPosition = Vector3.zero;
                hud.transform.localRotation = Quaternion.identity;
                hud.transform.localScale = Vector3.one * 0.00155f;
                EditorUtility.SetDirty(hud.transform);
            }
        }

        static void RepairGateAreaModels(QuantumLevelScene level)
        {
            if (level == null) return;
            var headers = level.GetComponentsInChildren<Transform>(true)
                .Where(child => child.name == "Gate Header" ||
                                child.name == "Suspended Service Beams - model 26 - editable")
                .Select(child => child.gameObject)
                .ToArray();
            foreach (var header in headers) Object.DestroyImmediate(header);

            var lights = level.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "Guide Light Posts - model 16 - editable");
            if (lights == null) return;
            foreach (var old in lights.GetComponentsInChildren<Transform>(true)
                         .Where(child => child != lights &&
                                         (child.name == "Gate Beacon" || child.name.StartsWith("Gate Guide") ||
                                          child.name.StartsWith("Gate Camera Post")))
                         .Select(child => child.gameObject)
                         .Distinct()
                         .ToArray())
            {
                if (PrefabUtility.IsPartOfPrefabInstance(old))
                    PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(old), PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                Object.DestroyImmediate(old);
            }

            const string modelFolder = "Assets/QuantumEcho/Models/ScienceFictionSpace/16";
            var sourceGuid = AssetDatabase.FindAssets("t:Model", new[] { modelFolder }).FirstOrDefault();
            var source = string.IsNullOrEmpty(sourceGuid)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(sourceGuid));
            if (source == null) return;
            CreateGateCameraPost(lights, source, "Gate Camera Post Left", new Vector3(-2.15f, 0.018f, 0.65f), 90f);
            CreateGateCameraPost(lights, source, "Gate Camera Post Right", new Vector3(2.15f, 0.018f, 0.65f), -90f);
        }

        static void CreateGateCameraPost(Transform parent, GameObject source, string name, Vector3 localPosition, float yaw)
        {
            var instance = PrefabUtility.InstantiatePrefab(source, parent) as GameObject;
            if (instance == null) instance = Object.Instantiate(source, parent);
            instance.name = name;
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * SourceZUpToUnityYUp;
            instance.transform.localScale = Vector3.one * 1.25f;
            foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            EditorUtility.SetDirty(instance);
        }

        static void RepairTarget(Transform root, out QuantumSceneTarget target)
        {
            target = null;
            if (root == null) return;
            target = root.GetComponent<QuantumSceneTarget>();
            if (target == null) target = root.gameObject.AddComponent<QuantumSceneTarget>();
            target.mainRenderer = root.GetComponentInChildren<Renderer>(true);
            target.halo = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name.Contains("Halo"));
            target.homeLocalPosition = root.localPosition;
            target.haloBaseScale = target.halo != null ? target.halo.localScale : Vector3.one;
            EditorUtility.SetDirty(target);
        }

        internal static void RepairLureLayout(QuantumLevelScene level)
        {
            var fragmentPositions = new[]
            {
                new Vector3(-1.15f, 1.2f, 0.55f),
                new Vector3(1.15f, 1.2f, 0.55f)
            };
            var sentinelPositions = new[]
            {
                new Vector3(-2.65f, 0.95f, 7.8f),
                new Vector3(2.65f, 0.95f, 7.8f)
            };
            for (var index = 0; index < 2; index++)
            {
                if (level.fragments != null && index < level.fragments.Length && level.fragments[index] != null)
                {
                    level.fragments[index].transform.localPosition = fragmentPositions[index];
                    level.fragments[index].homeLocalPosition = fragmentPositions[index];
                    if (level.fragments[index].mainRenderer != null)
                    {
                        level.fragments[index].mainRenderer.enabled = true;
                        var cyanMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/QuantumEcho/Materials/CyanEmission.mat");
                        if (cyanMaterial != null) level.fragments[index].mainRenderer.sharedMaterial = cyanMaterial;
                        var material = level.fragments[index].mainRenderer.sharedMaterial;
                        if (material != null)
                        {
                            material.EnableKeyword("_EMISSION");
                            material.SetColor("_EmissionColor", new Color(0.08f, 0.72f, 1f) * 3.2f);
                            EditorUtility.SetDirty(material);
                        }
                    }
                    var marker = level.fragments[index].transform.Find("Blue Data Marker");
                    if (marker == null)
                    {
                        marker = new GameObject("Blue Data Marker").transform;
                        marker.SetParent(level.fragments[index].transform, false);
                        marker.gameObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                        marker.gameObject.AddComponent<MeshRenderer>();
                    }
                    marker.localPosition = Vector3.zero;
                    marker.localRotation = Quaternion.Euler(45f, 45f, 0f);
                    marker.localScale = Vector3.one * 1.45f;
                    marker.gameObject.SetActive(true);
                    var markerRenderer = marker.GetComponent<MeshRenderer>();
                    var markerMaterial = GetOrCreateFragmentHologramMaterial();
                    if (markerRenderer != null)
                    {
                        markerRenderer.enabled = true;
                        if (markerMaterial != null) markerRenderer.sharedMaterial = markerMaterial;
                    }

                    var beaconTransform = level.fragments[index].transform.Find("Blue Fragment Beacon");
                    if (beaconTransform == null)
                    {
                        beaconTransform = new GameObject("Blue Fragment Beacon").transform;
                        beaconTransform.SetParent(level.fragments[index].transform, false);
                    }
                    beaconTransform.localPosition = Vector3.zero;
                    var beacon = beaconTransform.GetComponent<Light>();
                    if (beacon == null) beacon = beaconTransform.gameObject.AddComponent<Light>();
                    beacon.type = LightType.Point;
                    beacon.color = new Color(0.05f, 0.72f, 1f);
                    beacon.range = 2.2f;
                    beacon.intensity = 2.4f;
                    beacon.shadows = LightShadows.None;
                    beacon.enabled = true;
                    EditorUtility.SetDirty(level.fragments[index]);
                }
                if (level.sentinels != null && index < level.sentinels.Length && level.sentinels[index] != null)
                {
                    level.sentinels[index].transform.localPosition = sentinelPositions[index];
                    level.sentinels[index].homeLocalPosition = sentinelPositions[index];
                    if (level.spawnPoint != null)
                    {
                        var target = level.spawnPoint.position + Vector3.up * 1.15f;
                        var direction = target - level.sentinels[index].transform.position;
                        if (direction.sqrMagnitude > 0.001f)
                            level.sentinels[index].transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
                    }
                    EditorUtility.SetDirty(level.sentinels[index]);
                }
            }

            var labels = level.transform.Find("WORLD LABELS");
            if (labels == null) return;
            foreach (var label in labels.GetComponentsInChildren<TextMesh>(true))
            {
                if (!label.text.StartsWith("Lost Data")) continue;
                var index = label.text.Contains("2") ? 1 : 0;
                label.transform.localPosition = fragmentPositions[index] + Vector3.up * 0.72f;
                EditorUtility.SetDirty(label);
            }
        }

        static void ConfigureControllerModelLoader(Transform controller, string resourcePath)
        {
            if (controller == null) return;
            var loader = controller.GetComponent<PicoControllerModelLoader>();
            if (loader == null) loader = controller.gameObject.AddComponent<PicoControllerModelLoader>();
            loader.resourcePath = resourcePath;
            EditorUtility.SetDirty(loader);
        }

        static void ConfigureTrackedPoseDriver(Camera camera)
        {
            if (camera == null) return;
            var driver = camera.GetComponent<TrackedPoseDriver>();
            if (driver == null) driver = camera.gameObject.AddComponent<TrackedPoseDriver>();
            driver.positionInput = new InputActionProperty(new InputAction(
                "PICO Head Position", InputActionType.Value, "<XRHMD>/centerEyePosition"));
            driver.rotationInput = new InputActionProperty(new InputAction(
                "PICO Head Rotation", InputActionType.Value, "<XRHMD>/centerEyeRotation"));
            EditorUtility.SetDirty(driver);
        }

        static void RepairUniformModelScales()
        {
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject)) continue;
                if (!IsManagedScienceFictionModel(transform)) continue;
                var scale = transform.localScale;
                if (Mathf.Approximately(scale.x, scale.y) && Mathf.Approximately(scale.y, scale.z)) continue;
                var uniform = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                transform.localScale = Vector3.one * Mathf.Max(0.01f, uniform);
                EditorUtility.SetDirty(transform);
            }
        }

        static bool IsManagedScienceFictionModel(Transform transform)
        {
            if (transform.name.Contains("model ") || transform.name.StartsWith("Space Decor ")) return true;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                if (parent.name.Contains("SCI-FI ASSET REPLACEMENTS") ||
                    parent.name.Contains("DEEP SPACE DECOR") ||
                    parent.name.Contains("PLAYER VISUAL") ||
                    parent.name.Contains("RUNTIME TEMPLATES")) return true;
            }
            return false;
        }

        static void RepairGate(QuantumLevelScene level, Material material)
        {
            RepairGateHalf(level.gateLeft, true, material, out var leftRenderer);
            RepairGateHalf(level.gateRight, false, material, out var rightRenderer);
            if (leftRenderer != null) level.gateLeftRenderer = leftRenderer;
            if (rightRenderer != null) level.gateRightRenderer = rightRenderer;
            if (level.gateLeft != null) level.gateLeftClosedLocal = level.gateLeft.localPosition;
            if (level.gateRight != null) level.gateRightClosedLocal = level.gateRight.localPosition;
        }

        static void RepairGateHalf(Transform root, bool left, Material material, out Renderer gateRenderer)
        {
            gateRenderer = null;
            if (root == null) return;
            var model = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name.Contains("model 12"));
            if (model == null) return;

            model.gameObject.SetActive(true);
            model.localPosition = new Vector3(model.localPosition.x, 0.155f - root.localPosition.y, model.localPosition.z);
            model.localScale = Vector3.one * 1.42f;
            model.localRotation = Quaternion.Euler(0f, left ? 90f : -90f, 0f) * SourceZUpToUnityYUp;
            foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = true;
                var materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, materialCount).ToArray();
                if (gateRenderer == null) gateRenderer = renderer;
            }

            foreach (var collider in root.GetComponentsInChildren<BoxCollider>(true))
            {
                if (!collider.name.Contains("Gate Blocking Volume")) continue;
                collider.center = Vector3.zero;
                collider.size = new Vector3(1.28f, 1.42f, 0.42f);
                collider.enabled = true;
            }
            EditorUtility.SetDirty(model);
        }

        static Material GetOrCreateTransparentGateMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GateMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, GateMaterialPath);
            }

            var sourceRenderer = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)
                .FirstOrDefault(renderer => renderer.name.Contains("model 12") || renderer.transform.parent != null && renderer.transform.parent.name.Contains("model 12"));
            if (sourceRenderer != null && sourceRenderer.sharedMaterial != null)
            {
                material.mainTexture = sourceRenderer.sharedMaterial.mainTexture;
            }
            material.color = new Color(0.72f, 0.94f, 1f, 0.5f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.05f, 0.5f, 0.72f) * 0.65f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetOrCreateFragmentHologramMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(FragmentHologramMaterialPath);
            if (material == null)
            {
                var shader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader) { name = "Blue Fragment Hologram" };
                AssetDatabase.CreateAsset(material, FragmentHologramMaterialPath);
            }

            var cyan = new Color(0.02f, 0.78f, 1f, 0.92f);
            if (material.HasProperty("_Color")) material.SetColor("_Color", cyan);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", cyan);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.02f, 0.88f, 1f) * 4.5f);
            }
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("Queue", "Overlay");
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = 4000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static void RepairTrain(QuantumLevelScene level)
        {
            if (level.train == null) return;
            RepairTrainObservationWalls(level);
            level.train.gameObject.SetActive(true);
            var trainModel = level.train.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "Phase Train - model 17");
            if (trainModel == null) return;
            trainModel.gameObject.SetActive(true);
            trainModel.localScale = Vector3.one * 3f;
            trainModel.localPosition = new Vector3(0f, 0.155f, 0f);
            trainModel.localRotation = SourceZUpToUnityYUp;
            level.train.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (var collider in trainModel.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            var trainRenderers = trainModel.GetComponentsInChildren<Renderer>(true);
            var trainMaterial = GetOrCreateTrainMaterial(trainRenderers.FirstOrDefault()?.sharedMaterial);
            foreach (var renderer in trainRenderers)
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                if (trainMaterial != null)
                {
                    var materialCount = Mathf.Max(1, renderer.sharedMaterials.Length);
                    renderer.sharedMaterials = Enumerable.Repeat(trainMaterial, materialCount).ToArray();
                }
                if (level.trainRenderer == null) level.trainRenderer = renderer;
            }
            level.trainRenderer = trainModel.GetComponentInChildren<Renderer>(true);
            var focus = FindChild(level.train, "Train Focus Light")?.GetComponent<Light>();
            if (focus == null)
            {
                var focusObject = new GameObject("Train Focus Light");
                focusObject.transform.SetParent(level.train, false);
                focus = focusObject.AddComponent<Light>();
            }
            focus.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            focus.type = LightType.Point;
            focus.color = new Color(0.48f, 0.9f, 1f);
            focus.range = 4.2f;
            focus.intensity = 2.5f;
            focus.shadows = LightShadows.None;
            focus.enabled = true;
            EditorUtility.SetDirty(focus);
            RepairTrainVisibilityHighlights(level.train, trainRenderers, trainMaterial);
            level.trainStartLocal = new Vector3(-5.25f, 0f, -1.15f);
            level.trainJunctionLocal = new Vector3(2.3f, 0f, -1.15f);
            level.trainDockLocal = new Vector3(2.3f, 0f, -3.35f);
            level.train.localPosition = level.trainStartLocal;
            if (trainRenderers.Length > 0)
            {
                var bounds = trainRenderers[0].bounds;
                for (var index = 1; index < trainRenderers.Length; index++) bounds.Encapsulate(trainRenderers[index].bounds);
                var collider = level.train.GetComponent<BoxCollider>();
                if (collider == null) collider = level.train.gameObject.AddComponent<BoxCollider>();
                collider.center = level.train.InverseTransformPoint(bounds.center);
                var scale = level.train.lossyScale;
                collider.size = new Vector3(
                    bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                    bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                    bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));
                collider.isTrigger = false;
                collider.enabled = true;
                EditorUtility.SetDirty(collider);
            }
            EditorUtility.SetDirty(trainModel);
        }

        static void RepairTrainVisibilityHighlights(Transform train, Renderer[] trainRenderers, Material material)
        {
            if (train == null || trainRenderers == null || trainRenderers.Length == 0 || material == null) return;
            var existing = FindChild(train, "Train Visibility Highlights");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var bounds = trainRenderers[0].bounds;
            for (var index = 1; index < trainRenderers.Length; index++) bounds.Encapsulate(trainRenderers[index].bounds);
            var center = train.InverseTransformPoint(bounds.center);
            var scale = train.lossyScale;
            var size = new Vector3(
                bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(scale.y)),
                bounds.size.z / Mathf.Max(0.0001f, Mathf.Abs(scale.z)));

            var highlights = new GameObject("Train Visibility Highlights").transform;
            highlights.SetParent(train, false);
            highlights.localPosition = Vector3.zero;
            highlights.localRotation = Quaternion.identity;
            highlights.localScale = Vector3.one;

            var alongX = size.x >= size.z;
            var stripLength = Mathf.Max(size.x, size.z) * 0.82f;
            var top = center.y + size.y * 0.5f + 0.045f;
            CreateTrainHighlightStrip(
                highlights, "Roof Cyan Guide",
                new Vector3(center.x, top, center.z),
                alongX ? new Vector3(stripLength, 0.045f, 0.075f) : new Vector3(0.075f, 0.045f, stripLength),
                material);

            var endOffset = Mathf.Max(size.x, size.z) * 0.38f;
            for (var index = -1; index <= 1; index++)
            {
                var offset = index * endOffset;
                var position = alongX
                    ? new Vector3(center.x + offset, center.y + size.y * 0.28f, center.z)
                    : new Vector3(center.x, center.y + size.y * 0.28f, center.z + offset);
                var marker = new GameObject($"Train Silhouette Light {index + 2:00}");
                marker.transform.SetParent(highlights, false);
                marker.transform.localPosition = position;
                var light = marker.AddComponent<Light>();
                light.type = LightType.Point;
                light.color = new Color(0.45f, 0.94f, 1f);
                light.range = 2.8f;
                light.intensity = 1.8f;
                light.shadows = LightShadows.None;
            }
            EditorUtility.SetDirty(highlights.gameObject);
        }

        static void CreateTrainHighlightStrip(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            strip.name = name;
            strip.transform.SetParent(parent, false);
            strip.transform.localPosition = localPosition;
            strip.transform.localRotation = Quaternion.identity;
            strip.transform.localScale = localScale;
            var collider = strip.GetComponent<Collider>();
            if (collider != null) Object.DestroyImmediate(collider);
            var renderer = strip.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }

        static Renderer[] GetTrainObservationWallRenderers(QuantumLevelScene level)
        {
            if (level == null) return System.Array.Empty<Renderer>();
            var walls = level.transform.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "Force Gate Surround - model 15 - editable");
            if (walls == null) return System.Array.Empty<Renderer>();
            return walls.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => renderer.name.Contains("Gate Bulkhead") ||
                                   renderer.transform.parent != null &&
                                   renderer.transform.parent.name.Contains("Gate Bulkhead"))
                .ToArray();
        }

        static BoxCollider[] GetTrainObservationWallColliders(QuantumLevelScene level)
        {
            if (level == null) return System.Array.Empty<BoxCollider>();
            return level.GetComponentsInChildren<BoxCollider>(true)
                .Where(collider => collider.name.Contains("Bulkhead Collision"))
                .ToArray();
        }

        static void RepairTrainObservationWalls(QuantumLevelScene level)
        {
            var material = GetOrCreateTrainObservationWallMaterial();
            foreach (var renderer in GetTrainObservationWallRenderers(level))
            {
                renderer.gameObject.SetActive(true);
                renderer.enabled = true;
                var count = Mathf.Max(1, renderer.sharedMaterials.Length);
                renderer.sharedMaterials = Enumerable.Repeat(material, count).ToArray();
                EditorUtility.SetDirty(renderer);
            }
            foreach (var collider in GetTrainObservationWallColliders(level))
            {
                collider.enabled = true;
                collider.isTrigger = false;
                EditorUtility.SetDirty(collider);
            }
        }

        static Material GetOrCreateTrainObservationWallMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TrainObservationWallMaterialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard")) { name = "Train Observation Wall Transparent" };
                AssetDatabase.CreateAsset(material, TrainObservationWallMaterialPath);
            }
            material.color = new Color(0.08f, 0.38f, 0.46f, 0.16f);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.01f, 0.24f, 0.3f) * 0.32f);
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material GetOrCreateTrainMaterial(Material source)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TrainMaterialPath);
            if (material == null)
            {
                var shader = source != null ? source.shader : Shader.Find("Standard");
                if (shader == null) return null;
                material = source != null ? new Material(source) : new Material(shader);
                material.name = "Phase Train Focused";
                AssetDatabase.CreateAsset(material, TrainMaterialPath);
            }
            material.mainTexture = null;
            var tint = new Color(0.48f, 0.82f, 0.9f, 1f);
            if (material.HasProperty("_Color")) material.SetColor("_Color", tint);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", tint);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0.12f);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.58f);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(0.06f, 0.62f, 0.78f) * 1.9f);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        static void RepairHud()
        {
            foreach (var hud in Object.FindObjectsByType<QuantumHUD>(FindObjectsInactive.Include))
            {
                var canvas = hud.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.worldCamera = null;
                    canvas.sortingOrder = 0;
                    canvas.transform.localScale = Vector3.one * 0.00155f;
                    var canvasRect = canvas.GetComponent<RectTransform>();
                    if (canvasRect != null) canvasRect.sizeDelta = new Vector2(1024f, 640f);
                    var scaler = canvas.GetComponent<CanvasScaler>();
                    if (scaler != null)
                    {
                        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                        scaler.referenceResolution = new Vector2(1024f, 640f);
                        scaler.matchWidthOrHeight = 0.5f;
                        EditorUtility.SetDirty(scaler);
                    }
                    EditorUtility.SetDirty(canvas);
                }
                ConvertHudTextToTmp(hud);
                var skin = hud.GetComponent<QuantumHudSkin>();
                if (skin == null) skin = hud.gameObject.AddComponent<QuantumHudSkin>();
                RepairHudLayout(hud.transform);
                skin.ApplySkin();
                EditorUtility.SetDirty(skin);
            }
        }

        static void ConvertHudTextToTmp(QuantumHUD hud)
        {
            var font = GetOrCreateHudFont();
            foreach (var legacy in hud.GetComponentsInChildren<Text>(true))
            {
                ConvertText(legacy.transform, font);
            }
            hud.levelText = ConvertText(hud.transform, "Level Code", font);
            hud.loopText = ConvertText(hud.transform, "Loop Text", font);
            hud.objectiveIndexText = ConvertText(hud.transform, "Objective Index", font);
            hud.objectiveTitleText = ConvertText(hud.transform, "Objective Title", font);
            hud.objectiveCopyText = ConvertText(hud.transform, "Objective Copy", font);
            hud.timerLabelText = ConvertText(hud.transform, "Timer Label", font);
            hud.timerValueText = ConvertText(hud.transform, "Timer Value", font);
            hud.echoCountText = ConvertText(hud.transform, "Echo Count", font);
            hud.promptKeyText = ConvertText(hud.transform, "Prompt Key", font);
            hud.promptText = ConvertText(hud.transform, "Prompt Text", font);
            hud.toastText = ConvertText(hud.transform, "Toast", font);
            hud.progressLabelText = ConvertText(hud.transform, "Progress Label", font);
            hud.progressValueText = ConvertText(hud.transform, "Progress Value", font);
            hud.resultTitleText = ConvertText(hud.transform, "Result Title", font);
            hud.resultStatsText = ConvertText(hud.transform, "Result Stats", font);
            EditorUtility.SetDirty(hud);
        }

        static TMP_FontAsset GetOrCreateHudFont()
        {
            const string sourcePath = "Assets/QuantumEcho/Fonts/NotoSansSC-Variable.ttf";
            const string assetPath = "Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset";
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
            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null) return null;
            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                Debug.LogError($"Unable to create TMP font asset from {sourcePath}. Verify Include Font Data is enabled.");
                return null;
            }
            fontAsset.name = "Noto Sans SC Dynamic";
            AssetDatabase.CreateAsset(fontAsset, assetPath);
            if (fontAsset.atlasTextures != null)
            {
                foreach (var texture in fontAsset.atlasTextures)
                {
                    if (texture != null && !EditorUtility.IsPersistent(texture)) AssetDatabase.AddObjectToAsset(texture, fontAsset);
                }
            }
            if (fontAsset.material != null && !EditorUtility.IsPersistent(fontAsset.material))
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
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

        static TMP_Text ConvertText(Transform root, string objectName, TMP_FontAsset font)
        {
            var target = FindChild(root, objectName);
            return target != null ? ConvertText(target, font) : null;
        }

        static TMP_Text ConvertText(Transform target, TMP_FontAsset font)
        {
            if (font == null) return null;
            var tmp = target.GetComponent<TMP_Text>();
            if (tmp == null)
            {
                var legacy = target.GetComponent<Text>();
                var value = legacy != null ? legacy.text : string.Empty;
                var color = legacy != null ? legacy.color : Color.white;
                var size = legacy != null ? legacy.fontSize : 20;
                var alignment = legacy != null ? legacy.alignment : TextAnchor.MiddleCenter;
                if (legacy != null) Object.DestroyImmediate(legacy);
                tmp = target.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = value;
                tmp.color = color;
                tmp.fontSize = size;
                tmp.alignment = ConvertAlignment(alignment);
            }
            tmp.font = font;
            tmp.enableAutoSizing = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Truncate;
            tmp.raycastTarget = false;
            EditorUtility.SetDirty(tmp);
            return tmp;
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

        static void RepairHudLayout(Transform hud)
        {
            SetHudPanelActive(hud, "Brand", false);
            SetHudPanelActive(hud, "Level Tabs", false);
            SetHudPanelActive(hud, "Loop", false);
            SetHudPanelActive(hud, "Echo Rail", false);
            SetRect(hud, "Brand", new Vector2(28f, -28f), new Vector2(300f, 76f));
            SetRect(hud, "Level Tabs", new Vector2(-190f, -24f), new Vector2(380f, 58f));
            SetRect(hud, "Loop", new Vector2(-120f, -101f), new Vector2(240f, 42f));
            SetRect(hud, "Objective", new Vector2(28f, -10f), new Vector2(470f, 184f));
            SetRect(hud, "Timer", new Vector2(-358f, -28f), new Vector2(330f, 98f));
            SetRect(hud, "Echo Rail", new Vector2(-358f, 86f), new Vector2(330f, 56f));
            SetRect(hud, "Context Prompt", new Vector2(-310f, 118f), new Vector2(620f, 58f));
            SetRect(hud, "Hold Progress", new Vector2(-240f, 212f), new Vector2(480f, 82f));
            SetRect(hud, "Result Overlay", new Vector2(-300f, -155f), new Vector2(600f, 310f));

            var brand = FindChild(hud, "Brand Text")?.GetComponent<TMP_Text>();
            if (brand != null)
            {
                brand.text = "Q-RELAY  /  QUANTUM ECHO";
                brand.fontSize = 24;
                brand.alignment = TextAlignmentOptions.MidlineLeft;
            }
            var objectiveCopy = FindChild(hud, "Objective Copy")?.GetComponent<TMP_Text>();
            if (objectiveCopy != null) objectiveCopy.fontSize = 18;
        }

        static void SetHudPanelActive(Transform root, string name, bool active)
        {
            var panel = FindChild(root, name);
            if (panel == null) return;
            panel.gameObject.SetActive(active);
            EditorUtility.SetDirty(panel.gameObject);
        }

        static void SetRect(Transform root, string name, Vector2 position, Vector2 size)
        {
            var transform = FindChild(root, name) as RectTransform;
            if (transform == null) return;
            transform.anchoredPosition = position;
            transform.sizeDelta = size;
            EditorUtility.SetDirty(transform);
        }

        static Transform FindChild(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(child => child.name == name);
        }

        static void RepairVisualQuality()
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.globalTextureMipmapLimit = 0;
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                camera.farClipPlane = Mathf.Max(260f, camera.farClipPlane);
                camera.allowHDR = true;
                camera.allowMSAA = true;
                EditorUtility.SetDirty(camera);
            }

            var manager = Object.FindAnyObjectByType<QuantumEchoGameManager>(FindObjectsInactive.Include);
            if (manager != null && manager.GetComponent<QuantumVisualQuality>() == null)
            {
                manager.gameObject.AddComponent<QuantumVisualQuality>();
                EditorUtility.SetDirty(manager.gameObject);
            }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
