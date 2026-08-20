using UnityEngine;

namespace QuantumEcho
{
    /// <summary>
    /// The one authored player reference for an individual experiment room.
    /// Both the desktop overview camera and the PICO XR rig derive their
    /// starting composition from this marker: its position is the avatar/XR
    /// rig floor origin, its yaw faces the first landmark, and its eye-height
    /// and desktop distance describe the two viewing modes.
    /// </summary>
    public sealed class QuantumDesignPlayerStart : MonoBehaviour
    {
        [Header("Authoritative Starting Composition")]
        [Tooltip("The first physical game-space landmark the player faces on reset.")]
        public Transform firstLandmark;
        [Min(0.8f)] public float xrEyeHeight = 1.65f;
        [Tooltip("Shared close observation distance behind the character for PICO and desktop. The view does not follow stick locomotion.")]
        [Min(1.2f)] public float xrThirdPersonViewingDistance = 3.6f;
        [Tooltip("Matches the PICO observation distance so desktop and XR begin at the same world landmark.")]
        [Min(1f)] public float desktopViewingDistance = 3.6f;
        [Tooltip("Matches the authored PICO eye height; avoids an unwanted top-down board view.")]
        [Min(0.8f)] public float desktopViewingHeight = 1.65f;
        [Min(0f)] public float desktopFocusHeight = 0.9f;

        public Vector3 WorldPosition => transform.position;
        public Quaternion WorldYaw => Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        public Vector3 LandmarkPosition => firstLandmark != null ? firstLandmark.position : transform.position + transform.forward * 4f;
    }
}
