using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QuantumEcho.Editor
{
    [InitializeOnLoad]
    public static class QuantumEchoModelInspector
    {
        const string ModelRoot = "Assets/QuantumEcho/Models/ScienceFictionSpace";

        static QuantumEchoModelInspector()
        {
        }

        [MenuItem("Quantum Echo/Inspect Science Fiction Models")]
        public static void Inspect()
        {
            for (var number = 1; number <= 26; number++)
            {
                var folder = $"{ModelRoot}/{number}";
                var guid = AssetDatabase.FindAssets("t:Model", new[] { folder }).FirstOrDefault();
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogError($"SCI_FI_MODEL {number:00} missing in {folder}");
                    continue;
                }

                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var preview = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (preview == null) continue;
                preview.hideFlags = HideFlags.HideAndDontSave;
                preview.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                preview.transform.localScale = Vector3.one;

                if (TryGetBounds(preview, out var bounds))
                {
                    var renderers = preview.GetComponentsInChildren<Renderer>(true);
                    var materials = renderers.SelectMany(renderer => renderer.sharedMaterials)
                        .Where(material => material != null).Select(material => material.name).Distinct();
                    Debug.Log($"SCI_FI_MODEL {number:00} | file={Path.GetFileName(path)} | " +
                              $"size={bounds.size:F4} | center={bounds.center:F4} | " +
                              $"renderers={renderers.Length} | materials=[{string.Join(",", materials)}]");
                }

                Object.DestroyImmediate(preview);
            }
        }

        internal static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = default;
            if (renderers.Length == 0) return false;
            bounds = renderers[0].bounds;
            for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return true;
        }
    }
}
