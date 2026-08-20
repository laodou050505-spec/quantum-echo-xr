using UnityEngine;

namespace QuantumEcho
{
    public enum QuantumLevelMode
    {
        Tutorial,
        Paired,
        Train,
        Lure
    }

    public sealed class QuantumLevelScene : MonoBehaviour
    {
        [Header("Level")]
        public int levelIndex;
        public QuantumLevelMode mode;
        public string levelCode;
        public string title;
        [TextArea] public string description;
        public float loopDuration = 14f;

        [Header("Editable Layout")]
        [Tooltip("Single source of truth for desktop and XR reset composition in this room.")]
        public QuantumDesignPlayerStart designPlayerStart;
        public Transform spawnPoint;
        [Tooltip("Fixed world-space task terminal. It is deliberately not parented to the camera.")]
        public Transform worldTaskTerminalAnchor;
        public Transform padA;
        public Transform padB;
        public Renderer padARenderer;
        public Renderer padBRenderer;
        public Transform gateLeft;
        public Transform gateRight;
        public Renderer gateLeftRenderer;
        public Renderer gateRightRenderer;
        public Transform core;
        public QuantumSceneTarget coreTarget;

        [Header("Train Puzzle")]
        public Transform train;
        public Renderer trainRenderer;
        public Vector3 trainStartLocal;
        public Vector3 trainJunctionLocal;
        public Vector3 trainDockLocal;

        [Header("Lure Puzzle")]
        public Transform[] lureZones;
        public QuantumSceneTarget[] sentinels;
        public QuantumSceneTarget[] fragments;

        [HideInInspector] public Vector3 gateLeftClosedLocal;
        [HideInInspector] public Vector3 gateRightClosedLocal;
    }
}
