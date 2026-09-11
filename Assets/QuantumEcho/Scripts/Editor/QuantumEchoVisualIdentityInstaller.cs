using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuantumEcho.Editor
{
    /// <summary>Imports the generated art with XR-safe settings and applies the visual identity to both scenes.</summary>
    public static class QuantumEchoVisualIdentityInstaller
    {
        public const string PanelSourcePath = "Assets/Resources/QuantumEcho/QuantumPhasePanel.png";

        [MenuItem("Quantum Echo/Apply Generated Cover And Panel Identity")]
        public static void ApplyAll()
        {
            ConfigureTexture(QuantumEchoTitleSceneBuilder.CoverArtworkPath, false, 4096);
            ConfigureTexture(PanelSourcePath, true, 2048);
            AssetDatabase.Refresh();

            QuantumEchoTitleSceneBuilder.BuildTitleScene();
            ApplyGameplayPanels();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Quantum visual identity applied: Nano Banana cover, generated phase panels, TMP safe-area typography.");
        }

        static void ApplyGameplayPanels()
        {
            var previous = SceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(QuantumEchoTitleSceneBuilder.GameplayScenePath, OpenSceneMode.Single);
            var hud = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<QuantumHUD>(true)).FirstOrDefault();
            if (hud == null) throw new InvalidDataException("Gameplay HUD was not found.");

            var skin = hud.GetComponent<QuantumHudSkin>();
            if (skin == null) skin = hud.gameObject.AddComponent<QuantumHudSkin>();
            skin.ApplySkin();
            ApplyTextLayout(hud.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrWhiteSpace(previous) && previous != scene.path && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
        }

        static void ApplyTextLayout(Transform root)
        {
            ConfigureText(root, "Objective Index", 23f, new Color(0.22f, 0.91f, 0.95f), FontStyles.Normal);
            ConfigureText(root, "Objective Title", 30f, new Color(0.94f, 0.98f, 1f), FontStyles.Bold);
            ConfigureText(root, "Objective Copy", 20f, new Color(0.76f, 0.88f, 0.92f), FontStyles.Normal);
            ConfigureText(root, "Prompt Key", 19f, new Color(0.22f, 0.91f, 0.95f), FontStyles.Bold);
            ConfigureText(root, "Prompt Text", 21f, Color.white, FontStyles.Normal);
            ConfigureText(root, "Progress Label", 19f, Color.white, FontStyles.Normal);
            ConfigureText(root, "Progress Value", 21f, new Color(0.97f, 0.68f, 0.26f), FontStyles.Bold);
            ConfigureText(root, "Result Title", 38f, new Color(0.22f, 0.91f, 0.95f), FontStyles.Bold);
            ConfigureText(root, "Result Stats", 22f, Color.white, FontStyles.Normal);
        }

        static void ConfigureText(Transform root, string name, float size, Color color, FontStyles style)
        {
            var text = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(item => item.name == name);
            if (text == null) return;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
        }

        static void ConfigureTexture(string path, bool sprite, int maxSize)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
            importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = !sprite;
            importer.sRGBTexture = true;
            importer.alphaIsTransparency = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxSize;
            importer.textureCompression = sprite ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
            importer.anisoLevel = 4;
            if (sprite) importer.spriteBorder = new Vector4(170f, 170f, 170f, 170f);
            importer.SaveAndReimport();
        }
    }
}
