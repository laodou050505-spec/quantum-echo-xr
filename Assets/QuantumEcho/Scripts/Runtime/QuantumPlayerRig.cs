using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace QuantumEcho
{
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class QuantumPlayerRig : MonoBehaviour
    {
        [Header("Rig")]
        public XROrigin xrOrigin;
        public Transform cameraFloorOffset;
        public Camera xrCamera;
        public Transform leftController;
        public Transform rightController;
        public LineRenderer leftRay;
        public LineRenderer rightRay;
        [Header("Controller Presentation")]
        [Tooltip("Hide the physical controller meshes so they never cover the play space. Tracking, trigger input, haptics and the right-hand interaction ray remain active.")]
        public bool hideControllerModels = true;
        public Transform operationBodyVisual;
        [Header("XR Comfort Avatar Locomotion")]
        [Tooltip("Keeps the PICO view stationary while the world-space third-person avatar moves. Disable only for an explicitly reviewed first-person locomotion design.")]
        public bool xrAvatarOnlyLocomotion = true;
        [Tooltip("Move the XR observation rig by the avatar's real collision-resolved displacement. The viewing distance and raised third-person composition remain fixed while the user retains free head rotation.")]
        public bool xrCameraFollowsAvatar = true;
        [Tooltip("Collision controller for the visible third-person avatar. The XR rig itself never translates from stick input in comfort mode.")]
        public CharacterController xrAvatarController;
        [Tooltip("In comfort mode, left-stick directions stay aligned to the authored room rather than the user's changing head yaw. The avatar turns automatically; looking around never changes where it walks.")]
        public bool comfortMovementUsesDesignAxes = true;
        [Header("Authoritative Start")]
        [Tooltip("Assigned from the active QuantumLevelScene. Never use a separate XR-only spawn.")]
        public QuantumDesignPlayerStart designPlayerStart;
        [Tooltip("Additional yaw used only if the imported visible model needs a forward-axis correction.")]
        public float visibleModelForwardYawOffset;

        [Header("Third Person / Comfort")]
        public float moveSpeed = 2.2f;
        public float desktopSprintMultiplier = 1.65f;
        public float mouseSensitivity = 0.08f;
        public float snapTurnDegrees = 30f;
        public float controllerRayLength = 4.5f;
        [Tooltip("Desktop preview camera position behind the operation body.")]
        public Vector3 desktopThirdPersonOffset = new Vector3(0f, 4.8f, 8.8f);
        [Tooltip("Kept serialized for older scenes. PICO now starts directly at the design marker; the headset supplies eye height.")]
        public Vector3 xrThirdPersonOffset = Vector3.zero;
        public float thirdPersonLookDownDegrees = 25f;

        public bool InteractHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool CommitPressed { get; private set; }
        public bool UndoPressed { get; private set; }
        public bool ResetPressed { get; private set; }
        public bool IsMoving { get; private set; }
        public int LevelSelectPressed { get; private set; } = -1;
        // PICO's loader can report an active XR display while the Unity Editor or the
        // macOS preview is running without a tracked headset.  Treat those sessions as
        // desktop play so the camera always has a deterministic, visible fallback.
        // The Android PICO build still uses the real tracked camera and controllers.
        public bool IsXRActive => Application.platform == RuntimePlatform.Android && XRSettings.isDeviceActive;
        // Gameplay follows the operated body's feet in PICO comfort mode.  The XR
        // origin follows the same collision-resolved translation, preserving the
        // authored third-person distance without forcing a camera turn.
        public Vector3 GroundPosition => UsesComfortAvatarLocomotion && operationBodyVisual != null
            ? operationBodyVisual.position
            : transform.position;
        public Vector3 HeadTrackingPosition => IsXRActive && xrCamera != null
            ? xrCamera.transform.position
            : transform.position + Vector3.up * 1.15f;
        // Gameplay agents track the character being controlled, not the HMD. This
        // prevents a hunter's aim from jumping when the user simply looks around.
        public Vector3 AvatarHeadPosition => GroundPosition + Vector3.up * 1.15f;
        public Ray AimRay => new Ray(IsXRActive ? rightController.position : xrCamera.transform.position, IsXRActive ? rightController.forward : xrCamera.transform.forward);
        public Quaternion FacingRotation => _facingRotation;
        public QuantumDesignPlayerStart DesignStart => designPlayerStart;

        CharacterController _characterController;
        CapsuleCollider _penetrationShape;
        readonly Collider[] _solidOverlaps = new Collider[64];
        XRInputDevice _headDevice;
        XRInputDevice _leftDevice;
        XRInputDevice _rightDevice;
        bool _previousXRInteract;
        bool _previousXRCommit;
        bool _previousXRUndo;
        bool _previousXRMenu;
        bool _reportedControllerBinding;
        bool _snapReady = true;
        float _desktopPitch;
        float _verticalVelocity;
        Quaternion _thirdPersonPitch;
        Quaternion _facingRotation;

        bool UsesComfortAvatarLocomotion => IsXRActive && xrAvatarOnlyLocomotion && operationBodyVisual != null;

        void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            ConfigureBodyCollider(_characterController);
            // The desktop camera obtains its distance/height from the active design
            // marker. PICO has no artificial camera pull-back: the tracked eye starts
            // at that same floor marker, which keeps controller rays and physical
            // targets in the same world space.
            xrThirdPersonOffset = Vector3.zero;
            if (xrOrigin != null)
            {
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            }
            var trackedPoseDriver = xrCamera != null ? xrCamera.GetComponent<TrackedPoseDriver>() : null;
            if (trackedPoseDriver != null) trackedPoseDriver.enabled = Application.platform == RuntimePlatform.Android;
            _thirdPersonPitch = Quaternion.Euler(thirdPersonLookDownDegrees, 0f, 0f);
            _facingRotation = transform.rotation;
            EnsureAvatarController();
        }

        void Update()
        {
            RefreshDevices();
            ReadActions();
            UpdateTrackedPoses();
            UpdateComfortControllers();
            UpdateMovement();
            UpdateRays();
        }

        void LateUpdate()
        {
            // Gates, trains and hunters animate after locomotion in Update.
            // Resolve their contact even when the player supplies no input.
            var controller = UsesComfortAvatarLocomotion ? xrAvatarController : _characterController;
            if (controller == null || !controller.enabled) return;
            if (_penetrationShape == null)
            {
                var query = new GameObject("Avatar penetration query - non-solid trigger");
                query.transform.SetParent(transform, false);
                _penetrationShape = query.AddComponent<CapsuleCollider>();
                _penetrationShape.isTrigger = true;
                _penetrationShape.radius = controller.radius;
                _penetrationShape.height = controller.height;
                _penetrationShape.center = controller.center;
            }
            var before = controller.transform.position;
            Physics.SyncTransforms();
            for (var iteration = 0; iteration < 4; iteration++)
            {
                var centre = controller.transform.TransformPoint(controller.center);
                var axis = Vector3.up * Mathf.Max(0f, controller.height * 0.5f - controller.radius);
                var count = Physics.OverlapCapsuleNonAlloc(centre-axis, centre+axis,
                    controller.radius, _solidOverlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                var corrected = false;
                for (var i = 0; i < count; i++)
                {
                    var solid = _solidOverlaps[i];
                    if (solid == controller || solid.transform.IsChildOf(transform)) continue;
                    if (!Physics.ComputePenetration(_penetrationShape, controller.transform.position,
                        controller.transform.rotation, solid, solid.transform.position, solid.transform.rotation,
                        out var direction, out var depth) || depth <= controller.skinWidth + 0.005f) continue;
                    // Move() can reject every direction when a kinematic hull
                    // has already enveloped a stationary controller. Apply the
                    // minimum separating translation, then query neighbours again.
                    controller.enabled = false;
                    controller.transform.position += direction * (depth + 0.002f);
                    controller.enabled = true;
                    Physics.SyncTransforms();
                    corrected = true;
                }
                if (!corrected) break;
                Physics.SyncTransforms();
            }
            if (UsesComfortAvatarLocomotion) FollowAvatarTranslation(before);
        }

        void UpdateComfortControllers()
        {
            if (_characterController == null) return;
            // There must never be a second stationary capsule at the XR origin:
            // it would block the avatar as it leaves the design start.  Desktop
            // keeps the original root controller and third-person camera fallback.
            if (UsesComfortAvatarLocomotion)
            {
                if (_characterController.enabled) _characterController.enabled = false;
                EnsureAvatarController();
                if (xrAvatarController != null && !xrAvatarController.enabled) xrAvatarController.enabled = true;
            }
            else
            {
                if (!_characterController.enabled) _characterController.enabled = true;
                if (xrAvatarController != null && xrAvatarController.enabled) xrAvatarController.enabled = false;
            }
        }

        void EnsureAvatarController()
        {
            if (operationBodyVisual == null || operationBodyVisual == transform) return;
            if (xrAvatarController == null) xrAvatarController = operationBodyVisual.GetComponent<CharacterController>();
            if (xrAvatarController == null) xrAvatarController = operationBodyVisual.gameObject.AddComponent<CharacterController>();
            ConfigureBodyCollider(xrAvatarController);
        }

        public static void ConfigureBodyCollider(CharacterController controller)
        {
            if (controller == null) return;
            // The visible model is 1.80 m wide; the old 0.28 m radius let most
            // of its shell pass through walls. Cover its turning silhouette.
            controller.radius = 0.96f;
            controller.height = 1.92f;
            controller.center = new Vector3(0f, 0.96f, 0f);
            controller.skinWidth = 0.015f;
            controller.stepOffset = 0.20f;
            controller.minMoveDistance = 0f;
        }

        void RefreshDevices()
        {
            if (!_headDevice.isValid) _headDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            if (!_leftDevice.isValid) _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!_rightDevice.isValid) _rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        void ReadActions()
        {
            var keyboard = Keyboard.current;
            var desktopInteract = keyboard != null && (keyboard.eKey.isPressed || keyboard.spaceKey.isPressed);
            var desktopInteractPressed = keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            var desktopCommit = keyboard != null && keyboard.qKey.wasPressedThisFrame;
            var desktopUndo = keyboard != null && keyboard.zKey.wasPressedThisFrame;
            var desktopReset = keyboard != null && keyboard.rKey.wasPressedThisFrame;

            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var legacyTrigger);
            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var rightPrimary);
            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out var rightSecondary);
            _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out var leftPrimary);
            _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out var leftSecondary);
            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out var rightGrip);
            _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out var leftGrip);
            _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out var xrMenu);

            // PICO Integration SDK 6 exposes the controller through the Input System
            // as PXR_Controller. Some firmware/runtime combinations update that path
            // one frame earlier than the legacy XR CommonUsages path, so read both.
            // A/X always means "save this echo" and B/Y always means "undo" on
            // either hand. This also makes the mapping portable between PICO models.
            var inputSystemTrigger = ReadXRButton("triggerPressed", true) || ReadXRButton("triggerButton", true);
            var inputSystemPrimary = ReadXRButton("primaryButton", false);
            var inputSystemSecondary = ReadXRButton("secondaryButton", false);
            var inputSystemGrip = ReadXRButton("gripPressed", false) || ReadXRButton("gripButton", false);
            var xrInteract = legacyTrigger || inputSystemTrigger;
            var xrCommit = rightPrimary || leftPrimary || rightGrip || leftGrip || inputSystemPrimary || inputSystemGrip;
            var xrUndo = rightSecondary || leftSecondary || inputSystemSecondary;

            InteractHeld = desktopInteract || xrInteract;
            InteractPressed = desktopInteractPressed || xrInteract && !_previousXRInteract;
            CommitPressed = desktopCommit || xrCommit && !_previousXRCommit;
            UndoPressed = desktopUndo || xrUndo && !_previousXRUndo;
            ResetPressed = desktopReset || xrMenu && !_previousXRMenu;
            LevelSelectPressed = -1;

            if (keyboard != null)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) LevelSelectPressed = 0;
                if (keyboard.digit2Key.wasPressedThisFrame) LevelSelectPressed = 1;
                if (keyboard.digit3Key.wasPressedThisFrame) LevelSelectPressed = 2;
                if (keyboard.digit4Key.wasPressedThisFrame) LevelSelectPressed = 3;
                if (keyboard.f5Key.wasPressedThisFrame) Recenter();
            }

            _previousXRInteract = xrInteract;
            _previousXRCommit = xrCommit;
            _previousXRUndo = xrUndo;
            _previousXRMenu = xrMenu;

            if (IsXRActive && !_reportedControllerBinding && (_leftDevice.isValid || _rightDevice.isValid))
            {
                Debug.Log($"PICO INPUT ready: left={_leftDevice.name} right={_rightDevice.name}; Trigger=interact/hold, Grip or A/X=commit echo, B/Y=undo echo, Menu=reset.");
                _reportedControllerBinding = true;
            }
        }

        /// <summary>
        /// Reads a named button from every active Input-System XR controller.
        /// PICO's official layout names are primaryButton, secondaryButton and
        /// triggerPressed. Trigger remains right-hand-only because the interaction
        /// ray is deliberately attached to the right controller.
        /// </summary>
        static bool ReadXRButton(string controlName, bool rightHandOnly)
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is not XRController) continue;
                if (rightHandOnly && !HasUsage(device, UnityEngine.InputSystem.CommonUsages.RightHand)) continue;
                var button = device.TryGetChildControl<ButtonControl>(controlName);
                if (button != null && button.isPressed) return true;
            }
            return false;
        }

        static bool HasUsage(UnityEngine.InputSystem.InputDevice device, UnityEngine.InputSystem.Utilities.InternedString usage)
        {
            for (var index = 0; index < device.usages.Count; index++)
            {
                if (device.usages[index].ToString() == usage.ToString()) return true;
            }
            return false;
        }

        void UpdateTrackedPoses()
        {
            if (!IsXRActive)
            {
                UpdateDesktopLook();
                HideTrackedControllerVisuals();
                SetOperationBodyVisible(true);
                return;
            }

            // The tracked eye pose is mounted at the authored observation point.
            // Head rotation remains fully tracked.  In comfort mode the left stick
            // moves the visible character only; it deliberately never translates
            // this XR origin or the HMD view.
            if (cameraFloorOffset != null)
            {
                // The root is the authoritative design start. The tracked HMD keeps
                // its actual local movement, while this authored offset creates a
                // stable third-person observation position. Local -Z is behind
                // because rooms begin facing their -Z puzzle area.
                var distance = designPlayerStart != null ? designPlayerStart.xrThirdPersonViewingDistance : 5.2f;
                var overlookHeight = designPlayerStart != null ? designPlayerStart.xrThirdPersonObservationHeight : 0.65f;
                // Lift the observation point instead of pitching the tracked HMD.
                // The player gets the requested clearer downward third-person view,
                // while their real head rotation remains one-to-one and comfortable.
                cameraFloorOffset.localPosition = Vector3.back * distance + Vector3.up * overlookHeight;
                cameraFloorOffset.localRotation = Quaternion.identity;
            }
            ApplyPose(_headDevice, xrCamera.transform);
            ApplyPose(_leftDevice, leftController);
            ApplyPose(_rightDevice, rightController);
            // PICO controller meshes are useful for a controller demo, but in this
            // third-person puzzle they can cover the board and objective.  The
            // transforms remain fully tracked for the right trigger/ray interaction.
            HideTrackedControllerVisuals();
            SetOperationBodyVisible(true);
        }

        void HideTrackedControllerVisuals()
        {
            if (!hideControllerModels) return;
            HideControllerVisuals(leftController);
            HideControllerVisuals(rightController);
        }

        static void HideControllerVisuals(Transform controller)
        {
            if (controller == null) return;
            // PICO loads its controller prefab after the scene has started, so this
            // deliberate lightweight pass also catches renderers added at runtime.
            foreach (var renderer in controller.GetComponentsInChildren<Renderer>(true))
            {
                // Rays are interaction feedback, not a physical controller model.
                if (renderer is LineRenderer) continue;
                if (renderer.enabled) renderer.enabled = false;
            }
        }

        static void ApplyPose(XRInputDevice device, Transform target)
        {
            if (target == null || !device.isValid) return;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position)) target.localPosition = position;
            if (device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation)) target.localRotation = rotation;
        }

        void UpdateDesktopLook()
        {
            // Desktop free-look belongs to QuantumThirdPersonCamera.  Do not
            // rotate the player rig here: tying camera input to this root makes
            // the entire room and character spin together instead of providing
            // a stable third-person view.
        }

        void UpdateMovement()
        {
            var moveInput = Vector2.zero;
            if (IsXRActive)
            {
                _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out moveInput);
                // Comfort-avatar locomotion has no artificial camera or world turn.
                // The user looks around naturally with their head while the pawn is
                // commanded in stable room axes. Legacy first-person XR retains
                // snap turn only when this mode has been explicitly disabled.
                if (!UsesComfortAvatarLocomotion) UpdateSnapTurn();
            }
            else
            {
                var keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveInput.y += 1f;
                    if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveInput.y -= 1f;
                    if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveInput.x += 1f;
                    if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveInput.x -= 1f;
                }
            }

            moveInput = Vector2.ClampMagnitude(moveInput, 1f);
            IsMoving = moveInput.sqrMagnitude > 0.001f;
            Vector3 planar;
            if (UsesComfortAvatarLocomotion && comfortMovementUsesDesignAxes)
            {
                // The level's design start defines a stable map: stick up always
                // advances toward the first puzzle landmark, independent of head
                // movement. This avoids the disorienting need to rotate the headset
                // just to make a third-person avatar walk straight.
                var designYaw = designPlayerStart != null ? designPlayerStart.WorldYaw : transform.rotation;
                planar = designYaw * (Vector3.forward * moveInput.y + Vector3.right * moveInput.x);
            }
            else
            {
                var referenceTransform = xrCamera != null ? xrCamera.transform : transform;
                var headForward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up).normalized;
                var headRight = Vector3.ProjectOnPlane(referenceTransform.right, Vector3.up).normalized;
                if (headForward.sqrMagnitude < 0.001f) headForward = transform.forward;
                if (headRight.sqrMagnitude < 0.001f) headRight = transform.right;
                planar = headForward * moveInput.y + headRight * moveInput.x;
            }
            var speed = moveSpeed;
            if (!IsXRActive && Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                speed *= desktopSprintMultiplier;
            }

            if (UsesComfortAvatarLocomotion)
            {
                EnsureAvatarController();
                if (xrAvatarController != null)
                {
                    var avatarBeforeMove = operationBodyVisual.position;
                    _verticalVelocity = xrAvatarController.isGrounded
                        ? -0.5f
                        : _verticalVelocity + Physics.gravity.y * Time.deltaTime;
                    xrAvatarController.Move((planar * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);
                    FollowAvatarTranslation(avatarBeforeMove);
                }
            }
            else
            {
                _verticalVelocity = _characterController.isGrounded
                    ? -0.5f
                    : _verticalVelocity + Physics.gravity.y * Time.deltaTime;
                _characterController.Move((planar * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);
            }

            // The visible operation body and every recorded echo face their actual
            // travel direction. Camera yaw remains independent, so strafing never
            // swings the third-person camera around the avatar.
            if (planar.sqrMagnitude > 0.001f)
            {
                var desired = Quaternion.LookRotation(planar.normalized, Vector3.up);
                _facingRotation = Quaternion.RotateTowards(_facingRotation, desired, 720f * Time.deltaTime);
            }
            if (operationBodyVisual != null)
            {
                operationBodyVisual.rotation = _facingRotation * Quaternion.Euler(0f, visibleModelForwardYawOffset, 0f);
            }
        }

        /// <summary>
        /// The character visual is a child of the XR rig so its controller can own
        /// the collision. Once that controller reports its actual displacement,
        /// move the rig by the same amount and counter-shift the visual. This keeps
        /// the avatar's final world position untouched while the HMD/controller
        /// observation point follows at an exactly constant authored offset.
        /// </summary>
        void FollowAvatarTranslation(Vector3 avatarBeforeMove)
        {
            if (!UsesComfortAvatarLocomotion || !xrCameraFollowsAvatar || operationBodyVisual == null) return;
            var actualDisplacement = operationBodyVisual.position - avatarBeforeMove;
            if (actualDisplacement.sqrMagnitude < 0.0000001f) return;

            transform.position += actualDisplacement;
            operationBodyVisual.position -= actualDisplacement;
        }

        void UpdateSnapTurn()
        {
            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out var axis);
            if (Mathf.Abs(axis.x) < 0.45f) _snapReady = true;
            if (!_snapReady || Mathf.Abs(axis.x) < 0.75f) return;
            var angle = Mathf.Sign(axis.x) * snapTurnDegrees;
            // Turning is angular only. Preserve the avatar's world-space position
            // while the rig rotates around the tracked eye; otherwise the avatar
            // would arc sideways every time the user makes a comfort snap turn.
            var avatarPosition = operationBodyVisual != null ? operationBodyVisual.position : Vector3.zero;
            transform.RotateAround(xrCamera.transform.position, Vector3.up, angle);
            if (UsesComfortAvatarLocomotion && operationBodyVisual != null)
            {
                var avatarControllerWasEnabled = xrAvatarController != null && xrAvatarController.enabled;
                if (xrAvatarController != null) xrAvatarController.enabled = false;
                operationBodyVisual.position = avatarPosition;
                if (xrAvatarController != null) xrAvatarController.enabled = avatarControllerWasEnabled;
            }
            _snapReady = false;
        }

        void UpdateRays()
        {
            // This game is operated with the right trigger.  Keeping only that
            // hairline ray avoids a second bright diagonal across the play field.
            if (leftRay != null) leftRay.enabled = false;
            UpdateRay(rightRay, rightController);
        }

        void SetOperationBodyVisible(bool visible)
        {
            if (operationBodyVisual == null) return;
            foreach (var renderer in operationBodyVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.enabled != visible) renderer.enabled = visible;
            }
        }


        void UpdateRay(LineRenderer line, Transform source)
        {
            if (line == null || source == null) return;
            line.enabled = IsXRActive;
            if (!line.enabled) return;
            line.startWidth = 0.0035f;
            line.endWidth = 0.001f;
            line.SetPosition(0, source.position);
            line.SetPosition(1, source.position + source.forward * controllerRayLength);
        }

        public void MoveTo(Vector3 position, Quaternion rotation)
        {
            var rootControllerWasEnabled = _characterController != null && _characterController.enabled;
            if (_characterController != null) _characterController.enabled = false;
            var avatarControllerWasEnabled = xrAvatarController != null && xrAvatarController.enabled;
            if (xrAvatarController != null) xrAvatarController.enabled = false;
            var spawnRotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            transform.SetPositionAndRotation(position, spawnRotation);
            _facingRotation = spawnRotation;
            if (operationBodyVisual != null)
            {
                // Resetting, scene entry, and recentering all return the avatar to
                // the same design marker as the XR origin. This is intentionally a
                // local reset so the camera never has to be moved during gameplay.
                operationBodyVisual.localPosition = Vector3.zero;
                operationBodyVisual.rotation = _facingRotation * Quaternion.Euler(0f, visibleModelForwardYawOffset, 0f);
            }
            if (_characterController != null) _characterController.enabled = rootControllerWasEnabled && !UsesComfortAvatarLocomotion;
            if (xrAvatarController != null) xrAvatarController.enabled = avatarControllerWasEnabled || UsesComfortAvatarLocomotion;
            _verticalVelocity = 0f;
            _desktopPitch = 0f;
            if (!IsXRActive)
            {
                if (cameraFloorOffset != null)
                {
                    cameraFloorOffset.localPosition = Vector3.zero;
                    cameraFloorOffset.localRotation = Quaternion.identity;
                }
            }
        }

        /// <summary>Changes the active room's single design start without moving yet.</summary>
        public void SetDesignStart(QuantumDesignPlayerStart start)
        {
            designPlayerStart = start;
        }

        /// <summary>
        /// Returns both desktop and XR to the same authored floor marker. XR retains
        /// only its normal local tracked-pose offset above that marker.
        /// </summary>
        public void MoveToDesignStart()
        {
            if (designPlayerStart != null)
            {
                MoveTo(designPlayerStart.WorldPosition, designPlayerStart.WorldYaw);
                return;
            }
            MoveTo(transform.position, transform.rotation);
        }

        public void GetDesktopCameraPose(out Vector3 position, out Vector3 target)
        {
            var distance = designPlayerStart != null ? designPlayerStart.desktopViewingDistance : distanceFallback;
            var height = designPlayerStart != null ? designPlayerStart.desktopViewingHeight : heightFallback;
            var focusHeight = designPlayerStart != null ? designPlayerStart.desktopFocusHeight : 0.85f;
            var forward = Vector3.ProjectOnPlane(_facingRotation * Vector3.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.back;
            // Frame both the operated body and the same first physical landmark seen
            // directly from the XR start. This keeps the desktop composition legible
            // without inventing a second camera-start location.
            var bodyFocus = transform.position + Vector3.up * focusHeight;
            var landmarkFocus = designPlayerStart != null
                ? designPlayerStart.LandmarkPosition + Vector3.up * focusHeight
                : bodyFocus;
            target = Vector3.Lerp(bodyFocus, landmarkFocus, 0.58f);
            position = transform.position - forward * distance + Vector3.up * height;
        }

        // Fallbacks only protect manually opened legacy scenes before a level assigns
        // a design marker. Regular scene entry and every Reset use the marker above.
        const float distanceFallback = 9.2f;
        const float heightFallback = 4.6f;

        public void PulseHaptics(float amplitude = 0.5f, float duration = 0.08f)
        {
            if (_rightDevice.isValid) _rightDevice.SendHapticImpulse(0u, Mathf.Clamp01(amplitude), Mathf.Max(0.01f, duration));
        }

        public void Recenter()
        {
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems) subsystem.TryRecenter();
            // Apply the authored floor origin after device recentering: the HMD may
            // retain its normal local pose, but the locomotion/XR root is always back
            // on the same world marker used by the desktop preview and Reset.
            MoveToDesignStart();
        }
    }

}
