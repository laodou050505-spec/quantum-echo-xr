using UnityEngine;
using UnityEngine.XR;

namespace QuantumEcho
{
    /// <summary>Keeps the desktop preview and PICO render target crisp.</summary>
    [DefaultExecutionOrder(-200)]
    public sealed class QuantumVisualQuality : MonoBehaviour
    {
        void Awake()
        {
            QualitySettings.antiAliasing = 4;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            QualitySettings.globalTextureMipmapLimit = 0;
            QualitySettings.lodBias = Mathf.Max(1.5f, QualitySettings.lodBias);

            if (Application.platform == RuntimePlatform.Android && XRSettings.isDeviceActive)
            {
                XRSettings.eyeTextureResolutionScale = 1.15f;
                Application.targetFrameRate = 72;
            }
        }
    }
}
