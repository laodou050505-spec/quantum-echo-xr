using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuantumEcho.Editor
{
    /// <summary>
    /// Applies safe layout corrections to an already-authored editable scene.  This is
    /// deliberately separate from the builder so a designer's later scene edits are
    /// not discarded by regenerating all four rooms.
    /// </summary>
    public static class QuantumEchoSceneRepair
    {
        const string EnvironmentName = "ENVIRONMENT - walls floor lights";
        const string SafetyDeckName = "Spawn Safety Deck - editable";

        [MenuItem("Quantum Echo/Repair Current Scene Layout")]
        public static void RepairCurrentSceneLayout()
        {
            var levels = Object.FindObjectsByType<QuantumLevelScene>(FindObjectsInactive.Include);
            foreach (var level in levels)
            {
                RepairWorldLabels(level);
                RepairPad(level.padA);
                RepairPad(level.padB);
                RepairSpawnAndSafetyDeck(level);
                EditorUtility.SetDirty(level);
            }

            RepairHudCanvas();

            var scene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Quantum Echo scene layout repaired: world labels face the player, pads are walkable, and each spawn has a safety deck.");
        }

        // The task terminal is a fixed, walk-up world object. It must never be
        // restored to a Screen Space - Camera canvas by an older repair pass.
        static void RepairHudCanvas()
        {
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.worldCamera = null;
                canvas.transform.localRotation = Quaternion.identity;
                canvas.transform.localScale = Vector3.one * 0.00155f;
                var rect = canvas.GetComponent<RectTransform>();
                if (rect != null) rect.sizeDelta = new Vector2(1024f, 640f);
                EditorUtility.SetDirty(canvas.transform);
                EditorUtility.SetDirty(canvas);
            }
        }

        static void RepairWorldLabels(QuantumLevelScene level)
        {
            foreach (var label in level.GetComponentsInChildren<TextMesh>(true))
            {
                label.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                EditorUtility.SetDirty(label.transform);
            }
        }

        static void RepairPad(Transform pad)
        {
            if (pad == null) return;
            foreach (var collider in pad.GetComponentsInChildren<Collider>(true))
            {
                Object.DestroyImmediate(collider);
            }
            // Pad activation uses FlatDistance in QuantumEchoGameManager; it does
            // not need a physics collider.  Keeping the visual collider-free makes
            // the pad flush with the room floor and removes every possibility of an
            // invisible wall or a too-high CharacterController step.
            EditorUtility.SetDirty(pad.gameObject);
        }

        static void RepairSpawnAndSafetyDeck(QuantumLevelScene level)
        {
            if (level.spawnPoint != null && level.spawnPoint.localPosition.z > 4.8f)
            {
                level.spawnPoint.localPosition = new Vector3(level.spawnPoint.localPosition.x, 0.05f, 4f);
                EditorUtility.SetDirty(level.spawnPoint);
            }

            var environment = level.transform.Find(EnvironmentName);
            if (environment == null) return;
            var deck = environment.Find(SafetyDeckName);
            if (deck == null)
            {
                var deckObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                deckObject.name = SafetyDeckName;
                deckObject.transform.SetParent(environment, false);
                deck = deckObject.transform;
                var sourceFloor = environment.Find("Floor");
                var sourceRenderer = sourceFloor != null ? sourceFloor.GetComponent<Renderer>() : null;
                var deckRenderer = deckObject.GetComponent<Renderer>();
                if (sourceRenderer != null && deckRenderer != null) deckRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }

            deck.localPosition = new Vector3(0f, -0.2f, 8.8f);
            deck.localRotation = Quaternion.identity;
            deck.localScale = new Vector3(14.6f, 0.32f, 5.4f);
            EditorUtility.SetDirty(deck.gameObject);
        }
    }
}
