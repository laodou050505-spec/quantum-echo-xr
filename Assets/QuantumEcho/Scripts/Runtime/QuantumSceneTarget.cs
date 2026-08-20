using UnityEngine;

namespace QuantumEcho
{
    public sealed class QuantumSceneTarget : MonoBehaviour
    {
        public Renderer mainRenderer;
        public Renderer[] accentRenderers;
        public Transform halo;
        public Vector3 homeLocalPosition;
        public Vector3 haloBaseScale = Vector3.one;

        [HideInInspector] public bool collected;
        [HideInInspector] public float captureProgress;
    }
}
