using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuantumEcho.Editor
{
    public static class QuantumEchoCollisionQA
    {
        public static void RunBatch()
        {
            EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath);
            var report = new StringBuilder();
            var failures = 0;
            Action<bool, string> check = (ok, name) => { report.AppendLine((ok ? "PASS " : "FAIL ") + name); if (!ok) failures++; };
            var levels = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include).OrderBy(l => l.levelIndex).ToArray();
            var rig = Object.FindAnyObjectByType<QuantumPlayerRig>(FindObjectsInactive.Include);
            rig.gameObject.SetActive(false);
            var probe = new GameObject("Temporary collision QA avatar");
            var controller = probe.AddComponent<CharacterController>();
            QuantumPlayerRig.ConfigureBodyCollider(controller);
            Action<Vector3> place = point => { controller.enabled = false; probe.transform.position = point; controller.enabled = true; Physics.SyncTransforms(); };
            Action<Vector3, int> move = (step, frames) => { for (var n = 0; n < frames; n++) { controller.Move(step); Physics.SyncTransforms(); } };
            foreach (var level in levels)
            {
                foreach (var other in levels) other.gameObject.SetActive(other == level);
                Physics.SyncTransforms();
                var origin = level.transform.position;
                var meshes = level.GetComponentsInChildren<MeshFilter>(true).Where(m => AssetDatabase.GetAssetPath(m.sharedMesh).Contains("/Models/ScienceFictionSpace/") && m.GetComponent<Renderer>() != null && m.GetComponent<Renderer>().enabled && !QuantumEchoCollisionInstaller.IsNonSolidInteractionModel(m.transform)).ToArray();
                check(meshes.All(m => m.GetComponent<BoxCollider>() != null && m.GetComponent<BoxCollider>().enabled && !m.GetComponent<BoxCollider>().isTrigger), level.levelCode + " all " + meshes.Length + " solid meshes covered");
                if (level.mode == QuantumLevelMode.Lure)
                {
                    var lureModels = level.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Echo Lure Beacon")).ToArray();
                    check(lureModels.Length == 2 && lureModels.All(t => t.GetComponentsInChildren<Collider>(true).Length == 0), level.levelCode + " lure beacon models remain non-solid");
                    check(level.sentinels != null && level.sentinels.Length == 2 && level.sentinels.All(s => s != null && s.GetComponentsInChildren<Collider>(true).Length == 0), level.levelCode + " hunter models remain non-solid");
                }
                place(origin + new Vector3(0f, 1f, 4f));
                move(Vector3.down * 0.05f, 35);
                check(probe.transform.position.y > origin.y + 0.20f, level.levelCode + " visible deck supports avatar, y=" + (probe.transform.position.y-origin.y).ToString("F3"));
                if (level.padA != null)
                {
                    place(level.padA.position + new Vector3(0f, 0.8f, 2f));
                    move(Vector3.down * 0.05f, 25);
                    move(new Vector3(0f, -0.02f, -0.05f), 40);
                    check(Vector3.Distance(Vector3.ProjectOnPlane(probe.transform.position-level.padA.position, Vector3.up), Vector3.zero) < 0.4f, level.levelCode + " can step onto stabilizer centre");
                }
                var gate = (level.gateLeft.position + level.gateRight.position) * 0.5f;
                place(new Vector3(gate.x, origin.y + 0.32f, gate.z + 2f));
                move(new Vector3(0f, -0.015f, -0.10f), 45);
                check(probe.transform.position.z > gate.z + 0.80f, level.levelCode + " closed gate blocks swept movement z=" + (probe.transform.position.z-gate.z).ToString("F3"));
                // One oversized movement verifies tunnelling protection too.
                place(new Vector3(gate.x, origin.y + 0.32f, gate.z + 2f));
                controller.Move(Vector3.back * 5f); Physics.SyncTransforms();
                check(probe.transform.position.z > gate.z + 0.80f, level.levelCode + " closed gate blocks 5m single-frame move");
                level.gateLeft.localPosition += Vector3.left * 1.56f;
                level.gateRight.localPosition += Vector3.right * 1.56f;
                Physics.SyncTransforms();
                place(new Vector3(gate.x, origin.y + 0.32f, gate.z + 2f));
                move(new Vector3(0f, -0.015f, -0.05f), 75);
                check(probe.transform.position.z < gate.z - 1.1f, level.levelCode + " open gate permits full-size avatar z=" + (probe.transform.position.z-gate.z).ToString("F3"));
                var post = meshes.FirstOrDefault(m => m.name == "Guide Light Right");
                if (post != null)
                {
                    var bounds = post.GetComponent<Renderer>().bounds;
                    place(new Vector3(bounds.center.x - 2f, origin.y + 0.32f, bounds.center.z));
                    var collided = false;
                    var penetrated = false;
                    var shape = probe.AddComponent<CapsuleCollider>();
                    shape.isTrigger = true; shape.radius = controller.radius; shape.height = controller.height; shape.center = controller.center;
                    var postCollider = post.GetComponent<BoxCollider>();
                    for (var i=0; i<60; i++)
                    {
                        collided |= (controller.Move(new Vector3(0.05f, -0.015f, 0f)) & CollisionFlags.Sides) != 0;
                        Physics.SyncTransforms();
                        penetrated |= Physics.ComputePenetration(shape, probe.transform.position, probe.transform.rotation,
                            postCollider, postCollider.transform.position, postCollider.transform.rotation, out _, out var depth) && depth > 0.025f;
                    }
                    Object.DestroyImmediate(shape);
                    check(collided && !penetrated, level.levelCode + " guide post contact without shell penetration, end=" + probe.transform.position);
                }
            }
            Object.DestroyImmediate(probe);
            // Animate a solid into a stationary player and exercise the runtime
            // overlap resolver, using its real full-size controller.
            foreach(var level in levels) level.gameObject.SetActive(false);
            rig.gameObject.SetActive(true);
            var body = rig.GetComponent<CharacterController>();
            typeof(QuantumPlayerRig).GetField("_characterController", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(rig, body);
            body.enabled = false; rig.transform.position = new Vector3(0f, 50f, 0f); body.enabled = true;
            var moving = new GameObject("Temporary moving wall");
            var movingBox = moving.AddComponent<BoxCollider>(); movingBox.size = new Vector3(1f, 3f, 3f);
            moving.transform.position = rig.transform.position + new Vector3(1.1f, 1f, 0f);
            Physics.SyncTransforms();
            var runtimeLate = typeof(QuantumPlayerRig).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            runtimeLate.Invoke(rig, null);
            check(rig.transform.position.x < -0.25f, "stationary avatar is displaced out of moving solid, x=" + rig.transform.position.x);
            var hunter = new GameObject("Temporary swept hunter");
            var hunterBox = hunter.AddComponent<BoxCollider>(); hunterBox.size = new Vector3(1.5f, 0.7f, 1.5f);
            hunter.transform.position = new Vector3(4.5f, 51f, 0f); Physics.SyncTransforms();
            var manager = Object.FindAnyObjectByType<QuantumEchoGameManager>(FindObjectsInactive.Include);
            typeof(QuantumEchoGameManager).GetMethod("MoveSentinelWithCollision", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(manager, new object[] { hunter.transform, new Vector3(0f, 51f, 0f) });
            check(hunter.transform.position.x > 2.3f, "hunter swept movement cannot pass through a solid wall");
            Object.DestroyImmediate(hunter);
            Object.DestroyImmediate(moving);
            TestHud(check);
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/QuantumEcho-CollisionAndHudQA.txt", report.ToString());
            Debug.Log(report.ToString());
            if (failures != 0) throw new Exception("Collision/HUD QA failed: " + failures);
        }

        static void TestHud(Action<bool,string> check)
        {
            var cameraObject = new GameObject("Temporary HUD QA Camera");
            var camera = cameraObject.AddComponent<Camera>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/QuantumEcho/Fonts/NotoSansSC-Dynamic.asset");
            var hud = QuantumComfortObjectiveHUD.Create(camera, font);
            var rect = hud.GetComponent<RectTransform>();
            foreach(var aspect in new[] { 16f/9f, 4f/3f, 1f })
            foreach(var fov in new[] { 44f, 60f, 90f })
            {
                camera.aspect = aspect; camera.fieldOfView = fov;
                hud.SetHelpExpanded(false);
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                var top = camera.WorldToViewportPoint(corners[1]);
                check(Mathf.Abs(top.y-0.93f) < 0.002f && Mathf.Abs(top.x-0.06f) < 0.002f, "HUD anchors to top-left safe margin aspect="+aspect+" FOV="+fov);
                hud.SetHelpExpanded(true); rect.GetWorldCorners(corners);
                var expandedTopLeft = camera.WorldToViewportPoint(corners[1]);
                check(camera.WorldToViewportPoint(corners[0]).y > 0.05f && Mathf.Abs(expandedTopLeft.y-top.y)<0.002f && Mathf.Abs(expandedTopLeft.x-top.x)<0.002f, "HUD expanded stays on-screen with stable top-left anchor");
            }
            hud.SetContent("QER-03", "03", "Sync Stabilizer A", "Step onto the blue disc to keep the door open. Then seal it as an Echo.", "[Grip / A / X]", "Seal this action and start the next loop");
            hud.ShowFeedback("Switch beat hit — Phase Train heading to core dock", 2f);
            foreach(var text in hud.GetComponentsInChildren<TMP_Text>(true))
            {
                text.ForceMeshUpdate(true);
                check(text.preferredHeight <= text.rectTransform.rect.height + 1f && text.preferredWidth <= text.rectTransform.rect.width + 1f, "HUD text fits: " + text.name + " height="+text.preferredHeight);
            }
            var objectives = typeof(QuantumEchoGameManager).GetMethod("GetObjective", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            for(var level=0; level<4; level++) for(var step=0; step<3; step++)
            {
                var content = ((string title, string copy))objectives.Invoke(null, new object[] { level, step });
                hud.SetContent("QER-0"+level, "0"+(step+1), content.title, content.copy, "[Grip / A / X]", "Seal this action and start the next loop");
                foreach(var text in hud.GetComponentsInChildren<TMP_Text>(true))
                {
                    text.ForceMeshUpdate(true);
                    check(text.preferredHeight <= text.rectTransform.rect.height+1f, "QER-"+level+" step "+step+" text fits "+text.name);
                }
            }
            Object.DestroyImmediate(cameraObject);
        }
    }
}
