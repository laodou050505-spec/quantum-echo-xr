using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace QuantumEcho.Editor
{
    public static class QuantumEchoCollisionInstaller
    {
        // Primitive hulls are fitted in each source mesh's local coordinates.
        // This stays cheap on PICO and follows animated parents without stale
        // world-space boxes. Ghost replays and optical markers are not solids.
        public static int ApplyToOpenScene()
        {
            var count = 0;
            foreach (var mesh in Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include))
            {
                var asset = AssetDatabase.GetAssetPath(mesh.sharedMesh);
                if (!asset.Contains("/Models/ScienceFictionSpace/")) continue;
                if (IsNonSolidInteractionModel(mesh.transform))
                {
                    RemoveColliders(mesh.transform);
                    continue;
                }
                if (mesh.GetComponentInParent<QuantumPlayerRig>() != null ||
                    PathOf(mesh.transform).Contains("RUNTIME TEMPLATES")) continue;
                var renderer = mesh.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled) continue;
                var collider = mesh.GetComponent<BoxCollider>();
                if (collider == null) collider = mesh.gameObject.AddComponent<BoxCollider>();
                collider.center = mesh.sharedMesh.bounds.center;
                var size = mesh.sharedMesh.bounds.size;
                collider.size = new Vector3(Mathf.Max(size.x, 0.02f), Mathf.Max(size.y, 0.02f), Mathf.Max(size.z, 0.02f));
                collider.enabled = true;
                collider.isTrigger = false;
                EditorUtility.SetDirty(collider);
                count++;
            }
            foreach (var rig in Object.FindObjectsByType<QuantumPlayerRig>(FindObjectsInactive.Include))
            {
                QuantumPlayerRig.ConfigureBodyCollider(rig.GetComponent<CharacterController>());
                EditorUtility.SetDirty(rig.GetComponent<CharacterController>());
            }
            // Solid phase fields must match the visible width and height, not
            // the older small blockers that could be walked through at the edge.
            foreach (var level in Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include))
            {
                foreach (var half in new[] { level.gateLeft, level.gateRight })
                {
                    if (half == null) continue;
                    var field = half.GetComponentsInChildren<MeshFilter>(true)
                        .FirstOrDefault(m => m.name.StartsWith("Transparent Phase Field"));
                    if (field == null) continue;
                    var box = field.GetComponent<BoxCollider>();
                    if (box == null) box = field.gameObject.AddComponent<BoxCollider>();
                    box.center = field.sharedMesh.bounds.center;
                    box.size = field.sharedMesh.bounds.size;
                    box.enabled = true;
                    box.isTrigger = false;
                    EditorUtility.SetDirty(box);
                }
            }
            foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (IsNonSolidInteractionRoot(transform)) RemoveColliders(transform);
            Debug.Log("COLLISION fitted solid model hulls=" + count);
            return count;
        }

        public static void RunBatch()
        {
            var scene = EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath);
            ApplyToOpenScene();
            EditorSceneManager.SaveScene(scene);
        }

        public static void Inventory()
        {
            EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath);
            var report = new StringBuilder();
            foreach (var rig in Object.FindObjectsByType<QuantumPlayerRig>(FindObjectsInactive.Include))
            {
                report.AppendLine("RIG " + PathOf(rig.transform));
                report.AppendLine("BODY " + PathOf(rig.operationBodyVisual));
                foreach(var r in rig.operationBodyVisual.GetComponentsInChildren<Renderer>(true)) report.AppendLine("BODY RENDER " + PathOf(r.transform) + " bounds=" + r.bounds);
                foreach(var c in rig.GetComponentsInChildren<Collider>(true)) report.AppendLine("BODY COLLIDER " + PathOf(c.transform) + " bounds=" + c.bounds + " enabled=" + c.enabled);
            }
            foreach(var level in Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include))
            {
                report.AppendLine("LEVEL " + level.name);
                foreach(var mf in level.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = mf.GetComponent<Renderer>();
                    report.AppendLine("MESH " + PathOf(mf.transform) + " active=" + mf.gameObject.activeSelf + " visible=" + (r != null && r.enabled) + " bounds=" + (r != null ? r.bounds.ToString() : "none") + " vertices=" + (mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0));
                    foreach(var c in mf.GetComponents<Collider>()) report.AppendLine("  COLLIDER " + c.GetType().Name + " enabled=" + c.enabled + " trigger=" + c.isTrigger + " bounds=" + c.bounds);
                }
            }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/QuantumEcho-CollisionInventory.txt", report.ToString());
        }
        public static bool IsNonSolidInteractionModel(Transform transform)
        {
            for (var current = transform; current != null; current = current.parent)
                if (IsNonSolidInteractionRoot(current)) return true;
            return false;
        }

        static bool IsNonSolidInteractionRoot(Transform transform)
        {
            return transform != null &&
                (transform.name.StartsWith("Echo Lure Beacon") || transform.name.StartsWith("Hunter Sentinel"));
        }

        static void RemoveColliders(Transform root)
        {
            foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(collider, true);
        }

        static string PathOf(Transform t) => t == null ? "null" : t.parent == null ? t.name : PathOf(t.parent) + "/" + t.name;
    }
}
