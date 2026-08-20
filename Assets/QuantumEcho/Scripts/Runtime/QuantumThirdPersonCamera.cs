using UnityEngine;
using UnityEngine.InputSystem;

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

        [Header("Stable third-person follow")]
        [Tooltip("The avatar can turn without orbiting the camera automatically. The player may still orbit deliberately with the right mouse button.")]
        public bool keepAuthoredYaw = true;
        [Tooltip("Seconds used to settle the camera behind the avatar. A small value keeps the follow responsive without a visible snap.")]
        [Min(0f)] public float positionSmoothTime = 0.04f;
        [Header("Desktop free look")]
        [Tooltip("Hold the right mouse button to orbit the view. This is player-directed only; character movement never rotates the camera by itself.")]
        public bool allowDesktopFreeLook = true;
        [Min(0.01f)] public float mouseOrbitSensitivity = 0.08f;
        [Range(-80f, -5f)] public float minimumPitch = -55f;
        [Range(5f, 80f)] public float maximumPitch = 65f;

        float _followDistance;
        float _followHeight;
        float _yaw;
        float _pitch;
        Vector3 _positionVelocity;
        bool _initialized;

        void OnEnable()
        {
            _initialized = false;
            _positionVelocity = Vector3.zero;
        }

        void LateUpdate()
        {
            if (player == null || player.IsXRActive) return;
            EnsureInitialized();
            ReadDesktopFreeLook();

            // Follow the avatar's world position only. The orbit yaw comes from
            // deliberate right-mouse input, never from the avatar's own rotation.
            // This prevents the spinning view while retaining free player look.
            var orbitYaw = Quaternion.Euler(0f, _yaw, 0f);
            var desiredPosition = player.GroundPosition +
                                  orbitYaw * Vector3.back * _followDistance +
                                  Vector3.up * _followHeight;
            if (positionSmoothTime <= 0f)
            {
                transform.position = desiredPosition;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(
                    transform.position,
                    desiredPosition,
                    ref _positionVelocity,
                    positionSmoothTime);
            }

            // Rotation remains independent from the avatar. It changes only with
            // the user's deliberate free-look input.
            transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }

        void EnsureInitialized()
        {
            if (_initialized) return;

            player.GetDesktopCameraPose(out var position, out var target);
            var direction = target - position;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = player.DesignStart != null
                    ? player.DesignStart.WorldYaw * Vector3.forward
                    : Vector3.forward;
            }

            var authoredRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            var offset = position - player.GroundPosition;
            _followHeight = offset.y;
            _followDistance = Vector3.ProjectOnPlane(offset, Vector3.up).magnitude;
            _yaw = authoredRotation.eulerAngles.y;
            _pitch = NormalizePitch(authoredRotation.eulerAngles.x);
            transform.SetPositionAndRotation(position, authoredRotation);
            _initialized = true;
        }

        void ReadDesktopFreeLook()
        {
            if (!allowDesktopFreeLook) return;
            var mouse = Mouse.current;
            if (mouse == null || !mouse.rightButton.isPressed) return;

            var delta = mouse.delta.ReadValue() * mouseOrbitSensitivity;
            _yaw += delta.x;
            _pitch = Mathf.Clamp(_pitch - delta.y, minimumPitch, maximumPitch);
        }

        static float NormalizePitch(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
