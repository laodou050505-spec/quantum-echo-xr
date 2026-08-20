using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using XRInputDevice = UnityEngine.XR.InputDevice;

namespace QuantumEcho
{
    /// <summary>
    /// Owns the isolated title phase. This scene intentionally contains no gameplay
    /// root, gameplay camera, HUD, collider, audio, or logic. Loading the main scene
    /// with LoadSceneMode.Single destroys this whole title root before play begins.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class QuantumTitleController : MonoBehaviour
    {
        [Header("Independent Title Design Start")]
        public QuantumDesignPlayerStart designPlayerStart;
        public XROrigin xrOrigin;
        public Transform cameraFloorOffset;
        public Camera titleCamera;
        public Transform rightController;
        public LineRenderer rightRay;

        [Header("World-Space Entry Targets")]
        public Collider startTarget;
        public Collider exitTarget;
        public string gameplaySceneName = "QuantumEcho_Main";
        public float controllerRayLength = 5f;

        XRInputDevice _rightDevice;
        XRInputDevice _leftDevice;
        bool _previousTrigger;
        bool _previousMenu;
        bool _loading;

        bool IsXRActive => Application.platform == RuntimePlatform.Android && XRSettings.isDeviceActive;

        void Awake()
        {
            if (xrOrigin != null)
                xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
            var poseDriver = titleCamera != null ? titleCamera.GetComponent<TrackedPoseDriver>() : null;
            if (poseDriver != null) poseDriver.enabled = Application.platform == RuntimePlatform.Android;
            MoveToDesignStart();
        }

        void Update()
        {
            if (_loading) return;
            RefreshDevices();
            UpdateRightControllerPose();
            UpdateDesktopCamera();
            UpdateControllerRay();

            var keyboard = Keyboard.current;
            var startPressed = keyboard != null &&
                (keyboard.enterKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame);
            var exitPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
            _rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton, out var trigger);
            _leftDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.menuButton, out var menu);
            var triggerPressed = trigger && !_previousTrigger;
            var menuPressed = menu && !_previousMenu;
            _previousTrigger = trigger;
            _previousMenu = menu;

            if (menuPressed)
            {
                Recenter();
                return;
            }

            if (startPressed || triggerPressed && Hits(rightController, startTarget))
            {
                StartGame();
                return;
            }
            if (exitPressed || triggerPressed && Hits(rightController, exitTarget)) ExitGame();
        }

        void RefreshDevices()
        {
            if (!_rightDevice.isValid) _rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!_leftDevice.isValid) _leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        void UpdateDesktopCamera()
        {
            if (IsXRActive || titleCamera == null || designPlayerStart == null) return;
            var eye = designPlayerStart.WorldPosition + Vector3.up * designPlayerStart.xrEyeHeight;
            var target = designPlayerStart.LandmarkPosition;
            titleCamera.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(target - eye, Vector3.up));
        }

        void UpdateRightControllerPose()
        {
            if (!IsXRActive || rightController == null || !_rightDevice.isValid) return;
            if (_rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out var position))
                rightController.localPosition = position;
            if (_rightDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out var rotation))
                rightController.localRotation = rotation;
        }

        void UpdateControllerRay()
        {
            if (rightRay == null || rightController == null) return;
            rightRay.enabled = IsXRActive;
            if (!rightRay.enabled) return;
            rightRay.SetPosition(0, rightController.position);
            rightRay.SetPosition(1, rightController.position + rightController.forward * controllerRayLength);
        }

        bool Hits(Transform rayOrigin, Collider target)
        {
            if (target == null) return false;
            var origin = IsXRActive && rayOrigin != null
                ? rayOrigin.position
                : titleCamera != null ? titleCamera.transform.position : transform.position;
            var direction = IsXRActive && rayOrigin != null
                ? rayOrigin.forward
                : titleCamera != null ? titleCamera.transform.forward : transform.forward;
            return Physics.Raycast(new Ray(origin, direction), out var hit, controllerRayLength) &&
                   (hit.collider == target || hit.collider.transform.IsChildOf(target.transform));
        }

        public void MoveToDesignStart()
        {
            if (designPlayerStart == null) return;
            transform.SetPositionAndRotation(designPlayerStart.WorldPosition, designPlayerStart.WorldYaw);
            if (cameraFloorOffset != null) cameraFloorOffset.localPosition = Vector3.zero;
        }

        public void Recenter()
        {
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);
            foreach (var subsystem in subsystems) subsystem.TryRecenter();
            MoveToDesignStart();
        }

        public void StartGame()
        {
            if (_loading) return;
            _loading = true;
            SceneManager.LoadSceneAsync(gameplaySceneName, LoadSceneMode.Single);
        }

        public void ExitGame()
        {
            if (Application.platform != RuntimePlatform.OSXEditor) Application.Quit();
        }
    }
}
