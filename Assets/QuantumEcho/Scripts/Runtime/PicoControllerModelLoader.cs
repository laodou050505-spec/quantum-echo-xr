using UnityEngine;

namespace QuantumEcho
{
    public sealed class PicoControllerModelLoader : MonoBehaviour
    {
        public string resourcePath;

        void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab != null) Instantiate(prefab, transform, false);
#endif
        }
    }
}
