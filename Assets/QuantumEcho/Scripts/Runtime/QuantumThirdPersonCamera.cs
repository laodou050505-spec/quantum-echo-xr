using UnityEngine;

namespace QuantumEcho
{
    /// <summary>
    /// A close, eye-level desktop reference camera. It shares the active room's
    /// design start parameters with PICO, so neither view defaults to a top-down
    /// board view or an unrelated, remote camera composition.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Camera))]
    public sealed class QuantumThirdPersonCamera : MonoBehaviour
    {
        public QuantumPlayerRig player;
        public float distance = 9.2f;
        public float height = 4.6f;
        public float targetHeight = 0.85f;

        void LateUpdate()
        {
            if (player == null || player.IsXRActive) return;
            player.GetDesktopCameraPose(out var position, out var target);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));
        }
    }
}
