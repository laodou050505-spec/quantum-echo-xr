using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumEcho.Editor
{
    /// <summary>Creates the project-owned deep-space starfield and assigns it scene-wide.</summary>
    public static class QuantumEchoNebulaSkybox
    {
        const string MaterialPath = "Assets/QuantumEcho/Materials/QuantumNebulaSkybox.mat";

        [MenuItem("Quantum Echo/Apply Nebula Starfield Skybox")]
        public static void ApplyFromMenu()
        {
            EnsureForActiveScene();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
        }

        public static void EnsureForActiveScene()
        {
            var shader = Shader.Find("QuantumEcho/Nebula Starfield Skybox");
            if (shader == null)
            {
                Debug.LogError("Quantum Echo nebula skybox shader is not available yet.");
                return;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.name = "Quantum Nebula Starfield Skybox";
            material.SetColor("_Tint", new Color(0.10f, 0.74f, 0.98f, 1f));
            material.SetColor("_Magenta", new Color(0.68f, 0.19f, 0.96f, 1f));
            material.SetFloat("_Exposure", 1.2f);
            material.SetFloat("_StarDensity", 0.11f);
            material.SetFloat("_StarSize", 0.58f);
            material.SetFloat("_TwinkleStrength", 0.82f);
            material.SetFloat("_TwinkleSpeed", 1f);
            RenderSettings.skybox = material;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.06f, 0.13f);
            RenderSettings.reflectionIntensity = 0.5f;

            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                camera.clearFlags = CameraClearFlags.Skybox;
                EditorUtility.SetDirty(camera);
            }
            EditorUtility.SetDirty(material);
        }
    }
}
